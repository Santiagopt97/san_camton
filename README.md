# San Camton

## Nombre del proyecto

**San Camton**

## Integrantes

- [Nombre del integrante 1]
- [Nombre del integrante 2]
- [Nombre del integrante 3]

## Descripcion del proyecto

San Camton es una aplicacion para la administracion de un hotel en Medellin. El proyecto busca centralizar y facilitar la gestion de la informacion y los procesos relacionados con la operacion del hotel, ofreciendo una base organizada para administrar sus servicios y recursos.

## Estructura del repositorio

Monorepo con una carpeta por servicio (front y back independientes, cada uno con su propio proyecto y, mas adelante, su propio `Dockerfile`):

```
san_camton/
├── backend-auth/          # Integrante 1 — API de autenticacion (.NET)
├── backend-clientes/      # Integrante 2 — API de clientes + reportes CSV/PDF
├── backend-habitaciones/  # Integrante 3 — API de habitaciones
├── frontend-auth/         # Integrante 1 — Login, sesion, Home
├── frontend-clientes/     # Integrante 2 — Paginas de clientes
├── frontend-habitaciones/ # Integrante 3 — Interfaz de habitaciones
└── frontend-ui/           # Integrante 3 — Libreria de componentes reutilizables
```

Cada carpeta es un proyecto independiente que se comunica con los demas por HTTP.
