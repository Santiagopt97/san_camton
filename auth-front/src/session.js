const KEY = 'hotel_session'

export const saveSession = (s) => sessionStorage.setItem(KEY, JSON.stringify(s))
export const clearSession = () => sessionStorage.removeItem(KEY)
export function getSession() {
  try {
    const s = JSON.parse(sessionStorage.getItem(KEY))
    if (s && new Date(s.expira) > new Date()) return s
  } catch {}
  clearSession()
  return null
}
// Cada perfil aterriza en su propia pantalla
const RUTAS = { admin: '/admin', recepcion: '/recepcion', huesped: '/huesped' }
export const rutaPorPerfil = (perfil) => RUTAS[perfil] ?? '/login'
