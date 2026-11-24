# Pagina oculta de Marina

Todo lo de esta pagina esta en **esta carpeta** y en `wwwroot/assets/marina/`. No hay ningun enlace a ella desde
el resto de la web: solo se llega escribiendo `/CV/Marina`.

## Que hay aqui

| Archivo | Para que sirve |
|---|---|
| `MarinaPage.razor` / `.cs` / `.css` | La pagina: cuenta atras, reloj de arena, reloj gigante de fondo, boton y aviso de logros |
| `MarinaConfig.cs` | Ruta, foto y fechas (el momento de la cita y el inicio de la cuenta) |
| `MarinaLogros.cs` | **La coleccion de logros: aqui se va rellenando** |
| `Logros/` | El panel de logros (`LogrosPanel`) y el modelo `Logro` |
| `BlankLayout.razor` | Layout sin cabecera ni menu (solo lo usa esta pagina) |
| `../../wwwroot/assets/marina/marina.webp` | La foto |

## Como rellenar los logros

Edita `MarinaLogros.cs`: anade un `Logro` o sustituye uno de los secretos. Con `Conseguido: true` queda
desbloqueado; con `Desbloqueo: <fecha>` se muestra la fecha y, si es futura, se desbloquea solo cuando llegue.

## Como eliminar la pagina

1. Borra la carpeta `src/CV/Pages/Marina/`.
2. Borra la carpeta `src/CV/wwwroot/assets/marina/`.

No hay que tocar nada mas: ninguna otra parte del proyecto la referencia.
