-- Perfil "huesped": clientes que reservan por su cuenta. Ejecutar después de clientes.sql
alter table public.usuarios drop constraint if exists usuarios_perfil_check;
alter table public.usuarios add constraint usuarios_perfil_check
  check (perfil in ('admin','recepcion','huesped'));
alter table public.usuarios add column if not exists cliente_id uuid references public.clientes(id);
create unique index if not exists usuarios_cliente_unico on public.usuarios(cliente_id) where cliente_id is not null;
