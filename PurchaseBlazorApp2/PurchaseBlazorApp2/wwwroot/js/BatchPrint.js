window.batchPrintPdfs = async (base64List) => {
    if (!Array.isArray(base64List) || base64List.length === 0) {
        console.warn("[PrintMergedPdf] No PDFs to print");
        return;
    }

    console.log(`[PrintMergedPdf] Merging ${base64List.length} PDFs`);

    // Create a new PDF document
    const mergedPdf = await PDFLib.PDFDocument.create();

    for (const base64Pdf of base64List) {
        const pdfBytes = Uint8Array.from(atob(base64Pdf), c => c.charCodeAt(0));
        const pdf = await PDFLib.PDFDocument.load(pdfBytes);
        const copiedPages = await mergedPdf.copyPages(pdf, pdf.getPageIndices());
        copiedPages.forEach(p => mergedPdf.addPage(p));
    }

    const mergedBytes = await mergedPdf.save();
    const blob = new Blob([mergedBytes], { type: "application/pdf" });
    const url = URL.createObjectURL(blob);

    const win = window.open(url, "_blank");
    win.onload = () => win.print();
};