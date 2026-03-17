window.addEventListener('load', () => {
    feather.replace();
})

window.AppTheme = {
    prefersDark: () => window.matchMedia('(prefers-color-scheme: dark)').matches,
    setDataTheme: (mode) => document.documentElement.setAttribute('data-theme', mode)
};