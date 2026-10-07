-- Imágenes de habitaciones (carrusel). Ejecutar después de habitaciones.sql.
-- `ruta` es la ruta del objeto en el bucket de Storage (para poder borrarlo); `url` es la URL pública.
create table if not exists public.habitacion_imagenes (
  id uuid primary key default gen_random_uuid(),
  habitacion_id uuid not null references public.habitaciones(id),
  ruta text not null,
  url text not null,
  orden int not null default 0 check (orden >= 0),
  creado_en timestamptz not null default now()
);
create index if not exists habitacion_imagenes_hab_orden on public.habitacion_imagenes (habitacion_id, orden);

-- La API se conecta como dueño de la tabla (no le afecta); esto evita que la API de datos pública de Supabase la exponga.
alter table public.habitacion_imagenes enable row level security;
