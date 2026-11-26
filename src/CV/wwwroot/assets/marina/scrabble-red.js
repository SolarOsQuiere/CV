// Red del Scrabble de Marina: conexion directa (WebRTC) entre dos navegadores usando PeerJS.
// No hay servidor propio. PeerJS usa su servicio publico gratuito solo para que los dos navegadores se encuentren
// (el codigo de sala es el id del anfitrion); despues los datos viajan directamente entre ellos.
// Todo viaja como texto (JSON): lo montan y lo leen ScrabbleSesion.cs.

const PREFIJO = 'marina-scrabble-';
const VERSION_PEERJS = '1.5.4';
const ESPERA_MS = 12000;

let peer = null;
let conn = null;
let carga = null;

function cargarPeerJs() {
    if (window.Peer) {
        return Promise.resolve();
    }

    carga ??= new Promise((ok, ko) => {
        const s = document.createElement('script');
        s.src = `https://cdn.jsdelivr.net/npm/peerjs@${VERSION_PEERJS}/dist/peerjs.min.js`;
        s.onload = ok;
        s.onerror = () => ko(new Error('peerjs'));
        document.head.appendChild(s);
    }).catch(e => { carga = null; throw e; });

    return carga;
}

function enlazar(dotnet, c) {
    c.on('data', d => dotnet.invokeMethodAsync('AlMensaje', String(d)));
    c.on('close', () => {
        if (conn === c) {
            conn = null;
            dotnet.invokeMethodAsync('AlCierre');
        }
    });
    c.on('error', () => { });
}

// Anfitrion: abre la sala con ese codigo. Devuelve null si va bien o el texto del error.
export async function crear(dotnet, codigo) {
    cerrar();
    try { await cargarPeerJs(); } catch { return 'No se pudo cargar PeerJS (¿sin internet?).'; }

    return new Promise(resolve => {
        const p = new window.Peer(PREFIJO + codigo);
        peer = p;
        let abierta = false;

        const timer = setTimeout(() => {
            if (!abierta) { abierta = true; resolve('No se pudo contactar con el servicio de salas.'); cerrar(); }
        }, ESPERA_MS);

        p.on('open', () => { if (!abierta) { abierta = true; clearTimeout(timer); resolve(null); } });

        p.on('error', e => {
            if (!abierta) {
                abierta = true;
                clearTimeout(timer);
                resolve(e.type === 'unavailable-id' ? 'Ese código de sala ya está en uso.' : `No se pudo abrir la sala (${e.type}).`);
                cerrar();
            }
        });

        // Si se pierde el contacto con el servicio de salas, la partida en curso sigue; solo se vuelve a registrar la sala.
        p.on('disconnected', () => { if (peer === p && !p.destroyed) { p.reconnect(); } });

        // Solo cabe un invitado: cualquier otro se rechaza.
        p.on('connection', c => {
            if (conn && conn.open) {
                c.on('open', () => {
                    c.send(JSON.stringify({
                        type: 'JOIN_REJECTED', roomId: codigo, messageId: 'sala-llena', turn: 0,
                        payload: { text: 'La sala está llena (máximo dos jugadores).' }
                    }));
                    setTimeout(() => c.close(), 300);
                });
                return;
            }
            conn = c;
            enlazar(dotnet, c);
        });
    });
}

// Invitado: se conecta a la sala con ese codigo. Devuelve null si va bien o el texto del error.
export async function unirse(dotnet, codigo) {
    cerrar();
    try { await cargarPeerJs(); } catch { return 'No se pudo cargar PeerJS (¿sin internet?).'; }

    return new Promise(resolve => {
        const p = new window.Peer();
        peer = p;
        let listo = false;

        const terminar = texto => {
            if (listo) { return; }
            listo = true;
            clearTimeout(timer);
            resolve(texto);
            if (texto !== null) { cerrar(); }
        };

        const timer = setTimeout(() => terminar('No se encontró la sala. ¿El código es correcto y el anfitrión sigue conectado?'), ESPERA_MS);

        p.on('error', e => terminar(e.type === 'peer-unavailable' ? 'No existe ninguna sala con ese código.' : `No se pudo conectar (${e.type}).`));

        p.on('open', () => {
            const c = p.connect(PREFIJO + codigo, { reliable: true });
            c.on('open', () => {
                if (listo) { return; }
                conn = c;
                enlazar(dotnet, c);
                terminar(null);
            });
        });
    });
}

export function enviar(texto) {
    if (conn && conn.open) {
        conn.send(texto);
    }
}

export function cerrar() {
    const c = conn;
    const p = peer;
    conn = null;
    peer = null;
    try { c?.close(); } catch { /* ya cerrada */ }
    try { p?.destroy(); } catch { /* ya destruido */ }
}
