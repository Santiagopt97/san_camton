import { extraerError, fetchSeguro } from 'hotel-ui'
import { getToken, logout } from './auth.js'

const BASE = import.meta.env.VITE_API_URL
const auth = () => ({ Authorization: `Bearer ${getToken()}` })

async function req(path, options = {}) {
  const res = await fetchSeguro(`${BASE}${path}`, {
    // con FormData el navegador pone el Content-Type (multipart + boundary)
    headers: { ...(options.body instanceof FormData ? {} : { 'Content-Type': 'application/json' }), ...auth() },
    ...options,
  })
  if (res.status === 401) return logout()
  if (!res.ok) {
    throw new Error(await extraerError(res))
  }
  return res.status === 204 ? null : res.json()
}

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
