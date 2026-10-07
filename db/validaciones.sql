-- Reglas a nivel de base de datos (última línea de defensa). Ejecutar al final, después de reservas.sql y huespedes.sql.
create extension if not exists btree_gist;

-- Una habitación no puede tener dos reservas vigentes que se crucen en fechas
alter table public.reservas drop constraint if exists reservas_no_cruce;
alter table public.reservas add constraint reservas_no_cruce exclude using gist (
  habitacion_id with =,
  daterange(fecha_entrada, fecha_salida, '[)') with &&
) where (estado in ('Confirmada','Check-in'));

-- Estadía máxima de 30 noches
alter table public.reservas drop constraint if exists reservas_max_noches;
alter table public.reservas add constraint reservas_max_noches check (fecha_salida - fecha_entrada <= 30);

-- Valores razonables en habitaciones
alter table public.habitaciones drop constraint if exists habitaciones_valores_check;
alter table public.habitaciones add constraint habitaciones_valores_check
  check (piso between 0 and 50 and capacidad between 1 and 10 and precio_noche > 0);

-- Correo de cliente único (sin distinguir mayúsculas)
create unique index if not exists clientes_email_unico on public.clientes (lower(email)) where email is not null;
