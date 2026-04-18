/**
 * Preline UI + Blazor: reinicializar componentes tras render / navegación SPA.
 * Patrón alineado con A_PG (HSStaticMethods.autoInit).
 * Docs: https://preline.co/docs/index.html
 */
window.InitPreline = function () {
  requestAnimationFrame(function () {
    requestAnimationFrame(function () {
      if (window.HSStaticMethods && typeof window.HSStaticMethods.autoInit === "function") {
        window.HSStaticMethods.autoInit();
      }
    });
  });
};

/** Limpia overlays y scroll si Preline deja el body bloqueado al cambiar de ruta (SPA). */
window.iotCloseHsOverlayAndUnlockBody = function () {
  document.querySelectorAll(".hs-overlay-backdrop").forEach(function (b) {
    b.remove();
  });
  document.body.classList.remove("hs-overlay-body-open", "overflow-hidden");
  document.documentElement.classList.remove("overflow-hidden");
  document.body.style.removeProperty("overflow");
  document.body.style.removeProperty("padding-right");
  document.documentElement.style.removeProperty("overflow");
  document.documentElement.style.removeProperty("padding-right");
};
