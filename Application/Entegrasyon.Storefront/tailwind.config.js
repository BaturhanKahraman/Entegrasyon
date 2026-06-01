/** @type {import('tailwindcss').Config} */
module.exports = {
  content: ['./Views/**/*.cshtml', './wwwroot/js/**/*.js'],
  theme: {
    extend: {
      colors: {
        // CSS var'lar RGB triplet (255 143 177) formatında inject ediliyor _Layout'tan;
        // bu sayede Tailwind alpha varyantı (bg-primary/10, text-primary/60 vs.) çalışır.
        primary:   'rgb(var(--color-primary-rgb) / <alpha-value>)',
        secondary: 'rgb(var(--color-secondary-rgb) / <alpha-value>)',
        accent:    'rgb(var(--color-accent-rgb) / <alpha-value>)',
        cream:    { DEFAULT: '#FAF6F0', 300: '#E8DFD2' },
        charcoal: '#2C3E50',
        muted:    '#7B8794',
        success:  '#4CAF89',
        danger:   '#E27D7D',
        warning:  '#F2B544',
      },
      fontFamily: {
        heading: ['Fraunces', 'serif'],
        body: ['Inter', 'system-ui', '-apple-system', 'sans-serif'],
        sans: ['Inter', 'system-ui', '-apple-system', 'sans-serif'],
      },
    },
  },
  plugins: [],
}
