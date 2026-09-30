create table if not exists public.usuarios (
  id uuid primary key default gen_random_uuid(),
  email text not null unique,
  password_hash text not null,
  nombre text not null,
  perfil text not null check (perfil in ('admin','recepcion')),
  activo boolean not null default true
);
-- Los usuarios de prueba se crean solos al iniciar auth-api (ver Program.cs).
