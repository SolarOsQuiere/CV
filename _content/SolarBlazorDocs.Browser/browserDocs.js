// Modulo JS de SolarBlazorDocs.Browser: imprime un documento HTML mediante el navegador
// (el usuario puede elegir "Guardar como PDF"). Compatible con Blazor WebAssembly.

export function printHtml(html) {
    return new Promise((resolve) => {
        const iframe = document.createElement('iframe');
        iframe.setAttribute('aria-hidden', 'true');
        iframe.style.position = 'fixed';
        iframe.style.right = '0';
        iframe.style.bottom = '0';
        iframe.style.width = '0';
        iframe.style.height = '0';
        iframe.style.border = '0';
        document.body.appendChild(iframe);

        const cleanup = () => {
            setTimeout(() => {
                if (iframe.parentNode) {
                    iframe.parentNode.removeChild(iframe);
                }
                resolve();
            }, 1000);
        };

        let started = false;
        const doPrint = () => {
            if (started) return;
            started = true;
            const win = iframe.contentWindow;
            // Espera a que las imagenes terminen de cargar para incluirlas en la impresion.
            const images = Array.from(win.document.images || []);
            const pending = images.filter(img => !img.complete);

            const launch = () => {
                try {
                    win.focus();
                    win.print();
                } finally {
                    cleanup();
                }
            };

            if (pending.length === 0) {
                setTimeout(launch, 150);
                return;
            }

            let remaining = pending.length;
            const onDone = () => { if (--remaining <= 0) setTimeout(launch, 150); };
            pending.forEach(img => {
                img.addEventListener('load', onDone, { once: true });
                img.addEventListener('error', onDone, { once: true });
            });
            // Salvaguarda por si algun recurso no dispara evento.
            setTimeout(launch, 3000);
        };

        iframe.onload = doPrint;

        const doc = iframe.contentWindow.document;
        doc.open();
        doc.write(html);
        doc.close();

        // Si onload no se dispara (algunos navegadores con document.write), forzamos.
        if (doc.readyState === 'complete') {
            doPrint();
        }
    });
}
