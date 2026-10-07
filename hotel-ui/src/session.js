import { CABECERA_CSRF } from './http.js'

// Sesión de los fronts de módulo. El token vive en una cookie HttpOnly que el JavaScript no puede leer:
// aquí solo se guarda, en memoria, { nombre, perfil } que responde auth-api en /me.
// `irA` existe para poder probar la redirección (por defecto cambia la URL del navegador).
export function createSession({ authApiUrl, loginUrl, irA = (url) => { window.location.href = url } }) {
  let usuario = null

  const initSession = async () => {
    try {
      const res = await fetch(`${authApiUrl}/api/auth/me`, { credentials: 'include' })
      if (!res.ok) { usuario = null; return false }
      const datos = await res.json()
      usuario = { nombre: datos.nombre, perfil: datos.perfil }
      return true
    } catch {
      usuario = null
      return false
    }
  }

  const usuarioActual = () => usuario
  const tienePerfil = (...perfiles) => perfiles.includes(usuario?.perfil)

  const logout = async () => {
    usuario = null
    try {
      await fetch(`${authApiUrl}/api/auth/logout`, {
        method: 'POST', credentials: 'include', headers: { [CABECERA_CSRF.nombre]: CABECERA_CSRF.valor },
      })
    } catch { /* sin conexión: igual se vuelve al login */ }
    irA(`${loginUrl}/login`)
  }

  return { initSession, usuarioActual, tienePerfil, logout }
}
