"""Station-mode WiFi connection helper with retries and debug status."""

import gc
import network
import time


def _sta_status_name(st):
    for name in (
        "STAT_IDLE",
        "STAT_CONNECTING",
        "STAT_WRONG_PASSWORD",
        "STAT_NO_AP_FOUND",
        "STAT_CONNECT_FAIL",
        "STAT_GOT_IP",
    ):
        if hasattr(network, name) and getattr(network, name) == st:
            return "{}({})".format(name, st)
    return str(st)


def _disable_wifi_pm(wlan):
    """Desactiva ahorro de energía del WiFi STA (suele mejorar cortes y latencia)."""
    pm_none = getattr(network.WLAN, "PM_NONE", None)
    if pm_none is not None:
        try:
            wlan.config(pm=pm_none)
            return
        except (ValueError, TypeError, OSError):
            pass
    for val in (0, 1):
        try:
            wlan.config(pm=val)
            return
        except (ValueError, TypeError, OSError):
            continue


def connect(ssid, password, timeout_s=30, verbose=False):
    wlan = network.WLAN(network.STA_IF)
    wlan.active(True)
    if wlan.isconnected():
        _disable_wifi_pm(wlan)
        if verbose:
            print("[wifi] already connected", wlan.ifconfig(), _rssi_str(wlan))
        return True

    wlan.disconnect()
    time.sleep_ms(250)
    gc.collect()

    if verbose:
        print("[wifi] connect to ssid={!r} password={!r} timeout_s={}".format(ssid or "", password or "", timeout_s))
    wlan.connect(ssid, password)

    deadline = time.ticks_add(time.ticks_ms(), int(timeout_s * 1000))
    last_log = time.ticks_ms()
    last_st = None

    while time.ticks_diff(deadline, time.ticks_ms()) > 0:
        if wlan.isconnected():
            _disable_wifi_pm(wlan)
            if verbose:
                print("[wifi] OK", wlan.ifconfig(), _rssi_str(wlan))
            else:
                print("[wifi] OK", wlan.ifconfig())
            return True

        st = wlan.status()
        if st != last_st:
            last_st = st
            if verbose:
                print("[wifi] status ->", _sta_status_name(st))
        elif verbose and time.ticks_diff(time.ticks_ms(), last_log) > 2000:
            last_log = time.ticks_ms()
            print("[wifi] still waiting status=", _sta_status_name(st))

        fail_codes = []
        for name in ("STAT_WRONG_PASSWORD", "STAT_NO_AP_FOUND", "STAT_CONNECT_FAIL"):
            if hasattr(network, name):
                fail_codes.append(getattr(network, name))
        if st in fail_codes:
            print("[wifi] connect aborted:", _sta_status_name(st))
            try:
                wlan.disconnect()
            except OSError:
                pass
            return False

        time.sleep_ms(400)

    print("[wifi] connection failed (timeout) last_status=", _sta_status_name(wlan.status()))
    return False


def _rssi_str(wlan):
    try:
        r = wlan.status("rssi")
        return "rssi={}".format(r)
    except (ValueError, TypeError, OSError):
        return "rssi=?"


def is_connected():
    wlan = network.WLAN(network.STA_IF)
    return wlan.active() and wlan.isconnected()


def ifconfig():
    wlan = network.WLAN(network.STA_IF)
    return wlan.ifconfig() if wlan.isconnected() else None
