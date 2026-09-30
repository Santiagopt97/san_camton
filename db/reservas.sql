create table if not exists public.reservas (
  id uuid primary key default gen_random_uuid(),
  cliente_id uuid not null references public.clientes(id),
  habitacion_id uuid not null references public.habitaciones(id),
  fecha_entrada date not null,
  fecha_salida date not null,
  huespedes int not null default 1 check (huespedes > 0),
  total numeric(12,2) not null default 0,
  estado text not null default 'Confirmada'
    check (estado in ('Confirmada','Check-in','Check-out','Cancelada')),
  notas text,
  creado_en timestamptz not null default now(),
  check (fecha_salida > fecha_entrada)
);
create index if not exists reservas_hab_fechas on public.reservas (habitacion_id, fecha_entrada, fecha_salida);
