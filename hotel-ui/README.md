# hotel-ui

Librería de interfaz compartida por `clientes-front`, `habitaciones-front`, `reservas-front` y `auth-front`:
paleta del hotel, componentes reutilizables, manejo de sesión y validadores. Es un paquete local sin build propio:
cada front compila su código fuente con Vite.

## Instalación

En el `package.json` de cada front:

```json
"dependencies": { "hotel-ui": "file:../hotel-ui" }
```

En `vite.config.js` conviene mantener `resolve.dedupe: ['react', 'react-dom', 'react-router-dom']` y `server.fs.allow: ['..']`.
Luego `npm install` en el front. Los estilos se importan una vez en `main.jsx`:

```jsx
import 'hotel-ui/theme.css'
import { Button, Card } from 'hotel-ui'
```

## Componentes

### Button
Botón (o cualquier etiqueta con `as`).

| Prop | Tipo | Por defecto | Descripción |
|---|---|---|---|
| `variant` | `''`, `'ghost'`, `'danger'`, `'link'`, `'link danger'` | `''` | `link` se ve como enlace y no usa la clase `btn` |
| `size` | `''`, `'sm'` | `''` | Tamaño |
| `as` | etiqueta o componente | `'button'` | Ej. `as={Link} to="/x"` |
| resto | | | Se reenvía (`disabled`, `onClick`, `title`, `type`…) |

Sin `type`, un `Button` es de tipo `submit` (los botones de «Guardar», «Ingresar»… dependen de eso): usa `type="button"` cuando vaya dentro de un `<form>` y no deba enviarlo. `Modal`, `ConfirmModal`, `Pager` e `ImageManager` ya usan `type="button"` en todos sus botones.

```jsx
<Button variant="danger" size="sm" onClick={borrar}>Eliminar</Button>
<Button variant="link danger" onClick={desactivar}>Desactivar</Button>
```

### Card
Contenedor con sombra. Props: `as` (por defecto `'div'`; también `'button'` o `'form'`), `className`, resto reenviado.

```jsx
<Card as="form" className="login" onSubmit={enviar}>…</Card>
```

### Alert
Mensaje en línea. No renderiza nada si `children` está vacío. Props: `type` (`'error'` o `'success'`, por defecto `'error'`), `children`.

### Badge
Etiqueta de estado. Props: `tone` (`'ok'`, `'off'`, `'warn'`, `'danger'`, `'info'`; por defecto `'off'`), `children`.

### Spinner
Indicador de carga (`role="status"`). Sin props.

### Toolbar
Barra de filtros y acciones sobre una tabla. Props: `children`.

### DataTable
| Prop | Tipo | Descripción |
|---|---|---|
| `columns` | `[{ header, cell: (fila) => nodo, className? }]` | Definición de columnas |
| `rows` | `array` | Registros |
| `loading` | `boolean` | Muestra el spinner si no hay filas |
| `empty` | `string` | Texto sin resultados (por defecto «Sin resultados») |
| `rowKey` | `(fila) => clave` | Por defecto `fila.id` |
| `rowClass` | `(fila) => string` | Clase CSS por fila |

### Pager
Paginador. Props: `page`, `size`, `total`, `onPage(nuevaPagina)`, `noun` (por defecto «registros»).

### AppShell
Barra superior y contenedor `main`. Props: `section` (texto tras «Hotel ·»), `nav` (enlaces), `user` (texto), `onLogout`, `children`.

### RequirePerfil
Muestra `children` solo si `actual` está en `perfiles`; si no, un `Alert` de permiso. Props: `perfiles` (array), `actual` (string), `children`.

### Modal y ConfirmModal
`Modal`: `open`, `title`, `onClose`, `actions` (por defecto un botón «Cerrar»), `children`. Se cierra con Escape y al hacer clic fuera.
`ConfirmModal`: `open`, `title` (por defecto «Confirmar»), `message`, `confirmText`, `danger`, `onConfirm`, `onCancel`. Reemplaza a `window.confirm`.

### Formularios: Field, Input, Textarea, Select, Checkbox, FormGrid, FormActions
- `Field`: `label`, `full` (ocupa todo el ancho de `FormGrid`), `error` (mensaje bajo el campo), `children`.
- `Input` y `Textarea`: reenvían todas las props nativas.
- `Select`: `options` (`['A','B']` o `[{ value, label }]`), `placeholder` (opción vacía), resto reenviado.
- `Checkbox`: `label`, `full`, resto reenviado.
- `FormGrid`: rejilla de dos columnas. `FormActions`: fila de botones alineada a la derecha.

```jsx
<FormGrid>
  <Field label="Número" error={errs.numero}><Input value={f.numero} onChange={set('numero')} /></Field>
  <Field label="Tipo"><Select value={f.tipo} onChange={set('tipo')} options={['Sencilla', 'Doble']} /></Field>
</FormGrid>
<FormActions><Button>Guardar</Button></FormActions>
```

### Carousel
Carrusel de imágenes accesible, sin dependencias. Flechas, puntos, teclado ← →, deslizamiento táctil y vuelta al final. Una imagen que no carga se omite.

| Prop | Tipo | Por defecto | Descripción |
|---|---|---|---|
| `images` | `[{ id?, url, alt? }]` | `[]` | Imágenes en orden |
| `aspectRatio` | `string` | `'4 / 3'` | Proporción CSS del contenedor |
| `fallback` | `string` (URL) | | Imagen de relleno si no hay imágenes; sin ella se muestra «Sin imágenes» |
| `alt` | `string` | `'Imagen'` | Texto alternativo base («Habitación 101 1», «… 2»…) |
| `className` | `string` | | Clase adicional |

Con una sola imagen no se muestran flechas ni puntos.

```jsx
<Carousel images={h.imagenes} alt={`Habitación ${h.numero}`} fallback="/sin-foto.png" />
```

### ImageManager
Gestor de imágenes (subir, borrar, reordenar). No conoce la API: valida tipo y tamaño en el cliente y delega en los callbacks.

| Prop | Tipo | Por defecto | Descripción |
|---|---|---|---|
| `images` | `[{ id, url }]` | `[]` | Imágenes actuales, en orden |
| `onUpload` | `async (file) => void` | | Se llama con un archivo ya validado |
| `onDelete` | `async (id) => void` | | Borrar una imagen |
| `onReorder` | `async (ids) => void` | | Recibe la lista completa de ids en el nuevo orden |
| `max` | `number` | `6` | Máximo de imágenes (con el máximo, «Agregar imagen» se deshabilita) |
| `maxBytes` | `number` | `5242880` (5 MB) | Tamaño máximo por archivo |
| `accept` | `string[]` | JPG, PNG, WebP | Tipos MIME permitidos |
| `disabled` | `boolean` | `false` | Deshabilita todos los controles |

Si un callback lanza un error, su mensaje se muestra sobre las miniaturas. Mientras una acción está en curso los controles se deshabilitan. Sus botones son `type="button"`, así que pueden ir dentro de un `<form>`.

```jsx
<ImageManager images={imagenes} onUpload={subir} onDelete={borrar} onReorder={reordenar} />
```

### SIN_FOTO
Imagen de relleno (SVG incrustado) para habitaciones sin fotos. Se pasa como `fallback` de `Carousel`:
`<Carousel images={h.imagenes} fallback={SIN_FOTO} />`.

## Hooks
- `useDebouncedEffect(fn, deps, ms = 250)`: ejecuta `fn` tras una pausa cuando cambian `deps` (búsquedas).
- `useLoading(initial = false)`: devuelve `[loading, setLoading]`.

## Sesión: `createSession(loginUrl)`
Devuelve `{ initSession, getToken, logout, usuarioActual, tienePerfil }` para los módulos. El token llega desde `auth-front` en el fragmento `#token=…`.
- `initSession()`: guarda el token del fragmento, limpia la URL y devuelve `true` si hay sesión vigente.
- `getToken()`: el token, o `null` si falta, es inválido o venció.
- `usuarioActual()`: `{ nombre, perfil }` o `null`.
- `tienePerfil(...perfiles)`: `true` si el perfil actual está entre ellos.
- `logout()`: borra el token y redirige a `${loginUrl}/login`.

## Validadores
Cada validador recibe `(valor, todosLosValores)` y devuelve un mensaje o `null`.
`requerido(msg?)`, `patron(regex, msg)`, `maxLen(n)`, `minLen(n)`, `entre(min, max, msg?)`, `email`, `letras`, `telefono`, `documento(campoTipo?)`, `fechaNoFutura`, `edadMinimaSiTipo(tipo, años)`, `password`, `hoyLocal()`.

`validar(valores, esquema)` aplica varios validadores por campo y devuelve el primer error de cada uno:

```js
const errs = validar(f, { email: [requerido(), email], password: [requerido(), password] })
```

Utilidades de red para los `api.js`: `fetchSeguro(url, opciones)` (traduce el fallo de red a un mensaje claro) y `extraerError(res)` (mensaje del cuerpo o texto por estado).

## Pruebas

```bash
cd hotel-ui
npm install
npm test
```

Vitest con React Testing Library y jsdom. Las pruebas viven junto al código (`*.test.js`, `*.test.jsx`).
