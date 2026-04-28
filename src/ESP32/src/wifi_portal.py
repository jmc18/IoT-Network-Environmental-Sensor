"""WiFi setup portal using Microdot routes."""

import gc
import machine
import network
import time
from config_store import default_config, save
from microdot import Microdot


def _html_escape(s):
    return (s or "").replace("&", "&amp;").replace("<", "&lt;").replace(">", "&gt;").replace('"', "&quot;")


def scan_ssids():
    sta = network.WLAN(network.STA_IF)
    sta.active(True)
    time.sleep_ms(200)
    nets = []
    seen = set()
    try:
        for x in sta.scan():
            ssid = x[0].decode("utf-8", "replace") if isinstance(x[0], (bytes, bytearray)) else str(x[0])
            if ssid and ssid not in seen:
                seen.add(ssid)
                nets.append(ssid)
    except OSError:
        pass
    nets.sort()
    return nets


def _load_portal_template():
    for p in ("portal.html", "/flash/portal.html"):
        try:
            with open(p, "r") as f:
                return f.read()
        except OSError:
            pass
    return "<html><body><h3>Missing portal.html</h3></body></html>"


def _to_float_or_none(v):
    try:
        s = (v or "").strip()
        return float(s) if s else None
    except (ValueError, TypeError):
        return None


def run_captive_portal(ap_ssid="IoT-Config", ap_password="iotsetup123", listen_port=80):
    gc.collect()

    ap = network.WLAN(network.AP_IF)
    ap.active(True)
    try:
        ap.config(essid=ap_ssid, password=ap_password, authmode=network.AUTH_WPA_WPA2_PSK)
    except Exception:
        ap.config(essid=ap_ssid, password=ap_password)

    time.sleep_ms(500)
    if ap.ifconfig()[0] == "0.0.0.0":
        ap.ifconfig(("192.168.4.1", "255.255.255.0", "192.168.4.1", "8.8.8.8"))

    ip = ap.ifconfig()[0]
    print("[portal] AP '{}' IP {}".format(ap_ssid, ip))

    app = Microdot()

    def render_portal():
        opts = "".join('<option value="{}">{}</option>'.format(_html_escape(n), _html_escape(n)) for n in scan_ssids())
        tpl = _load_portal_template()
        return (
            tpl.replace("{{AP_SSID}}", _html_escape(ap_ssid))
            .replace("{{AP_PASSWORD}}", _html_escape(ap_password))
            .replace("{{AP_IP}}", _html_escape(ip))
            .replace("{{SSID_OPTIONS}}", opts or '<option value="">(no networks found)</option>')
        )

    @app.get("/")
    @app.get("/hotspot-detect.html")
    @app.get("/hotspot-detect")
    def index(request):
        return render_portal(), 200, {"Content-Type": "text/html; charset=utf-8"}

    @app.get("/generate_204")
    def generate_204(request):
        return "", 204

    @app.post("/save")
    def save_route(request):
        form = request.form or {}

        ssid = (form.get("ssid_manual") or form.get("ssid") or "").strip()
        password = (form.get("password") or "").strip()
        api_base = (form.get("api_base") or "").strip().rstrip("/")
        api_key = (form.get("api_key") or "").strip()
        node_id = (form.get("node_id") or "esp32-1").strip()
        lat = _to_float_or_none(form.get("latitude"))
        lon = _to_float_or_none(form.get("longitude"))

        cfg = default_config()
        cfg["wifi"]["ssid"] = ssid
        cfg["wifi"]["password"] = password
        cfg["device"]["api_base"] = api_base
        cfg["device"]["api_key"] = api_key
        cfg["device"]["node_id"] = node_id
        cfg["location"]["latitude"] = lat
        cfg["location"]["longitude"] = lon
        save(cfg)

        def delayed_reboot():
            time.sleep_ms(450)
            try:
                request.app.shutdown()
            except Exception:
                pass
            try:
                ap.active(False)
            except Exception:
                pass
            machine.reset()

        try:
            import _thread

            _thread.start_new_thread(delayed_reboot, ())
        except Exception:
            delayed_reboot()

        return "<html><body><p>Saved. Rebooting...</p></body></html>", 200, {
            "Content-Type": "text/html; charset=utf-8"
        }

    app.run(host="0.0.0.0", port=listen_port, debug=False)
