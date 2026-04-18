window.iotTheme = {
  apply: function (isDark) {
    document.documentElement.classList.toggle("dark", !!isDark);
    try {
      localStorage.setItem("iot-theme", isDark ? "dark" : "light");
    } catch (e) {}
  },
  isDark: function () {
    return document.documentElement.classList.contains("dark");
  },
  toggle: function () {
    var next = !document.documentElement.classList.contains("dark");
    window.iotTheme.apply(next);
    return next;
  },
};
