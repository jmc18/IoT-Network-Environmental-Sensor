"""Lightweight admin panel over STA (router network)."""

import gc
import machine
import time
import usocket
import ujson

from config_store import default_config, load, save


def _safe_send(sock, data, chunk_size=512):
    if isinstance(data, str):
        data = data.encode("utf-8")
    sent = 0
    total = len(data)
    while sent < total:
        try:
            n = sock.send(data[sent : sent + chunk_size])
        except OSError:
            break
        if not n:
            break
        sent += n
        time.sleep_ms(1)


class AdminServer:
    def __init__(self, port=80):
        self._port = int(port)
        self._sock = None
        self._state = {
            "wifi_connected": False,
            "sta_ifconfig": None,
            "node_id": "",
            "local_only": False,
            "last_payload": {},
            "last_error": None,
            "last_event": None,
            "loop_interval": None,
            "uptime_ms": 0,
            "mem_free": 0,
        }
        self._logs = []
        self._max_logs = 120

    def log(self, msg):
        line = "{} {}".format(time.ticks_ms(), msg)
        self._logs.append(line)
        if len(self._logs) > self._max_logs:
            del self._logs[: len(self._logs) - self._max_logs]

    def update(self, **kwargs):
        self._state.update(kwargs)
        self._state["uptime_ms"] = time.ticks_ms()
        try:
            self._state["mem_free"] = gc.mem_free()
        except Exception:
            pass

    def start(self):
        if self._sock is not None:
            return
        gc.collect()
        s = usocket.socket(usocket.AF_INET, usocket.SOCK_STREAM)
        s.setsockopt(usocket.SOL_SOCKET, usocket.SO_REUSEADDR, 1)
        s.bind(("0.0.0.0", self._port))
        s.listen(1)
        s.settimeout(0.02)
        self._sock = s
        self.log("[admin] listening on :{}".format(self._port))

    def _send(self, cl, code, body, ctype="text/plain; charset=utf-8"):
        if isinstance(body, str):
            body = body.encode("utf-8")
        reason = "OK" if code < 400 else "ERROR"
        head = (
            "HTTP/1.0 {} {}\r\n"
            "Content-Type: {}\r\n"
            "Content-Length: {}\r\n"
            "Connection: close\r\n"
            "Cache-Control: no-store\r\n\r\n"
        ).format(code, reason, ctype, len(body))
        _safe_send(cl, head.encode("utf-8"), 256)
        _safe_send(cl, body, 512)

    def _read_request(self, cl):
        data = b""
        try:
            while b"\r\n\r\n" not in data and len(data) < 1536:
                chunk = cl.recv(256)
                if not chunk:
                    break
                data += chunk
        except OSError:
            return None, None
        if not data:
            return None, None
        line = data.split(b"\r\n", 1)[0].decode("utf-8", "replace")
        parts = line.split()
        if len(parts) < 2:
            return None, None
        return parts[0], parts[1]

    def _html_home(self):
        ip = None
        ifcfg = self._state.get("sta_ifconfig")
        if ifcfg and isinstance(ifcfg, (tuple, list)) and len(ifcfg) > 0:
            ip = ifcfg[0]
        tpl = self._load_template(
            "admin_home.html",
            "<html><body><h2>ESP32 Admin</h2><p>IP: __IP__</p><p>Node: __NODE__</p></body></html>",
        )
        return (
            tpl.replace("__IP__", str(ip or "n/a"))
            .replace("__NODE__", str(self._state.get("node_id") or ""))
            .replace("__WIFI__", "ok" if self._state.get("wifi_connected") else "down")
            .replace("__EV__", str(self._state.get("last_event") or "n/a"))
            .replace("__MEM__", str(self._state.get("mem_free") or 0))
        )

    def _html_status(self):
        self.update()
        body = ujson.dumps(self._state)
        tpl = self._load_template(
            "admin_status.html",
            "<html><body><h2>Status</h2><pre>__BODY__</pre></body></html>",
        )
        return tpl.replace("__BODY__", body)

    def _html_logs(self):
        logs = "\n".join(self._logs[-80:]) or "(sin logs)"
        tpl = self._load_template(
            "admin_logs.html",
            "<html><body><h2>Logs</h2><pre>__LOGS__</pre></body></html>",
        )
        return tpl.replace("__LOGS__", logs)

    def _html_config(self):
        cfg = load()
        try:
            pwd = cfg.get("wifi", {}).get("password") or ""
            cfg["wifi"]["password"] = "*" * len(pwd) if pwd else ""
        except Exception:
            pass
        body = ujson.dumps(cfg)
        tpl = self._load_template(
            "admin_config.html",
            "<html><body><h2>Config</h2><pre>__BODY__</pre></body></html>",
        )
        return tpl.replace("__BODY__", body)

    def _load_template(self, name, fallback):
        for p in (
            "templates/{}".format(name),
            "/flash/templates/{}".format(name),
        ):
            try:
                with open(p, "r") as f:
                    return f.read()
            except OSError:
                pass
        return fallback

    def poll(self):
        if self._sock is None:
            return
        cl = None
        try:
            cl, _ = self._sock.accept()
        except OSError:
            return
        try:
            cl.settimeout(0.6)
            method, path = self._read_request(cl)
            if not method:
                return
            if method == "GET" and path == "/":
                self._send(cl, 200, self._html_home(), "text/html; charset=utf-8")
            elif method == "GET" and path == "/status":
                self._send(cl, 200, self._html_status(), "text/html; charset=utf-8")
            elif method == "GET" and path.startswith("/status.json"):
                self.update()
                self._send(cl, 200, ujson.dumps(self._state), "application/json")
            elif method == "GET" and path == "/logs":
                self._send(cl, 200, self._html_logs(), "text/html; charset=utf-8")
            elif method == "GET" and path.startswith("/logs.json"):
                self._send(cl, 200, ujson.dumps({"lines": self._logs[-80:]}), "application/json")
            elif method == "GET" and path == "/config":
                self._send(cl, 200, self._html_config(), "text/html; charset=utf-8")
            elif method == "GET" and path.startswith("/config.json"):
                cfg = load()
                try:
                    pwd = cfg.get("wifi", {}).get("password") or ""
                    cfg["wifi"]["password"] = "*" * len(pwd) if pwd else ""
                except Exception:
                    pass
                self._send(cl, 200, ujson.dumps(cfg), "application/json")
            elif method == "POST" and path.startswith("/reboot"):
                self._send(cl, 200, ujson.dumps({"ok": True}), "application/json")
                time.sleep_ms(250)
                machine.reset()
            elif method == "POST" and path.startswith("/factory-reset"):
                cfg = default_config()
                save(cfg)
                self._send(cl, 200, ujson.dumps({"ok": True, "message": "Reiniciando en modo portal"}), "application/json")
                time.sleep_ms(300)
                machine.reset()
            elif method == "POST" and path.startswith("/portal-force"):
                cfg = load()
                cfg["portal_force"] = True
                save(cfg)
                self._send(cl, 200, ujson.dumps({"ok": True, "message": "Reinicia para entrar al portal"}), "application/json")
            else:
                self._send(cl, 404, "Not found")
        except Exception as ex:
            try:
                self._send(cl, 500, ujson.dumps({"ok": False, "error": str(ex)}), "application/json")
            except Exception:
                pass
        finally:
            try:
                if cl:
                    cl.close()
            except Exception:
                pass
            gc.collect()
