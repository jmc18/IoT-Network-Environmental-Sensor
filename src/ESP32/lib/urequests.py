"""Subconjunto urequests.post; usa http_client en lib/."""

import http_client


def post(url, json=None, headers=None):
    class Response:
        def __init__(self, status):
            self.status_code = status

        def close(self):
            pass

    code, _ = http_client.post_json(url, json or {}, headers=headers or {}, timeout_s=30)
    return Response(code)
