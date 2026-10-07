create table if not exists public.habitaciones (
  id uuid primary key default gen_random_uuid(),
  numero text not null unique,
  tipo text not null default 'Sencilla',
  piso int not null default 1,
  capacidad int not null default 1 check (capacidad > 0),
  precio_noche numeric(12,2) not null default 0 check (precio_noche >= 0),
  estado text not null default 'Disponible'
    check (estado in ('Disponible','Ocupada','Limpieza','Mantenimiento')),
  descripcion text,
  activo boolean not null default true,
  creado_en timestamptz not null default now()
);

insert into public.habitaciones (numero, tipo, piso, capacidad, precio_noche, estado)
select * from (values
  ('101','Sencilla',1,1,120000,'Disponible'),
  ('102','Doble',1,2,180000,'Disponible'),
  ('201','Doble',2,2,190000,'Ocupada'),
  ('202','Familiar',2,4,260000,'Limpieza'),
  ('301','Suite',3,3,420000,'Disponible')
) as v(numero,tipo,piso,capacidad,precio_noche,estado)
where not exists (select 1 from public.habitaciones);
