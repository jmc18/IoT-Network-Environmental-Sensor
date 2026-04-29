"""
ESP32 firmware (MicroPython): DHT11, KY-038, MQ-135, WiFi portal, API ingest.

Sends telemetry only when WiFi + TCP to the server is available.
Set config local_only true to print readings on serial and skip the API.
Critical push thresholds are handled by the backend (FirebasePushNotificationService).
"""

import gc
import time
import machine

try:
    from machine import WDT

    _wdt = WDT(timeout=120000)
except (ImportError, ValueError, OSError):
    _wdt = None

# TEMP: trazas extra WiFi/arranque; pon False cuando depures
_WIFI_IO_DEBUG = True


def _mdbg(msg):
    if _WIFI_IO_DEBUG:
        print("[boot-dbg]", msg)


def _feed_wdt():
    if _wdt is not None:
        _wdt.feed()


def _sync_time():
    try:
        import ntptime

        for _ in range(4):
            try:
                ntptime.settime()
                print("[ntp] time synced (UTC)")
                return
            except OSError:
                time.sleep_ms(500)
    except ImportError:
        pass


def _internet_ok(cfg):
    """Checks DNS resolution + TCP reachability to the API host."""
    dev = cfg.get("device") or {}
    base = (dev.get("api_base") or "").strip()
    if not base:
        return False
    host = base.split("://", 1)[-1].split("/", 1)[0].split(":")[0]
    if not host:
        return False
    try:
        import usocket

        ai = usocket.getaddrinfo(host, 443 if base.startswith("https") else 80)
        addr = ai[0][-1]
        s = usocket.socket()
        s.settimeout(5)
        s.connect(addr)
        s.close()
        return True
    except OSError as ex:
        print("[net] no route to API host:", ex)
        return False


def _minutes_to_seconds(v, default_minutes):
    try:
        x = int(v)
    except (TypeError, ValueError):
        x = default_minutes
    if x < 1:
        x = 1
    return x * 60


def _idle_with_admin(admin, seconds):
    """Sleeps in short slices so admin/web keeps responsive."""
    ms_total = int(max(0, seconds) * 1000)
    elapsed = 0
    while elapsed < ms_total:
        if admin is not None:
            admin.poll()
        time.sleep_ms(100)
        elapsed += 100


def main():
    import sys

    for p in ("lib", "/flash/lib", "src", "/flash/src", ".", ""):
        if p not in sys.path:
            sys.path.insert(0, p)

    try:
        from config_store import load, wifi_configured, device_configured
    except ImportError:
        try:
            import os

            print("[boot] missing module: config_store.py")
            print("[boot] sys.path =", sys.path)
            for d in (".", "/flash", "src", "/flash/src", "lib", "/flash/lib"):
                try:
                    print("[boot] ls {} -> {}".format(d, os.listdir(d)))
                except Exception:
                    pass
            print("[boot] expected files in src/: config_store.py, wifi_portal.py, wifi_sta.py, api_client.py, sensors.py")
        except Exception:
            pass
        raise
    from wifi_portal import (
        run_captive_portal,
        DEFAULT_CAPTIVE_AP_SSID,
        DEFAULT_CAPTIVE_AP_PASSWORD,
        CAPTIVE_AP_USE_PASSWORD,
    )
    import wifi_sta
    from api_client import ApiClient
    from sensors import SensorReader
    from admin_server import AdminServer

    gc.collect()
    cfg = load()
    local_only = bool(cfg.get("local_only"))
    wifi_ok = wifi_configured(cfg)

    # portal_force=True permite volver al portal desde config.json.
    # El factory-reset del panel admin regresa a defaults y también fuerza portal por falta de WiFi.
    force_portal = bool(cfg.get("portal_force"))
    need_portal = force_portal or (not wifi_ok and not local_only) or (
        not device_configured(cfg) and not local_only
    )
    if need_portal:
        print("[boot] portal mode: configure WiFi" + (" y API" if not local_only else ""))
        if CAPTIVE_AP_USE_PASSWORD:
            print(
                "[boot] AP: SSID={!r} contraseña WiFi del ESP32={!r}".format(
                    DEFAULT_CAPTIVE_AP_SSID,
                    DEFAULT_CAPTIVE_AP_PASSWORD,
                )
            )
        else:
            print(
                "[boot] AP: SSID={!r} — sin contraseña (red abierta); abre http://192.168.4.1/".format(
                    DEFAULT_CAPTIVE_AP_SSID
                )
            )
        _mdbg("mem_free={}".format(gc.mem_free()))
        run_captive_portal()
    elif not wifi_ok and local_only:
        print("[boot] local_only: sin WiFi en config; lecturas por USB (sin STA)")

    w = cfg.get("wifi") or {}
    sta_used = False
    if wifi_ok:
        _mdbg("STA first connect ssid_len={} mem_free={}".format(len(w.get("ssid", "")), gc.mem_free()))
        if wifi_sta.connect(
            w.get("ssid", ""),
            w.get("password", ""),
            timeout_s=45,
            verbose=_WIFI_IO_DEBUG,
        ):
            sta_used = True
            print("[boot] WiFi connected; STA:", wifi_sta.ifconfig())
        else:
            print("[boot] WiFi connect failed")
            if not local_only:
                time.sleep(10)
                if CAPTIVE_AP_USE_PASSWORD:
                    print(
                        "[boot] AP: SSID={!r} contraseña={!r}".format(
                            DEFAULT_CAPTIVE_AP_SSID,
                            DEFAULT_CAPTIVE_AP_PASSWORD,
                        )
                    )
                else:
                    print(
                        "[boot] AP: SSID={!r} — sin contraseña; http://192.168.4.1/".format(
                            DEFAULT_CAPTIVE_AP_SSID
                        )
                    )
                run_captive_portal()
            else:
                print("[boot] local_only: continúo sin STA (solo sensores)")

    import network

    network.WLAN(network.AP_IF).active(False)

    if sta_used:
        _sync_time()
    _feed_wdt()
    admin = AdminServer(port=80) if sta_used else None
    if admin is not None:
        try:
            admin.start()
            admin.update(wifi_connected=True, sta_ifconfig=wifi_sta.ifconfig())
            print("[admin] abre http://{}/".format(wifi_sta.ifconfig()[0]))
            admin.log("[boot] admin started")
        except Exception as ex:
            print("[admin] start failed:", ex)
            admin = None

    dev = cfg.get("device") or {}
    loc = cfg.get("location") or {}
    api = ApiClient(dev.get("api_base", ""), dev.get("api_key", ""))
    node_id = dev.get("node_id", "esp32-1")
    latitude = loc.get("latitude")
    longitude = loc.get("longitude")
    if latitude is None or longitude is None:
        print("[geo] location missing in config; will ingest without coordinates until provided")
    else:
        print("[geo] location loaded lat={}, lon={}".format(latitude, longitude))
    loop_interval = int(cfg.get("interval_sec", 10))
    if loop_interval < 5:
        loop_interval = 5

    sampling = cfg.get("sampling_minutes") or {}
    due_co2_s = _minutes_to_seconds(sampling.get("co2"), 2)
    due_noise_s = _minutes_to_seconds(sampling.get("noise"), 5)
    due_dht_s = _minutes_to_seconds(sampling.get("temp_humidity"), 4)

    if local_only:
        # En modo local queremos lectura periódica real por serial.
        local_sample_s = int(cfg.get("local_sample_sec", 10))
        if local_sample_s < 1:
            local_sample_s = 1
        due_co2_s = local_sample_s
        due_noise_s = local_sample_s
        due_dht_s = local_sample_s
        loop_interval = local_sample_s
        print("[sample] local_only: co2/noise/temp_hum cada {}s".format(local_sample_s))
        print("[boot] local_only: lecturas por serial; no se llama a la API")
    else:
        print("[sample] co2={}m noise={}m temp/hum={}m loop={}s".format(
            due_co2_s // 60,
            due_noise_s // 60,
            due_dht_s // 60,
            loop_interval,
        ))

    try:
        sensors = SensorReader(cfg)
    except Exception as ex:
        print("[sensores] init error:", ex)
        machine.reset()

    state = {
        "temperature": None,
        "humidity": None,
        "co2": None,
        "noiseLevel": None,
    }
    now = time.time()
    last = {
        "co2": now - due_co2_s,
        "noise": now - due_noise_s,
        "temp_humidity": now - due_dht_s,
    }

    while True:
        _feed_wdt()
        gc.collect()

        if sta_used and not wifi_sta.is_connected():
            print("[wifi] disconnected; reconnecting...")
            if admin is not None:
                admin.log("[wifi] disconnected")
                admin.update(wifi_connected=False, last_event="wifi_disconnected")
            _mdbg("mem_free={}".format(gc.mem_free()))
            if not wifi_sta.connect(
                w.get("ssid", ""),
                w.get("password", ""),
                timeout_s=40,
                verbose=_WIFI_IO_DEBUG,
            ):
                print("[sync] reconnect failed; sleeping {}s".format(loop_interval))
                if not local_only:
                    _idle_with_admin(admin, loop_interval)
                    continue
            else:
                print("[wifi] reconnected; STA:", wifi_sta.ifconfig())
                if admin is not None:
                    admin.log("[wifi] reconnected {}".format(wifi_sta.ifconfig()))
                    admin.update(
                        wifi_connected=True,
                        sta_ifconfig=wifi_sta.ifconfig(),
                        last_event="wifi_reconnected",
                    )
                _sync_time()

        now = time.time()
        updated = False

        try:
            if now - last["co2"] >= due_co2_s:
                state["co2"] = sensors.read_co2_ppm_est()
                last["co2"] = now
                updated = True
        except Exception as ex:
            print("[sensors] co2 read error:", ex)

        try:
            if now - last["noise"] >= due_noise_s:
                state["noiseLevel"] = sensors.read_noise_db()
                last["noise"] = now
                updated = True
        except Exception as ex:
            print("[sensors] noise read error:", ex)

        try:
            if now - last["temp_humidity"] >= due_dht_s:
                t, h = sensors.read_dht()
                state["temperature"] = t
                state["humidity"] = h
                last["temp_humidity"] = now
                updated = True
        except Exception as ex:
            print("[sensors] dht read error:", ex)

        if not updated:
            _idle_with_admin(admin, loop_interval)
            continue

        payload = dict(state)
        payload["latitude"] = latitude
        payload["longitude"] = longitude
        if all(v is None for v in payload.values()):
            print("[sync] payload empty; skipping tx")
            _idle_with_admin(admin, loop_interval)
            continue

        if local_only:
            noise_adc = sensors.read_noise_adc_raw()
            co2_adc = sensors.read_co2_adc_raw()
            print(
                "[local] T={}C H={}% CO2~={}ppm (adc={}) ruido~={}dB (adc={})".format(
                    state["temperature"],
                    state["humidity"],
                    state["co2"],
                    co2_adc,
                    state["noiseLevel"],
                    noise_adc,
                )
            )
            if admin is not None:
                admin.update(
                    node_id=node_id,
                    local_only=True,
                    last_payload=payload,
                    loop_interval=loop_interval,
                    last_event="local_sample",
                )
                admin.log("[sample] local {}".format(payload))
        else:
            if not _internet_ok(cfg):
                print("[net] no internet path; retry in", loop_interval, "s")
                if admin is not None:
                    admin.update(last_error="no internet path", last_event="net_down")
                    admin.log("[net] no internet path")
                _idle_with_admin(admin, loop_interval)
                continue

            print("[sync] sending telemetry for node", node_id)
            if api.post_reading(node_id, payload):
                print("[tx] OK", payload)
                if admin is not None:
                    admin.update(
                        node_id=node_id,
                        local_only=False,
                        last_payload=payload,
                        loop_interval=loop_interval,
                        last_error=None,
                        last_event="tx_ok",
                    )
                    admin.log("[tx] ok {}".format(payload))
            else:
                print("[tx] failed (check API base/key/connectivity)")
                if admin is not None:
                    admin.update(
                        node_id=node_id,
                        local_only=False,
                        last_payload=payload,
                        loop_interval=loop_interval,
                        last_error="tx failed",
                        last_event="tx_failed",
                    )
                    admin.log("[tx] failed")

        _idle_with_admin(admin, loop_interval)


if __name__ == "__main__":
    try:
        main()
    except Exception as e:
        print("fatal:", e)
        try:
            import sys

            sys.print_exception(e)
        except Exception:
            pass
        # Evita bucle de reset cuando hay errores recuperables de arranque.
        if isinstance(e, (SyntaxError, ImportError, OSError)):
            raise
        try:
            machine.reset()
        except Exception:
            pass
