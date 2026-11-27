# Pagina oculta de Marina

Todo lo de esta pagina esta en **esta carpeta** y en `wwwroot/assets/marina/`. No hay ningun enlace a ella desde
el resto de la web: solo se llega escribiendo `/CV/Marina`.

## Que hay aqui

| Archivo | Para que sirve |
|---|---|
| `MarinaPage.razor` / `.cs` / `.css` | La pagina: cuenta atras, reloj de arena, reloj gigante de fondo, boton y aviso de logros |
| `MarinaConfig.cs` | Ruta, foto y fechas (el momento de la cita y el inicio de la cuenta) |
| `MarinaLogros.cs` | **La coleccion de logros: aqui se va rellenando** |
| `Logros/` | La vitrina de logros (`LogrosPanel`: mueble de madera con puertas de cristal que se abren en 3D, objetos sobre peanas y ficha de detalle) y el modelo `Logro` |
| `Scrabble/` | El Scrabble para dos con chat (ver abajo) |
| `BlankLayout.razor` | Layout sin cabecera ni menu (solo lo usa esta pagina) |
| `../../wwwroot/assets/marina/marina.webp` | La foto |
| `../../wwwroot/assets/marina/scrabble-mqtt.js` | La red del Scrabble (broker MQTT publico por WebSocket) |

## La vitrina

El boton de la pagina te lleva **de viaje** a la vitrina: un salto en el tiempo (rayos, anillos y destello; se salta pulsando
en cualquier sitio) que termina en una sala de museo. Al pulsar el cristal (o con Enter) las puertas giran y se enciende la luz.
Cada logro es un objeto sobre su peana; al elegirlo (clic o flechas izquierda/derecha) la ficha de abajo lo muestra en detalle.
Escape vuelve. Todo es CSS y C#, sin JavaScript propio.

## Animacion de cada logro

Al pulsar un logro desbloqueado se lanza una animacion a pantalla completa (~5 s) segun su `Efecto`.
Los efectos estan en `Logros/EfectosLogro.cs` (generacion) y `Logros/LogrosPanel.razor.css` (animaciones):
`Verduras` (cocina), `Palabras` (ingles), `Sueno`, `Tiempo`, `Corazones`, `Estrellas`, `Notas`, `Flashes`, `Titanes` (Attack on Titan), `JuegosDeMesa`, `Consola` (PowerShell y calaveras), `Elogio` (un cumplido), `Millon` (el millon de segundos) y `Confeti` (por defecto).
Los logros **bloqueados no tienen ninguna animacion**: al pulsarlos solo se ve su ficha; el efecto y el movimiento al pasar el raton son solo de los desbloqueados.

## Como rellenar los logros

Edita `MarinaLogros.cs`: anade un `Logro` a la lista. Con `Conseguido: true` queda
desbloqueado; con `Desbloqueo: <fecha>` se muestra la fecha y, si es futura, se desbloquea solo cuando llegue.
Con `Efecto: EfectoLogro.X` eliges la animacion al pulsarlo.

## Red flags (buscaminas)

El boton de la bandera roja abre un buscaminas: hay que marcar con bandera todas las minas (cada mina es una red flag enterrada) sin pisar
ninguna. Al conseguirlo llueven banderas y se abre el expediente, un panel como el de los logros con todas las red flags. Se recuerda que ya
esta resuelto (localStorage); el boton "Volver a jugar" lo repite.

- `MarinaRedFlags.cs`: **la coleccion de red flags**. El buscaminas tiene tantas minas como entradas haya aqui (y el campo crece con ellas).
- `RedFlags/`: `RedFlagsPanel` (juego y expediente), `Tablero` (logica del buscaminas), `RedFlag` (modelo) e iconos SVG.
- Controles: toque/clic descubre; mantener pulsado, clic derecho o el boton de modo ponen bandera.

## Scrabble (dos jugadores + chat)

El boton "Scrabble" abre un Scrabble en castellano para dos personas, con chat en tiempo real. **No hay servidor propio**: todo corre en el navegador (WebAssembly).

- Uno pulsa **Crear sala** (se genera un codigo de 5 caracteres, client side) y le pasa el **enlace de invitacion** (`/Marina?sala=ABC12`, con boton "Copiar enlace"). Quien lo abre ve el Scrabble con el codigo ya escrito: pone su nombre y pulsa **Entrar** (tambien se puede escribir el codigo a mano). Maximo dos por sala; una tercera persona recibe "sala llena".
- Los mensajes pasan por un **broker MQTT publico y gratuito** por WebSocket seguro (`wwwroot/assets/marina/scrabble-mqtt.js`, carga `mqtt@5.10.1` desde jsDelivr y prueba en orden HiveMQ, EMQX y Mosquitto). Se eligio en vez de WebRTC (que se probo antes con PeerJS) porque la conexion directa fallaba entre redes restrictivas (movil con datos + wifi de empresa): con el broker cada navegador solo necesita salir por HTTPS/WebSocket. A cambio **no es privado** (quien escuche el broker puede ver las salas y lo que se envia, incluido el chat) y no tiene garantias de servicio. Temas: `marina-scrabble-v1/<sala>/...`; el "ultimo deseo" (will) de MQTT avisa al otro lado de una caida en unos 15-20 s. Para ver en que paso se queda una conexion, abre la consola (F12): se escribe cada paso con el prefijo `[scrabble-mqtt]`.
- Quien crea la sala es el **anfitrion**: la partida vive en su navegador y es la autoridad; el invitado solo manda acciones y recibe su estado (el atril del rival nunca viaja). Si el anfitrion cierra la pagina, la sala se acaba; si se va el invitado, puede volver con el mismo nombre.
- Capas (cada una solo conoce la de debajo): `ScrabblePanel.*` (interfaz) → `ScrabbleSesion.cs` (anfitrion/invitado, validacion y reparto) → `Red/IGameTransport.cs` (canal; `Red/JsGameTransport.cs` lo implementa llamando a un modulo JavaScript con `crear`, `unirse`, `enviar` y `cerrar`: para cambiar de tecnologia basta otro modulo asi). Las reglas estan en `MotorScrabble.cs` (sin red ni chat) y `SalaPartida.cs` (plazas, conexiones y chat); datos fijos en `Modelo/`.
- Protocolo (`Red/GameMessage.cs`): sobre comun `{type, roomId, messageId, turn, payload, token}` con los tipos `JOIN_REQUEST`, `JOIN_ACCEPTED`, `JOIN_REJECTED`, `GAME_INITIAL_STATE`, `GAME_STATE_UPDATED`, `PLAYER_MOVE`, `MOVE_REJECTED`, `CHAT_SEND` y `CHAT_MESSAGE`. El anfitrion entrega un **token de sesion** al aceptar al invitado y descarta lo que no lo lleve, los `messageId` repetidos, los movimientos con `turn` desactualizado y los mensajes de mas de 32 KB. El anfitrion tambien pide que la pantalla del movil siga encendida y, si pierde la conexion con el broker, reconecta solo y reabre la sala. Si el invitado se va, su sitio se libera en unos segundos y puede volver con el mismo nombre. Pendiente de la especificacion: `PING` y resincronizacion completa tras una reconexion.
- Reglas: 95 fichas (sin CH/LL/RR ni comodines), premios clasicos, primera palabra por el centro, bonus de 50 por usar las 7 fichas, cambiar fichas, pasar (4 pases seguidos acaban la partida) y fin al vaciar atril y bolsa. **No hay diccionario**: las palabras no se validan.
- Controles: toca una ficha del atril y luego una casilla; toca una ficha colocada para recogerla; **Jugar** confirma.

## Como eliminar la pagina

1. Borra la carpeta `src/CV/Pages/Marina/`.
2. Borra la carpeta `src/CV/wwwroot/assets/marina/`.

No hay que tocar nada mas: ninguna otra parte del proyecto la referencia.
