import { extraerError, fetchSeguro } from 'hotel-ui'
import { getToken, logout } from './auth.js'

const BASE = import.meta.env.VITE_API_URL
const auth = () => ({ Authorization: `Bearer ${getToken()}` })

async function req(path, options = {}) {
  const res = await fetchSeguro(`${BASE}${path}`, {
    headers: { 'Content-Type': 'application/json', ...auth() },
    ...options,
  })
  if (res.status === 401) return logout()
  if (!res.ok) {
    throw new Error(await extraerError(res))
  }
  return res.status === 204 ? null : res.json()
}

export const clientesApi = {
  listar: ({ q = '', page = 1, size = 10, soloActivos = false }) =>
    req(`/api/clientes?q=${encodeURIComponent(q)}&page=${page}&size=${size}&soloActivos=${soloActivos}`),
  obtener: (id) => req(`/api/clientes/${id}`),
  crear: (data) => req('/api/clientes', { method: 'POST', body: JSON.stringify(data) }),
  actualizar: (id, data) => req(`/api/clientes/${id}`, { method: 'PUT', body: JSON.stringify(data) }),
  desactivar: (id) => req(`/api/clientes/${id}`, { method: 'DELETE' }),
}

export async function descargarReporte(tipo, { q = '', soloActivos = false } = {}) {
  const res = await fetchSeguro(`${BASE}/api/reportes/clientes/${tipo}?q=${encodeURIComponent(q)}&soloActivos=${soloActivos}`, { headers: auth() })
  if (res.status === 401) return logout()
  if (!res.ok) throw new Error('No se pudo generar el reporte')
  const url = URL.createObjectURL(await res.blob())
  const a = document.createElement('a')
  a.href = url
  a.download = `clientes.${tipo}`
  a.click()
  URL.revokeObjectURL(url)
}
