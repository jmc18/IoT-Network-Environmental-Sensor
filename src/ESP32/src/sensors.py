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
        self._co2_reads = 0

    def read_dht(self):
        self._dht.measure()
        return float(self._dht.temperature()), float(self._dht.humidity())

    def _ema(self, prev, value, alpha=0.3):
        if prev is None:
            return value
        return alpha * value + (1.0 - alpha) * prev

    def read_noise_adc_raw(self):
        return int(self._adc_noise.read())

    def read_co2_adc_raw(self):
        return int(self._adc_co2.read())

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
        MQ-135 estimated CO2 in ppm (heuristic, not NDIR):
        - Calibrate in outdoor clean air (~400-420 ppm) using mq135_clean_air_adc.
        - Place sensor away from direct breathing to avoid spikes.
        - First readings are stabilized by warm-up damping.
        """
        raw = float(self._adc_co2.read())
        sc = self._sensor_cfg
        clean_adc = float(sc.get("mq135_clean_air_adc", 3000))
        clean_ppm = float(sc.get("co2_outdoor_ppm", 420))
        ppm_per_adc = float(sc.get("mq135_ppm_per_adc", 1.6))
        ppm_min = float(sc.get("co2_min_ppm", 350))
        ppm_max = float(sc.get("co2_max_ppm", 5000))
        warmup_reads = int(sc.get("mq135_warmup_reads", 8))

        if raw < 1.0:
            raw = 1.0

        self._co2_reads += 1
        delta_adc = clean_adc - raw
        ppm = clean_ppm + (delta_adc * ppm_per_adc)

        # Warm-up stabilization: keep first samples closer to clean-air baseline.
        if self._co2_reads <= warmup_reads:
            ppm = (ppm + (2.0 * clean_ppm)) / 3.0
            alpha = 0.18
        else:
            alpha = 0.35

        if ppm < ppm_min:
            ppm = ppm_min
        if ppm > ppm_max:
            ppm = ppm_max

        self._co2_ema = self._ema(self._co2_ema, ppm, alpha=alpha)
        return round(self._co2_ema, 1)

    def read_all(self):
        t, h = self.read_dht()
        noise = self.read_noise_db()
        co2 = self.read_co2_ppm_est()
        return {"temperature": t, "humidity": h, "noise_level": noise, "co2": co2}
