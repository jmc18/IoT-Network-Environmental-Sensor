/** @type {import('tailwindcss').Config} */
module.exports = {
 content: [
    './**/*.{razor,razor.cs,cshtml,html}',
    './wwwroot/preline/**/*.{js,ts}',
    './node_modules/preline/dist/*.js',
  ],
  // Modo oscuro: clase `dark` en <html> (ver tailwind-styles.css y theme.js)
  important: true,
  theme: {
    extend: {
      spacing: {
        'safe-top': 'env(safe-area-inset-top)',
        'safe-bottom': 'env(safe-area-inset-bottom)',
        'safe-left': 'env(safe-area-inset-left)',
        'safe-right': 'env(safe-area-inset-right)',
      },
    },
    container: {
      center: true,
    },
  },
  plugins: [require('@tailwindcss/forms')],
};
