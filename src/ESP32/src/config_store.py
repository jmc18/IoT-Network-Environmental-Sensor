"""Persistencia de WiFi + API en config.json (flash)."""

import gc
import time
import ujson
import os

DEFAULT_PATH = "config.json"


def default_config():
    return {
        "wifi": {"ssid": "", "password": ""},
        "device": {
            "node_id": "",
            "api_base": "https://iotapi.javiermc.tech",
            "api_key": "iotnetwork-generic-2026-shared-key",
        },
        "location": {
            "latitude": None,
            "longitude": None,
        },
        "pins": {"dht": 4, "noise_adc": 34, "co2_adc": 35},
        "interval_sec": 10,
        "local_only": False,
        "local_sample_sec": 10,
        "portal_force": False,
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
            "mq135_clean_air_adc": 3000,
            "mq135_ppm_per_adc": 1.6,
            "mq135_warmup_reads": 8,
            "co2_outdoor_ppm": 420,
            "co2_min_ppm": 350,
            "co2_max_ppm": 5000,
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
    # config.json antiguo podía dejar api_key/api_base vacíos y el API devuelve 401
    ddef = default_config().get("device") or {}
    dev = base.get("device") or {}
    if not (dev.get("api_base") or "").strip():
        dev["api_base"] = ddef.get("api_base", "")
    if not (dev.get("api_key") or "").strip():
        dev["api_key"] = ddef.get("api_key", "")
    base["device"] = dev
    return base


def save(cfg, path=DEFAULT_PATH):
    """Escribe en flash; reintenta por interferencias WiFi/stack en algunos firmwares."""
    last_err = None
    for attempt in range(4):
        try:
            tmp = path + ".tmp"
            with open(tmp, "w") as f:
                ujson.dump(cfg, f)
            try:
                os.remove(path)
            except OSError:
                pass
            os.rename(tmp, path)
            return
        except OSError as ex:
            last_err = ex
            try:
                os.remove(tmp)
            except OSError:
                pass
            time.sleep_ms(40 + attempt * 80)
            gc.collect()
    raise last_err


def wifi_configured(cfg):
    w = cfg.get("wifi") or {}
    return bool((w.get("ssid") or "").strip())


def device_configured(cfg):
    d = cfg.get("device") or {}
    return bool((d.get("api_base") or "").strip() and (d.get("api_key") or "").strip())
