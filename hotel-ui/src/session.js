// Manejo de sesión para los fronts de módulo. El token llega desde auth-front en #token=...
export function createSession(loginUrl) {
  const decode = () => {
    try {
      const t = sessionStorage.getItem('token')
      const b64 = t.split('.')[1].replace(/-/g, '+').replace(/_/g, '/')
      const bytes = Uint8Array.from(atob(b64), (c) => c.charCodeAt(0))
      return JSON.parse(new TextDecoder().decode(bytes))
    } catch { return null }
  }
  const vigente = () => { const p = decode(); return !!p && (!p.exp || p.exp * 1000 > Date.now()) }

  const logout = () => {
    sessionStorage.removeItem('token')
    window.location.href = `${loginUrl}/login`
  }
  const initSession = () => {
    const m = window.location.hash.match(/token=([^&]+)/)
    if (m) {
      sessionStorage.setItem('token', decodeURIComponent(m[1]))
      history.replaceState(null, '', window.location.pathname + window.location.search)
    }
    return vigente()
  }
  const getToken = () => (vigente() ? sessionStorage.getItem('token') : null)
  const usuarioActual = () => { const p = decode(); return p ? { nombre: p.name, perfil: p.role } : null }
  const tienePerfil = (...perfiles) => perfiles.includes(usuarioActual()?.perfil)
  return { initSession, getToken, logout, usuarioActual, tienePerfil }
}
