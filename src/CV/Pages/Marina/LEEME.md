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
| `../../wwwroot/assets/marina/scrabble-red.js` | La red del Scrabble (PeerJS) |

## La vitrina

El boton de la pagina te lleva **de viaje** a la vitrina: un salto en el tiempo (rayos, anillos y destello; se salta pulsando
en cualquier sitio) que termina en una sala de museo. Al pulsar el cristal (o con Enter) las puertas giran y se enciende la luz.
Cada logro es un objeto sobre su peana; al elegirlo (clic o flechas izquierda/derecha) la ficha de abajo lo muestra en detalle.
Escape vuelve. Todo es CSS y C#, sin JavaScript propio.

## Animacion de cada logro

Al pulsar un logro desbloqueado se lanza una animacion a pantalla completa (~5 s) segun su `Efecto`; si esta bloqueado, el objeto se sacude.
Los efectos estan en `Logros/EfectosLogro.cs` (generacion) y `Logros/LogrosPanel.razor.css` (animaciones):
`Verduras` (cocina), `Palabras` (ingles), `Tiempo`, `Corazones`, `Estrellas`, `Notas`, `Flashes` y `Confeti` (por defecto).

## Como rellenar los logros

Edita `MarinaLogros.cs`: anade un `Logro` o sustituye uno de los secretos. Con `Conseguido: true` queda
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

- Uno pulsa **Crear sala** (se genera un codigo de 5 caracteres, client side) y se lo pasa al otro, que lo escribe en **Entrar**. Maximo dos por sala; una tercera persona recibe "sala llena".
- Los dos navegadores se conectan directamente por WebRTC con **PeerJS** (`wwwroot/assets/marina/scrabble-red.js`, carga `peerjs@1.5.4` desde jsDelivr). El servicio publico gratuito de PeerJS solo sirve para que se encuentren; el codigo de sala es el id del anfitrion. Sin TURN, alguna red muy restrictiva (algunas corporativas o 4G) puede no conectar.
- Quien crea la sala es el **anfitrion**: la partida vive en su navegador (`Scrabble/SalaPartida.cs`, la autoridad) y el invitado solo manda acciones y recibe su estado (el atril del rival nunca viaja). Si el anfitrion cierra la pagina, la sala se acaba; si se va el invitado, puede volver con el mismo nombre.
- `Scrabble/ScrabbleSesion.cs`: red y reparto de estado/chat. `Scrabble/Modelo/`: reglas (`Reglas.cs`) y mensajes (`Mensajes.cs`). `Scrabble/ScrabblePanel.*`: la interfaz.
- Reglas: 95 fichas (sin CH/LL/RR ni comodines), premios clasicos, primera palabra por el centro, bonus de 50 por usar las 7 fichas, cambiar fichas, pasar (4 pases seguidos acaban la partida) y fin al vaciar atril y bolsa. **No hay diccionario**: las palabras no se validan.
- Controles: toca una ficha del atril y luego una casilla; toca una ficha colocada para recogerla; **Jugar** confirma.

## Como eliminar la pagina

1. Borra la carpeta `src/CV/Pages/Marina/`.
2. Borra la carpeta `src/CV/wwwroot/assets/marina/`.

No hay que tocar nada mas: ninguna otra parte del proyecto la referencia.
