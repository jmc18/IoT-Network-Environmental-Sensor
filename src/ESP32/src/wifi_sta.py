"""Station-mode WiFi connection helper with retries."""

import network
import time


def connect(ssid, password, timeout_s=30):
    wlan = network.WLAN(network.STA_IF)
    wlan.active(True)
    if wlan.isconnected():
        return True
    wlan.disconnect()
    time.sleep_ms(100)
    wlan.connect(ssid, password)
    deadline = time.ticks_add(time.ticks_ms(), int(timeout_s * 1000))
    while time.ticks_diff(deadline, time.ticks_ms()) > 0:
        if wlan.isconnected():
            print("[wifi] OK", wlan.ifconfig())
            return True
        time.sleep_ms(500)
    print("[wifi] connection failed")
    return False


def is_connected():
    wlan = network.WLAN(network.STA_IF)
    return wlan.active() and wlan.isconnected()


def ifconfig():
    wlan = network.WLAN(network.STA_IF)
    return wlan.ifconfig() if wlan.isconnected() else None
