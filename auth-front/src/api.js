import { fetchSeguro } from 'hotel-ui'

const BASE = import.meta.env.VITE_API_URL

export async function login(email, password) {
  const res = await fetchSeguro(`${BASE}/api/auth/login`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ email, password }),
  })
  const body = await res.json().catch(() => ({}))
  if (!res.ok) throw new Error(body.message || 'No se pudo iniciar sesión')
  return body
}

export async function registro(datos) {
  const res = await fetchSeguro(`${BASE}/api/auth/registro`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(datos),
  })
  const body = await res.json().catch(() => ({}))
  if (!res.ok) {
    const primero = body.errors && Object.values(body.errors)[0]?.[0]
    throw new Error(body.message || primero || 'No se pudo crear la cuenta')
  }
  return body
}
