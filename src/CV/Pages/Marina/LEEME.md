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
| `BlankLayout.razor` | Layout sin cabecera ni menu (solo lo usa esta pagina) |
| `../../wwwroot/assets/marina/marina.webp` | La foto |

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

## Como eliminar la pagina

1. Borra la carpeta `src/CV/Pages/Marina/`.
2. Borra la carpeta `src/CV/wwwroot/assets/marina/`.

No hay que tocar nada mas: ninguna otra parte del proyecto la referencia.
