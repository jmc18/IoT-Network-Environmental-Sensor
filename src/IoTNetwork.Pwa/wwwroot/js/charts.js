/**
 * Helpers ApexCharts para charts en tiempo real (Preline expone su SparklineChart sobre ApexCharts).
 * Mantenemos una cola rolling por chart (<= 60 puntos) para evitar relayout costosos.
 * Docs: https://apexcharts.com/docs/update-chart-data/
 */
(function () {
  const charts = new Map();

  function isDark() {
    return document.documentElement.classList.contains("dark");
  }

  function baseOptions(series, unit) {
    return {
      chart: {
        type: "area",
        height: 160,
        sparkline: { enabled: true },
        animations: { enabled: false },
        toolbar: { show: false },
        zoom: { enabled: false }
      },
      stroke: { curve: "smooth", width: 2 },
      series: [{ name: "valor", data: series || [] }],
      fill: {
        type: "gradient",
        gradient: {
          shadeIntensity: 1,
          opacityFrom: 0.35,
          opacityTo: 0.05,
          stops: [0, 100]
        }
      },
      colors: ["#0d9488"],
      tooltip: {
        theme: isDark() ? "dark" : "light",
        x: { format: "HH:mm:ss" },
        y: {
          formatter: function (val) {
            return (val == null ? "" : val) + (unit || "");
          }
        }
      },
      xaxis: {
        type: "datetime",
        labels: { show: false },
        axisBorder: { show: false },
        axisTicks: { show: false }
      },
      yaxis: { labels: { show: false } },
      grid: { show: false, padding: { top: 0, bottom: 0, left: 0, right: 0 } }
    };
  }

  window.iotChartInit = function (elementId, series, unit, critical) {
    const el = document.getElementById(elementId);
    if (!el || typeof ApexCharts === "undefined") return;

    if (charts.has(elementId)) {
      try { charts.get(elementId).destroy(); } catch (e) { }
      charts.delete(elementId);
    }

    const options = baseOptions(series, unit);
    options.colors = [critical ? "#e11d48" : "#0d9488"];

    const chart = new ApexCharts(el, options);
    chart.render();
    charts.set(elementId, chart);
  };

  window.iotChartUpdate = function (elementId, series, critical) {
    const chart = charts.get(elementId);
    if (!chart) return;
    chart.updateOptions({
      series: [{ name: "valor", data: series || [] }],
      colors: [critical ? "#e11d48" : "#0d9488"]
    }, false, false);
  };

  window.iotChartDestroy = function (elementId) {
    const chart = charts.get(elementId);
    if (!chart) return;
    try { chart.destroy(); } catch (e) { }
    charts.delete(elementId);
  };
})();
