# Sistema Hotel — arquitectura por módulos (HTTP)

| Módulo | Proyecto | Puerto |
|---|---|---|
| Auth (Integrante 1) | `auth-api` / `auth-front` | 5001 / 5173 |
| Clientes + Reportes (Integrante 2) | `clientes-api` / `clientes-front` | 5002 / 5175 |
| Habitaciones (Integrante 3) | `habitaciones-api` / `habitaciones-front` | 5003 / 5174 |
| Reservas (cuarto módulo) | `reservas-api` / `reservas-front` | 5004 / 5176 |
| Landing pública | `landing-front` (usa `GET /api/publico/habitaciones` de `habitaciones-api`) | — / 5177 |

## Puesta en marcha

### 1. Base de datos
En Supabase → SQL Editor: ejecutar `db/usuarios.sql`, `db/clientes.sql` y `db/habitaciones.sql` y `db/habitacion_imagenes.sql` y `db/reservas.sql` y `db/huespedes.sql` y, al final, `db/validaciones.sql` (en ese orden, reservas depende de las otras dos).

### 2. Variables de entorno (.env)
Cada API lee su configuración de un archivo `.env`, que **no se sube a git** (ver `.gitignore`).

En `auth-api/`, `clientes-api/`, `habitaciones-api/` y `reservas-api/`:
```bash
cp .env.example .env
```
Editar el `.env` y completar:
- `SUPABASE_HOST`, `SUPABASE_USER`, `SUPABASE_PASSWORD` → de Supabase → Project Settings → Database →
  Connection string → modo **Transaction pooler**.
- `JWT_KEY` → una clave larga y aleatoria. **Debe ser idéntica** en `auth-api/.env` y `clientes-api/.env`
  (y en el `.env` de cualquier otro backend que se agregue, como Habitaciones).
- `CORS_ORIGINS` → ya viene con el puerto correcto de cada front, normalmente no hay que tocarlo.

Si `SUPABASE_HOST`/`USER`/`PASSWORD` o `JWT_KEY` faltan, el backend lanza un error claro al arrancar
en vez de fallar en silencio.

### Imágenes de habitaciones (Supabase Storage)
1. En Supabase → Storage → New bucket: nombre `habitaciones`, con **Public bucket** activado (límite 5 MB; tipos `image/jpeg`, `image/png`, `image/webp`).
2. En `habitaciones-api/.env` agregar `SUPABASE_URL` (Settings → API), `SUPABASE_SECRET_KEY` (Settings → API Keys → secret key; **solo backend, nunca en el front ni en git**) y, si el bucket se llama distinto, `SUPABASE_BUCKET`.
3. Sin `SUPABASE_URL` o `SUPABASE_SECRET_KEY`, `habitaciones-api` no arranca y lo dice con un mensaje claro.
4. Pruebas del backend de habitaciones: `dotnet test habitaciones-api.Tests`.

### 3. Backends
```bash
cd auth-api && dotnet restore && dotnet run --urls http://localhost:5001
cd clientes-api && dotnet restore && dotnet run --urls http://localhost:5002
cd habitaciones-api && dotnet restore && dotnet run --urls http://localhost:5003
cd reservas-api && dotnet restore && dotnet run --urls http://localhost:5004
```

### 4. Fronts
Antes, en cada front: `cp .env.example .env` (define las URLs de las APIs). En los fronts de módulo, `VITE_AUTH_API_URL` es la URL de `auth-api` (por defecto `http://localhost:5001`); en `auth-front`, `VITE_API_URL` ya apunta a `auth-api`.
```bash
cd auth-front && npm install && npm run dev
cd clientes-front && npm install && npm run dev
cd habitaciones-front && npm install && npm run dev
cd reservas-front && npm install && npm run dev
cd landing-front && npm install && npm run dev
```

**Landing pública** (http://localhost:5177): muestra las habitaciones activas por tipo, con fotos y precio «desde», y lleva a `/login` y `/registro` de `auth-front`. Lee `GET /api/publico/habitaciones`, un endpoint anónimo que solo devuelve tipo, capacidad, precio desde e imágenes (nunca números de habitación, estados ni ids). Los textos, servicios y datos de contacto se editan en `landing-front/src/contenido.js`; las variables (`VITE_API_URL`, `VITE_APP_URL`) están en su `.env.example`. `habitaciones-api` debe aceptar el origen de la landing en `CORS_ORIGINS` (ver su `.env.example`). Pruebas: `cd landing-front && npm test`.

### 5. Probar
Abrir http://localhost:5173 → login → Home → módulo Clientes.

Usuarios de prueba (se crean solos si la tabla `usuarios` está vacía):
`admin@hotel.com / Admin123*` y `recepcion@hotel.com / Recep123*`.

## Flujo de sesión (cookie HttpOnly)
Login → `auth-api` valida con BCrypt, firma un JWT (solo `sub`, `role` y, si es huésped, `cliente_id`) y lo pone en la cookie
`hotel_token` (`HttpOnly`, `SameSite=Lax`, `Secure` con HTTPS); responde **204 sin cuerpo**. El navegador no puede leer la cookie ni
guarda nada en `sessionStorage`. Los fronts preguntan quién es el usuario a `GET /api/auth/me`, que devuelve solo `{ nombre, perfil }`,
y lo guardan únicamente en memoria. Los módulos no necesitan que se les pase ningún token: la cookie viaja sola a cada API, que valida
firma, issuer y audience con la misma `JWT_KEY`. Sin sesión o con un 401, el front vuelve al login. Salir llama a `POST /api/auth/logout`.

- **CSRF:** las peticiones POST, PUT, PATCH y DELETE deben traer `X-Requested-With: hotel-ui` (lo añade `hotel-ui`); si falta, 400.
  Las peticiones con `Authorization: Bearer` (Swagger, Postman) no la necesitan, y las APIs siguen aceptándolas.
- **Mismo host:** la cookie se comparte entre puertos porque todo corre en `localhost`. En otro entorno, fronts y APIs deben compartir host
  o dominio.
- **CORS:** `CORS_ORIGINS` de `auth-api` debe listar los 4 fronts (ver `auth-api/.env.example`).
- **Cambio incompatible:** las sesiones abiertas antes de este cambio dejan de valer; hay que volver a iniciar sesión.

## Habitaciones (Integrante 3)
- API: `GET/POST /api/habitaciones`, `GET/PUT /api/habitaciones/{id}`, `PATCH /api/habitaciones/{id}/estado`,
  `GET /api/habitaciones/resumen`, `DELETE /api/habitaciones/{id}` (baja lógica, solo `admin`). Swagger en `:5003/swagger`.
- Estados: Disponible, Ocupada, Limpieza, Mantenimiento.
- Front: listado con filtros, resumen por estado, cambio rápido de estado y formulario crear/editar.
- Pendiente del Parcial 1: sustituir los estilos propios por la librería de UI y extraer componentes reutilizables.

## Reservas (cuarto módulo)
`GET/POST /api/reservas`, `GET/PUT /api/reservas/{id}`, `PATCH /api/reservas/{id}/estado`, `GET /api/reservas/opciones`.
Valida fechas, capacidad, cliente/habitación activos y cruces de fechas (409 si la habitación ya está reservada); calcula el total por noches.

## Parcial 1 — estado
| Requisito | Dónde |
|---|---|
| Login, sesión con vencimiento, redirección por perfil, rutas protegidas, Home por perfil | `auth-front` (`session.js`, `App.jsx`, `Home.jsx`) |
| Sesión y perfil en los módulos (token con `exp`, `tienePerfil`) | `hotel-ui/src/session.js` |
| 2 páginas de clientes (listado y formulario) + CRUD + CSV/PDF | `clientes-front`, `clientes-api` |
| Librería de UI y componentes reutilizables (botón, tabla, formulario, modal, paginador…) | `hotel-ui`, usada por clientes, habitaciones y reservas |
| Fronts y backends conectados por HTTP | ver tabla de puertos |

`hotel-ui` se resuelve con un alias de Vite (`hotel-ui` → `../hotel-ui/src`), no requiere instalar nada extra.

## Portal del huésped (reservas por su cuenta)
- Perfil nuevo `huesped`: se registra en `auth-front` (`/registro`) y queda enlazado a su ficha de cliente (`cliente_id` en el token).
  Si recepción ya lo había registrado, el correo debe coincidir con el de la ficha.
- `reservas-api`: `GET /api/mis-reservas/disponibles`, `GET/POST /api/mis-reservas`, `PATCH /api/mis-reservas/{id}/cancelar`.
  El cliente siempre sale del token; solo ve y cancela lo suyo (cancelación solo si está Confirmada y antes del día de entrada).
- `reservas-front`: si el perfil es huésped ve "Reservar" y "Mis reservas"; recepción/admin ven el listado de siempre.

## Validaciones
- **Frontend** (`hotel-ui/src/validators.js`, copia en `auth-front`): validación por campo con mensajes bajo cada input tras el primer intento de envío
  (obligatorios, formato de documento según tipo, solo letras en nombres, correo, teléfono, fechas, rangos, contraseña fuerte y confirmación).
- **Backend** (DataAnnotations + `IValidatableObject` + reglas de negocio): mismas reglas que el front, sin confiar en él. Errores en JSON `{ "message": ... }`.
- **Negocio:** documento y correo únicos; CC solo mayores de edad; no se desactiva un cliente/habitación con reservas vigentes;
  reservas sin cruces, máx. 30 noches, capacidad respetada, huésped sin fechas pasadas ni a más de 1 año;
  flujo de estados Confirmada → Check-in → Check-out (o Cancelada), con actualización automática del estado de la habitación.
- **Seguridad:** límite de 10 intentos por minuto en login y registro (429), contraseña con mayúscula/minúscula/número, manejador global de errores.
- **Base de datos** (`db/validaciones.sql`): restricción que impide reservas cruzadas incluso con solicitudes simultáneas, límites en habitaciones y correo único.
