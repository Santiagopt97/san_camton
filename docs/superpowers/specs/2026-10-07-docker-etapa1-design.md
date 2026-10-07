# Dockerización, Etapa 1 — Diseño

Fecha: 2026-10-07 · Rama: `feat/docker` (sale de `develop`, que ya incluye imágenes, sesión por cookie y landing)

## Objetivo
Empaquetar las 4 APIs y los 5 fronts en imágenes Docker y arrancarlos todos con `docker compose up`, sin cambiar el comportamiento de la
aplicación y sin copiar credenciales. Es el primer paso para aprender Kubernetes: cada imagen será luego un Deployment.

## Estado actual (verificado en el código)
- No hay ningún archivo de Docker en el repositorio. Docker Desktop (29.7) está instalado y funcionando.
- 4 APIs ASP.NET Core 8 (`AuthApi`, `ClientesApi`, `HabitacionesApi`, `ReservasApi`) con puertos 5001 a 5004; dependen del proyecto compartido
  `hotel-security` por referencia a una carpeta hermana. Leen su configuración de variables de entorno (`DotNetEnv.Load()` solo añade las del `.env` si existe).
- 5 fronts Vite + React (`auth-front`, `clientes-front`, `habitaciones-front`, `reservas-front`, `landing-front`) con puertos 5173 a 5177;
  dependen de `hotel-ui` con `"hotel-ui": "file:../hotel-ui"`. Sus variables `VITE_*` se graban al compilar.
- Cada API tiene su `.env` local (ignorado por git) con `SUPABASE_*`, `JWT_*` y `CORS_ORIGINS`; `habitaciones-api` además `SUPABASE_URL` y `SUPABASE_SECRET_KEY`.
  Ninguno contiene `$` ni comillas. La base de datos y el almacenamiento están en Supabase (fuera de Docker).
- Todas las APIs tienen `GET /health`.

## Decisiones acordadas
- Etapa 1: nueve contenedores independientes, con los mismos puertos de `localhost` de hoy (el navegador llama a cada servicio por su puerto).
- Solo dos Dockerfiles parametrizados (uno para las APIs, otro para los fronts), no nueve.
- Las APIs usan el `.env` que ya tienen mediante `env_file` en `docker-compose.yml`: no se crea ni se copia ningún archivo de secretos.
- Ningún `.env` ni secreto entra en una imagen.
- Las imágenes no corren como root.

## Diseño

### 1. `docker/api.Dockerfile`
Argumentos: `API_DIR` (carpeta, p. ej. `auth-api`) y `ASSEMBLY` (nombre del proyecto, p. ej. `AuthApi`). Contexto de compilación: la raíz del repositorio.
- Etapa `build` (`mcr.microsoft.com/dotnet/sdk:8.0`): copia `hotel-security/` y el `.csproj` de la API, hace `dotnet restore` (capa en caché),
  copia el código y ejecuta `dotnet publish -c Release --no-restore -o /out` del proyecto de la API.
- Etapa final (`mcr.microsoft.com/dotnet/aspnet:8.0`): copia `/out`, corre como el usuario no root `app` (`USER $APP_UID`), escucha en el puerto 8080
  y arranca con `dotnet ${ASSEMBLY}.dll`.

### 2. `docker/front.Dockerfile` y `docker/nginx.conf`
Argumentos: `FRONT_DIR` (carpeta del front) y las variables `VITE_*` que ese front necesite. Contexto: la raíz.
- Etapa `build` (`node:22-alpine`): copia `hotel-ui/` y los `package.json`/`package-lock.json` del front, hace `npm ci`, copia el código y ejecuta `vite build`
  con las `VITE_*` recibidas como argumentos (los `.env` locales quedan fuera del contexto, así que mandan solo los argumentos).
- Etapa final (`nginxinc/nginx-unprivileged:alpine`): copia `dist/` y `docker/nginx.conf`, escucha en el puerto 8080 sin ser root.
- `nginx.conf`: `try_files $uri /index.html` (React Router), caché larga para `/assets/` (archivos con hash), sin caché para `index.html`,
  compresión gzip y la cabecera `X-Content-Type-Options: nosniff`.

### 3. `.dockerignore` (raíz)
Excluye `**/node_modules`, `**/bin`, `**/obj`, `**/dist`, `.git`, `docs`, `.superpowers`, las carpetas `*.Tests` y, sobre todo, `**/.env` (se conservan los `.env.example`).

### 4. `docker-compose.yml` (raíz)
Nueve servicios con imágenes etiquetadas `hotel-<servicio>:dev`:
- APIs (`auth-api`, `clientes-api`, `habitaciones-api`, `reservas-api`): `build` con `api.Dockerfile` y sus argumentos, `env_file: ./<api>/.env`,
  puertos `5001`..`5004` hacia el `8080`, y `healthcheck` sobre `/health` con `bash` (`/dev/tcp`), porque la imagen de ASP.NET no trae `curl`.
- Fronts (`auth-front`, `clientes-front`, `habitaciones-front`, `reservas-front`, `landing-front`): `build` con `front.Dockerfile` y las `VITE_*` con las URLs de `localhost`
  (las mismas de cada `.env.example`), puertos `5173`..`5177` hacia el `8080`, y `healthcheck` con `wget`.
- Sin volúmenes (todo es sin estado) y sin `depends_on` (los servicios no se llaman entre sí desde dentro de Docker).

### 5. Documentación
README: sección «Docker» con requisitos (los `.env` de las 4 APIs deben existir), `docker compose up -d --build`, las URLs, `docker compose logs`, `docker compose down`,
y que hay que detener antes los servidores de desarrollo para liberar los puertos.

## Pruebas y verificación
Esta pieza no tiene lógica de aplicación, así que se verifica ejecutándola:
- `docker compose build` compila las 9 imágenes.
- `docker compose up -d`: los 9 contenedores quedan `healthy`; `/health` responde en las 4 APIs y cada front responde 200.
- Ningún contenedor corre como root (`docker inspect` y `id` dentro del contenedor).
- **Ninguna imagen contiene un `.env`** (se lista el sistema de archivos de cada imagen).
- Los secretos no aparecen en `docker history` ni en `docker inspect` de las imágenes (solo en el contenedor de ejecución, vía `env_file`).
- De extremo a extremo en el navegador, con todo en contenedores: la landing con tus fotos, el login, los tres módulos, y una subida y borrado de una imagen de prueba
  (se borra al terminar).
- `docker compose down` deja todo apagado.

## Riesgos y límites
- Las `VITE_*` se graban al compilar: las imágenes quedan atadas a las URLs de `localhost`. La Etapa 2 las sustituye por configuración en tiempo de ejecución.
- `env_file` lee el archivo tal cual: un `$` en un valor se interpretaría y las comillas se conservarían; hoy ningún `.env` los tiene. Cambiar un `.env` exige
  recrear el contenedor (`docker compose up -d`).
- Los `.env` están ignorados por git: quien clone el repositorio debe crearlos a partir de los `.env.example` antes de levantar las APIs.
- La cookie de sesión sigue sin `Secure` (HTTP en `localhost`); se resuelve en la Etapa 2 con el punto de entrada único.
- Imágenes solo locales (`:dev`), sin registro ni versionado; construcción para la arquitectura de esta máquina.
- Las pruebas no se ejecutan dentro de Docker (cuando exista integración continua se añadirán).

## Fuera de alcance
Etapa 2 (punto de entrada único, configuración en tiempo de ejecución, cookie `Secure` detrás de un proxy), registro de imágenes, integración continua,
manifiestos de Kubernetes, HTTPS, volúmenes y construcción multiarquitectura.
