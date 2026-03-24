window.addEventListener('load', () => {
    feather.replace();
})

window.AppTheme = {
    prefersDark: () => window.matchMedia('(prefers-color-scheme: dark)').matches,
    setDataTheme: (mode) => document.documentElement.setAttribute('data-theme', mode)
};

window.downloadFile = function (fileName, base64Data) {
    var bytes = atob(base64Data);
    var arr = new Uint8Array(bytes.length);
    for (var i = 0; i < bytes.length; i++) arr[i] = bytes.charCodeAt(i);
    var blob = new Blob([arr], { type: "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" });
    var url = URL.createObjectURL(blob);
    var a = document.createElement("a");
    a.href = url;
    a.download = fileName;
    document.body.appendChild(a);
    a.click();
    document.body.removeChild(a);
    URL.revokeObjectURL(url);
};