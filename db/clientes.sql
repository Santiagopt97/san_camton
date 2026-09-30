create table if not exists public.clientes (
  id uuid primary key default gen_random_uuid(),
  tipo_documento text not null default 'CC',
  numero_documento text not null unique,
  nombres text not null,
  apellidos text not null,
  email text,
  telefono text,
  nacionalidad text,
  fecha_nacimiento date,
  direccion text,
  activo boolean not null default true,
  creado_en timestamptz not null default now()
);
