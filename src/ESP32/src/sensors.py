"""
Sensor readers: DHT11, KY-038 (analog), MQ-135 (analog).

MQ-135 is not an NDIR CO2 sensor; output is sent as estimated ppm
to align with the backend (calibrate in clean air around ~400 ppm).
"""

from machine import Pin, ADC
import dht


class SensorReader:
    def __init__(self, cfg):
        pins = cfg.get("pins") or {}
        self._dht_pin = int(pins.get("dht", 4))
        self._noise_pin = int(pins.get("noise_adc", 34))
        self._co2_pin = int(pins.get("co2_adc", 35))
        self._sensor_cfg = cfg.get("sensor") or {}

        self._dht = dht.DHT11(Pin(self._dht_pin))

        self._adc_noise = ADC(Pin(self._noise_pin))
        self._adc_noise.atten(ADC.ATTN_11DB)
        self._adc_co2 = ADC(Pin(self._co2_pin))
        self._adc_co2.atten(ADC.ATTN_11DB)

        self._noise_ema = None
        self._co2_ema = None

    def read_dht(self):
        self._dht.measure()
        return float(self._dht.temperature()), float(self._dht.humidity())

    def _ema(self, prev, value, alpha=0.3):
        if prev is None:
            return value
        return alpha * value + (1.0 - alpha) * prev

    def read_noise_db(self):
        raw = self._adc_noise.read()
        sc = self._sensor_cfg
        lo = float(sc.get("noise_adc_silence", 500))
        hi = float(sc.get("noise_adc_loud", 3500))
        db_lo = float(sc.get("noise_db_silence", 35))
        db_hi = float(sc.get("noise_db_loud", 85))
        if hi <= lo:
            hi = lo + 1.0
        n = (raw - lo) / (hi - lo)
        if n < 0.0:
            n = 0.0
        if n > 1.0:
            n = 1.0
        db = db_lo + n * (db_hi - db_lo)
        self._noise_ema = self._ema(self._noise_ema, db)
        return round(self._noise_ema, 1)

    def read_co2_ppm_est(self):
        """
        ADC-based heuristic for MQ-135 (typical 3.3V ESP32 wiring).
        Tune mq135_r0 and curve after outdoor clean-air calibration (~400 ppm).
        """
        raw = float(self._adc_co2.read())
        r0 = float(self._sensor_cfg.get("mq135_r0", 10.0))
        if raw < 1.0:
            raw = 1.0
        ratio = (4095.0 - raw) / raw
        ppm = 400.0 + ratio * (120.0 / max(r0, 0.1))
        if ppm < 300.0:
            ppm = 300.0
        if ppm > 10000.0:
            ppm = 10000.0
        self._co2_ema = self._ema(self._co2_ema, ppm)
        return round(self._co2_ema, 1)

    def read_all(self):
        t, h = self.read_dht()
        noise = self.read_noise_db()
        co2 = self.read_co2_ppm_est()
        return {"temperature": t, "humidity": h, "noise_level": noise, "co2": co2}
