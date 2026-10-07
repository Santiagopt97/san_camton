# Imágenes de habitaciones (carrusel) — Diseño

Fecha: 2026-10-07 · Rama: `feat/imagenes-habitaciones`

## Objetivo
Cada habitación puede tener varias imágenes que se muestran como carrusel, en dos lugares:
el módulo de habitaciones (admin y recepción las ven y las gestionan) y el portal del huésped
(al elegir habitación para reservar). Hoy las habitaciones solo se muestran como texto.

## Decisiones acordadas
- Varias imágenes por habitación (no una sola).
- Carrusel en el portal del huésped y en el módulo de habitaciones. Fuera de alcance: listado de reservas.
- Almacenamiento: Supabase Storage, bucket público `habitaciones` (ya creado: 5 MB, JPG/PNG/WebP).
- La API sube los archivos; el navegador nunca recibe la clave de Supabase.
- Gestionan imágenes (subir, borrar, reordenar): `admin` y `recepcion`. El huésped solo las ve.

## Datos
Tabla nueva `public.habitacion_imagenes` (script `db/habitacion_imagenes.sql`):

| Columna | Tipo | Nota |
|---|---|---|
| id | uuid pk | `gen_random_uuid()` |
| habitacion_id | uuid not null | referencia a `habitaciones(id)` |
| ruta | text not null | ruta del objeto en el bucket (`{habitacion_id}/{uuid}.{ext}`) |
| url | text not null | URL pública completa |
| orden | int not null | 0 = portada |
| creado_en | timestamptz | `now()` |

Índice por `(habitacion_id, orden)`. Se guarda `ruta` además de `url` para poder borrar el objeto del bucket.
Dar de baja una habitación no borra sus imágenes.

## API de habitaciones (`habitaciones-api`)
Roles `admin,recepcion`:
- `POST /api/habitaciones/{id}/imagenes` — multipart, campo `archivo`. Valida y sube al bucket; la imagen queda al final del orden.
- `DELETE /api/habitaciones/{id}/imagenes/{imagenId}` — borra el registro y el objeto del bucket.
- `PUT /api/habitaciones/{id}/imagenes/orden` — recibe la lista ordenada de ids y reasigna `orden`.

Lectura: `GET /api/habitaciones` y `GET /api/habitaciones/{id}` incluyen `imagenes: [{ id, url, orden }]` ordenadas.

Validaciones (también en el front): tipo `image/jpeg|png|webp` verificado por contenido (firma de bytes) y no solo por extensión;
máximo 5 MB; máximo 6 imágenes por habitación (409 al excederlo); habitación inexistente o dada de baja → 404/409.
Errores en JSON `{ "message": ... }`, igual que el resto de la API.

Configuración (`.env`, nunca en git): `SUPABASE_URL`, `SUPABASE_SECRET_KEY`, `SUPABASE_BUCKET=habitaciones` (opcional, por defecto ese valor).
Si faltan, la API falla al arrancar con un mensaje claro, como con las variables de base de datos.
Se actualiza `.env.example` con los nombres, sin valores.

## API de reservas (`reservas-api`)
`GET /api/mis-reservas/disponibles` incluye `imagenes: [{ url }]` por habitación (solo lectura de `habitacion_imagenes`).

## UI
- `hotel-ui`: componente `Carousel` (flechas, puntos, deslizamiento táctil, teclado ← →, sin dependencias nuevas). Sin imágenes muestra una imagen genérica de relleno.
- `habitaciones-front`: carrusel en cada habitación del listado y gestor de imágenes en el formulario de edición (subir, borrar, reordenar, con errores visibles).
- `reservas-front` (`Reservar`): carrusel en cada habitación disponible.

## Pruebas
- API: subir válida (200), tipo inválido (400), mayor de 5 MB (400), séptima imagen (409), borrar, reordenar, rol sin permiso (403), sin token (401).
- Reglas de sesión y permisos existentes no cambian.
- Verificación en navegador del carrusel en ambos fronts.

## Fuera de alcance
Recorte o redimensionado de imágenes, miniaturas, arrastrar y soltar, y imágenes en el listado de reservas.
