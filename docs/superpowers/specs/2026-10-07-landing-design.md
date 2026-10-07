# Landing page pública (`landing-front`) — Diseño

Fecha: 2026-10-07 · Rama: `feat/landing` (sale de `develop`, que ya incluye imágenes y sesión por cookie)

## Objetivo
Una página pública de presentación del hotel, como proyecto aparte, que muestre las habitaciones reales (fotos y precios) y lleve al
visitante a registrarse o iniciar sesión en la aplicación. Ocupará la raíz `/` del sitio cuando haya un único punto de entrada (Docker/Kubernetes).

## Estado actual (verificado en el código)
- `auth-front` es hoy la puerta de entrada: sus rutas son `/login`, `/registro` y todo lo demás redirige a `/login`.
- Ninguna lectura de habitaciones es pública: `GET /api/habitaciones` exige `admin` o `recepcion` y `mis-reservas/disponibles` exige `huesped`.
- Las habitaciones tienen imágenes (`habitacion_imagenes`) y el `Carousel` y `SIN_FOTO` están en `hotel-ui`.
- `habitaciones-api` solo acepta el origen del front de habitaciones en CORS (`CORS_ORIGINS`, por defecto `http://localhost:5174`).

## Decisiones acordadas
- Proyecto aparte: `landing-front` (Vite + React, puerto 5177), estático, que reutiliza el tema y el `Carousel` de `hotel-ui`.
- La landing muestra habitaciones reales con fotos y precios, mediante un endpoint público mínimo.
- Una tarjeta **por tipo de habitación** (no por habitación), solo de habitaciones activas.
- Exposición mínima: nunca número de habitación, piso, estado, descripción ni ids internos.
- Una sola página con anclas, sin router. No pide sesión.
- Textos, servicios y datos de contacto en un único archivo editable (`src/contenido.js`) con redacción genérica y el nombre «Hotel».

## Diseño

### 1. Endpoint público en `habitaciones-api`
`GET /api/publico/habitaciones`, `[AllowAnonymous]`, solo lectura. Respuesta (lista ordenada por `precioDesde` ascendente y luego por `tipo`):

```json
[ { "tipo": "Doble", "capacidad": 2, "precioDesde": 180000, "imagenes": [ { "url": "https://..." } ] } ]
```

- Agrupa las habitaciones **activas** por `tipo`. `capacidad` es la máxima del grupo; `precioDesde` es el menor `precio_noche`.
- `imagenes`: las de las habitaciones del grupo, tomando primero la habitación más barata y, dentro de cada una, por `orden`; máximo 6 en total.
  Un tipo sin imágenes devuelve `imagenes: []` (la landing muestra «Sin foto»).
- No se filtra por `estado` (una habitación ocupada hoy sigue siendo parte de la oferta). Las habitaciones dadas de baja no aparecen.
- La respuesta lleva `Cache-Control: public, max-age=60`.
- Es una lectura (GET): no necesita la cabecera anti-CSRF ni cookie. No hay límite de peticiones (queda fuera de alcance; la caché de 60 s lo mitiga).
- CORS: el endpoint público tiene su propia política (`publico`): acepta el origen de la landing (`CORS_ORIGINS_PUBLICO`, por defecto `http://localhost:5177`), solo `GET` y sin credenciales.
  El CORS general de `habitaciones-api` (con sesión) sigue siendo únicamente el del front de habitaciones, para que la landing no pueda tocar las rutas con sesión (mínimo privilegio).
- La respuesta varía por `Origin` (`Vary: Origin`) y la lista se guarda en memoria en el servidor 60 s, de modo que la base se consulta como mucho una vez por minuto.

### 2. `landing-front` (puerto 5177)
- Estructura: `index.html` (con `lang="es"`, título, descripción y etiquetas Open Graph), `src/main.jsx`, `App.jsx`, `contenido.js`, `api.js`, `formato.js`
  (precio en COP), secciones `Encabezado`, `Portada`, `Habitaciones`, `Servicios`, `Contacto`, `Pie`, y `styles.css` con clases propias (prefijo `ln-`) sobre el tema de `hotel-ui`.
- `Encabezado`: nombre del hotel, anclas a las secciones y dos botones: **Iniciar sesión** (`{VITE_APP_URL}/login`) y **Reservar** (`{VITE_APP_URL}/registro`).
- `Portada`: título, subtítulo y botón **Reservar**.
- `Habitaciones`: pide el endpoint público y dibuja una tarjeta por tipo con `Carousel` (con `SIN_FOTO` de relleno), tipo, «Hasta N personas» y «Desde $X / noche».
  Estados: cargando (`Spinner`), error («No pudimos cargar las habitaciones. Intenta de nuevo más tarde.») y vacío («Próximamente»).
- `Servicios` y `Contacto` salen de `contenido.js`; el pie lleva el año actual.
- Variables: `VITE_API_URL` (por defecto `http://localhost:5003`, la de `habitaciones-api`) y `VITE_APP_URL` (por defecto `http://localhost:5173`, `auth-front`).
  Se hace `fetch` sin credenciales (no hay sesión).
- Accesible y adaptable al móvil: marcas de región (`header`, `main`, `section`, `footer`), textos alternativos en las imágenes y diseño de una columna en pantallas estrechas.

### 3. Configuración y documentación
- README: puerto 5177 en la tabla de módulos y cómo arrancarla; `.env.example` de `landing-front`.
- Cuando llegue Docker: una imagen nginx estática, y en el punto de entrada único la landing ocupará `/`.

## Pruebas
- Backend (xUnit, `habitaciones-api.Tests`): solo habitaciones activas; agrupación por tipo con el precio mínimo y la capacidad máxima; orden por precio; máximo 6 imágenes y orden de las imágenes;
  tipo sin imágenes devuelve lista vacía; **la respuesta no contiene** número, piso, estado, descripción ni ids; responde 200 sin credenciales con la API arrancada; lleva `Cache-Control`;
  el preflight del origen de la landing se acepta.
- Landing (Vitest + React Testing Library, desde el inicio del proyecto): formato de precio; sección de habitaciones en carga, error, vacío y con datos (tarjetas, «Desde $…»);
  los botones apuntan a `/login` y `/registro` de la aplicación.
- Verificación real: levantar los servicios, ver la landing con las fotos reales, comprobar que el endpoint no devuelve campos sensibles y entrar por **Reservar** al registro.

## Riesgos y límites
- Endpoint público sin límite de peticiones: el contenido es mínimo y la lista se calcula como mucho una vez por minuto (caché en memoria del servidor); un límite por IP queda para más adelante.
- El precio «desde» es el menor del tipo; si las habitaciones de un tipo tienen precios distintos, la tarjeta no los detalla.
- La URL pública de cada imagen incluye en su ruta el id de la habitación (`.../habitaciones/{id}/{archivo}`, así se guardan en el bucket). El id no se expone como campo, pero sí es visible dentro de la URL; es un GUID aleatorio y las rutas autenticadas siguen exigiendo sesión.
- Los datos de contacto y servicios son texto de ejemplo hasta que se reemplacen en `contenido.js`.
- SEO: al ser una aplicación de una sola página, el contenido de habitaciones se pinta en el navegador; el resto del texto sí está en el HTML inicial solo si se prerrenderiza (fuera de alcance).

## Fuera de alcance
Reserva desde la landing (se hace dentro de la aplicación), blog, multilenguaje, formulario de contacto con envío, prerenderizado para SEO, límite de peticiones y analítica.
