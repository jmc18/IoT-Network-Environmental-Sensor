window.iotMapDestroy = function (elementId) {
  const el = document.getElementById(elementId);
  if (!el) {
    return;
  }
  const map = el._iotLeafletMap;
  if (map) {
    map.remove();
    el._iotLeafletMap = null;
  }
};

window.iotMapInit = function (elementId, lat, lng, zoom) {
  const el = document.getElementById(elementId);
  if (!el || typeof L === "undefined") {
    return;
  }
  if (el._iotLeafletMap) {
    el._iotLeafletMap.remove();
    el._iotLeafletMap = null;
  }
  const z = zoom && zoom > 0 ? zoom : 13;
  const map = L.map(el).setView([lat, lng], z);
  L.tileLayer("https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png", {
    maxZoom: 19,
    attribution: "&copy; OpenStreetMap contributors",
  }).addTo(map);
  L.marker([lat, lng]).addTo(map);
  el._iotLeafletMap = map;
  setTimeout(() => map.invalidateSize(), 250);
};
