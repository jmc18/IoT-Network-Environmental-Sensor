"""WiFi setup portal using low-level sockets."""

import gc
import machine
import network
import time
import ujson
import usocket
from config_store import default_config, load, save

_PORTAL_STATE = {
    "saved": False,
    "wifiConnecting": False,
    "wifiConnected": False,
    "staIp": None,
    "internetOk": False,
    "apiReachable": False,
    "lastError": None,
    "locationSaved": False,
    "factoryReset": False,
    "wifiStatus": None,
    "savedConfig": {},
}
_PORTAL_LOGS = []
_PORTAL_LOG_MAX = 20
# SSIDs found in a STA-only scan before AP starts (avoids STA+AP RAM issues).
_SSID_CACHE = []

# Captive portals / phones sometimes stall; lwIP often surfaces recv timeouts as errno 116.
_MAX_FORM_BODY_BYTES = 2048
_CLIENT_SOCK_TIMEOUT_S = 4.0

# TEMP: logs extra en consola (sin contraseña WiFi del usuario). Pon False cuando ya no depures.
_PORTAL_VERBOSE_LOGS = False

# Credenciales del AP de configuración.
DEFAULT_CAPTIVE_AP_SSID = "IoT-Config"
DEFAULT_CAPTIVE_AP_PASSWORD = "iotsetup123"  # Solo si CAPTIVE_AP_USE_PASSWORD es True (≥8 caracteres).

# Usa WPA2 en el AP del portal para compatibilidad en entornos donde red abierta falla.
CAPTIVE_AP_USE_PASSWORD = False


def _ap_auth_hint(ap_is_open, ap_password):
    if ap_is_open:
        return "Sin contraseña en el WiFi del ESP32 (red abierta)."
    return "Contraseña del WiFi del ESP32: {} (WPA2).".format(ap_password)


def _apply_soft_ap_security(ap, ap_ssid, ap_password):
    """
    Configura el SoftAP. Devuelve ap_is_open=True si queda abierto (sin WPA).
    """
    if not CAPTIVE_AP_USE_PASSWORD:
        ap.config(essid=ap_ssid, authmode=network.AUTH_OPEN)
        try:
            ap.config(channel=6)
        except Exception:
            pass
        try:
            ap.config(max_clients=1)
        except Exception:
            pass
        print("[portal] AP OPEN essid={!r} (el teléfono no pide contraseña para esta red)".format(ap_ssid))
        return True

    try:
        if not ap_password or len(ap_password) < 8:
            raise ValueError("AP password must be >= 8 chars for WPA2")
        auth = getattr(network, "AUTH_WPA_WPA2_PSK", network.AUTH_WPA2_PSK)
        ap.config(essid=ap_ssid, password=ap_password, authmode=auth)
        try:
            ap.config(channel=6)
        except Exception:
            pass
        try:
            ap.config(max_clients=1)
        except Exception:
            pass
        print("[portal] AP WPA essid={!r}".format(ap_ssid))
        return False
    except Exception as ex:
        print("[portal] WPA AP failed ({}), using OPEN".format(ex))
        ap.config(essid=ap_ssid, authmode=network.AUTH_OPEN)
        return True


def _pdbg(msg):
    _log_line("[portal-dbg] {}".format(msg))
    if _PORTAL_VERBOSE_LOGS:
        print("[portal-dbg]", msg)


def _log_line(msg):
    if not _PORTAL_VERBOSE_LOGS:
        return
    try:
        stamp = time.ticks_ms()
    except Exception:
        stamp = 0
    _PORTAL_LOGS.append("{} {}".format(stamp, msg))
    if len(_PORTAL_LOGS) > _PORTAL_LOG_MAX:
        del _PORTAL_LOGS[: len(_PORTAL_LOGS) - _PORTAL_LOG_MAX]


def _set_state(**kwargs):
    for k, v in kwargs.items():
        _PORTAL_STATE[k] = v


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


def _cfg_summary(cfg):
    w = cfg.get("wifi") or {}
    d = cfg.get("device") or {}
    loc = cfg.get("location") or {}
    return {
        "ssid": w.get("ssid", ""),
        "password_len": len(w.get("password") or ""),
        "node_id": d.get("node_id", ""),
        "latitude": loc.get("latitude"),
        "longitude": loc.get("longitude"),
    }


def _extract_host_port(base_url):
    raw = (base_url or "").strip()
    if not raw:
        return "", 0
    tls = raw.startswith("https://")
    rest = raw.split("://", 1)[-1]
    hostport = rest.split("/", 1)[0]
    if ":" in hostport:
        host, _, port_s = hostport.partition(":")
        try:
            port = int(port_s)
        except ValueError:
            port = 443 if tls else 80
    else:
        host = hostport
        port = 443 if tls else 80
    return host, port


def _tcp_reachable(host, port, timeout_s=5):
    if not host:
        return False
    try:
        addr = usocket.getaddrinfo(host, port)[0][-1]
        sock = usocket.socket()
        sock.settimeout(timeout_s)
        sock.connect(addr)
        sock.close()
        return True
    except OSError:
        return False


def _probe_connectivity(cfg):
    sta = network.WLAN(network.STA_IF)
    ip = sta.ifconfig()[0] if sta.isconnected() else None
    internet_ok = _tcp_reachable("8.8.8.8", 53, timeout_s=3) or _tcp_reachable("1.1.1.1", 53, timeout_s=3)
    dev = cfg.get("device") or {}
    host, port = _extract_host_port(dev.get("api_base"))
    api_ok = _tcp_reachable(host, port, timeout_s=5)
    _set_state(staIp=ip, internetOk=internet_ok, apiReachable=api_ok)


def _connect_sta_background(cfg):
    _set_state(
        wifiConnecting=True,
        wifiConnected=False,
        staIp=None,
        internetOk=False,
        apiReachable=False,
        lastError=None,
        wifiStatus=None,
    )
    try:
        import wifi_sta

        w = cfg.get("wifi") or {}
        _pdbg("STA bg connect ssid={!r} password={!r}".format(w.get("ssid", ""), w.get("password", "")))
        ok = wifi_sta.connect(w.get("ssid", ""), w.get("password", ""), timeout_s=40, verbose=_PORTAL_VERBOSE_LOGS)
        sta = network.WLAN(network.STA_IF)
        st = sta.status()
        st_name = _sta_status_name(st)
        _set_state(wifiConnecting=False, wifiConnected=bool(ok), wifiStatus=st_name)
        if not ok:
            if hasattr(network, "STAT_WRONG_PASSWORD") and st == getattr(network, "STAT_WRONG_PASSWORD"):
                err = "WiFi rechazado: contraseña incorrecta."
            elif hasattr(network, "STAT_NO_AP_FOUND") and st == getattr(network, "STAT_NO_AP_FOUND"):
                err = "WiFi no encontrado (SSID no visible)."
            elif hasattr(network, "STAT_CONNECT_FAIL") and st == getattr(network, "STAT_CONNECT_FAIL"):
                err = "Fallo de conexión WiFi (STAT_CONNECT_FAIL)."
            else:
                err = "No fue posible conectar el ESP32 a la red WiFi indicada."
            _pdbg("STA bg connect failed status={}".format(st_name))
            _set_state(lastError=err)
            return
        _pdbg("STA bg OK, probing API/internet")
        _probe_connectivity(cfg)
    except Exception as ex:
        _pdbg("STA bg exception: {!r}".format(ex))
        _set_state(wifiConnecting=False, wifiConnected=False, lastError="Error STA: {}".format(ex))


def _html_escape(s):
    return (s or "").replace("&", "&amp;").replace("<", "&lt;").replace(">", "&gt;").replace('"', "&quot;")


def _scan_ssids_sta_only_before_ap():
    """
    Scan while AP is still off and only STA is active.
    STA+AP together triggers WiFi OOM on some ESP32 builds.
    """
    global _SSID_CACHE
    _SSID_CACHE = []
    ap_if = network.WLAN(network.AP_IF)
    sta = network.WLAN(network.STA_IF)
    try:
        ap_if.active(False)
    except Exception:
        pass
    time.sleep_ms(200)
    gc.collect()
    try:
        sta.active(True)
        time.sleep_ms(250)
        seen = set()
        for x in sta.scan():
            ssid = x[0].decode("utf-8", "replace") if isinstance(x[0], (bytes, bytearray)) else str(x[0])
            if ssid and ssid not in seen:
                seen.add(ssid)
                _SSID_CACHE.append(ssid)
        _SSID_CACHE.sort()
        print("[portal] wifi scan ok, {} networks".format(len(_SSID_CACHE)))
    except OSError as ex:
        print("[portal] wifi scan failed:", ex)
        _SSID_CACHE = []
    finally:
        try:
            sta.active(False)
        except Exception:
            pass
    time.sleep_ms(200)
    gc.collect()


def scan_ssids():
    return list(_SSID_CACHE)


def _scan_ssids_on_demand():
    """
    Best-effort scan triggered by user action.
    Keeps portal alive if scan fails.
    """
    try:
        sta = network.WLAN(network.STA_IF)
        prev_active = False
        try:
            prev_active = bool(sta.active())
        except Exception:
            prev_active = False
        if not prev_active:
            sta.active(True)
            time.sleep_ms(200)

        seen = set()
        found = []
        for x in sta.scan():
            ssid = x[0].decode("utf-8", "replace") if isinstance(x[0], (bytes, bytearray)) else str(x[0])
            if ssid and ssid not in seen:
                seen.add(ssid)
                found.append(ssid)
        found.sort()
        if found:
            _SSID_CACHE[:] = found
        if not prev_active:
            try:
                sta.active(False)
            except Exception:
                pass
        return True, list(_SSID_CACHE)
    except Exception as ex:
        _pdbg("on-demand scan failed: {!r}".format(ex))
        return False, list(_SSID_CACHE)


def _load_portal_template():
    for p in ("portal.html", "/flash/portal.html"):
        try:
            with open(p, "r") as f:
                return f.read()
        except OSError:
            pass
    return "<html><body><h3>Missing portal.html</h3></body></html>"


def _render_portal_page(ap_ssid, ap_password, ip, ap_is_open, ssid_opts_html):
    tpl = _load_portal_template()
    api_base = (default_config().get("device") or {}).get("api_base", "")
    pwd_disp = "— (ninguna)" if ap_is_open else (ap_password or "")
    return (
        tpl.replace("{{AP_SSID}}", _html_escape(ap_ssid))
        .replace("{{AP_PASSWORD}}", _html_escape(pwd_disp))
        .replace("{{AP_AUTH_HINT}}", _html_escape(_ap_auth_hint(ap_is_open, ap_password)))
        .replace("{{AP_IP}}", _html_escape(ip))
        .replace("{{API_BASE}}", _html_escape(api_base))
        .replace("{{SSID_OPTIONS}}", ssid_opts_html or '<option value="">(no networks found)</option>')
    )


def _to_float_or_none(v):
    try:
        s = (v or "").strip()
        return float(s) if s else None
    except (ValueError, TypeError):
        return None


def _parse_form_urlencoded(body):
    if isinstance(body, str):
        body = body.encode("utf-8")
    out = {}
    for pair in body.split(b"&"):
        if not pair:
            continue
        if b"=" in pair:
            k, v = pair.split(b"=", 1)
        else:
            k, v = pair, b""
        key = k.decode("utf-8", "replace").replace("+", " ")
        val = v.decode("utf-8", "replace").replace("+", " ")
        out[key] = val
    return out


def _recv_headers(sock):
    buf = b""
    try:
        while b"\r\n\r\n" not in buf:
            chunk = sock.recv(128)
            if not chunk:
                break
            buf += chunk
            if len(buf) > 8192:
                break
    except OSError:
        return None, b""
    if b"\r\n\r\n" not in buf:
        return None, b""
    head, _, rest = buf.partition(b"\r\n\r\n")
    lines = head.split(b"\r\n")
    if not lines:
        return None, rest
    req_line = lines[0].decode("utf-8", "replace")
    headers = {}
    for line in lines[1:]:
        if b":" in line:
            hk, hv = line.split(b":", 1)
            headers[hk.decode("utf-8", "replace").strip().lower()] = hv.decode("utf-8", "replace").strip()
    return (req_line, headers), rest


def _read_body(sock, rest, content_length):
    """Read exactly content_length bytes (capped); OSError/timeout returns partial."""
    try:
        want = int(content_length)
    except (ValueError, TypeError):
        want = 0
    if want <= 0:
        return b""
    want = min(want, _MAX_FORM_BODY_BYTES)
    body = rest if isinstance(rest, (bytes, bytearray)) else b""
    if len(body) > want:
        return body[:want]
    try:
        while len(body) < want:
            chunk = sock.recv(min(1024, want - len(body)))
            if not chunk:
                break
            body += chunk
    except OSError:
        pass
    return body[:want] if len(body) >= want else body


def _http_response(sock, code, body, ctype="text/html; charset=utf-8"):
    def _safe_send(data, chunk_size):
        sent = 0
        total = len(data)
        while sent < total:
            try:
                n = sock.send(data[sent : sent + chunk_size])
            except (OSError, KeyboardInterrupt):
                break
            if not n:
                break
            sent += n
            time.sleep_ms(1)

    if isinstance(body, str):
        body = body.encode("utf-8")
    head = "HTTP/1.0 {} OK\r\nContent-Type: {}\r\nContent-Length: {}\r\nConnection: close\r\nCache-Control: no-store\r\nPragma: no-cache\r\n\r\n".format(
        code, ctype, len(body)
    )
    _safe_send(head.encode("utf-8"), 256)
    _safe_send(body, 512)


def _save_form_to_config(form):
    defaults = default_config()
    ssid = (form.get("ssid") or "").strip()
    password = (form.get("password") or "").strip()
    _pdbg("save: ssid={!r} password={!r} node_id={!r}".format(ssid, password, (form.get("node_id") or "")[:32]))
    api_base = (defaults.get("device") or {}).get("api_base", "").strip().rstrip("/")
    api_key = (defaults.get("device") or {}).get("api_key", "").strip()
    node_id = (form.get("node_id") or "").strip()
    lat = _to_float_or_none(form.get("latitude"))
    lon = _to_float_or_none(form.get("longitude"))

    if not ssid:
        return False, "SSID is required."
    if not node_id:
        return False, "Node ID is required."
    if lat is None or lon is None:
        return False, "Latitude and longitude are required."
    if not api_base or not api_key:
        return False, "Device defaults missing api_base/api_key."

    cfg = default_config()
    cfg["wifi"]["ssid"] = ssid
    cfg["wifi"]["password"] = password
    cfg["device"]["api_base"] = api_base
    cfg["device"]["api_key"] = api_key
    cfg["device"]["node_id"] = node_id
    cfg["location"]["latitude"] = lat
    cfg["location"]["longitude"] = lon
    save(cfg)
    _set_state(savedConfig=_cfg_summary(cfg))
    _pdbg("save: flash OK mem_free={}".format(gc.mem_free()))
    print(
        "[portal] config saved ssid={!r} password={!r} node_id={!r} lat={} lon={}".format(
            ssid, password, node_id, lat, lon
        )
    )
    _log_line("[portal] config saved ssid={!r} node_id={!r}".format(ssid, node_id))
    _set_state(saved=True, locationSaved=(lat is not None and lon is not None), lastError=None)
    return True, "Saved."


def _factory_reset_config():
    cfg = default_config()
    save(cfg)
    _set_state(
        saved=False,
        wifiConnecting=False,
        wifiConnected=False,
        staIp=None,
        internetOk=False,
        apiReachable=False,
        locationSaved=False,
        factoryReset=True,
        wifiStatus=None,
        savedConfig=_cfg_summary(cfg),
        lastError=None,
    )
    print("[portal] factory reset done (config restored to defaults)")


def _open_server_socket(listen_port):
    """Open HTTP socket once. If lwIP has no buffers, back off instead of spinning."""
    last = None
    for attempt in range(8):
        s_try = None
        try:
            gc.collect()
            s_try = usocket.socket(usocket.AF_INET, usocket.SOCK_STREAM)
            s_try.setsockopt(usocket.SOL_SOCKET, usocket.SO_REUSEADDR, 1)
            s_try.bind(("0.0.0.0", listen_port))
            s_try.listen(1)
            s_try.settimeout(2.0)
            return s_try
        except OSError as ex:
            last = ex
            try:
                if s_try:
                    s_try.close()
            except Exception:
                pass
            errno = ex.args[0] if ex.args else None
            if errno == 105:  # ENOBUFS
                _pdbg("socket open ENOBUFS; backoff attempt {} mem_free={}".format(attempt + 1, gc.mem_free()))
                gc.collect()
                time.sleep_ms(900 + attempt * 200)
                continue
            if errno == 112:  # EADDRINUSE
                _pdbg("socket EADDRINUSE; waiting for port release")
                gc.collect()
                time.sleep_ms(1200)
                continue
            raise
    raise last or OSError("socket open failed")


def _run_socket_portal(ap_ssid, ap_password, ip, listen_port, ap_is_open):
    print("[portal] using low-memory socket fallback")
    _pdbg("listen {}:{} mem_free={}".format(ip, listen_port, gc.mem_free()))

    def render_portal():
        opts = "".join('<option value="{}">{}</option>'.format(_html_escape(n), _html_escape(n)) for n in scan_ssids())
        return _render_portal_page(ap_ssid, ap_password, ip, ap_is_open, opts)

    gc.collect()
    s = _open_server_socket(listen_port)
    _pdbg("socket ready mem_free={}".format(gc.mem_free()))

    while True:
        cl = None
        try:
            cl, _ = s.accept()
        except OSError as ex:
            errno = ex.args[0] if ex.args else None
            if errno == 105:
                _pdbg("accept ENOBUFS; yielding mem_free={}".format(gc.mem_free()))
                time.sleep_ms(500)
            else:
                time.sleep_ms(30)
            gc.collect()
            continue

        try:
            cl.settimeout(_CLIENT_SOCK_TIMEOUT_S)
            gc.collect()
            parsed, rest = _recv_headers(cl)
            if not parsed:
                try:
                    cl.close()
                except Exception:
                    pass
                time.sleep_ms(30)
                gc.collect()
                continue

            req_line, headers = parsed
            parts = req_line.split()
            method = parts[0] if parts else "GET"
            path = parts[1] if len(parts) > 1 else "/"
            if not path.startswith("/status"):
                _pdbg("http {} {} clen={} rest={} mem={}".format(method, path, headers.get("content-length", "?"), len(rest), gc.mem_free()))

            if method == "GET" and (
                path == "/"
                or path.startswith("/hotspot-detect")
                or path.startswith("/ncsi.txt")
                or path.startswith("/connecttest.txt")
                or path.startswith("/redirect")
                or path.startswith("/fwlink")
                or path.startswith("/success")
                or path.startswith("/canonical.html")
                or path.startswith("/generate_204")
            ):
                body = render_portal()
                _http_response(cl, 200, body)
                body = None
            elif method == "GET" and path.startswith("/status"):
                _http_response(cl, 200, ujson.dumps(_PORTAL_STATE), "application/json")
            elif method == "GET" and path.startswith("/logs"):
                _http_response(cl, 200, ujson.dumps({"lines": _PORTAL_LOGS[-40:]}), "application/json")
            elif method == "GET" and path.startswith("/scan"):
                ok, ssids = _scan_ssids_on_demand()
                _http_response(
                    cl,
                    200,
                    ujson.dumps({"ok": bool(ok), "ssids": ssids}),
                    "application/json",
                )
            elif method == "GET" and path.startswith("/health"):
                _http_response(cl, 200, "ok", "text/plain; charset=utf-8")
            elif method == "GET" and path.startswith("/favicon.ico"):
                _http_response(cl, 204, "", "text/plain; charset=utf-8")
            elif method == "POST" and path.startswith("/save"):
                try:
                    clen = int(headers.get("content-length", "0") or "0")
                except ValueError:
                    clen = 0
                if clen > _MAX_FORM_BODY_BYTES:
                    _http_response(cl, 413, ujson.dumps({"ok": False, "error": "Body too large."}), "application/json")
                else:
                    body = _read_body(cl, rest, clen) if clen > 0 else b""
                    if clen > 0 and len(body) < clen:
                        _http_response(cl, 400, ujson.dumps({"ok": False, "error": "Incomplete POST body."}), "application/json")
                    else:
                        form = _parse_form_urlencoded(body)
                        ok, msg = _save_form_to_config(form)
                        if not ok:
                            _set_state(saved=False, lastError=msg)
                            _http_response(cl, 400, ujson.dumps({"ok": False, "error": msg}), "application/json")
                        else:
                            _http_response(cl, 200, ujson.dumps({"ok": True, "message": "Guardado. Reiniciando para conectar a tu WiFi."}), "application/json")
                            time.sleep_ms(700)
                            machine.reset()
            elif method == "POST" and path.startswith("/location"):
                try:
                    clen = int(headers.get("content-length", "0") or "0")
                except ValueError:
                    clen = 0
                body = _read_body(cl, rest, clen) if clen > 0 else b""
                form = _parse_form_urlencoded(body)
                lat = _to_float_or_none(form.get("latitude"))
                lon = _to_float_or_none(form.get("longitude"))
                if lat is None or lon is None:
                    _http_response(cl, 400, ujson.dumps({"ok": False, "error": "Invalid lat/lon."}), "application/json")
                else:
                    cfg = load()
                    cfg["location"]["latitude"] = lat
                    cfg["location"]["longitude"] = lon
                    save(cfg)
                    _set_state(locationSaved=True, lastError=None)
                    _http_response(cl, 200, ujson.dumps({"ok": True}), "application/json")
            elif method == "POST" and path.startswith("/reboot"):
                _http_response(cl, 200, ujson.dumps({"ok": True}), "application/json")
                time.sleep_ms(300)
                machine.reset()
            elif method == "POST" and path.startswith("/factory-reset"):
                _factory_reset_config()
                _http_response(cl, 200, ujson.dumps({"ok": True, "message": "Factory reset aplicado. Reiniciando ESP32."}), "application/json")
                time.sleep_ms(350)
                machine.reset()
            else:
                _http_response(cl, 404, "Not found", "text/plain; charset=utf-8")
        except Exception as ex:
            _pdbg("handler exception: {!r}".format(ex))
            try:
                _http_response(cl, 500, ujson.dumps({"ok": False, "error": "Portal error: {}".format(ex)}), "application/json")
            except Exception:
                pass
        finally:
            try:
                if cl:
                    cl.close()
            except Exception:
                pass
            time.sleep_ms(40)
            gc.collect()

def run_captive_portal(ap_ssid=None, ap_password=None, listen_port=80):
    if ap_ssid is None:
        ap_ssid = DEFAULT_CAPTIVE_AP_SSID
    if ap_password is None:
        ap_password = DEFAULT_CAPTIVE_AP_PASSWORD
    gc.collect()
    _pdbg("run_captive_portal ssid={!r} wpa_len={} mem_free={}".format(ap_ssid, len(ap_password or ""), gc.mem_free()))
    try:
        _set_state(savedConfig=_cfg_summary(load()))
    except Exception:
        pass
    # Scan once BEFORE AP starts. Do not scan while AP is active.
    _scan_ssids_sta_only_before_ap()

    # Strong WiFi cleanup before AP init to reduce netif duplicate-key/OOM states.
    ap = None
    last_err = None
    try:
        sta = network.WLAN(network.STA_IF)
        try:
            sta.disconnect()
        except Exception:
            pass
        try:
            sta.active(False)
        except Exception:
            pass
    except Exception:
        pass

    ap_try = network.WLAN(network.AP_IF)
    try:
        ap_try.active(False)
    except Exception:
        pass
    gc.collect()
    time.sleep_ms(300)

    for attempt in range(6):
        gc.collect()
        try:
            if not ap_try.active():
                ap_try.active(True)
                time.sleep_ms(500)
            ap_is_open = _apply_soft_ap_security(ap_try, ap_ssid, ap_password)
            ap = ap_try
            last_err = None
            break
        except OSError as ex:
            last_err = ex
            print("[portal] AP init retry {}:".format(attempt + 1), ex)
            try:
                ap_try.active(False)
            except Exception:
                pass
            gc.collect()
            time.sleep_ms(700 + attempt * 200)

    if ap is None:
        raise OSError("AP init failed: {}".format(last_err))

    time.sleep_ms(200)
    # DNS del AP = propia IP: los móviles obtienen IP por DHCP y el portal cautivo resuelve mejor que con 8.8.8.8.
    ap.ifconfig(("192.168.4.1", "255.255.255.0", "192.168.4.1", "192.168.4.1"))

    ip = ap.ifconfig()[0]
    try:
        mac = ap.config("mac")
        mac_s = ":".join("{:02x}".format(b) for b in mac) if mac else "?"
    except Exception:
        mac_s = "?"
    _pdbg("AP ifconfig={} mac={}".format(ap.ifconfig(), mac_s))
    # Separador ASCII para consolas serie que no muestran Unicode (em dash, etc.)
    print("[portal] AP '{}' IP {} | {}".format(ap_ssid, ip, _ap_auth_hint(ap_is_open, ap_password)))

    # Portal liviano por sockets puros.
    _run_socket_portal(ap_ssid, ap_password, ip, listen_port, ap_is_open)
