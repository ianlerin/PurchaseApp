window.Print = base64List => {
    base64List.forEach(base64Pdf => {
        const bytes = Uint8Array.from(atob(base64Pdf), c => c.charCodeAt(0));
        const blob = new Blob([bytes], { type: "application/pdf" });
        const url = URL.createObjectURL(blob);

        const win = window.open(url, "_blank");
        setTimeout(() => win.print(), 300);
    });
};
