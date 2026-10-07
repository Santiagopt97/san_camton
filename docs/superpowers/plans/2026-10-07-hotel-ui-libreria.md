# Librería de UI `hotel-ui` Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Convertir `hotel-ui` en un paquete documentado y probado que usen los 4 fronts, e incluir `Carousel` e `ImageManager` para las imágenes de habitaciones.

**Architecture:** `hotel-ui` pasa a ser un paquete npm local (`package.json` con `exports`) que cada front consume como `"hotel-ui": "file:../hotel-ui"`, sin build propio (Vite compila el código fuente). Se prueba con Vitest + React Testing Library dentro del propio paquete. `auth-front` migra a los componentes y validadores de la librería, y los componentes nuevos se desarrollan con TDD.

**Tech Stack:** React 18.3, Vite 5.4, Vitest 2, React Testing Library 16, jsdom, CSS plano (`theme.css`).

**Spec:** `docs/superpowers/specs/2026-10-07-hotel-ui-libreria-design.md` (los componentes de imágenes también responden a `docs/superpowers/specs/2026-10-07-imagenes-habitaciones-design.md`).

## Global Constraints

- Rama de trabajo: `feat/hotel-ui-libreria`. No se hace `push`; el usuario lo hará.
- Commits con el correo `santirramos@gmail.com` (ya configurado en el repo) y **sin** línea `Co-Authored-By` ni menciones a Claude (preferencia del usuario).
- Nunca commitear secretos ni archivos `.env`. No tocar ningún `.env`.
- `hotel-ui/package.json`: `name: "hotel-ui"`, `private: true`, `type: "module"`, `exports` con `"."` → `./src/index.js` y `"./theme.css"` → `./src/theme.css`, `peerDependencies` de `react` y `react-dom`.
- Cada front declara `"hotel-ui": "file:../hotel-ui"` y no usa alias de Vite; se conservan `resolve.dedupe` (con `react`, `react-dom`, `react-router-dom`) y `server.fs.allow: ['..']`. Respaldo si falla la resolución: volver al alias (ver Task 2).
- Sin dependencias nuevas de producción (el carrusel no usa librerías).
- Todos los textos de interfaz y mensajes de error en español.
- Sin cambios de comportamiento en `auth-front` (login, registro de huésped, redirección por perfil, envío del token a los módulos).
- Puertos de los fronts: auth 5173, habitaciones 5174, clientes 5175, reservas 5176.

## Review Focus

1. `Carousel` cuando la lista se acorta (se borra la imagen que se estaba viendo): debe mostrar una imagen válida, no quedar en blanco ni lanzar error. (Task 7)
2. `Carousel` con una imagen cuya URL no carga: se omite; si ninguna carga, muestra el relleno. (Task 7)
3. `ImageManager` con un archivo de `type` vacío (algunos navegadores con `.webp`) o de exactamente `maxBytes`: el vacío se rechaza con mensaje claro y el de tamaño exacto se permite. (Task 8)
4. `ImageManager` cuando `onUpload` rechaza (el API responde 400 o 409): el mensaje se muestra y los controles vuelven a habilitarse para reintentar. (Task 8)
5. Botones de la librería dentro de un `<form>` (el gestor irá dentro del formulario de habitación): no deben enviar el formulario. (Task 8)

---

## File Structure

| Archivo | Responsabilidad |
|---|---|
| `hotel-ui/package.json` (nuevo) | Contrato del paquete: exports, peers, scripts de prueba |
| `hotel-ui/vitest.config.js`, `hotel-ui/src/test/setup.js` (nuevos) | Entorno de pruebas (jsdom, jest-dom) |
| `hotel-ui/src/*.test.js(x)`, `hotel-ui/src/components/*.test.jsx` (nuevos) | Pruebas junto al código que cubren |
| `hotel-ui/src/components/Button.jsx`, `Card.jsx`, `AppShell.jsx` | Variante `link`, prop `as` en `Card`, `Salir` con `Button` |
| `hotel-ui/src/components/Carousel.jsx` (nuevo) | Carrusel de imágenes |
| `hotel-ui/src/components/ImageManager.jsx` (nuevo) | Gestor presentacional de imágenes (subir, borrar, reordenar) |
| `hotel-ui/src/index.js`, `hotel-ui/src/theme.css` | Exports y estilos de los componentes |
| `hotel-ui/README.md` | Documentación de componentes |
| `*-front/package.json`, `*-front/vite.config.js` | Consumo del paquete por `file:` |
| `clientes-front`, `habitaciones-front`, `reservas-front` (páginas y `styles.css`) | Retiro de elementos crudos y CSS duplicado |
| `auth-front/src/{main.jsx,api.js,styles.css,pages/*}` | Migración a la librería; se elimina `validators.js` |

---

### Task 1: Paquete `hotel-ui` con herramientas de prueba

**Files:**
- Create: `hotel-ui/package.json`
- Create: `hotel-ui/vitest.config.js`
- Create: `hotel-ui/src/test/setup.js`
- Create: `hotel-ui/src/index.test.js`

**Interfaces:**
- Produces: script `npm test` en `hotel-ui` (Vitest, entorno jsdom, `globals: true`). Todas las tareas siguientes crean pruebas con extensión `.test.js` o `.test.jsx` dentro de `hotel-ui/src`.

- [ ] **Step 1: Crear `hotel-ui/package.json`**

```json
{
  "name": "hotel-ui",
  "version": "1.0.0",
  "private": true,
  "type": "module",
  "description": "Librería de interfaz compartida por los fronts del sistema Hotel",
  "main": "./src/index.js",
  "exports": {
    ".": "./src/index.js",
    "./theme.css": "./src/theme.css",
    "./package.json": "./package.json"
  },
  "files": ["src"],
  "scripts": { "test": "vitest run", "test:watch": "vitest" },
  "peerDependencies": { "react": "^18.3.1", "react-dom": "^18.3.1" },
  "devDependencies": {
    "@testing-library/dom": "^10.4.0",
    "@testing-library/jest-dom": "^6.5.0",
    "@testing-library/react": "^16.0.1",
    "@vitejs/plugin-react": "^4.3.1",
    "jsdom": "^25.0.1",
    "react": "^18.3.1",
    "react-dom": "^18.3.1",
    "vite": "^5.4.0",
    "vitest": "^2.1.0"
  }
}
```

- [ ] **Step 2: Crear `hotel-ui/vitest.config.js`**

```js
import { defineConfig } from 'vitest/config'
import react from '@vitejs/plugin-react'

export default defineConfig({
  plugins: [react()],
  test: { environment: 'jsdom', globals: true, setupFiles: ['./src/test/setup.js'], css: false },
})
```

- [ ] **Step 3: Crear `hotel-ui/src/test/setup.js`**

```js
import '@testing-library/jest-dom/vitest'
import { afterEach } from 'vitest'
import { cleanup } from '@testing-library/react'

afterEach(() => {
  cleanup()
  sessionStorage.clear()
  window.location.hash = ''
})
```

- [ ] **Step 4: Crear la prueba de humo `hotel-ui/src/index.test.js`**

```js
import * as ui from './index.js'

const PUBLICOS = [
  'Button', 'Card', 'Alert', 'Badge', 'Spinner', 'Toolbar', 'DataTable', 'Pager', 'AppShell',
  'RequirePerfil', 'Modal', 'ConfirmModal', 'Field', 'Input', 'Textarea', 'Select', 'Checkbox',
  'FormGrid', 'FormActions', 'createSession', 'useDebouncedEffect', 'useLoading', 'validar', 'fetchSeguro',
]

test('el paquete exporta su API pública', () => {
  for (const nombre of PUBLICOS) expect(ui[nombre], nombre).toBeDefined()
})
```

- [ ] **Step 5: Instalar y ejecutar**

Run: `cd hotel-ui && npm install && npm test`
Expected: `npm install` termina sin errores y Vitest muestra `1 passed`.

- [ ] **Step 6: Commit**

```bash
git add hotel-ui/package.json hotel-ui/package-lock.json hotel-ui/vitest.config.js hotel-ui/src/test/setup.js hotel-ui/src/index.test.js
git commit -m "build(hotel-ui): convertir la librería en paquete con entorno de pruebas"
```

---

### Task 2: Conectar los 4 fronts al paquete

**Files:**
- Modify: `auth-front/package.json`, `clientes-front/package.json`, `habitaciones-front/package.json`, `reservas-front/package.json`
- Modify: `auth-front/vite.config.js`, `clientes-front/vite.config.js`, `habitaciones-front/vite.config.js`, `reservas-front/vite.config.js`

**Interfaces:**
- Consumes: paquete `hotel-ui` de la Task 1 (`exports` `"."` y `"./theme.css"`).
- Produces: en cada front, `import ... from 'hotel-ui'` e `import 'hotel-ui/theme.css'` se resuelven por el paquete enlazado, sin alias.

- [ ] **Step 1: Agregar la dependencia en los 4 `package.json`**

En `dependencies` de cada uno, dejar (conservando las demás):

```json
"dependencies": { "hotel-ui": "file:../hotel-ui", "react": "^18.3.1", "react-dom": "^18.3.1", "react-router-dom": "^6.26.0" },
```

- [ ] **Step 2: Reemplazar `vite.config.js` en cada front**

`clientes-front/vite.config.js` (puerto 5175):

```js
import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

export default defineConfig({
  plugins: [react()],
  resolve: { dedupe: ['react', 'react-dom', 'react-router-dom'] },
  server: { port: 5175, fs: { allow: ['..'] } },
})
```

Mismo contenido, solo cambia el puerto, en `habitaciones-front/vite.config.js` (5174), `reservas-front/vite.config.js` (5176) y `auth-front/vite.config.js` (5173).

- [ ] **Step 3: Instalar y compilar cada front**

Run:
```bash
for d in auth-front clientes-front habitaciones-front reservas-front; do (cd $d && npm install 2>&1 | tail -1 && npx vite build 2>&1 | tail -3); done
```
Expected: cada front termina con `✓ built in ...` y sin `Failed to resolve import "hotel-ui"`.

- [ ] **Step 4 (solo si el Step 3 falla por la resolución de `hotel-ui`): respaldo con alias**

En el `vite.config.js` del front que falle, volver a la configuración original:

```js
import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'
import path from 'node:path'

const ui = path.resolve(__dirname, '../hotel-ui/src')
export default defineConfig({
  plugins: [react()],
  resolve: { alias: { 'hotel-ui': ui }, dedupe: ['react', 'react-dom'] },
  server: { port: 5175, fs: { allow: ['..'] } },
})
```

(con el puerto de ese front). Dejar el `package.json` de `hotel-ui` como contrato y anotar en el commit que el alias se mantuvo. Repetir el Step 3.

- [ ] **Step 5: Commit**

```bash
git add auth-front/package.json auth-front/package-lock.json auth-front/vite.config.js clientes-front/package.json clientes-front/package-lock.json clientes-front/vite.config.js habitaciones-front/package.json habitaciones-front/package-lock.json habitaciones-front/vite.config.js reservas-front/package.json reservas-front/package-lock.json reservas-front/vite.config.js
git commit -m "build(fronts): consumir hotel-ui como paquete local en lugar de alias de Vite"
```

---

### Task 3: Pruebas de validadores y sesión

**Files:**
- Create: `hotel-ui/src/validators.test.js`
- Create: `hotel-ui/src/session.test.js`

**Interfaces:**
- Consumes: `hotel-ui/src/validators.js` (`requerido`, `email`, `letras`, `telefono`, `documento`, `password`, `maxLen`, `entre`, `fechaNoFutura`, `hoyLocal`, `validar`, `fetchSeguro`, `extraerError`) y `hotel-ui/src/session.js` (`createSession(loginUrl)` → `{ initSession, getToken, logout, usuarioActual, tienePerfil }`).
- Produces: pruebas de caracterización: describen el comportamiento actual y deben pasar sin cambiar el código.

- [ ] **Step 1: Escribir `hotel-ui/src/validators.test.js`**

```js
import {
  requerido, email, letras, telefono, documento, password, maxLen, entre,
  fechaNoFutura, hoyLocal, validar, fetchSeguro, extraerError,
} from './validators.js'

describe('validadores simples', () => {
  test('requerido rechaza vacío y espacios', () => {
    expect(requerido()('')).toBe('Este campo es obligatorio')
    expect(requerido()('   ')).toBe('Este campo es obligatorio')
    expect(requerido()('a')).toBeNull()
  })
  test('requerido acepta mensaje propio', () => {
    expect(requerido('Falta')('')).toBe('Falta')
  })
  test('email', () => {
    expect(email('ana@hotel.com')).toBeNull()
    expect(email('ana@hotel')).toBe('Correo no válido')
    expect(email('')).toBeNull()
  })
  test('letras permite tildes y rechaza números', () => {
    expect(letras('María José')).toBeNull()
    expect(letras('Ana2')).toBe('Solo se permiten letras')
  })
  test('telefono', () => {
    expect(telefono('+57 300 123 4567')).toBeNull()
    expect(telefono('123')).toBe('Teléfono no válido (7 a 20 dígitos)')
  })
  test('maxLen y entre', () => {
    expect(maxLen(3)('abcd')).toBe('Máximo 3 caracteres')
    expect(maxLen(3)('abc')).toBeNull()
    expect(entre(1, 10)('11')).toBe('Debe estar entre 1 y 10')
    expect(entre(1, 10)('5')).toBeNull()
  })
})

describe('documento según tipo', () => {
  test('CC exige 5 a 12 dígitos', () => {
    expect(documento()('12345', { tipoDocumento: 'CC' })).toBeNull()
    expect(documento()('1234', { tipoDocumento: 'CC' })).toBe('Debe tener entre 5 y 12 dígitos')
  })
  test('pasaporte admite letras y números', () => {
    expect(documento()('AB12345', { tipoDocumento: 'PA' })).toBeNull()
    expect(documento()('AB-1', { tipoDocumento: 'PA' })).toBe('Pasaporte: 5 a 20 letras o números')
  })
})

describe('password', () => {
  test('exige mayúscula, minúscula, número y 8 caracteres', () => {
    expect(password('Abcdef12')).toBeNull()
    expect(password('abcdef12')).toBe('Debe incluir mayúscula, minúscula y número')
    expect(password('Ab1')).toBe('Debe tener entre 8 y 72 caracteres')
  })
})

describe('fechas', () => {
  test('fechaNoFutura', () => {
    expect(fechaNoFutura(hoyLocal())).toBeNull()
    expect(fechaNoFutura('2999-01-01')).toBe('No puede ser una fecha futura')
  })
})

describe('validar', () => {
  test('devuelve solo el primer error de cada campo', () => {
    const errores = validar({ nombre: '', correo: 'x' }, {
      nombre: [requerido(), maxLen(5)],
      correo: [requerido(), email],
    })
    expect(errores).toEqual({ nombre: 'Este campo es obligatorio', correo: 'Correo no válido' })
  })
  test('devuelve objeto vacío cuando todo es válido', () => {
    expect(validar({ nombre: 'Ana' }, { nombre: [requerido()] })).toEqual({})
  })
})

describe('utilidades de red', () => {
  afterEach(() => vi.restoreAllMocks())

  test('fetchSeguro traduce el fallo de red a un mensaje claro', async () => {
    vi.stubGlobal('fetch', vi.fn().mockRejectedValue(new TypeError('fail')))
    await expect(fetchSeguro('/x')).rejects.toThrow('No se pudo conectar con el servidor')
    vi.unstubAllGlobals()
  })
  test('extraerError usa el mensaje del cuerpo', async () => {
    const res = { status: 409, json: async () => ({ message: 'Ya existe' }) }
    expect(await extraerError(res)).toBe('Ya existe')
  })
  test('extraerError usa el texto por estado si no hay cuerpo', async () => {
    const res = { status: 403, json: async () => { throw new Error('sin cuerpo') } }
    expect(await extraerError(res)).toBe('No tienes permiso para esta acción.')
  })
})
```

- [ ] **Step 2: Escribir `hotel-ui/src/session.test.js`**

```js
import { createSession } from './session.js'

const b64url = (obj) => btoa(JSON.stringify(obj)).replace(/=/g, '').replace(/\+/g, '-').replace(/\//g, '_')
const jwt = (payload) => `${b64url({ alg: 'HS256', typ: 'JWT' })}.${b64url(payload)}.firma`
const enUnaHora = () => Math.floor(Date.now() / 1000) + 3600
const haceUnaHora = () => Math.floor(Date.now() / 1000) - 3600

describe('createSession', () => {
  let sesion
  beforeEach(() => { sesion = createSession('http://localhost:5173') })

  test('initSession guarda el token del fragmento y limpia la URL', () => {
    const token = jwt({ name: 'Ana', role: 'admin', exp: enUnaHora() })
    window.location.hash = `#token=${encodeURIComponent(token)}`
    expect(sesion.initSession()).toBe(true)
    expect(sesion.getToken()).toBe(token)
    expect(window.location.hash).toBe('')
  })

  test('sin token no hay sesión', () => {
    expect(sesion.initSession()).toBe(false)
    expect(sesion.getToken()).toBeNull()
    expect(sesion.usuarioActual()).toBeNull()
  })

  test('un token vencido no cuenta como sesión', () => {
    sessionStorage.setItem('token', jwt({ name: 'Ana', role: 'admin', exp: haceUnaHora() }))
    expect(sesion.initSession()).toBe(false)
    expect(sesion.getToken()).toBeNull()
  })

  test('usuarioActual y tienePerfil leen el perfil del token', () => {
    sessionStorage.setItem('token', jwt({ name: 'Ana', role: 'recepcion', exp: enUnaHora() }))
    expect(sesion.usuarioActual()).toEqual({ nombre: 'Ana', perfil: 'recepcion' })
    expect(sesion.tienePerfil('admin', 'recepcion')).toBe(true)
    expect(sesion.tienePerfil('admin')).toBe(false)
  })

  test('un token con formato inválido se trata como sin sesión', () => {
    sessionStorage.setItem('token', 'basura')
    expect(sesion.getToken()).toBeNull()
    expect(sesion.usuarioActual()).toBeNull()
  })

  test('logout borra el token', () => {
    // jsdom no implementa la navegación y lo avisa por consola; se silencia solo aquí
    vi.spyOn(console, 'error').mockImplementation(() => {})
    sessionStorage.setItem('token', jwt({ name: 'Ana', role: 'admin', exp: enUnaHora() }))
    sesion.logout()
    expect(sessionStorage.getItem('token')).toBeNull()
    vi.restoreAllMocks()
  })
})
```

- [ ] **Step 3: Ejecutar**

Run: `cd hotel-ui && npm test`
Expected: todas las pruebas pasan (`validators.test.js`, `session.test.js` e `index.test.js`). Si alguna falla porque el mensaje real del código difiere del esperado, corregir **la expectativa de la prueba** con el texto real que muestra el error (son pruebas de caracterización) y repetir.

- [ ] **Step 4: Commit**

```bash
git add hotel-ui/src/validators.test.js hotel-ui/src/session.test.js
git commit -m "test(hotel-ui): pruebas de validadores y manejo de sesión"
```

---

### Task 4: `Button` con variante `link`, `Card` con `as`, `AppShell` y pruebas de componentes base

**Files:**
- Modify: `hotel-ui/src/components/Button.jsx`
- Modify: `hotel-ui/src/components/Card.jsx`
- Modify: `hotel-ui/src/components/AppShell.jsx`
- Create: `hotel-ui/src/components/Button.test.jsx`
- Create: `hotel-ui/src/components/base.test.jsx`

**Interfaces:**
- Produces: `<Button variant="link|link danger|ghost|danger" size="sm" as={Tag} />` (con `variant` que contenga `link` no se añade la clase `btn`); `<Card as="button|form|div" className />`. Task 5 y Task 6 los usan.

- [ ] **Step 1: Escribir las pruebas que fallan, `hotel-ui/src/components/Button.test.jsx`**

```jsx
import { render, screen } from '@testing-library/react'
import Button from './Button.jsx'
import Card from './Card.jsx'

describe('Button', () => {
  test('por defecto es un botón con clase btn', () => {
    render(<Button>Guardar</Button>)
    const b = screen.getByRole('button', { name: 'Guardar' })
    expect(b).toHaveClass('btn')
  })
  test('variantes ghost, danger y tamaño', () => {
    render(<Button variant="ghost" size="sm">X</Button>)
    expect(screen.getByRole('button')).toHaveClass('btn', 'ghost', 'sm')
  })
  test('la variante link no usa el aspecto de botón', () => {
    render(<Button variant="link danger">Borrar</Button>)
    const b = screen.getByRole('button', { name: 'Borrar' })
    expect(b).toHaveClass('link', 'danger')
    expect(b).not.toHaveClass('btn')
  })
  test('reenvía props como disabled y title', () => {
    render(<Button disabled title="Ayuda">X</Button>)
    expect(screen.getByRole('button')).toBeDisabled()
    expect(screen.getByTitle('Ayuda')).toBeInTheDocument()
  })
  test('permite renderizar otra etiqueta con as', () => {
    render(<Button as="a" href="/x">Ir</Button>)
    expect(screen.getByRole('link', { name: 'Ir' })).toHaveClass('btn')
  })
})

describe('Card', () => {
  test('por defecto es un div con clase card', () => {
    const { container } = render(<Card>Hola</Card>)
    expect(container.firstChild.tagName).toBe('DIV')
    expect(container.firstChild).toHaveClass('card')
  })
  test('con as="button" renderiza un botón con clase card', () => {
    render(<Card as="button" className="stat">Tile</Card>)
    expect(screen.getByRole('button', { name: 'Tile' })).toHaveClass('card', 'stat')
  })
})
```

- [ ] **Step 2: Ejecutar y comprobar que falla**

Run: `cd hotel-ui && npx vitest run src/components/Button.test.jsx`
Expected: FAIL en `la variante link no usa el aspecto de botón` (hoy siempre añade `btn`) y en `Card con as="button"` (hoy `Card` ignora `as`).

- [ ] **Step 3: Implementar `Button.jsx`**

```jsx
// <Button variant="ghost|danger|link|link danger" size="sm" as={Link} to="/x">Texto</Button>
// La variante `link` se ve como enlace y no lleva la clase `btn`.
export default function Button({ as: Tag = 'button', variant = '', size = '', className = '', ...rest }) {
  const base = variant.split(' ').includes('link') ? '' : 'btn'
  const clases = `${base} ${variant} ${size} ${className}`.replace(/\s+/g, ' ').trim()
  return <Tag className={clases} {...rest} />
}
```

- [ ] **Step 4: Implementar `Card.jsx`**

```jsx
// <Card as="button" className="stat">…</Card>  (por defecto es un <div>)
export default function Card({ as: Tag = 'div', className = '', ...rest }) {
  return <Tag className={`card ${className}`.trim()} {...rest} />
}
```

- [ ] **Step 5: Ejecutar y comprobar que pasa**

Run: `cd hotel-ui && npx vitest run src/components/Button.test.jsx`
Expected: PASS (7 pruebas).

- [ ] **Step 6: Usar `Button` en `AppShell.jsx`**

```jsx
import Button from './Button.jsx'

// Barra superior + contenedor. `nav` recibe los NavLink del front.
export default function AppShell({ section, nav, user, onLogout, children }) {
  return (
    <>
      <header className="topbar">
        <h1>Hotel <span>· {section}</span></h1>
        <nav>{nav}</nav>
        <div className="user">{user} <Button variant="link" onClick={onLogout}>Salir</Button></div>
      </header>
      <main>{children}</main>
    </>
  )
}
```

- [ ] **Step 7: Pruebas de `AppShell`, `Pager` y `DataTable`, `hotel-ui/src/components/base.test.jsx`**

```jsx
import { fireEvent, render, screen } from '@testing-library/react'
import AppShell from './AppShell.jsx'
import Pager from './Pager.jsx'
import DataTable from './DataTable.jsx'

describe('AppShell', () => {
  test('muestra sección, usuario y llama a onLogout', () => {
    const onLogout = vi.fn()
    render(<AppShell section="Clientes" user="Ana" onLogout={onLogout}><p>Contenido</p></AppShell>)
    expect(screen.getByText('· Clientes')).toBeInTheDocument()
    expect(screen.getByText('Contenido')).toBeInTheDocument()
    fireEvent.click(screen.getByRole('button', { name: 'Salir' }))
    expect(onLogout).toHaveBeenCalledTimes(1)
  })
})

describe('Pager', () => {
  test('en la primera página no se puede retroceder', () => {
    render(<Pager page={1} size={10} total={25} onPage={() => {}} />)
    expect(screen.getByRole('button', { name: 'Anterior' })).toBeDisabled()
    expect(screen.getByText('Página 1 de 3 · 25 registros')).toBeInTheDocument()
  })
  test('en la última página no se puede avanzar y navega con onPage', () => {
    const onPage = vi.fn()
    render(<Pager page={3} size={10} total={25} onPage={onPage} />)
    expect(screen.getByRole('button', { name: 'Siguiente' })).toBeDisabled()
    fireEvent.click(screen.getByRole('button', { name: 'Anterior' }))
    expect(onPage).toHaveBeenCalledWith(2)
  })
  test('sin registros muestra una sola página', () => {
    render(<Pager page={1} size={10} total={0} onPage={() => {}} />)
    expect(screen.getByText('Página 1 de 1 · 0 registros')).toBeInTheDocument()
  })
})

describe('DataTable', () => {
  const columns = [{ header: 'Nombre', cell: (r) => r.nombre }]
  test('pinta una fila por registro', () => {
    render(<DataTable columns={columns} rows={[{ id: 1, nombre: 'Ana' }, { id: 2, nombre: 'Luis' }]} />)
    expect(screen.getByText('Ana')).toBeInTheDocument()
    expect(screen.getByText('Luis')).toBeInTheDocument()
  })
  test('sin filas muestra el mensaje vacío', () => {
    render(<DataTable columns={columns} rows={[]} empty="Nada por aquí" />)
    expect(screen.getByText('Nada por aquí')).toBeInTheDocument()
  })
  test('mientras carga y sin filas muestra el spinner', () => {
    render(<DataTable columns={columns} rows={[]} loading />)
    expect(screen.getByRole('status', { name: 'Cargando' })).toBeInTheDocument()
    expect(screen.queryByText('Sin resultados')).not.toBeInTheDocument()
  })
})
```

- [ ] **Step 8: Ejecutar toda la suite**

Run: `cd hotel-ui && npm test`
Expected: todas las pruebas pasan.

- [ ] **Step 9: Commit**

```bash
git add hotel-ui/src/components/Button.jsx hotel-ui/src/components/Card.jsx hotel-ui/src/components/AppShell.jsx hotel-ui/src/components/Button.test.jsx hotel-ui/src/components/base.test.jsx
git commit -m "feat(hotel-ui): variante link en Button, prop as en Card y pruebas de componentes base"
```

---

### Task 5: Retirar elementos crudos y CSS duplicado en clientes, habitaciones y reservas

**Files:**
- Modify: `clientes-front/src/pages/ClientesList.jsx:37`
- Modify: `habitaciones-front/src/pages/HabitacionesList.jsx:50-52,56,64-67`
- Modify: `reservas-front/src/pages/ReservasList.jsx:33-35`
- Modify: `reservas-front/src/pages/MisReservas.jsx:32`
- Modify: `hotel-ui/src/theme.css` (agregar `select.estado`)
- Modify: `habitaciones-front/src/styles.css`, `reservas-front/src/styles.css` (quitar `select.estado` base)

**Interfaces:**
- Consumes: `Button variant="link danger"`, `Card as="button"` (Task 4); `Select` de `hotel-ui` (ya existe, reenvía `className`, `value`, `onChange`, `disabled`).

- [ ] **Step 1: `ClientesList.jsx`, línea 37**

Reemplazar:

```jsx
      {c.activo && <button className="link danger" onClick={() => setADesactivar(c)}>Desactivar</button>}
```

por:

```jsx
      {c.activo && <Button variant="link danger" onClick={() => setADesactivar(c)}>Desactivar</Button>}
```

(`Button` ya está importado en ese archivo).

- [ ] **Step 2: `MisReservas.jsx`, líneas 31-32**

Reemplazar:

```jsx
    { header: '', className: 'actions', cell: (r) => r.estado === 'Confirmada'
      && <button className="link danger" onClick={() => setACancelar(r)}>Cancelar</button> },
```

por:

```jsx
    { header: '', className: 'actions', cell: (r) => r.estado === 'Confirmada'
      && <Button variant="link danger" onClick={() => setACancelar(r)}>Cancelar</Button> },
```

(`Button` ya está importado).

- [ ] **Step 3: `ReservasList.jsx`, selector de estado (líneas 33-35)**

Reemplazar:

```jsx
      <select className="estado" value={r.estado} onChange={(e) => cambiar(r, e.target.value)}>
        {ESTADOS.map((s) => <option key={s}>{s}</option>)}
      </select>
```

por:

```jsx
      <Select className="estado" value={r.estado} onChange={(e) => cambiar(r, e.target.value)} options={ESTADOS} />
```

(`Select` ya está importado).

- [ ] **Step 4: `HabitacionesList.jsx`, selector de estado (líneas 50-52)**

Reemplazar:

```jsx
      <select className={`estado ${h.estado}`} value={h.estado} disabled={!h.activo} onChange={(e) => cambiarEstado(h, e.target.value)}>
        {ESTADOS.map((s) => <option key={s}>{s}</option>)}
      </select>
```

por:

```jsx
      <Select className={`estado ${h.estado}`} value={h.estado} disabled={!h.activo} onChange={(e) => cambiarEstado(h, e.target.value)} options={ESTADOS} />
```

- [ ] **Step 5: `HabitacionesList.jsx`, botón "Dar de baja" (línea 56)**

Reemplazar la línea:

```jsx
      {perfil === 'admin' && h.activo && <button className="link danger" disabled={h.estado === 'Ocupada'} title={h.estado === 'Ocupada' ? 'No se puede dar de baja una habitación ocupada' : undefined} onClick={() => setABaja(h)}>Dar de baja</button>}
```

por:

```jsx
      {perfil === 'admin' && h.activo && <Button variant="link danger" disabled={h.estado === 'Ocupada'} title={h.estado === 'Ocupada' ? 'No se puede dar de baja una habitación ocupada' : undefined} onClick={() => setABaja(h)}>Dar de baja</Button>}
```

- [ ] **Step 6: `HabitacionesList.jsx`, tiles del resumen (líneas 64-67)**

Reemplazar:

```jsx
          <button key={r.estado} className={`card stat ${estado === r.estado ? 'sel' : ''}`}
            onClick={() => { setEstado(estado === r.estado ? '' : r.estado); setPage(1) }}>
            <span className={`dot ${r.estado}`} /> <strong>{r.cantidad}</strong> <small>{r.estado}</small>
          </button>
```

por:

```jsx
          <Card as="button" key={r.estado} className={`stat ${estado === r.estado ? 'sel' : ''}`}
            onClick={() => { setEstado(estado === r.estado ? '' : r.estado); setPage(1) }}>
            <span className={`dot ${r.estado}`} /> <strong>{r.cantidad}</strong> <small>{r.estado}</small>
          </Card>
```

(`Card` ya está importado).

- [ ] **Step 7: Mover el estilo base de `select.estado` a la librería**

En `hotel-ui/src/theme.css`, añadir al final:

```css
select.estado{width:auto;padding:4px 8px;border-radius:99px;font-size:.85rem;border:1px solid var(--bg2)}
```

Borrar esa misma línea (`select.estado{width:auto;padding:4px 8px;border-radius:99px;font-size:.85rem;border:1px solid var(--bg2)}`) de `habitaciones-front/src/styles.css` y de `reservas-front/src/styles.css`. Conservar en habitaciones las variantes `select.estado.Disponible` y `select.estado.Mantenimiento`.

- [ ] **Step 8: Verificar**

Run:
```bash
for d in clientes-front habitaciones-front reservas-front; do (cd $d && npx vite build 2>&1 | tail -2); done
grep -rn "<button\|<select\|<input" clientes-front/src habitaciones-front/src reservas-front/src
```
Expected: los 3 fronts terminan con `✓ built in ...` y el `grep` no imprime nada.

- [ ] **Step 9: Commit**

```bash
git add clientes-front/src habitaciones-front/src reservas-front/src hotel-ui/src/theme.css
git commit -m "refactor(fronts): usar componentes de hotel-ui en lugar de elementos crudos y estilos duplicados"
```

---

### Task 6: Migrar `auth-front` a la librería

**Files:**
- Modify: `auth-front/src/main.jsx`
- Modify: `auth-front/src/api.js`
- Modify: `auth-front/src/pages/Login.jsx`
- Modify: `auth-front/src/pages/Registro.jsx`
- Modify: `auth-front/src/pages/Home.jsx`
- Modify: `auth-front/src/styles.css`
- Delete: `auth-front/src/validators.js`

**Interfaces:**
- Consumes: de `hotel-ui`: `Alert`, `Button`, `Card` (con `as`), `Field`, `Input`, `Select`, `AppShell`, `fetchSeguro`, `validar`, `requerido`, `documento`, `letras`, `email`, `telefono`, `password`, `maxLen`, y `hotel-ui/theme.css`. `auth-front/src/session.js` no cambia.

- [ ] **Step 1: `auth-front/src/main.jsx`**

```jsx
import React from 'react'
import { createRoot } from 'react-dom/client'
import { BrowserRouter } from 'react-router-dom'
import App from './App.jsx'
import 'hotel-ui/theme.css'
import './styles.css'

createRoot(document.getElementById('root')).render(<BrowserRouter><App /></BrowserRouter>)
```

- [ ] **Step 2: `auth-front/src/api.js`: usar `fetchSeguro` de la librería**

Reemplazar las primeras 6 líneas (hasta el cierre de `fetchSeguro`) por:

```js
import { fetchSeguro } from 'hotel-ui'

const BASE = import.meta.env.VITE_API_URL
```

El resto del archivo (`login` y `registro`) queda igual.

- [ ] **Step 3: `auth-front/src/pages/Login.jsx`**

```jsx
import { useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { Alert, Button, Card, Field, Input } from 'hotel-ui'
import { login } from '../api.js'
import { saveSession, rutaPorPerfil } from '../session.js'

export default function Login() {
  const nav = useNavigate()
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [error, setError] = useState('')
  const [loading, setLoading] = useState(false)

  const enviar = async (e) => {
    e.preventDefault()
    setLoading(true); setError('')
    try {
      const s = await login(email.trim(), password)
      saveSession(s)
      nav(rutaPorPerfil(s.usuario.perfil), { replace: true })
    } catch (err) { setError(err.message) } finally { setLoading(false) }
  }

  return (
    <div className="login-wrap">
      <Card as="form" className="login" onSubmit={enviar}>
        <h1>Hotel</h1>
        <p className="sub">Ingresa con tu cuenta</p>
        <Alert>{error}</Alert>
        <Field label="Correo"><Input type="email" required autoFocus value={email} onChange={(e) => setEmail(e.target.value)} /></Field>
        <Field label="Contraseña"><Input type="password" required value={password} onChange={(e) => setPassword(e.target.value)} /></Field>
        <Button disabled={loading}>{loading ? 'Ingresando…' : 'Ingresar'}</Button>
        <p className="sub"><Link to="/registro">¿Eres huésped? Crea tu cuenta</Link></p>
      </Card>
    </div>
  )
}
```

- [ ] **Step 4: `auth-front/src/pages/Registro.jsx`**

```jsx
import { useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import {
  Alert, Button, Card, Field, Input, Select,
  validar, requerido, documento, letras, email, telefono, password, maxLen,
} from 'hotel-ui'
import { registro } from '../api.js'
import { saveSession, rutaPorPerfil } from '../session.js'

const vacio = { tipoDocumento: 'CC', numeroDocumento: '', nombres: '', apellidos: '', email: '', telefono: '', password: '', confirmar: '' }
const ESQUEMA = {
  numeroDocumento: [requerido(), documento()],
  nombres: [requerido(), letras, maxLen(80)],
  apellidos: [requerido(), letras, maxLen(80)],
  email: [requerido(), email, maxLen(120)],
  telefono: [telefono],
  password: [requerido(), password],
  confirmar: [requerido('Confirma tu contraseña'), (v, f) => (v !== f.password ? 'Las contraseñas no coinciden' : null)],
}

export default function Registro() {
  const nav = useNavigate()
  const [f, setF] = useState(vacio)
  const [error, setError] = useState('')
  const [loading, setLoading] = useState(false)
  const [intento, setIntento] = useState(false)
  const errs = intento ? validar(f, ESQUEMA) : {}
  const set = (k) => (e) => setF({ ...f, [k]: e.target.value })

  const enviar = async (e) => {
    e.preventDefault()
    setIntento(true)
    if (Object.keys(validar(f, ESQUEMA)).length) return
    setLoading(true); setError('')
    try {
      const { confirmar, ...datos } = f
      const s = await registro({ ...datos, email: f.email.trim(), telefono: f.telefono || null })
      saveSession(s)
      nav(rutaPorPerfil(s.usuario.perfil), { replace: true })
    } catch (err) { setError(err.message) } finally { setLoading(false) }
  }

  return (
    <div className="login-wrap">
      <Card as="form" className="login" onSubmit={enviar} noValidate>
        <h1>Crear cuenta</h1>
        <p className="sub">Regístrate para reservar tu estadía</p>
        <Alert>{error}</Alert>
        <Field label="Tipo de documento">
          <Select value={f.tipoDocumento} onChange={set('tipoDocumento')} options={['CC', 'CE', 'TI', 'PA']} />
        </Field>
        <Field label="Número de documento" error={errs.numeroDocumento}><Input value={f.numeroDocumento} onChange={set('numeroDocumento')} /></Field>
        <Field label="Nombres" error={errs.nombres}><Input value={f.nombres} onChange={set('nombres')} /></Field>
        <Field label="Apellidos" error={errs.apellidos}><Input value={f.apellidos} onChange={set('apellidos')} /></Field>
        <Field label="Correo" error={errs.email}><Input type="email" value={f.email} onChange={set('email')} /></Field>
        <Field label="Teléfono (opcional)" error={errs.telefono}><Input value={f.telefono} onChange={set('telefono')} /></Field>
        <Field label="Contraseña (8+ caracteres, mayúscula, minúscula y número)" error={errs.password}><Input type="password" value={f.password} onChange={set('password')} /></Field>
        <Field label="Confirmar contraseña" error={errs.confirmar}><Input type="password" value={f.confirmar} onChange={set('confirmar')} /></Field>
        <Button disabled={loading}>{loading ? 'Creando…' : 'Crear cuenta'}</Button>
        <p className="sub"><Link to="/login">Ya tengo cuenta</Link></p>
      </Card>
    </div>
  )
}
```

- [ ] **Step 5: `auth-front/src/pages/Home.jsx`**

```jsx
import { useNavigate } from 'react-router-dom'
import { AppShell, Card } from 'hotel-ui'
import { clearSession, getSession } from '../session.js'

const CLIENTES = import.meta.env.VITE_CLIENTES_URL
const HABITACIONES = import.meta.env.VITE_HABITACIONES_URL
const RESERVAS = import.meta.env.VITE_RESERVAS_URL

const ETIQUETAS = { admin: 'Administrador', recepcion: 'Recepción', huesped: 'Huésped' }

// Módulos visibles por perfil
const MODULOS = [
  { nombre: 'Clientes', desc: 'Registro, consulta y reportes CSV/PDF', url: CLIENTES, perfiles: ['admin', 'recepcion'] },
  { nombre: 'Habitaciones', desc: 'Disponibilidad y gestión de habitaciones', url: HABITACIONES, perfiles: ['admin', 'recepcion'] },
  { nombre: 'Reservas', desc: 'Reservas, check-in y check-out', url: RESERVAS, perfiles: ['admin', 'recepcion'] },
  { nombre: 'Mis reservas', desc: 'Reserva tu estadía y consulta tus reservas', url: RESERVAS, perfiles: ['huesped'] },
  { nombre: 'Usuarios', desc: 'Administración de cuentas y perfiles', url: null, perfiles: ['admin'] },
]

export default function Home() {
  const nav = useNavigate()
  const { usuario, token } = getSession()
  const salir = () => { clearSession(); nav('/login', { replace: true }) }
  // El token viaja en el fragmento (#) para que no llegue a ningún servidor ni log
  const abrir = (url) => { window.location.href = `${url}/#token=${encodeURIComponent(token)}` }

  return (
    <AppShell section="Inicio" user={`${usuario.nombre} · ${ETIQUETAS[usuario.perfil]}`} onLogout={salir}>
      <h2>Bienvenido, {usuario.nombre}</h2>
      <div className="modules">
        {MODULOS.filter((m) => m.perfiles.includes(usuario.perfil)).map((m) => (
          <Card as="button" key={m.nombre} className="module" disabled={!m.url} onClick={() => abrir(m.url)}>
            <strong>{m.nombre}</strong><span>{m.desc}</span>
            {!m.url && <small>Próximamente</small>}
          </Card>
        ))}
      </div>
    </AppShell>
  )
}
```

- [ ] **Step 6: `auth-front/src/styles.css`: dejar solo lo propio de estas pantallas**

```css
.login-wrap{min-height:100vh;display:grid;place-items:center;background:linear-gradient(160deg,var(--primary) 45%,var(--bg) 45%)}
.login{width:min(380px,92vw);display:flex;flex-direction:column;gap:14px;border-top:4px solid var(--accent)}
.login h1{margin:0;text-align:center;color:var(--primary);font-family:Georgia,serif}
.login label{display:flex;flex-direction:column;gap:6px;font-size:.9rem;color:var(--muted)}
.login .alert{margin:0}
.sub{margin:0;text-align:center;color:var(--muted)}
main h2{color:var(--primary);font-family:Georgia,serif}
.modules{display:grid;grid-template-columns:repeat(auto-fill,minmax(250px,1fr));gap:18px}
.module{text-align:left;border:0;border-left:4px solid var(--accent);cursor:pointer;display:flex;flex-direction:column;gap:6px;font:inherit}
.module:hover:not(:disabled){transform:translateY(-2px)}
.module:disabled{opacity:.6;cursor:default}
.module strong{color:var(--primary);font-size:1.1rem}.module span{color:var(--muted)}.module small{color:var(--accent)}
```

- [ ] **Step 7: Borrar la copia de validadores**

Run: `git rm auth-front/src/validators.js`
Expected: `rm 'auth-front/src/validators.js'`.

- [ ] **Step 8: Verificar**

Run:
```bash
cd auth-front && npx vite build 2>&1 | tail -3; cd ..
grep -rn "<button\|<input\|<select" auth-front/src; ls auth-front/src
```
Expected: `✓ built in ...`; el `grep` no imprime nada; `ls` no lista `validators.js`.

- [ ] **Step 9: Commit**

```bash
git add auth-front/src
git commit -m "refactor(auth-front): migrar a hotel-ui y eliminar copia de validadores"
```

---

### Task 7: Componente `Carousel` (TDD)

**Files:**
- Create: `hotel-ui/src/components/Carousel.test.jsx`
- Create: `hotel-ui/src/components/Carousel.jsx`
- Modify: `hotel-ui/src/index.js` (export)
- Modify: `hotel-ui/src/index.test.js` (agregar `'Carousel'` a `PUBLICOS`)
- Modify: `hotel-ui/src/theme.css` (estilos)

**Interfaces:**
- Produces: `<Carousel images={[{ id, url, alt? }]} aspectRatio="4 / 3" fallback="/url-opcional.png" alt="Habitación 101" className="" />`. El contenedor es `role="region"` con `aria-label="Carrusel de imágenes"`; cada imagen puede traer `id` (si no, se usa `url` como clave). Botones accesibles: `Imagen anterior`, `Imagen siguiente`, `Ir a la imagen N`. Texto para lectores: `Imagen X de Y`. La Task 9 lo documenta y el plan de imágenes de habitaciones lo usa.

- [ ] **Step 1: Escribir las pruebas que fallan, `hotel-ui/src/components/Carousel.test.jsx`**

```jsx
import { fireEvent, render, screen } from '@testing-library/react'
import Carousel from './Carousel.jsx'

const fotos = [
  { id: 'a', url: '/a.jpg' },
  { id: 'b', url: '/b.jpg' },
  { id: 'c', url: '/c.jpg' },
]
const region = () => screen.getByRole('region', { name: 'Carrusel de imágenes' })

describe('Carousel', () => {
  test('sin imágenes ni relleno muestra un texto', () => {
    render(<Carousel images={[]} />)
    expect(screen.getByText('Sin imágenes')).toBeInTheDocument()
  })

  test('sin imágenes con relleno muestra la imagen de relleno', () => {
    const { container } = render(<Carousel images={[]} fallback="/relleno.png" alt="Habitación" />)
    expect(container.querySelector('img')).toHaveAttribute('src', '/relleno.png')
  })

  test('con una sola imagen no muestra flechas ni puntos', () => {
    render(<Carousel images={[fotos[0]]} />)
    expect(screen.queryByRole('button', { name: 'Imagen siguiente' })).not.toBeInTheDocument()
    expect(screen.queryByRole('button', { name: /Ir a la imagen/ })).not.toBeInTheDocument()
  })

  test('siguiente avanza y anterior desde la primera da la vuelta', () => {
    render(<Carousel images={fotos} />)
    expect(screen.getByText('Imagen 1 de 3')).toBeInTheDocument()
    fireEvent.click(screen.getByRole('button', { name: 'Imagen siguiente' }))
    expect(screen.getByText('Imagen 2 de 3')).toBeInTheDocument()
    fireEvent.click(screen.getByRole('button', { name: 'Imagen anterior' }))
    fireEvent.click(screen.getByRole('button', { name: 'Imagen anterior' }))
    expect(screen.getByText('Imagen 3 de 3')).toBeInTheDocument()
  })

  test('un punto lleva a su imagen y queda marcado como actual', () => {
    render(<Carousel images={fotos} />)
    fireEvent.click(screen.getByRole('button', { name: 'Ir a la imagen 3' }))
    expect(screen.getByText('Imagen 3 de 3')).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Ir a la imagen 3' })).toHaveAttribute('aria-current', 'true')
    expect(screen.getByRole('button', { name: 'Ir a la imagen 1' })).toHaveAttribute('aria-current', 'false')
  })

  test('las flechas del teclado navegan', () => {
    render(<Carousel images={fotos} />)
    fireEvent.keyDown(region(), { key: 'ArrowRight' })
    expect(screen.getByText('Imagen 2 de 3')).toBeInTheDocument()
    fireEvent.keyDown(region(), { key: 'ArrowLeft' })
    expect(screen.getByText('Imagen 1 de 3')).toBeInTheDocument()
  })

  test('deslizar con el dedo cambia de imagen; un roce corto no', () => {
    render(<Carousel images={fotos} />)
    fireEvent.touchStart(region(), { touches: [{ clientX: 200 }] })
    fireEvent.touchEnd(region(), { changedTouches: [{ clientX: 195 }] })
    expect(screen.getByText('Imagen 1 de 3')).toBeInTheDocument()
    fireEvent.touchStart(region(), { touches: [{ clientX: 200 }] })
    fireEvent.touchEnd(region(), { changedTouches: [{ clientX: 100 }] })
    expect(screen.getByText('Imagen 2 de 3')).toBeInTheDocument()
    fireEvent.touchStart(region(), { touches: [{ clientX: 100 }] })
    fireEvent.touchEnd(region(), { changedTouches: [{ clientX: 200 }] })
    expect(screen.getByText('Imagen 1 de 3')).toBeInTheDocument()
  })

  // Review Focus 1
  test('si la lista se acorta mientras se ve la última, muestra una imagen válida', () => {
    const { rerender } = render(<Carousel images={fotos} />)
    fireEvent.click(screen.getByRole('button', { name: 'Ir a la imagen 3' }))
    rerender(<Carousel images={[fotos[0]]} />)
    expect(screen.getByText('Imagen 1 de 1')).toBeInTheDocument()
  })

  // Review Focus 2
  test('una imagen que no carga se omite del carrusel', () => {
    const { container } = render(<Carousel images={fotos} />)
    fireEvent.error(container.querySelectorAll('img')[1])
    expect(screen.getByText('Imagen 1 de 2')).toBeInTheDocument()
    expect(container.querySelectorAll('img')).toHaveLength(2)
  })

  test('si ninguna imagen carga, muestra el relleno', () => {
    const { container } = render(<Carousel images={[fotos[0]]} fallback="/relleno.png" />)
    fireEvent.error(container.querySelector('img'))
    expect(container.querySelector('img')).toHaveAttribute('src', '/relleno.png')
  })

  test('si ninguna imagen carga y no hay relleno, muestra el texto', () => {
    const { container } = render(<Carousel images={[fotos[0]]} />)
    fireEvent.error(container.querySelector('img'))
    expect(screen.getByText('Sin imágenes')).toBeInTheDocument()
  })
})
```

- [ ] **Step 2: Ejecutar y comprobar que falla**

Run: `cd hotel-ui && npx vitest run src/components/Carousel.test.jsx`
Expected: FAIL con `Failed to resolve import "./Carousel.jsx"`.

- [ ] **Step 3: Implementar `hotel-ui/src/components/Carousel.jsx`**

```jsx
import { useRef, useState } from 'react'

const UMBRAL_TACTIL = 40 // px mínimos de deslizamiento para cambiar de imagen

// <Carousel images={[{ id, url, alt? }]} aspectRatio="4 / 3" fallback="/sin-foto.png" alt="Habitación 101" />
export default function Carousel({ images = [], aspectRatio = '4 / 3', fallback, alt = 'Imagen', className = '' }) {
  const [actual, setActual] = useState(0)
  const [rotas, setRotas] = useState({}) // imágenes cuya URL no cargó
  const inicioX = useRef(null)

  const clave = (img) => img.id ?? img.url
  const visibles = images.filter((img) => !rotas[clave(img)])
  const total = visibles.length
  const indice = total ? Math.min(actual, total - 1) : 0 // evita índices fuera de rango si la lista se acorta

  const ir = (n) => setActual(((n % total) + total) % total)
  const marcarRota = (img) => setRotas((r) => ({ ...r, [clave(img)]: true }))

  const alTeclado = (e) => {
    if (total < 2) return
    if (e.key === 'ArrowRight') { e.preventDefault(); ir(indice + 1) }
    if (e.key === 'ArrowLeft') { e.preventDefault(); ir(indice - 1) }
  }
  const alTocar = (e) => { inicioX.current = e.touches[0].clientX }
  const alSoltar = (e) => {
    if (inicioX.current === null || total < 2) return
    const dx = e.changedTouches[0].clientX - inicioX.current
    inicioX.current = null
    if (Math.abs(dx) >= UMBRAL_TACTIL) ir(dx < 0 ? indice + 1 : indice - 1)
  }

  return (
    <div
      className={`carousel ${className}`.trim()}
      style={{ aspectRatio }}
      role="region"
      aria-roledescription="carrusel"
      aria-label="Carrusel de imágenes"
      tabIndex={0}
      onKeyDown={alTeclado}
      onTouchStart={alTocar}
      onTouchEnd={alSoltar}
    >
      {total === 0 ? (
        fallback
          ? <img className="carousel-fallback" src={fallback} alt={alt} />
          : <div className="carousel-empty">Sin imágenes</div>
      ) : (
        <>
          <div className="carousel-track" style={{ transform: `translateX(-${indice * 100}%)` }}>
            {visibles.map((img, i) => (
              <div className="carousel-slide" key={clave(img)} aria-hidden={i !== indice}>
                <img
                  src={img.url}
                  alt={img.alt || `${alt} ${i + 1}`}
                  loading={i === 0 ? 'eager' : 'lazy'}
                  onError={() => marcarRota(img)}
                />
              </div>
            ))}
          </div>
          <span className="sr-only" aria-live="polite">Imagen {indice + 1} de {total}</span>
          {total > 1 && (
            <>
              <button type="button" className="carousel-btn carousel-prev" aria-label="Imagen anterior" onClick={() => ir(indice - 1)}>‹</button>
              <button type="button" className="carousel-btn carousel-next" aria-label="Imagen siguiente" onClick={() => ir(indice + 1)}>›</button>
              <div className="carousel-dots">
                {visibles.map((img, i) => (
                  <button
                    type="button"
                    key={clave(img)}
                    className="carousel-dot"
                    aria-label={`Ir a la imagen ${i + 1}`}
                    aria-current={i === indice}
                    onClick={() => ir(i)}
                  />
                ))}
              </div>
            </>
          )}
        </>
      )}
    </div>
  )
}
```

- [ ] **Step 4: Ejecutar y comprobar que pasa**

Run: `cd hotel-ui && npx vitest run src/components/Carousel.test.jsx`
Expected: PASS (11 pruebas).

- [ ] **Step 5: Exportar, probar el export y añadir estilos**

En `hotel-ui/src/index.js`, añadir la línea:

```js
export { default as Carousel } from './components/Carousel.jsx'
```

En `hotel-ui/src/index.test.js`, añadir `'Carousel'` a la lista `PUBLICOS`.

En `hotel-ui/src/theme.css`, añadir al final:

```css
/* ---- imágenes ---- */
.sr-only{position:absolute;width:1px;height:1px;margin:-1px;padding:0;overflow:hidden;clip:rect(0,0,0,0);white-space:nowrap;border:0}
.carousel{position:relative;overflow:hidden;border-radius:12px;background:var(--bg2);width:100%}
.carousel:focus-visible{outline:2px solid var(--accent);outline-offset:2px}
.carousel-track{display:flex;height:100%;transition:transform .3s ease}
.carousel-slide{flex:0 0 100%;height:100%}
.carousel-slide img,.carousel-fallback{width:100%;height:100%;object-fit:cover;display:block}
.carousel-empty{display:grid;place-items:center;height:100%;color:var(--muted);font-size:.9rem}
.carousel-btn{position:absolute;top:50%;transform:translateY(-50%);width:34px;height:34px;border-radius:50%;border:0;background:rgba(15,45,61,.6);color:#fff;font-size:1.3rem;line-height:1;cursor:pointer}
.carousel-btn:hover{background:rgba(15,45,61,.85)}
.carousel-prev{left:8px}.carousel-next{right:8px}
.carousel-dots{position:absolute;bottom:8px;left:0;right:0;display:flex;justify-content:center;gap:6px}
.carousel-dot{width:9px;height:9px;border-radius:50%;border:0;padding:0;background:rgba(255,255,255,.65);cursor:pointer}
.carousel-dot[aria-current="true"]{background:var(--accent)}
```

- [ ] **Step 6: Ejecutar toda la suite**

Run: `cd hotel-ui && npm test`
Expected: todas las pruebas pasan.

- [ ] **Step 7: Commit**

```bash
git add hotel-ui/src/components/Carousel.jsx hotel-ui/src/components/Carousel.test.jsx hotel-ui/src/index.js hotel-ui/src/index.test.js hotel-ui/src/theme.css
git commit -m "feat(hotel-ui): componente Carousel con teclado, táctil y manejo de imágenes rotas"
```

---

### Task 8: Componente `ImageManager` (TDD)

**Files:**
- Create: `hotel-ui/src/components/ImageManager.test.jsx`
- Create: `hotel-ui/src/components/ImageManager.jsx`
- Modify: `hotel-ui/src/index.js` (export)
- Modify: `hotel-ui/src/index.test.js` (agregar `'ImageManager'`)
- Modify: `hotel-ui/src/theme.css` (estilos)

**Interfaces:**
- Consumes: `Button` (variante `ghost`, `size="sm"`, ahora con `type` reenviado) y `Alert` de la librería.
- Produces: `<ImageManager images={[{ id, url }]} onUpload={async (file) => {}} onDelete={async (id) => {}} onReorder={async (ids) => {}} max={6} maxBytes={5242880} accept={['image/jpeg','image/png','image/webp']} disabled={false} />`. Controles accesibles: `Agregar imagen`, campo de archivo con `aria-label="Seleccionar imagen"`, y por miniatura `Mover imagen N a la izquierda`, `Mover imagen N a la derecha`, `Eliminar imagen N`. Los callbacks pueden ser asíncronos; si lanzan, el mensaje del error se muestra. El plan de imágenes de habitaciones lo conecta a la API.

- [ ] **Step 1: Escribir las pruebas que fallan, `hotel-ui/src/components/ImageManager.test.jsx`**

```jsx
import { fireEvent, render, screen, waitFor } from '@testing-library/react'
import ImageManager from './ImageManager.jsx'

const fotos = [
  { id: 'a', url: '/a.jpg' },
  { id: 'b', url: '/b.jpg' },
  { id: 'c', url: '/c.jpg' },
]
const archivo = (nombre, tipo, bytes = 1000) => {
  const f = new File(['x'], nombre, { type: tipo })
  Object.defineProperty(f, 'size', { value: bytes })
  return f
}
const subir = (f) => fireEvent.change(screen.getByLabelText('Seleccionar imagen'), { target: { files: [f] } })

describe('ImageManager', () => {
  test('muestra las miniaturas y el contador', () => {
    render(<ImageManager images={fotos} max={6} />)
    expect(screen.getAllByRole('img')).toHaveLength(3)
    expect(screen.getByText('3 de 6 imágenes')).toBeInTheDocument()
  })

  test('sube un archivo válido', async () => {
    const onUpload = vi.fn().mockResolvedValue()
    render(<ImageManager images={[]} onUpload={onUpload} />)
    const f = archivo('foto.png', 'image/png')
    subir(f)
    await waitFor(() => expect(onUpload).toHaveBeenCalledWith(f))
  })

  test('rechaza un formato no permitido sin llamar a onUpload', () => {
    const onUpload = vi.fn()
    render(<ImageManager images={[]} onUpload={onUpload} />)
    subir(archivo('doc.pdf', 'application/pdf'))
    expect(screen.getByText('Formato no permitido. Usa JPG, PNG, WebP.')).toBeInTheDocument()
    expect(onUpload).not.toHaveBeenCalled()
  })

  // Review Focus 3
  test('rechaza un archivo sin tipo (algunos navegadores con .webp)', () => {
    const onUpload = vi.fn()
    render(<ImageManager images={[]} onUpload={onUpload} />)
    subir(archivo('foto.webp', ''))
    expect(screen.getByText('Formato no permitido. Usa JPG, PNG, WebP.')).toBeInTheDocument()
    expect(onUpload).not.toHaveBeenCalled()
  })

  test('acepta exactamente maxBytes y rechaza un byte más', async () => {
    const onUpload = vi.fn().mockResolvedValue()
    render(<ImageManager images={[]} onUpload={onUpload} maxBytes={5 * 1024 * 1024} />)
    subir(archivo('justa.jpg', 'image/jpeg', 5 * 1024 * 1024))
    await waitFor(() => expect(onUpload).toHaveBeenCalledTimes(1))
    subir(archivo('grande.jpg', 'image/jpeg', 5 * 1024 * 1024 + 1))
    expect(await screen.findByText('La imagen supera 5 MB.')).toBeInTheDocument()
    expect(onUpload).toHaveBeenCalledTimes(1)
  })

  test('con el máximo de imágenes no se puede agregar otra', () => {
    render(<ImageManager images={fotos} max={3} />)
    expect(screen.getByRole('button', { name: 'Agregar imagen' })).toBeDisabled()
  })

  test('eliminar llama a onDelete con el id', async () => {
    const onDelete = vi.fn().mockResolvedValue()
    render(<ImageManager images={fotos} onDelete={onDelete} />)
    fireEvent.click(screen.getByRole('button', { name: 'Eliminar imagen 2' }))
    await waitFor(() => expect(onDelete).toHaveBeenCalledWith('b'))
  })

  test('mover a la derecha envía el nuevo orden', async () => {
    const onReorder = vi.fn().mockResolvedValue()
    render(<ImageManager images={fotos} onReorder={onReorder} />)
    fireEvent.click(screen.getByRole('button', { name: 'Mover imagen 1 a la derecha' }))
    await waitFor(() => expect(onReorder).toHaveBeenCalledWith(['b', 'a', 'c']))
  })

  test('la primera no se mueve a la izquierda ni la última a la derecha', () => {
    render(<ImageManager images={fotos} />)
    expect(screen.getByRole('button', { name: 'Mover imagen 1 a la izquierda' })).toBeDisabled()
    expect(screen.getByRole('button', { name: 'Mover imagen 3 a la derecha' })).toBeDisabled()
  })

  // Review Focus 4
  test('si onUpload rechaza, muestra el mensaje y permite reintentar', async () => {
    const onUpload = vi.fn()
      .mockRejectedValueOnce(new Error('Máximo 6 imágenes por habitación.'))
      .mockResolvedValueOnce()
    render(<ImageManager images={[]} onUpload={onUpload} />)
    subir(archivo('a.png', 'image/png'))
    expect(await screen.findByText('Máximo 6 imágenes por habitación.')).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Agregar imagen' })).toBeEnabled()
    subir(archivo('b.png', 'image/png'))
    await waitFor(() => expect(onUpload).toHaveBeenCalledTimes(2))
    await waitFor(() => expect(screen.queryByText('Máximo 6 imágenes por habitación.')).not.toBeInTheDocument())
  })

  test('mientras una acción está en curso los controles quedan deshabilitados', async () => {
    let terminar
    const onDelete = vi.fn(() => new Promise((res) => { terminar = res }))
    render(<ImageManager images={fotos} onDelete={onDelete} />)
    fireEvent.click(screen.getByRole('button', { name: 'Eliminar imagen 1' }))
    await waitFor(() => expect(screen.getByRole('button', { name: 'Eliminar imagen 2' })).toBeDisabled())
    terminar()
    await waitFor(() => expect(screen.getByRole('button', { name: 'Eliminar imagen 2' })).toBeEnabled())
  })

  // Review Focus 5
  test('los botones no envían el formulario que los contiene', async () => {
    const onSubmit = vi.fn((e) => e.preventDefault())
    const onDelete = vi.fn().mockResolvedValue()
    render(<form onSubmit={onSubmit}><ImageManager images={fotos} onDelete={onDelete} /></form>)
    fireEvent.click(screen.getByRole('button', { name: 'Eliminar imagen 1' }))
    fireEvent.click(screen.getByRole('button', { name: 'Mover imagen 2 a la derecha' }))
    await waitFor(() => expect(onDelete).toHaveBeenCalled())
    expect(onSubmit).not.toHaveBeenCalled()
  })

  test('disabled deshabilita todos los controles', () => {
    render(<ImageManager images={fotos} disabled />)
    expect(screen.getByRole('button', { name: 'Agregar imagen' })).toBeDisabled()
    expect(screen.getByRole('button', { name: 'Eliminar imagen 1' })).toBeDisabled()
  })
})
```

- [ ] **Step 2: Ejecutar y comprobar que falla**

Run: `cd hotel-ui && npx vitest run src/components/ImageManager.test.jsx`
Expected: FAIL con `Failed to resolve import "./ImageManager.jsx"`.

- [ ] **Step 3: Implementar `hotel-ui/src/components/ImageManager.jsx`**

```jsx
import { useRef, useState } from 'react'
import Alert from './Alert.jsx'
import Button from './Button.jsx'

const MB = 1024 * 1024
const NOMBRES = { 'image/jpeg': 'JPG', 'image/png': 'PNG', 'image/webp': 'WebP' }
const FORMATOS = Object.keys(NOMBRES)

// Gestor presentacional: no conoce la API. Cada front le pasa onUpload / onDelete / onReorder (pueden ser async).
export default function ImageManager({
  images = [], onUpload, onDelete, onReorder, max = 6, maxBytes = 5 * MB, accept = FORMATOS, disabled = false,
}) {
  const [error, setError] = useState('')
  const [ocupado, setOcupado] = useState(false)
  const input = useRef(null)
  const bloqueado = disabled || ocupado
  const lleno = images.length >= max

  const ejecutar = async (accion) => {
    setError(''); setOcupado(true)
    try { await accion() } catch (e) { setError(e?.message || 'No se pudo completar la acción.') } finally { setOcupado(false) }
  }

  const elegir = (e) => {
    const archivo = e.target.files?.[0]
    e.target.value = '' // permite volver a elegir el mismo archivo
    if (!archivo) return
    if (!accept.includes(archivo.type)) {
      setError(`Formato no permitido. Usa ${accept.map((t) => NOMBRES[t] ?? t).join(', ')}.`)
      return
    }
    if (archivo.size > maxBytes) {
      setError(`La imagen supera ${+(maxBytes / MB).toFixed(1)} MB.`)
      return
    }
    ejecutar(() => onUpload(archivo))
  }

  const mover = (i, delta) => {
    const ids = images.map((img) => img.id)
    const j = i + delta
    ;[ids[i], ids[j]] = [ids[j], ids[i]]
    ejecutar(() => onReorder(ids))
  }

  return (
    <div className="image-manager">
      <Alert>{error}</Alert>
      <ul className="image-manager-list">
        {images.map((img, i) => (
          <li key={img.id}>
            <img src={img.url} alt={`Imagen ${i + 1}`} />
            <div className="image-manager-actions">
              <Button type="button" variant="ghost" size="sm" disabled={bloqueado || i === 0}
                aria-label={`Mover imagen ${i + 1} a la izquierda`} onClick={() => mover(i, -1)}>←</Button>
              <Button type="button" variant="ghost" size="sm" disabled={bloqueado || i === images.length - 1}
                aria-label={`Mover imagen ${i + 1} a la derecha`} onClick={() => mover(i, 1)}>→</Button>
              <Button type="button" variant="danger" size="sm" disabled={bloqueado}
                aria-label={`Eliminar imagen ${i + 1}`} onClick={() => ejecutar(() => onDelete(img.id))}>Eliminar</Button>
            </div>
          </li>
        ))}
      </ul>
      <div>
        <Button type="button" disabled={bloqueado || lleno} onClick={() => input.current?.click()}>Agregar imagen</Button>
        <span className="image-manager-count">{images.length} de {max} imágenes</span>
        <input ref={input} type="file" hidden accept={accept.join(',')} aria-label="Seleccionar imagen" onChange={elegir} />
      </div>
    </div>
  )
}
```

- [ ] **Step 4: Ejecutar y comprobar que pasa**

Run: `cd hotel-ui && npx vitest run src/components/ImageManager.test.jsx`
Expected: PASS (13 pruebas). Si falla `el máximo de imágenes` o `ocupado` por tiempos, no cambiar el componente: revisar que la prueba use `waitFor` como está escrita.

- [ ] **Step 5: Exportar, probar el export y añadir estilos**

En `hotel-ui/src/index.js`, añadir:

```js
export { default as ImageManager } from './components/ImageManager.jsx'
```

En `hotel-ui/src/index.test.js`, añadir `'ImageManager'` a `PUBLICOS`.

En `hotel-ui/src/theme.css`, añadir al final:

```css
.image-manager-list{list-style:none;margin:12px 0;padding:0;display:grid;grid-template-columns:repeat(auto-fill,minmax(130px,1fr));gap:12px}
.image-manager-list li{background:var(--bg);border:1px solid var(--bg2);border-radius:8px;padding:6px;display:flex;flex-direction:column;gap:6px}
.image-manager-list img{width:100%;aspect-ratio:4/3;object-fit:cover;border-radius:6px;display:block}
.image-manager-actions{display:flex;justify-content:space-between;gap:4px}
.image-manager-count{color:var(--muted);font-size:.85rem;margin-left:10px}
```

- [ ] **Step 6: Ejecutar toda la suite**

Run: `cd hotel-ui && npm test`
Expected: todas las pruebas pasan.

- [ ] **Step 7: Commit**

```bash
git add hotel-ui/src/components/ImageManager.jsx hotel-ui/src/components/ImageManager.test.jsx hotel-ui/src/index.js hotel-ui/src/index.test.js hotel-ui/src/theme.css
git commit -m "feat(hotel-ui): componente ImageManager para subir, borrar y reordenar imágenes"
```

---

### Task 9: README de `hotel-ui`

**Files:**
- Modify: `hotel-ui/README.md` (reemplazo completo)

**Interfaces:**
- Consumes: la API de todos los componentes exportados por `hotel-ui/src/index.js` (incluidos `Carousel` e `ImageManager`).

- [ ] **Step 1: Reemplazar `hotel-ui/README.md`**

````markdown
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
````

- [ ] **Step 2: Comprobar que documenta todos los exports**

Run:
```bash
cd hotel-ui && for n in Button Card Alert Badge Spinner Toolbar DataTable Pager AppShell RequirePerfil Modal ConfirmModal Field Input Textarea Select Checkbox FormGrid FormActions Carousel ImageManager useDebouncedEffect useLoading createSession validar fetchSeguro extraerError; do grep -q "$n" README.md || echo "FALTA: $n"; done; echo revisado
```
Expected: solo imprime `revisado` (ninguna línea `FALTA:`).

- [ ] **Step 3: Commit**

```bash
git add hotel-ui/README.md
git commit -m "docs(hotel-ui): documentar props y uso de todos los componentes"
```

---

### Task 10: Verificación final (compilación, pruebas y navegador)

**Files:** ninguno (solo verificación). Los hallazgos que obliguen a cambiar código se corrigen en la tarea correspondiente y se vuelven a verificar.

**Interfaces:**
- Consumes: todo lo anterior.

- [ ] **Step 1: Pruebas de la librería y compilación de los 4 fronts**

Run:
```bash
cd hotel-ui && npm test 2>&1 | tail -6; cd ..
for d in auth-front clientes-front habitaciones-front reservas-front; do echo "== $d"; (cd $d && npx vite build 2>&1 | tail -2); done
```
Expected: todas las pruebas pasan y los 4 fronts terminan con `✓ built in ...`.

- [ ] **Step 2: Criterios de aceptación automáticos**

Run:
```bash
grep -rn "<button\|<input\|<select" auth-front/src clientes-front/src habitaciones-front/src reservas-front/src
test ! -f auth-front/src/validators.js && echo "sin validators propio en auth-front"
grep -rn "alias" */vite.config.js
git status --short
```
Expected: el primer `grep` no imprime nada; aparece `sin validators propio en auth-front`; el segundo `grep` no imprime nada (o solo los fronts donde se aplicó el respaldo de la Task 2); `git status` limpio.

- [ ] **Step 3: Levantar el sistema**

Run (los `.env` de las APIs y de los fronts ya existen en el equipo del usuario; no se tocan):
```bash
export DOTNET_ROOT=/opt/homebrew/opt/dotnet@8/libexec; export PATH=/opt/homebrew/opt/dotnet@8/bin:$PATH
p=5001; for d in auth-api clientes-api habitaciones-api reservas-api; do (cd $d && nohup dotnet run --urls http://localhost:$p > "$TMPDIR/$d.log" 2>&1 &); p=$((p+1)); done
for d in auth-front clientes-front habitaciones-front reservas-front; do (cd $d && nohup npm run dev > "$TMPDIR/$d.log" 2>&1 &); done
sleep 20; for p in 5001 5002 5003 5004 5173 5174 5175 5176; do echo "$p -> $(curl -s -o /dev/null -w '%{http_code}' --max-time 5 http://localhost:$p/)"; done
```
Expected: los 8 puertos responden (404 en las APIs y 200 en los fronts).

- [ ] **Step 4: Verificar en el navegador integrado (comportamiento igual que antes)**

1. Abrir `http://localhost:5173/login`: se ve el formulario de acceso con el estilo de siempre (fondo dividido, tarjeta centrada).
2. Entrar con el usuario de prueba `admin@hotel.com` (contraseña del README del proyecto): llega a `/admin` y muestra «Bienvenido» y las tarjetas de módulos.
3. Abrir Clientes, Habitaciones y Reservas desde el Home: cada uno carga su listado y mantiene la sesión.
4. En Habitaciones: el selector de estado se ve como una pastilla y los tiles del resumen se pueden pulsar para filtrar.
5. Abrir `http://localhost:5173/registro`, pulsar «Crear cuenta» con el formulario vacío: aparecen los mensajes de validación bajo cada campo.
6. Revisar la consola del navegador: sin errores (en especial, sin «Invalid hook call» de React duplicado).

Expected: todo igual que antes de la migración. Cualquier diferencia visual relevante se anota y se corrige en `auth-front/src/styles.css` (Task 6).

- [ ] **Step 5: Apagar los servicios**

Run:
```bash
pkill -f "dotnet run --urls"; pkill -f vite
for p in 5001 5002 5003 5004 5173 5174 5175 5176; do lsof -tiTCP:$p -sTCP:LISTEN | xargs kill 2>/dev/null; done
```
Expected: ningún proceso escuchando en esos puertos.

- [ ] **Step 6: Informar al usuario**

Resumen de lo verificado, commits de la rama `feat/hotel-ui-libreria` y que el `push` y el pull request quedan para el usuario (la rama `Entrega1` está protegida).
