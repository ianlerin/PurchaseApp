window.printSection = function (elementId) {
    setTimeout(() => {
        const element = document.getElementById(elementId);
        if (!element) {
            alert("Element not found");
            return;
        }

        const clonedElement = element.cloneNode(true);
        const printWindow = window.open('', '_blank', 'width=800,height=600');
        const doc = printWindow.document;

        doc.open();
        doc.write('<html><head><title>Print Preview</title>');

        // Load CSS from main page
        [...document.styleSheets].forEach(styleSheet => {
            try {
                if (styleSheet.href) {
                    doc.write(`<link rel="stylesheet" href="${styleSheet.href}">`);
                }
            } catch (e) {
                console.warn("Style load error:", e);
            }
        });

        // Optional: remove headers/footers and apply padding
        doc.write(`
            <style>
                @page { margin: 0; size: auto; }
                body { margin: 1cm; font-family: sans-serif; }
            </style>
        `);

        doc.write('</head><body>');
        doc.body.appendChild(clonedElement);
        doc.write('</body></html>');
        doc.close();

        // Set title so "about:blank" doesn't show
        doc.title = "Purchase Requisition Print";

        // Wait for render, then print
        setTimeout(() => {
            printWindow.focus();
            printWindow.print();
            printWindow.close();
        }, 500);
    }, 0);
};