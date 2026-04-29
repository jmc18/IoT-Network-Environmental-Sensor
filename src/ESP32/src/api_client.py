"""Cliente del endpoint de ingest de IoTNetwork.Api."""

import http_client


class ApiClient:
    def __init__(self, base_url, api_key):
        self.base_url = (base_url or "").rstrip("/")
        self.api_key = api_key or ""

    def post_reading(self, node_id, payload):
        """
        payload: dict con claves camelCase para System.Text.Json
        (temperature, humidity, co2, noiseLevel, timestampUtc opcional).
        """
        node_id = (node_id or "").strip().strip("/")
        if not self.base_url or not self.api_key or not node_id:
            print("[api] falta api_base, api_key o node_id")
            return False
        url = "{}/api/ingest/nodes/{}/readings".format(self.base_url, node_id)
        headers = {"X-Api-Key": self.api_key}
        try:
            code, body = http_client.post_json(url, payload, headers=headers, timeout_s=30)
            if code in (200, 201):
                return True
            print("[api] HTTP", code)
            if code == 401 and body:
                try:
                    msg = bytes(body[:160]).decode("utf-8", "replace")
                    print("[api] respuesta:", msg)
                except Exception:
                    pass
                print("[api] revisa que Security:ApiKey en el servidor coincida con device.api_key en config / defaults")
            return False
        except Exception as ex:
            print("[api] error:", ex)
            return False
