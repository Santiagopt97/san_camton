# Sesión con cookie HttpOnly y mínima exposición del usuario — Diseño

Fecha: 2026-10-07 · Rama: `feat/sesion-cookies` (sale de `feat/imagenes-habitaciones-impl`, porque ambas tocan el `api.js` de los fronts)

## Objetivo
Que la sesión viva en una cookie que el JavaScript de la página no pueda leer, en lugar de `sessionStorage`, y que se exponga
lo mínimo del usuario: el login deja de devolver sus datos, el token deja de llevar nombre y correo y deja de viajar por la URL.

## Estado actual (verificado en el código)
- `auth-api` devuelve en login y registro `{ token, expira, usuario: { id, nombre, email, perfil } }`.
- El JWT lleva `sub`, `name`, `email`, `role` y `cliente_id`; cualquiera que lo tenga puede leerlo.
- `auth-front` guarda todo en `sessionStorage` (`hotel_session`); los módulos guardan el token en otro `sessionStorage` (`token`),
  lo decodifican en el navegador para obtener nombre y perfil y lo envían en `Authorization: Bearer`.
- El paso de un módulo a otro lleva el token en la URL (`#token=...`).
- Las APIs solo leen del token el perfil (`role`) y, en `mis-reservas`, `cliente_id`. Nadie lee `name` ni `email`.
- `GET /api/auth/me` ya existe y devuelve nombre, correo y perfil.

## Decisiones acordadas
- Cookie `HttpOnly` emitida por `auth-api`; los fronts nunca ven el token.
- `/me` devuelve solo `{ nombre, perfil }` (opción A); el front lo pide con la cookie y lo guarda únicamente en memoria.
- Se mantiene `Authorization: Bearer` como segunda vía de autenticación en las APIs (Swagger, Postman).
- Protección CSRF con una cabecera obligatoria en las peticiones que modifican datos.
- Sin refresh tokens: la cookie vence cuando vence el token.

## Diseño

### 1. `auth-api`
- Login y registro establecen la cookie `hotel_token`: `HttpOnly`, `SameSite=Lax`, `Path=/`, `Secure` cuando la petición es HTTPS,
  `Expires` igual al `exp` del token. Responden **204 sin cuerpo**. Los errores siguen siendo `{ "message": ... }`.
- El token lleva `sub`, `role`, `cliente_id` (solo huéspedes), `iss`, `aud` y `exp`. Se eliminan `name` y `email`.
- `GET /api/auth/me` (requiere sesión): devuelve `{ nombre, perfil }` leyendo la base de datos con el `sub` del token. 401 sin sesión.
  Si el usuario fue desactivado, 401.
- `POST /api/auth/logout`: borra la cookie (mismos atributos y `Path`), responde 204. No requiere sesión válida.
- CORS: orígenes de los 4 fronts (`CORS_ORIGINS`) con `AllowCredentials`.
- La validación del token no cambia (firma, issuer, audience, expiración).

### 2. APIs de clientes, habitaciones y reservas
- `JwtBearer` toma el token de la cookie `hotel_token` cuando la petición no trae `Authorization` (evento `OnMessageReceived`).
- CORS con `AllowCredentials` (orígenes explícitos, nunca comodín).
- CSRF: toda petición POST, PUT, PATCH o DELETE debe traer `X-Requested-With: hotel-ui`; si falta, 400 `{ "message": "Falta la cabecera de seguridad." }`.
  Las peticiones de otro origen no pueden añadir esa cabecera sin pasar el preflight de CORS. La regla también aplica a `auth-api`
  (login, registro y logout). Las lecturas (GET) no la exigen. Las peticiones que traen `Authorization: Bearer` no la necesitan
  (no dependen de la cookie).
- `/health` y Swagger no cambian.

### 3. `hotel-ui` (sesión)
- `createSession({ authApiUrl, loginUrl })` se reescribe: sin `sessionStorage` ni decodificación del JWT.
  - `initSession()` (asíncrona): `GET {authApiUrl}/api/auth/me` con `credentials: 'include'`; si responde, guarda `{ nombre, perfil }`
    en memoria y devuelve `true`; si es 401 devuelve `false`.
  - `usuarioActual()` y `tienePerfil(...)` leen la memoria (sincrónicos).
  - `logout()`: `POST {authApiUrl}/api/auth/logout` y redirige a `{loginUrl}/login`.
- Nuevo helper de peticiones (`crearCliente({ base, onNoAutorizado })`) que añade `credentials: 'include'` y, en métodos que modifican datos,
  la cabecera `X-Requested-With`. En 401 llama a `onNoAutorizado` (cierra sesión). Los `api.js` de los módulos lo usan.
- Los módulos esperan `await initSession()` antes de dibujar la aplicación; si es `false`, redirigen al login de `auth-front`.

### 4. `auth-front`
- La sesión es un contexto de React (`SesionProvider`): al cargar pregunta a `/me`; mientras responde muestra una pantalla de carga;
  expone `usuario`, `entrar(email, password)`, `registrar(datos)` y `salir()`.
- Login y registro llaman al API (204) y después a `/me` para conocer el perfil y redirigir (`/admin`, `/recepcion`, `/huesped`).
- Abrir un módulo es un enlace normal a su URL (sin `#token`). Salir llama a `/logout`.
- Se elimina `src/session.js` basado en `sessionStorage`. Al ser reactivo, desaparece el bucle de redirecciones al pulsar «Salir».

### 5. Configuración y documentación
- Fronts de módulo: nueva variable `VITE_AUTH_API_URL` (por defecto `http://localhost:5001`). Se agregan `.env.example` a los 4 fronts
  (hoy no existen) y se documentan en el README.
- `CORS_ORIGINS` de `auth-api` incluye los 4 fronts (`.env.example` actualizado, sin valores secretos).
- README: explicar el flujo de cookie, que todo debe correr en el mismo host y que las sesiones abiertas dejan de valer.

## Pruebas
- `auth-api` (proyecto xUnit nuevo): login válido pone la cookie `HttpOnly` y responde 204 sin cuerpo; credenciales inválidas 401 sin cookie;
  el token no contiene `name` ni `email`; `/me` devuelve solo nombre y perfil y da 401 sin sesión o con usuario desactivado; logout borra la cookie.
- Módulos (`habitaciones-api.Tests` y equivalentes donde existan pruebas): el token se lee de la cookie; la cabecera `Authorization` sigue funcionando;
  un POST/PUT/PATCH/DELETE sin `X-Requested-With` da 400; un GET no la necesita.
- `hotel-ui` (Vitest): `initSession` con `/me` correcto e incorrecto, `logout`, el helper añade credenciales y cabecera, 401 dispara el cierre de sesión,
  nada se guarda en `sessionStorage` ni `localStorage`.
- Verificación real con los servicios: login por la interfaz, comprobar en las herramientas del navegador que la cookie es `HttpOnly` y que no queda
  nada en `sessionStorage`, recorrer los 3 módulos, subir una imagen (multipart con cookie), cerrar sesión y volver a entrar sin recargar.

## Riesgos y límites
- La cookie se comparte entre puertos solo porque todo corre en el mismo host (`localhost`). En Docker o Kubernetes hará falta un único host de entrada
  o un dominio común; se resuelve en la pieza de Docker.
- Cambio incompatible: las sesiones abiertas dejan de valer y todos deben volver a iniciar sesión.
- `SameSite=Lax` no impide peticiones entre puertos del mismo host; por eso la cabecera obligatoria.
- Si se mantiene `Authorization: Bearer`, un token filtrado sigue sirviendo hasta que venza (igual que hoy).

## Fuera de alcance
Refresh tokens, revocación de sesiones en el servidor, recuperación de contraseña, 2FA, y el rediseño visual.
