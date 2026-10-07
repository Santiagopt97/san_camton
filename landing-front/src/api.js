import { extraerError, fetchSeguro } from 'hotel-ui'

const BASE = import.meta.env.VITE_API_URL ?? 'http://localhost:5003'
// A donde llevan los botones «Iniciar sesión» y «Reservar» (auth-front)
export const APP_URL = import.meta.env.VITE_APP_URL ?? 'http://localhost:5173'

// Lectura pública: sin credenciales (no hay sesión en la landing)
export async function listarHabitaciones() {
  const res = await fetchSeguro(`${BASE}/api/publico/habitaciones`)
  if (!res.ok) throw new Error(await extraerError(res))
  return res.json()
}
