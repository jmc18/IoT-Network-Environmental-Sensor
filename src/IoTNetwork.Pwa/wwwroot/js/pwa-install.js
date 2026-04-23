// Lógica del prompt "Instalar app".
//
// Chrome/Edge (Android y desktop) disparan `beforeinstallprompt` cuando se
// cumplen los criterios de PWA (manifest válido + service worker + HTTPS).
// Guardamos el evento y exponemos una API mínima para que un componente
// Blazor pueda mostrar su propio botón con el look de Preline/Tailwind.
//
// iOS/Safari NO soportan `beforeinstallprompt`: hay que mostrar instrucciones
// manuales ("Compartir -> Añadir a pantalla de inicio").
(function () {
    const state = {
        deferredPrompt: null,
        installed: false,
        listeners: new Map(),
        nextListenerId: 1,
    };

    function isStandalone() {
        return (
            window.matchMedia('(display-mode: standalone)').matches ||
            window.matchMedia('(display-mode: minimal-ui)').matches ||
            // iOS Safari expone navigator.standalone en lugar del media query.
            window.navigator.standalone === true
        );
    }

    function isIOS() {
        const ua = window.navigator.userAgent || '';
        const isIDevice = /iPad|iPhone|iPod/.test(ua) && !window.MSStream;
        // iPad moderno reporta Mac; detectamos por touch.
        const isMacWithTouch =
            /Macintosh/.test(ua) &&
            typeof navigator.maxTouchPoints === 'number' &&
            navigator.maxTouchPoints > 1;
        return isIDevice || isMacWithTouch;
    }

    function notify() {
        const snapshot = {
            canInstall: !!state.deferredPrompt,
            installed: state.installed || isStandalone(),
            isIOS: isIOS(),
            isStandalone: isStandalone(),
        };
        for (const [id, cb] of state.listeners) {
            try {
                cb(snapshot);
            } catch {
                // Listener disposed; lo removemos para no seguir invocándolo.
                state.listeners.delete(id);
            }
        }
    }

    window.addEventListener('beforeinstallprompt', (event) => {
        event.preventDefault();
        state.deferredPrompt = event;
        notify();
    });

    window.addEventListener('appinstalled', () => {
        state.deferredPrompt = null;
        state.installed = true;
        notify();
    });

    window.pwaInstall = {
        getStatus() {
            return {
                canInstall: !!state.deferredPrompt,
                installed: state.installed || isStandalone(),
                isIOS: isIOS(),
                isStandalone: isStandalone(),
            };
        },
        async prompt() {
            const deferred = state.deferredPrompt;
            if (!deferred) return 'unavailable';
            try {
                deferred.prompt();
                const choice = await deferred.userChoice;
                state.deferredPrompt = null;
                notify();
                return choice && choice.outcome ? choice.outcome : 'dismissed';
            } catch {
                state.deferredPrompt = null;
                notify();
                return 'error';
            }
        },
        // El componente Blazor pasa un DotNetObjectReference; lo adaptamos a callback.
        // Devuelve un ID numérico para poder desuscribir desde Blazor.
        registerDotNetListener(dotNetRef) {
            const id = state.nextListenerId++;
            const cb = (snapshot) => {
                dotNetRef.invokeMethodAsync('OnPwaInstallStatusChanged', snapshot);
            };
            state.listeners.set(id, cb);
            // Notificación inicial para sincronizar estado al montar el componente.
            cb(window.pwaInstall.getStatus());
            return id;
        },
        unregisterDotNetListener(id) {
            if (typeof id === 'number') {
                state.listeners.delete(id);
            }
        },
    };
})();
