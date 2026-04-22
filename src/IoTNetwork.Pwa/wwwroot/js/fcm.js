/**
 * FCM Web helper para IoT Network.
 * - Inicializa Firebase (compat) con la config que viene desde appsettings.json.
 * - Registra el service worker dedicado pasando la config por query-string.
 * - Pide permiso, obtiene el token y lo devuelve a Blazor.
 * - Maneja mensajes en foreground: toast visual + notificación nativa con
 *   deep-link (click navega a payload.data.route), imitando CICA.PWA.
 *
 * Expone:
 *   iotFcmInit(config, vapidKey, dotNetRef) -> Promise<string|null>
 *   iotFcmCheckPermission()   -> 'granted'|'denied'|'default'|'unsupported'
 *   iotFcmRequestPermission() -> 'granted'|'denied'|'unsupported'
 */
(function () {
  let messagingInstance = null;
  let dotNetCallback = null;

  async function registerSw(config) {
    if (!("serviceWorker" in navigator)) return null;
    const qs = new URLSearchParams({
      apiKey: config.apiKey || "",
      authDomain: config.authDomain || "",
      projectId: config.projectId || "",
      storageBucket: config.storageBucket || "",
      messagingSenderId: config.messagingSenderId || "",
      appId: config.appId || ""
    }).toString();

    return navigator.serviceWorker.register(`/firebase-messaging-sw.js?${qs}`, { scope: "/" });
  }

  window.iotFcmInit = async function (config, vapidKey, dotNetRef) {
    try {
      if (!config || !config.apiKey || typeof firebase === "undefined") return null;
      if (!("Notification" in window) || !("serviceWorker" in navigator)) return null;

      if (!firebase.apps.length) {
        firebase.initializeApp(config);
      }

      const permission = await Notification.requestPermission();
      if (permission !== "granted") return null;

      const swReg = await registerSw(config);
      if (!swReg) return null;

      messagingInstance = firebase.messaging();
      dotNetCallback = dotNetRef || null;

      const tokenOptions = { serviceWorkerRegistration: swReg };
      if (vapidKey) tokenOptions.vapidKey = vapidKey;

      const token = await messagingInstance.getToken(tokenOptions);

      messagingInstance.onMessage(function (payload) {
        const title = (payload && payload.notification && payload.notification.title) || "IoT crítico";
        const body = (payload && payload.notification && payload.notification.body) || "Lectura crítica recibida.";
        const route = (payload && payload.data && payload.data.route) || null;

        showForegroundToast(title, body, route);
        showSystemNotification(title, body, route);

        if (dotNetCallback) {
          try { dotNetCallback.invokeMethodAsync("OnForegroundPush", title, body); } catch (e) { }
        }
      });

      return token || null;
    } catch (e) {
      console.warn("iotFcmInit error", e);
      return null;
    }
  };

  window.iotFcmCheckPermission = function () {
    if (!("Notification" in window)) return "unsupported";
    return Notification.permission;
  };

  window.iotFcmRequestPermission = async function () {
    if (!("Notification" in window)) return "unsupported";
    try {
      return await Notification.requestPermission();
    } catch (e) {
      return "denied";
    }
  };

  function showSystemNotification(title, body, route) {
    if (!("Notification" in window) || Notification.permission !== "granted") return;
    try {
      const n = new Notification(title, {
        body: body,
        icon: "/icon-192.png",
        badge: "/icon-192.png",
        tag: "iot-critical",
        data: { url: route || "/" }
      });
      n.onclick = function (ev) {
        ev.preventDefault();
        const url = (ev.currentTarget && ev.currentTarget.data && ev.currentTarget.data.url) || route || "/";
        try { ev.currentTarget.close(); } catch (e) { }
        if (url && url !== window.location.pathname) {
          window.location.href = url;
        } else {
          window.focus();
        }
      };
    } catch (e) { }
  }

  function showForegroundToast(title, body, route) {
    try {
      const host = document.createElement("div");
      host.setAttribute("role", "status");
      host.className = "fixed top-4 right-4 z-[999] max-w-sm cursor-pointer rounded-2xl border border-rose-200 bg-white p-4 text-sm text-slate-900 shadow-xl ring-1 ring-rose-200 transition hover:shadow-2xl dark:border-rose-800 dark:bg-slate-900 dark:text-white dark:ring-rose-800";
      host.innerHTML = `<p class="font-bold text-rose-600 dark:text-rose-300">${escapeHtml(title)}</p><p class="mt-1 leading-relaxed">${escapeHtml(body)}</p>`;
      if (route) {
        host.addEventListener("click", function () { window.location.href = route; });
      }
      document.body.appendChild(host);
      setTimeout(function () { host.remove(); }, 8000);
    } catch (e) { }
  }

  function escapeHtml(s) {
    return String(s || "").replace(/[&<>"']/g, function (c) {
      return ({ "&": "&amp;", "<": "&lt;", ">": "&gt;", '"': "&quot;", "'": "&#39;" })[c];
    });
  }
})();
