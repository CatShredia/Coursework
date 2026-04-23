window.runews = window.runews || {};

window.runews.downloadTextFile = (fileName, content, mimeType) => {
    const blob = new Blob([content ?? ""], { type: mimeType || "text/plain;charset=utf-8" });
    const url = URL.createObjectURL(blob);
    const a = document.createElement("a");
    a.href = url;
    a.download = fileName || "download.txt";
    document.body.appendChild(a);
    a.click();
    a.remove();
    URL.revokeObjectURL(url);
};
