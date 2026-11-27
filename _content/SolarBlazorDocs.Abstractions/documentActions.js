// Modulo JS del componente DocumentActions (SolarBlazorDocs.Abstractions).
// Se importa dinamicamente desde el componente; no requiere configuracion en la app host.

export function printPage() {
    window.print();
}

export async function downloadFileFromStream(fileName, contentType, streamRef) {
    const arrayBuffer = await streamRef.arrayBuffer();
    const blob = new Blob([arrayBuffer], { type: contentType });
    const url = URL.createObjectURL(blob);
    const anchor = document.createElement('a');
    anchor.href = url;
    anchor.download = fileName ?? 'documento';
    document.body.appendChild(anchor);
    anchor.click();
    document.body.removeChild(anchor);
    URL.revokeObjectURL(url);
}
