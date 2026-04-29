ESP32 MicroPython - proyecto optimizado

Cambios incluidos:
- Microdot eliminado del flujo de arranque.
- Portal WiFi por socket ligero en http://192.168.4.1/
- HTML del portal reducido para evitar ENOBUFS.
- Socket del portal se abre antes de renderizar HTML.
- Reintentos ENOBUFS con backoff, no loop agresivo.
- Al guardar WiFi, el ESP32 reinicia y entra en modo STA.
- Panel admin después de conectarse al router:
  http://IP_DEL_ESP32/
  /status
  /logs
  /config
  POST /reboot
  POST /factory-reset
  POST /portal-force

Uso:
1. Sube todo el contenido de esta carpeta al ESP32.
2. Borra archivos viejos que no estén en este ZIP, especialmente microdot.py si quedó en la flash.
3. Reinicia el ESP32.
4. Si no hay WiFi configurado, conecta tu teléfono/laptop a IoT-Config.
5. Abre http://192.168.4.1/
6. Guarda red, node_id, latitud y longitud.
7. El ESP32 reiniciará y se conectará a tu router.
8. Revisa la consola para ver la IP:
   [admin] abre http://192.168.x.x/
9. Entra a esa IP para logs, estado o reset.

Nota:
No se recomienda dejar AP + STA activos todo el tiempo en ESP32 con MicroPython.
Por estabilidad, el portal solo se usa para configurar; luego se usa el panel admin por la red del router.
