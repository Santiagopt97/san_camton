import { crearCliente } from 'hotel-ui'
import { logout } from './auth.js'

const { req } = crearCliente({ base: import.meta.env.VITE_API_URL, onNoAutorizado: logout })

export const habitacionesApi = {
  listar: ({ q = '', estado = '', tipo = '', page = 1, size = 12, soloActivas = false }) =>
    req(`/api/habitaciones?q=${encodeURIComponent(q)}&estado=${estado}&tipo=${tipo}&page=${page}&size=${size}&soloActivas=${soloActivas}`),
  resumen: () => req('/api/habitaciones/resumen'),
  obtener: (id) => req(`/api/habitaciones/${id}`),
  crear: (data) => req('/api/habitaciones', { method: 'POST', body: JSON.stringify(data) }),
  actualizar: (id, data) => req(`/api/habitaciones/${id}`, { method: 'PUT', body: JSON.stringify(data) }),
  cambiarEstado: (id, estado) => req(`/api/habitaciones/${id}/estado`, { method: 'PATCH', body: JSON.stringify({ estado }) }),
  desactivar: (id) => req(`/api/habitaciones/${id}`, { method: 'DELETE' }),
  subirImagen: (id, archivo) => {
    const datos = new FormData()
    datos.append('archivo', archivo)
    return req(`/api/habitaciones/${id}/imagenes`, { method: 'POST', body: datos })
  },
  borrarImagen: (id, imagenId) => req(`/api/habitaciones/${id}/imagenes/${imagenId}`, { method: 'DELETE' }),
  reordenarImagenes: (id, ids) => req(`/api/habitaciones/${id}/imagenes/orden`, { method: 'PUT', body: JSON.stringify({ ids }) }),
}

export const ESTADOS = ['Disponible', 'Ocupada', 'Limpieza', 'Mantenimiento']
export const TIPOS = ['Sencilla', 'Doble', 'Familiar', 'Suite']
