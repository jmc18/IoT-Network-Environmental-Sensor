"""Persistencia de WiFi + API en config.json (flash)."""

import ujson
import os

DEFAULT_PATH = "config.json"


def default_config():
    return {
        "wifi": {"ssid": "", "password": ""},
        "device": {
            "node_id": "esp32-1",
            "api_base": "https://iotapi.javiermc.tech",
            "api_key": "iotnetwork-generic-2026-shared-key",
        },
        "location": {
            "latitude": None,
            "longitude": None,
        },
        "pins": {"dht": 4, "noise_adc": 34, "co2_adc": 35},
        "interval_sec": 10,
        "sampling_minutes": {
            "co2": 2,
            "noise": 5,
            "temp_humidity": 4,
        },
        "sensor": {
            "noise_adc_silence": 500,
            "noise_adc_loud": 3500,
            "noise_db_silence": 35,
            "noise_db_loud": 85,
            "mq135_r0": 10.0,
        },
    }


def load(path=DEFAULT_PATH):
    try:
        with open(path, "r") as f:
            data = ujson.load(f)
    except (OSError, ValueError):
        return default_config()

    base = default_config()
    if isinstance(data, dict):
        for k, v in base.items():
            if k in data and isinstance(data[k], dict) and isinstance(v, dict):
                merged = dict(v)
                merged.update(data[k])
                base[k] = merged
            elif k in data:
                base[k] = data[k]
    return base


def save(cfg, path=DEFAULT_PATH):
    tmp = path + ".tmp"
    with open(tmp, "w") as f:
        ujson.dump(cfg, f)
    try:
        os.remove(path)
    except OSError:
        pass
    os.rename(tmp, path)


def wifi_configured(cfg):
    w = cfg.get("wifi") or {}
    return bool((w.get("ssid") or "").strip())


def device_configured(cfg):
    d = cfg.get("device") or {}
    return bool((d.get("api_base") or "").strip() and (d.get("api_key") or "").strip())
