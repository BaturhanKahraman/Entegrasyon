window.addEventListener('load', () => {
    feather.replace();
})

window.AppTheme = {
    prefersDark: () => window.matchMedia('(prefers-color-scheme: dark)').matches,
    setDataTheme: (mode) => document.documentElement.setAttribute('data-theme', mode)
};

window.downloadFileFromStream = async function (fileName, contentStreamReference) {
    const arrayBuffer = await contentStreamReference.arrayBuffer();
    const blob = new Blob([arrayBuffer]);
    const url = URL.createObjectURL(blob);
    const a = document.createElement("a");
    a.href = url;
    a.download = fileName;
    document.body.appendChild(a);
    a.click();
    document.body.removeChild(a);
    URL.revokeObjectURL(url);
};
