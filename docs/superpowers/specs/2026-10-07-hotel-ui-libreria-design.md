# Librería de interfaz de usuario (`hotel-ui`) — Diseño

Fecha: 2026-10-07 · Rama: `feat/hotel-ui-libreria`

## Objetivo
Terminar de construir `hotel-ui` como librería de UI compartida por los 4 fronts: que todos la usen, que sea un
paquete con interfaz clara, que esté documentada y probada, y que incluya los componentes de imágenes que necesita
el carrusel de habitaciones (ver `2026-10-07-imagenes-habitaciones-design.md`).

## Estado actual (verificado en el código)
- `hotel-ui` tiene 13 componentes, sesión, hooks, validadores y `theme.css`, y la usan clientes, habitaciones y reservas.
- `auth-front` no la usa: 4 `<button>`, 9 `<input>` y 1 `<select>` crudos, 28 líneas de CSS propio y una copia de `validators.js`
  (la versión de la librería lo supera y además trae `fetchSeguro`, duplicado en `auth-front/src/api.js`).
- No es un paquete: no hay `package.json`; los fronts la resuelven con un alias de Vite a `../hotel-ui/src`.
- Sin pruebas. El README solo lista nombres de componentes, sin props.
- Quedan restos de CSS propio y elementos crudos en habitaciones (12 líneas) y reservas (5), y botones con aspecto de enlace sin variante en `Button`.

## Alcance acordado
1. Convertir `hotel-ui` en paquete.
2. Migrar `auth-front` a la librería (componentes y validadores).
3. Retirar estilos propios y elementos crudos restantes en habitaciones y reservas.
4. Agregar `Carousel` y `ImageManager`.
5. Documentar las props de cada componente y agregar pruebas.

Fuera de alcance: Storybook, publicar en un registro npm, TypeScript, rediseño visual.

## Diseño

### 1. Paquete
`hotel-ui/package.json`: `name: "hotel-ui"`, `private`, `type: "module"`, `exports` con `"."` → `./src/index.js` y `"./theme.css"` → `./src/theme.css`,
`peerDependencies` de `react`, `react-dom` y `react-router-dom` (versiones de los fronts). No hay paso de build propio:
cada front compila el código fuente con Vite.
Cada front declara `"hotel-ui": "file:../hotel-ui"` y se elimina el alias de `vite.config.js`; se conservan `resolve.dedupe`
(se agrega `react-router-dom`) y `server.fs.allow`. **Riesgo:** resolución de módulos con el paquete enlazado fuera del
proyecto; si `vite build` o `vite dev` fallan por eso, se mantiene el alias como respaldo y el `package.json` queda solo como contrato.
Nota para Docker (pieza 5): los Dockerfiles usarán la raíz del repo como contexto para poder copiar `hotel-ui`.

### 2. Migrar `auth-front`
`Login`, `Registro` y `Home` pasan a `Button`, `Field/Input/Select`, `Alert`, `Card` y `AppShell` donde corresponda.
Se elimina `auth-front/src/validators.js` y `fetchSeguro` local, y se importan de `hotel-ui`. `styles.css` queda solo con el layout propio de
las pantallas de acceso (fondo y centrado). El comportamiento no cambia: login, registro de huésped, redirección por perfil y envío del token a los módulos.

### 3. Restos en habitaciones y reservas
`Button` gana la variante `link` (aspecto de enlace, con `danger`). El selector rápido de estado usa `Select` con una clase de estado.
Se retira el CSS propio que quede cubierto por `theme.css`. Sin cambios de comportamiento.

### 4. Componentes nuevos
- `Carousel`: props `images` (`[{ id, url, alt? }]`), `aspectRatio` (por defecto 4/3), `fallback` (imagen genérica si no hay imágenes).
  Flechas, puntos indicadores, deslizamiento táctil, teclado ← →, `aria-label` en controles y `aria-live` discreto. Sin dependencias nuevas.
  Con una sola imagen oculta flechas y puntos.
- `ImageManager`: componente presentacional, props `images`, `onUpload(file)`, `onDelete(id)`, `onReorder(ids)`, `max`, `maxBytes`, `accept`, `disabled`.
  Valida tipo y tamaño en el cliente antes de llamar a `onUpload`, muestra los errores con `Alert`, miniaturas con borrar y mover izquierda/derecha.
  No conoce la API: cada front le pasa las funciones.

### 5. Documentación y pruebas
- `hotel-ui/README.md`: por componente, descripción, tabla de props y un ejemplo; sección de instalación (`file:`), tema y sesión.
- Pruebas con Vitest y React Testing Library (devDependencies de `hotel-ui`, script `npm test`): `validators`, `createSession`,
  `Carousel`, `ImageManager`, y pruebas básicas de `Button`, `Pager` y `DataTable`. No se busca cobertura total.

## Criterios de aceptación
- Los 4 fronts hacen `vite build` sin errores y arrancan con `npm run dev`.
- `auth-front` no tiene `<button>`, `<input>` ni `<select>` crudos ni `validators.js` propio.
- `npm test` en `hotel-ui` pasa.
- El README documenta todos los componentes exportados.
- Verificación en navegador: login, paso a clientes, habitaciones y reservas, y registro de huésped funcionan igual que antes.

## Riesgos
- Resolución del paquete enlazado en Vite (ver Paquete).
- Diferencias visuales menores al migrar `auth-front`; se revisan en navegador.
