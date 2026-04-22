/* global importScripts, firebase, clients */
// Service worker dedicado de Firebase Cloud Messaging.
// Debe vivir en la raíz del sitio (/firebase-messaging-sw.js) para que FCM Web
// lo use como "firebase-cloud-messaging-push-scope". La configuración de Firebase
// se entrega vía query-string al registrar el SW desde js/fcm.js:
//   navigator.serviceWorker.register('/firebase-messaging-sw.js?apiKey=...&projectId=...', ...)
importScripts("https://www.gstatic.com/firebasejs/12.12.1/firebase-app-compat.js");
importScripts("https://www.gstatic.com/firebasejs/12.12.1/firebase-messaging-compat.js");

const params = new URL(self.location.href).searchParams;
const config = {
  apiKey: params.get("apiKey"),
  authDomain: params.get("authDomain"),
  projectId: params.get("projectId"),
  storageBucket: params.get("storageBucket"),
  messagingSenderId: params.get("messagingSenderId"),
  appId: params.get("appId")
};

if (config.apiKey) {
  firebase.initializeApp(config);
  const messaging = firebase.messaging();

  messaging.onBackgroundMessage(function (payload) {
    const title = (payload.notification && payload.notification.title) || "IoT crítico";
    const body = (payload.notification && payload.notification.body) || "Se detectó una lectura crítica.";
    const route = (payload.data && payload.data.route) || "/";

    return self.registration.showNotification(title, {
      body: body,
      icon: "/icon-192.png",
      badge: "/icon-192.png",
      tag: "iot-critical",
      renotify: true,
      requireInteraction: false,
      data: Object.assign({}, payload.data || {}, { url: route })
    });
  });
}

// Al hacer clic en la notificación: enfoca una pestaña abierta con la ruta o
// abre una nueva — mismo patrón que CICA.PWA.
self.addEventListener("notificationclick", function (event) {
  event.notification.close();
  const targetUrl = (event.notification.data && event.notification.data.url) || "/";

  event.waitUntil(
    clients.matchAll({ type: "window", includeUncontrolled: true }).then(function (clientList) {
      for (const client of clientList) {
        if (client.url.includes(targetUrl) && "focus" in client) {
          return client.focus();
        }
      }
      if (clients.openWindow) {
        return clients.openWindow(targetUrl);
      }
      return null;
    })
  );
});
