"""Minimal HTTP client (usocket) for MicroPython, no requests dependency."""

import usocket
import ujson


def _parse_url(url):
    if not url.startswith("http://") and not url.startswith("https://"):
        raise ValueError("url debe ser http o https")
    tls = url.startswith("https://")
    rest = url.split("://", 1)[1]
    hostport, _, path = rest.partition("/")
    if not path:
        path = ""
    path = "/" + path
    if ":" in hostport:
        host, _, port_s = hostport.partition(":")
        port = int(port_s)
    else:
        host = hostport
        port = 443 if tls else 80
    return tls, host, port, path


def post_json(url, obj, headers=None, timeout_s=30):
    """
    POST application/json. Devuelve (status_code, body_bytes o None).
    TLS: en algunos firmwares falla el handshake; prueba HTTP en desarrollo.
    """
    tls, host, port, path = _parse_url(url)
    hdrs = {"Content-Type": "application/json", "Connection": "close"}
    if headers:
        hdrs.update(headers)
    body = ujson.dumps(obj)
    if isinstance(body, str):
        body = body.encode()

    if tls:
        import ssl

        addr = usocket.getaddrinfo(host, port)[0][-1]
        s = usocket.socket()
        s.settimeout(timeout_s)
        s.connect(addr)
        try:
            sock = ssl.wrap_socket(s, server_hostname=host)
        except TypeError:
            sock = ssl.wrap_socket(s)
    else:
        addr = usocket.getaddrinfo(host, port)[0][-1]
        sock = usocket.socket()
        sock.settimeout(timeout_s)
        sock.connect(addr)

    try:
        lines = ["POST {} HTTP/1.0\r\n".format(path), "Host: {}\r\n".format(host)]
        for k, v in hdrs.items():
            lines.append("{}: {}\r\n".format(k, v))
        lines.append("Content-Length: {}\r\n\r\n".format(len(body)))
        sock.write("".join(lines).encode() + body)

        buf = b""
        while True:
            chunk = sock.read(512)
            if not chunk:
                break
            buf += chunk
            if len(buf) > 65536:
                break

        if b"\r\n\r\n" not in buf:
            return -1, None
        head, _, rest = buf.partition(b"\r\n\r\n")
        status = 0
        for line in head.split(b"\r\n"):
            if line.startswith(b"HTTP/"):
                parts = line.split(None, 2)
                if len(parts) >= 2:
                    try:
                        status = int(parts[1])
                    except ValueError:
                        status = 0
                break
        return status, rest
    finally:
        try:
            sock.close()
        except OSError:
            pass
