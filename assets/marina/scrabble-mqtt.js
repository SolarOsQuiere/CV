// Red del Scrabble de Marina: mensajeria por un broker MQTT publico (WebSocket seguro), sin servidor propio.
// A diferencia de WebRTC no hace falta una conexion directa entre los dos navegadores (que fallaba entre redes
// restrictivas, p. ej. movil con datos + wifi de empresa): cada uno sale por HTTPS/WebSocket al broker y este reenvia.
// Todo viaja como texto (JSON): lo montan y lo leen ScrabbleSesion.cs a traves de JsGameTransport.cs.
//
// Temas (todos bajo marina-scrabble-v1/<sala>/):
//   guest/<id>      invitado -> anfitrion         to/<id>        anfitrion -> ese invitado
//   presence/host   "1" (retenido) mientras la sala esta abierta; el broker publica "0" solo si el anfitrion se cae
//   presence/<id>   el broker publica "0" si ese invitado se cae
// Ojo: un broker publico no es privado. Cualquiera que escuche el broker puede ver las salas.

const BROKERS = [
    'wss://broker.hivemq.com:8884/mqtt',
    'wss://broker.emqx.io:8084/mqtt',
    'wss://test.mosquitto.org:8081'
];
const VERSION_MQTT = '5.10.1';
const RAIZ = 'marina-scrabble-v1';
const ESPERA_MS = 6000;
const ESPERA_PRESENCIA_MS = 3000;
const MAX_LONGITUD = 32768;

let dotnet = null;
let cliente = null;
let base = '';
let miId = '';
let esAnfitrion = false;
let invitado = null;
let carga = null;
let pantalla = null;

// Traza en la consola del navegador (F12): sirve para ver en que paso se queda una conexion.
const log = (...datos) => console.info('[scrabble-mqtt]', ...datos);

const aleatorio = n => Array.from(crypto.getRandomValues(new Uint8Array(n)), b => (b % 36).toString(36)).join('');
const texto = payload => new TextDecoder().decode(payload);

function cargarMqtt() {
    if (window.mqtt) {
        return Promise.resolve();
    }

    carga ??= new Promise((ok, ko) => {
        const s = document.createElement('script');
        s.src = `https://cdn.jsdelivr.net/npm/mqtt@${VERSION_MQTT}/dist/mqtt.min.js`;
        s.onload = ok;
        s.onerror = () => ko(new Error('mqtt'));
        document.head.appendChild(s);
    }).catch(e => { carga = null; throw e; });

    return carga;
}

// Conecta con un broker. Sin reconexion automatica al principio: si falla se prueba el siguiente.
function abrir(url, opciones) {
    return new Promise((ok, ko) => {
        const c = window.mqtt.connect(url, { reconnectPeriod: 0, connectTimeout: ESPERA_MS, keepalive: 15, clean: true, ...opciones });
        const limite = setTimeout(() => { c.end(true); ko(new Error('tiempo agotado')); }, ESPERA_MS + 1000);
        c.once('connect', () => { clearTimeout(limite); ok(c); });
        c.once('error', e => { clearTimeout(limite); c.end(true); ko(e); });
    });
}

async function conectarAlPrimero(opciones) {
    for (const url of BROKERS) {
        try {
            const c = await abrir(url, opciones);
            log('conectado con el broker', url);
            return c;
        } catch (e) {
            log('broker no disponible', url, e?.message ?? e);
        }
    }
    return null;
}

// Pregunta si hay una sala abierta: el broker entrega enseguida el ultimo valor retenido de presence/host.
function consultarPresencia(c, tema, ms) {
    return new Promise(resolver => {
        const limite = setTimeout(() => { c.off('message', alRecibir); resolver(false); }, ms);
        const alRecibir = (t, payload) => {
            if (t === tema) {
                clearTimeout(limite);
                c.off('message', alRecibir);
                resolver(texto(payload) === '1');
            }
        };
        c.on('message', alRecibir);
        c.subscribe(tema, { qos: 1 });
    });
}

// El anfitrion (sobre todo en movil) pierde la sala si la pantalla se apaga o la pestana pasa a segundo plano:
// se pide que la pantalla siga encendida mientras dure la sala (y se vuelve a pedir al volver a la pestana).
async function mantenerPantalla() {
    try {
        if ('wakeLock' in navigator && document.visibilityState === 'visible') {
            pantalla = await navigator.wakeLock.request('screen');
            log('pantalla: se mantiene encendida mientras la sala este abierta');
        }
    } catch (e) {
        log('pantalla: no se pudo mantener encendida', e?.message ?? e);
    }
}

document.addEventListener('visibilitychange', () => {
    if (document.visibilityState === 'visible' && esAnfitrion && cliente && (!pantalla || pantalla.released)) {
        mantenerPantalla();
    }
});

function responder(id, objeto) {
    cliente?.publish(`${base}/to/${id}`, JSON.stringify(objeto), { qos: 1 });
}

// Pasa un mensaje a .NET.
function entregar(mensaje) {
    log('recibido', mensaje.slice(0, 80));
    dotnet.invokeMethodAsync('AlMensaje', mensaje).catch(e => log('error al pasar el mensaje a .NET', e));
}

// ---------- Anfitrion ----------

function alRecibirComoAnfitrion(c, codigo) {
    return (tema, payload) => {
        if (cliente !== c) { return; }

        const [tipo, id] = tema.slice(base.length + 1).split('/');
        const cuerpo = texto(payload);

        if (tipo === 'presence') {
            // Un invitado se ha caido (el broker publica su "ultima voluntad"): se libera su sitio.
            if (id !== 'host' && id === invitado && cuerpo === '0') {
                log('invitado desconectado');
                invitado = null;
                dotnet.invokeMethodAsync('AlCierre').catch(e => log('error en AlCierre', e));
            }
            return;
        }

        if (tipo !== 'guest' || cuerpo.length > MAX_LONGITUD) { return; }

        let mensaje;
        try { mensaje = JSON.parse(cuerpo); } catch { return; }

        // Solo cabe un invitado: otro distinto se rechaza (hasta que el primero se vaya).
        if (invitado && invitado !== id) {
            if (mensaje?.type === 'JOIN_REQUEST') {
                log('sala llena: se rechaza a', id);
                responder(id, {
                    type: 'JOIN_REJECTED', roomId: codigo, messageId: `llena-${id}`, turn: 0,
                    payload: { text: 'La sala está llena (máximo dos jugadores). Si acabas de salir de ella, espera unos segundos y vuelve a entrar.' }
                });
            }
            return;
        }

        if (!invitado) {
            if (mensaje?.type !== 'JOIN_REQUEST') { return; }
            invitado = id;
            log('entra un invitado', id);
        }

        entregar(cuerpo);
    };
}

// Abre la sala con ese codigo. Devuelve null si va bien o el texto del error.
export async function crear(referencia, codigo) {
    await cerrar();
    dotnet = referencia;
    base = `${RAIZ}/${codigo}`;

    try { await cargarMqtt(); } catch { return 'No se pudo cargar la librería de mensajería (¿sin internet?).'; }

    const c = await conectarAlPrimero({
        clientId: `marina-host-${aleatorio(12)}`,
        will: { topic: `${base}/presence/host`, payload: '0', qos: 1, retain: true }
    });
    if (!c) { return 'No se pudo contactar con ningún servicio de mensajería (la red puede estar bloqueando WebSocket).'; }

    if (await consultarPresencia(c, `${base}/presence/host`, 1500)) {
        // Cierre ordenado (sin `true`): asi el broker descarta la "ultima voluntad" de esta conexion. Con un cierre
        // brusco la publicaria y marcaria como cerrada la sala del anfitrion que ya existe.
        c.end();
        return 'Ese código de sala ya está en uso.';
    }

    cliente = c;
    esAnfitrion = true;
    c.on('message', alRecibirComoAnfitrion(c, codigo));
    c.subscribe([`${base}/guest/+`, `${base}/presence/+`], { qos: 1 });

    // Si se pierde el contacto con el broker (movil que duerme, cobertura...), se reconecta solo y se vuelve a abrir la sala.
    c.options.reconnectPeriod = 3000;
    c.on('connect', () => {
        c.publish(`${base}/presence/host`, '1', { qos: 1, retain: true });
        log('sala abierta', codigo);
    });
    c.on('offline', () => log('sin conexion con el broker: reintentando'));

    c.publish(`${base}/presence/host`, '1', { qos: 1, retain: true });
    mantenerPantalla();
    log('sala abierta', codigo);
    return null;
}

// ---------- Invitado ----------

// Se une a la sala con ese codigo. Devuelve null si va bien o el texto del error.
export async function unirse(referencia, codigo) {
    await cerrar();
    dotnet = referencia;
    base = `${RAIZ}/${codigo}`;
    miId = aleatorio(10);

    try { await cargarMqtt(); } catch { return 'No se pudo cargar la librería de mensajería (¿sin internet?).'; }

    let hayBroker = false;
    for (const url of BROKERS) {
        let c;
        try {
            c = await abrir(url, {
                clientId: `marina-${miId}`,
                will: { topic: `${base}/presence/${miId}`, payload: '0', qos: 1, retain: false }
            });
        } catch (e) {
            log('broker no disponible', url, e?.message ?? e);
            continue;
        }

        hayBroker = true;
        log('conectado con el broker', url, '- buscando la sala');

        if (!await consultarPresencia(c, `${base}/presence/host`, ESPERA_PRESENCIA_MS)) {
            c.end(true);
            continue;
        }

        cliente = c;
        esAnfitrion = false;
        c.on('message', (tema, payload) => {
            if (cliente !== c) { return; }
            const cuerpo = texto(payload);

            if (tema === `${base}/presence/host`) {
                // El anfitrion se ha caido (ultima voluntad) o ha cerrado la sala.
                if (cuerpo === '0') { perder(c, 'el anfitrión se ha ido'); }
            } else if (tema === `${base}/to/${miId}` && cuerpo.length <= MAX_LONGITUD) {
                entregar(cuerpo);
            }
        });
        c.on('close', () => perder(c, 'conexión con el broker cerrada'));
        c.subscribe(`${base}/to/${miId}`, { qos: 1 });
        log('invitado: dentro de la sala', codigo);
        return null;
    }

    return hayBroker
        ? 'No existe ninguna sala con ese código.'
        : 'No se pudo contactar con ningún servicio de mensajería (la red puede estar bloqueando WebSocket).';
}

// El invitado da por perdida la sala (una sola vez) y avisa a .NET.
function perder(c, motivo) {
    if (cliente !== c) { return; }
    log('sala perdida:', motivo);
    cliente = null;
    try { c.end(true); } catch { /* ya cerrado */ }
    dotnet.invokeMethodAsync('AlCierre').catch(e => log('error en AlCierre', e));
}

// ---------- Comun ----------

export function enviar(mensaje) {
    if (!cliente) { return; }

    if (esAnfitrion) {
        if (invitado) { cliente.publish(`${base}/to/${invitado}`, mensaje, { qos: 1 }); }
    } else {
        cliente.publish(`${base}/guest/${miId}`, mensaje, { qos: 1 });
    }
}

// Sale de la sala sin avisar a .NET; deja constancia de la salida para que el otro lado se entere al instante.
export async function cerrar() {
    const c = cliente;
    cliente = null;
    invitado = null;
    const eraAnfitrion = esAnfitrion;
    esAnfitrion = false;
    try { pantalla?.release(); } catch { /* ya liberada */ }
    pantalla = null;

    if (!c) { return; }

    await new Promise(listo => {
        try {
            const tema = eraAnfitrion ? `${base}/presence/host` : `${base}/presence/${miId}`;
            c.publish(tema, '0', { qos: 1, retain: eraAnfitrion }, () => listo());
            setTimeout(listo, 1000);
        } catch { listo(); }
    });
    try { c.end(true); } catch { /* ya cerrado */ }
}
