# Dockerización Etapa 1 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Empaquetar las 4 APIs y los 5 fronts en imágenes Docker y levantarlos con `docker compose up`, con los mismos puertos de `localhost` de hoy.

**Architecture:** Dos Dockerfiles parametrizados (APIs y fronts), contexto en la raíz del repositorio, un `docker-compose.yml` con 9 servicios. Las APIs reciben su configuración con `env_file` apuntando al `.env` que ya existe; los fronts reciben las `VITE_*` como argumento de compilación. Nada de secretos en las imágenes.

**Tech Stack:** Docker 29 / Compose, `mcr.microsoft.com/dotnet/sdk:8.0` y `aspnet:8.0`, `node:22-alpine`, `nginxinc/nginx-unprivileged:alpine`.

**Spec:** `docs/superpowers/specs/2026-10-07-docker-etapa1-design.md`

## Global Constraints

- Contexto de compilación: la raíz del repositorio. Dockerfiles en `docker/`.
- Imágenes etiquetadas `hotel-<servicio>:dev`; servicios: `auth-api`, `clientes-api`, `habitaciones-api`, `reservas-api`, `auth-front`, `clientes-front`, `habitaciones-front`, `reservas-front`, `landing-front`.
- Puertos publicados iguales a los actuales: APIs 5001 a 5004, fronts auth 5173, habitaciones 5174, clientes 5175, reservas 5176, landing 5177; dentro del contenedor siempre 8080.
- Ningún `.env` ni secreto dentro de una imagen; las APIs usan `env_file: ./<api>/.env`; no se crea ningún archivo de secretos nuevo.
- Contenedores sin root. Sin volúmenes, sin `depends_on`.
- Ensamblados: `AuthApi`, `ClientesApi`, `HabitacionesApi`, `ReservasApi` (proyectos `<carpeta>/<Ensamblado>.csproj`, todos referencian `../hotel-security/HotelSecurity.csproj`).
- Commits sin línea de coautoría de Claude (preferencia explícita del usuario). Nunca imprimir valores de los `.env`.

## Review Focus

- Un `.env` colado en una imagen (por `.dockerignore` mal escrito o `COPY . .`): ninguna imagen debe contenerlo (Task 4 lo comprueba en las 9).
- `VITE_*` vacías que anulan los valores por defecto del front: solo se pasan las variables que se definen (Task 2 comprueba que la URL queda grabada en el bundle).
- Rutas de React Router al recargar (`/habitaciones`): nginx debe devolver `index.html`, no 404 (Task 2).
- Un contenedor que arranca pero no está sano (puerto o variable mal): `healthcheck` en los 9 y todos deben quedar `healthy` (Task 3).
- Puertos ocupados por los servidores de desarrollo: comprobar y avisar antes de `up` (Task 3).

---

### Task 1: `.dockerignore` e imagen de las APIs

**Files:**
- Create: `.dockerignore`, `docker/api.Dockerfile`

**Interfaces:**
- Produces: imagen construible con `docker build -f docker/api.Dockerfile --build-arg API_DIR=<carpeta> --build-arg ASSEMBLY=<Ensamblado> -t hotel-<carpeta>:dev .`; escucha en 8080.

- [ ] **Step 1: Crear `.dockerignore`**

```
**/node_modules
**/bin
**/obj
**/dist
.git
.gitignore
.superpowers
docs
db
**/*.Tests
**/.env
**/.env.local
**/.DS_Store
```

(Los `.env.example` no se excluyen.)

- [ ] **Step 2: Crear `docker/api.Dockerfile`**

```dockerfile
# Imagen común de las 4 APIs. Construir desde la raíz del repositorio:
#   docker build -f docker/api.Dockerfile --build-arg API_DIR=auth-api --build-arg ASSEMBLY=AuthApi -t hotel-auth-api:dev .
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG API_DIR
ARG ASSEMBLY
WORKDIR /src
COPY hotel-security/HotelSecurity.csproj hotel-security/
COPY ${API_DIR}/${ASSEMBLY}.csproj ${API_DIR}/
RUN dotnet restore ${API_DIR}/${ASSEMBLY}.csproj
COPY hotel-security/ hotel-security/
COPY ${API_DIR}/ ${API_DIR}/
RUN dotnet publish ${API_DIR}/${ASSEMBLY}.csproj -c Release --no-restore -o /out

FROM mcr.microsoft.com/dotnet/aspnet:8.0
ARG ASSEMBLY
ENV ASSEMBLY=${ASSEMBLY} \
    ASPNETCORE_HTTP_PORTS=8080
WORKDIR /app
COPY --from=build /out .
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["sh", "-c", "exec dotnet ${ASSEMBLY}.dll"]
```

`hotel-security/` tiene carpetas `bin/obj` locales que `.dockerignore` excluye; el `.csproj` de la API no hace globbing de `hotel-security.Tests` (está excluido).

- [ ] **Step 3: Construir las 4 imágenes**

Run (una por una, desde la raíz):
```bash
docker build -f docker/api.Dockerfile --build-arg API_DIR=auth-api --build-arg ASSEMBLY=AuthApi -t hotel-auth-api:dev .
docker build -f docker/api.Dockerfile --build-arg API_DIR=clientes-api --build-arg ASSEMBLY=ClientesApi -t hotel-clientes-api:dev .
docker build -f docker/api.Dockerfile --build-arg API_DIR=habitaciones-api --build-arg ASSEMBLY=HabitacionesApi -t hotel-habitaciones-api:dev .
docker build -f docker/api.Dockerfile --build-arg API_DIR=reservas-api --build-arg ASSEMBLY=ReservasApi -t hotel-reservas-api:dev .
```
Expected: las 4 terminan sin error. Si `habitaciones-api` falla por archivos de `habitaciones-api.Tests` dentro de su carpeta, revisar que `.dockerignore` los excluya.

- [ ] **Step 4: Comprobar que arranca con el `.env` real y no corre como root**

Run:
```bash
docker run -d --name t-auth -p 5001:8080 --env-file auth-api/.env hotel-auth-api:dev
sleep 6; curl -s localhost:5001/health; echo
docker exec t-auth id -u
docker rm -f t-auth
```
Expected: `{"status":"ok","service":"auth-api"}` y un uid distinto de 0 (1654). Si hay un puerto 5001 ocupado por un servidor de desarrollo, detenerlo antes.

- [ ] **Step 5: Commit**

```bash
git add .dockerignore docker/api.Dockerfile
git commit -m "feat(docker): imagen parametrizada de las APIs y .dockerignore"
```

---

### Task 2: Imagen de los fronts

**Files:**
- Create: `docker/front.Dockerfile`, `docker/nginx.conf`

**Interfaces:**
- Produces: `docker build -f docker/front.Dockerfile --build-arg FRONT_DIR=<carpeta> --build-arg "VITE_VARS=VITE_A=x VITE_B=y" -t hotel-<carpeta>:dev .`; sirve el `dist` en 8080.

- [ ] **Step 1: Crear `docker/nginx.conf`** (bloque `server`; va a `/etc/nginx/conf.d/default.conf`)

```nginx
server {
    listen 8080;
    server_name _;
    root /usr/share/nginx/html;
    index index.html;

    gzip on;
    gzip_types text/css application/javascript application/json image/svg+xml;

    add_header X-Content-Type-Options nosniff always;

    location /assets/ {
        add_header Cache-Control "public, max-age=31536000, immutable";
        add_header X-Content-Type-Options nosniff always;
        try_files $uri =404;
    }

    location / {
        add_header Cache-Control "no-cache";
        add_header X-Content-Type-Options nosniff always;
        try_files $uri /index.html;
    }
}
```

- [ ] **Step 2: Crear `docker/front.Dockerfile`**

```dockerfile
# Imagen común de los 5 fronts. Construir desde la raíz del repositorio:
#   docker build -f docker/front.Dockerfile --build-arg FRONT_DIR=landing-front \
#     --build-arg "VITE_VARS=VITE_API_URL=http://localhost:5003 VITE_APP_URL=http://localhost:5173" -t hotel-landing-front:dev .
# VITE_VARS lleva solo las variables que se quieren fijar: una variable vacía anularía el valor por defecto del front.
FROM node:22-alpine AS build
ARG FRONT_DIR
ARG VITE_VARS=""
WORKDIR /src
COPY hotel-ui/package.json hotel-ui/package-lock.json* hotel-ui/
COPY ${FRONT_DIR}/package.json ${FRONT_DIR}/package-lock.json ${FRONT_DIR}/
COPY hotel-ui/ hotel-ui/
WORKDIR /src/${FRONT_DIR}
RUN npm ci
COPY ${FRONT_DIR}/ ./
RUN env ${VITE_VARS} npm run build

FROM nginxinc/nginx-unprivileged:alpine
ARG FRONT_DIR
COPY docker/nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /src/${FRONT_DIR}/dist /usr/share/nginx/html
EXPOSE 8080
```

`ARG FRONT_DIR` se repite en la etapa final porque los argumentos no cruzan etapas.

- [ ] **Step 3: Construir el de la landing y comprobar rutas y URL grabada**

Run:
```bash
docker build -f docker/front.Dockerfile --build-arg FRONT_DIR=landing-front --build-arg "VITE_VARS=VITE_API_URL=http://localhost:5003 VITE_APP_URL=http://localhost:5173" -t hotel-landing-front:dev .
docker run -d --name t-landing -p 5177:8080 hotel-landing-front:dev
sleep 2
curl -s -o /dev/null -w "raiz %{http_code}\n" localhost:5177/
curl -s -o /dev/null -w "ruta-inexistente %{http_code}\n" localhost:5177/una/ruta
curl -s -I localhost:5177/ | grep -i cache-control
docker exec t-landing sh -c 'grep -l "localhost:5003" /usr/share/nginx/html/assets/*.js | wc -l'
docker exec t-landing id -u
docker rm -f t-landing
```
Expected: `raiz 200`, `ruta-inexistente 200` (devuelve `index.html`), `Cache-Control: no-cache`, `1` (URL grabada), uid distinto de 0 (101). Si el paso 3 falla en `npm ci` por `hotel-ui`, comprobar que `hotel-ui/package-lock.json` existe (si no, `npm install` en su lugar solo para `hotel-ui` NO es necesario: el front lo enlaza por `file:`).

- [ ] **Step 4: Construir los otros 4 fronts**

```bash
docker build -f docker/front.Dockerfile --build-arg FRONT_DIR=auth-front --build-arg "VITE_VARS=VITE_API_URL=http://localhost:5001 VITE_CLIENTES_URL=http://localhost:5175 VITE_HABITACIONES_URL=http://localhost:5174 VITE_RESERVAS_URL=http://localhost:5176" -t hotel-auth-front:dev .
docker build -f docker/front.Dockerfile --build-arg FRONT_DIR=clientes-front --build-arg "VITE_VARS=VITE_API_URL=http://localhost:5002 VITE_AUTH_URL=http://localhost:5173 VITE_AUTH_API_URL=http://localhost:5001" -t hotel-clientes-front:dev .
docker build -f docker/front.Dockerfile --build-arg FRONT_DIR=habitaciones-front --build-arg "VITE_VARS=VITE_API_URL=http://localhost:5003 VITE_AUTH_URL=http://localhost:5173 VITE_AUTH_API_URL=http://localhost:5001" -t hotel-habitaciones-front:dev .
docker build -f docker/front.Dockerfile --build-arg FRONT_DIR=reservas-front --build-arg "VITE_VARS=VITE_API_URL=http://localhost:5004 VITE_AUTH_URL=http://localhost:5173 VITE_AUTH_API_URL=http://localhost:5001" -t hotel-reservas-front:dev .
```
Expected: las 4 terminan sin error.

- [ ] **Step 5: Commit**

```bash
git add docker/front.Dockerfile docker/nginx.conf
git commit -m "feat(docker): imagen parametrizada de los fronts con nginx"
```

---

### Task 3: `docker-compose.yml` y arranque de los 9 servicios

**Files:**
- Create: `docker-compose.yml`

**Interfaces:**
- Consumes: Dockerfiles de Task 1 y 2; `<api>/.env` existentes.
- Produces: `docker compose up -d --build` levanta los 9 servicios, todos `healthy`.

- [ ] **Step 1: Crear `docker-compose.yml`**

```yaml
# Etapa 1: 9 contenedores independientes con los mismos puertos de localhost que en desarrollo.
# Requiere los .env de las 4 APIs (a partir de cada .env.example). Ver la sección «Docker» del README.
x-api: &api
  restart: unless-stopped
  healthcheck:
    test: ["CMD", "bash", "-c", "exec 3<>/dev/tcp/127.0.0.1/8080 && printf 'GET /health HTTP/1.0\\r\\n\\r\\n' >&3 && head -n1 <&3 | grep -q 200"]
    interval: 15s
    timeout: 5s
    retries: 5
    start_period: 20s

x-front: &front
  restart: unless-stopped
  healthcheck:
    test: ["CMD", "wget", "-q", "-O", "/dev/null", "http://127.0.0.1:8080/"]
    interval: 15s
    timeout: 5s
    retries: 5

services:
  auth-api:
    <<: *api
    image: hotel-auth-api:dev
    build: { context: ., dockerfile: docker/api.Dockerfile, args: { API_DIR: auth-api, ASSEMBLY: AuthApi } }
    env_file: ./auth-api/.env
    ports: ["5001:8080"]

  clientes-api:
    <<: *api
    image: hotel-clientes-api:dev
    build: { context: ., dockerfile: docker/api.Dockerfile, args: { API_DIR: clientes-api, ASSEMBLY: ClientesApi } }
    env_file: ./clientes-api/.env
    ports: ["5002:8080"]

  habitaciones-api:
    <<: *api
    image: hotel-habitaciones-api:dev
    build: { context: ., dockerfile: docker/api.Dockerfile, args: { API_DIR: habitaciones-api, ASSEMBLY: HabitacionesApi } }
    env_file: ./habitaciones-api/.env
    ports: ["5003:8080"]

  reservas-api:
    <<: *api
    image: hotel-reservas-api:dev
    build: { context: ., dockerfile: docker/api.Dockerfile, args: { API_DIR: reservas-api, ASSEMBLY: ReservasApi } }
    env_file: ./reservas-api/.env
    ports: ["5004:8080"]

  auth-front:
    <<: *front
    image: hotel-auth-front:dev
    build:
      context: .
      dockerfile: docker/front.Dockerfile
      args:
        FRONT_DIR: auth-front
        VITE_VARS: "VITE_API_URL=http://localhost:5001 VITE_CLIENTES_URL=http://localhost:5175 VITE_HABITACIONES_URL=http://localhost:5174 VITE_RESERVAS_URL=http://localhost:5176"
    ports: ["5173:8080"]

  habitaciones-front:
    <<: *front
    image: hotel-habitaciones-front:dev
    build:
      context: .
      dockerfile: docker/front.Dockerfile
      args:
        FRONT_DIR: habitaciones-front
        VITE_VARS: "VITE_API_URL=http://localhost:5003 VITE_AUTH_URL=http://localhost:5173 VITE_AUTH_API_URL=http://localhost:5001"
    ports: ["5174:8080"]

  clientes-front:
    <<: *front
    image: hotel-clientes-front:dev
    build:
      context: .
      dockerfile: docker/front.Dockerfile
      args:
        FRONT_DIR: clientes-front
        VITE_VARS: "VITE_API_URL=http://localhost:5002 VITE_AUTH_URL=http://localhost:5173 VITE_AUTH_API_URL=http://localhost:5001"
    ports: ["5175:8080"]

  reservas-front:
    <<: *front
    image: hotel-reservas-front:dev
    build:
      context: .
      dockerfile: docker/front.Dockerfile
      args:
        FRONT_DIR: reservas-front
        VITE_VARS: "VITE_API_URL=http://localhost:5004 VITE_AUTH_URL=http://localhost:5173 VITE_AUTH_API_URL=http://localhost:5001"
    ports: ["5176:8080"]

  landing-front:
    <<: *front
    image: hotel-landing-front:dev
    build:
      context: .
      dockerfile: docker/front.Dockerfile
      args:
        FRONT_DIR: landing-front
        VITE_VARS: "VITE_API_URL=http://localhost:5003 VITE_APP_URL=http://localhost:5173"
    ports: ["5177:8080"]
```

- [ ] **Step 2: Validar el archivo sin imprimir secretos**

Run: `docker compose config -q && echo OK`
Expected: `OK`. (No usar `docker compose config` sin `-q`: expandiría los `env_file`.)

- [ ] **Step 3: Confirmar que los puertos están libres**

Run: `lsof -nP -iTCP:5001-5004 -iTCP:5173-5177 -sTCP:LISTEN | wc -l`
Expected: `0`. Si no, detener esos procesos (son servidores de desarrollo) antes de seguir.

- [ ] **Step 4: Levantar y esperar a `healthy`**

Run:
```bash
docker compose up -d --build
sleep 40
docker compose ps --format '{{.Service}} {{.Status}}'
```
Expected: los 9 servicios `Up ... (healthy)`. Si una API queda `unhealthy`: `docker compose logs <servicio> --tail 30` (los logs de las APIs no imprimen secretos; revisar igualmente antes de pegarlos).

- [ ] **Step 5: Probar los endpoints desde el anfitrión**

Run:
```bash
for p in 5001 5002 5003 5004; do curl -s localhost:$p/health; echo; done
for p in 5173 5174 5175 5176 5177; do curl -s -o /dev/null -w "$p %{http_code}\n" localhost:$p/; done
curl -s localhost:5003/api/publico/habitaciones | head -c 200; echo
```
Expected: 4 respuestas `status ok`, cinco `200` y un JSON de habitaciones.

- [ ] **Step 6: Commit**

```bash
git add docker-compose.yml
git commit -m "feat(docker): docker-compose con las 4 APIs y los 5 fronts"
```

---

### Task 4: Verificación de seguridad y de extremo a extremo

**Files:** ninguno (verificación; corrige lo que falle en los archivos de Task 1 a 3 y vuelve a ejecutar).

**Interfaces:**
- Consumes: los 9 contenedores `healthy` de Task 3.

- [ ] **Step 1: Ningún contenedor corre como root**

Run:
```bash
for s in auth-api clientes-api habitaciones-api reservas-api auth-front clientes-front habitaciones-front reservas-front landing-front; do echo "$s $(docker compose exec -T $s id -u)"; done
```
Expected: ningún `0`.

- [ ] **Step 2: Ninguna imagen contiene un `.env`**

Run:
```bash
for s in auth-api clientes-api habitaciones-api reservas-api auth-front clientes-front habitaciones-front reservas-front landing-front; do echo "$s: $(docker run --rm --entrypoint sh hotel-$s:dev -c 'find / -xdev -name ".env" -not -path "/proc/*" 2>/dev/null | wc -l')"; done
```
Expected: `0` en las 9.

- [ ] **Step 3: Los secretos no están en las imágenes**

Run (sin imprimir valores; el patrón sale del `.env` en una variable local y solo se muestra el conteo):
```bash
P=$(grep '^SUPABASE_PASSWORD=' auth-api/.env | cut -d= -f2-)
for s in auth-api auth-front landing-front; do echo "$s: $(docker history --no-trunc hotel-$s:dev | grep -c -F -- "$P") $(docker inspect hotel-$s:dev | grep -c -F -- "$P")"; done
```
Expected: `0 0` en cada línea.

- [ ] **Step 4: Extremo a extremo en el navegador (Claude Browser)**

Con los contenedores arriba: abrir `http://localhost:5177` (la landing muestra habitaciones con sus fotos); desde allí «Iniciar sesión» lleva a `http://localhost:5173`; entrar con el administrador de pruebas que el usuario ya conoce (las credenciales de la sesión anterior); navegar a los módulos de habitaciones (5174), clientes (5175) y reservas (5176) y comprobar que cargan datos y que el logout vuelve al login. Subir una imagen de prueba a una habitación desde `5174`, verla en la landing tras ≤60 s o recargando, y borrarla. Verificar que no queda ninguna imagen de prueba ni se tocaron las del usuario.

Expected: todo funciona igual que en desarrollo; sin errores en consola.

- [ ] **Step 5: Recarga en una ruta interna**

Run: `curl -s -o /dev/null -w "%{http_code}\n" localhost:5174/habitaciones`
Expected: `200`.

- [ ] **Step 6: Apagar**

Run: `docker compose down && docker compose ps -q | wc -l`
Expected: `0`. Las imágenes `:dev` se conservan.

- [ ] **Step 7: Commit solo si hubo correcciones**

```bash
git add -A docker docker-compose.yml .dockerignore
git commit -m "fix(docker): correcciones de la verificación"
```

---

### Task 5: Documentación

**Files:**
- Modify: `README.md` (añadir sección «Docker» tras la tabla de puertos)

- [ ] **Step 1: Añadir la sección**

```markdown
## Docker

Todo el sistema (4 APIs y 5 fronts) se puede levantar en contenedores, con los mismos puertos que en desarrollo.

Requisitos: Docker Desktop y los `.env` de las 4 APIs (`auth-api`, `clientes-api`, `habitaciones-api`, `reservas-api`), creados a partir de cada `.env.example`. Detén antes los servidores de desarrollo para liberar los puertos 5001–5004 y 5173–5177.

```bash
docker compose up -d --build   # construye y arranca los 9 servicios
docker compose ps              # deben quedar (healthy)
docker compose logs -f habitaciones-api
docker compose down            # apaga todo
```

La landing queda en http://localhost:5177 y el login en http://localhost:5173. Las APIs leen su `.env` en tiempo de ejecución (si lo cambias, `docker compose up -d` recrea el contenedor); los fronts llevan sus URLs grabadas al construir la imagen, así que apuntan a `localhost`. Ningún `.env` entra en las imágenes.
```

- [ ] **Step 2: Comprobar y commit**

Run: `git diff --stat README.md`
```bash
git add README.md
git commit -m "docs: sección Docker en el README"
```
