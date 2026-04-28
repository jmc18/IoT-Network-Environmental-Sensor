"""
ESP32 firmware (MicroPython): DHT11, KY-038, MQ-135, WiFi portal, API ingest.

Sends telemetry only when WiFi + TCP to the server is available.
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


def main():
    import sys

    for p in ("lib", "/flash/lib", "src", "/flash/src", ".", ""):
        if p not in sys.path:
            sys.path.insert(0, p)

    from config_store import load, wifi_configured, device_configured
    from wifi_portal import run_captive_portal
    import wifi_sta
    from api_client import ApiClient
    from sensors import SensorReader

    gc.collect()
    cfg = load()

    if not wifi_configured(cfg) or not device_configured(cfg):
        print("[boot] portal mode: configure WiFi and API")
        run_captive_portal()

    w = cfg.get("wifi") or {}
    if not wifi_sta.connect(w.get("ssid", ""), w.get("password", ""), timeout_s=45):
        print("[boot] WiFi connect failed; starting portal in 10s")
        time.sleep(10)
        run_captive_portal()

    import network

    network.WLAN(network.AP_IF).active(False)

    _sync_time()
    _feed_wdt()

    dev = cfg.get("device") or {}
    loc = cfg.get("location") or {}
    api = ApiClient(dev.get("api_base", ""), dev.get("api_key", ""))
    node_id = dev.get("node_id", "esp32-1")
    latitude = loc.get("latitude")
    longitude = loc.get("longitude")
    loop_interval = int(cfg.get("interval_sec", 10))
    if loop_interval < 5:
        loop_interval = 5

    sampling = cfg.get("sampling_minutes") or {}
    due_co2_s = _minutes_to_seconds(sampling.get("co2"), 2)
    due_noise_s = _minutes_to_seconds(sampling.get("noise"), 5)
    due_dht_s = _minutes_to_seconds(sampling.get("temp_humidity"), 4)

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

        if not wifi_sta.is_connected():
            print("[wifi] disconnected; reconnecting...")
            if not wifi_sta.connect(w.get("ssid", ""), w.get("password", ""), timeout_s=40):
                time.sleep(loop_interval)
                continue
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
            time.sleep(loop_interval)
            continue

        if not _internet_ok(cfg):
            print("[net] no internet path; retry in", loop_interval, "s")
            time.sleep(loop_interval)
            continue

        payload = dict(state)
        payload["latitude"] = latitude
        payload["longitude"] = longitude
        if all(v is None for v in payload.values()):
            time.sleep(loop_interval)
            continue

        if api.post_reading(node_id, payload):
            print("[tx] OK", payload)
        else:
            print("[tx] failed")

        time.sleep(loop_interval)


if __name__ == "__main__":
    try:
        main()
    except Exception as e:
        print("fatal:", e)
        try:
            machine.reset()
        except Exception:
            pass
