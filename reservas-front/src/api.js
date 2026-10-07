import { extraerError, fetchSeguro } from 'hotel-ui'
import { getToken, logout } from './auth.js'

const BASE = import.meta.env.VITE_API_URL

async function req(path, options = {}) {
  const res = await fetchSeguro(`${BASE}${path}`, {
    headers: { 'Content-Type': 'application/json', Authorization: `Bearer ${getToken()}` },
    ...options,
  })
  if (res.status === 401) return logout()
  if (!res.ok) {
    throw new Error(await extraerError(res))
  }
  return res.status === 204 ? null : res.json()
}

export const reservasApi = {
  listar: ({ q = '', estado = '', page = 1, size = 10 }) =>
    req(`/api/reservas?q=${encodeURIComponent(q)}&estado=${estado}&page=${page}&size=${size}`),
  opciones: () => req('/api/reservas/opciones'),
  obtener: (id) => req(`/api/reservas/${id}`),
  crear: (d) => req('/api/reservas', { method: 'POST', body: JSON.stringify(d) }),
  actualizar: (id, d) => req(`/api/reservas/${id}`, { method: 'PUT', body: JSON.stringify(d) }),
  cambiarEstado: (id, estado) => req(`/api/reservas/${id}/estado`, { method: 'PATCH', body: JSON.stringify({ estado }) }),
}
export const ESTADOS = ['Confirmada', 'Check-in', 'Check-out', 'Cancelada']
export const cop = (n) => new Intl.NumberFormat('es-CO', { style: 'currency', currency: 'COP', maximumFractionDigits: 0 }).format(n)

export const miApi = {
  disponibles: ({ entrada, salida, huespedes }) =>
    req(`/api/mis-reservas/disponibles?entrada=${entrada}&salida=${salida}&huespedes=${huespedes}`),
  listar: () => req('/api/mis-reservas'),
  crear: (d) => req('/api/mis-reservas', { method: 'POST', body: JSON.stringify(d) }),
  cancelar: (id) => req(`/api/mis-reservas/${id}/cancelar`, { method: 'PATCH' }),
}
