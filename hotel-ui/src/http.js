import { extraerError, fetchSeguro } from './validators.js'

// Como la sesión viaja en una cookie, las peticiones que modifican datos llevan esta cabecera (las APIs la exigen)
export const CABECERA_CSRF = { nombre: 'X-Requested-With', valor: 'hotel-ui' }

const LECTURAS = ['GET', 'HEAD', 'OPTIONS']
const modifica = (metodo = 'GET') => !LECTURAS.includes(metodo.toUpperCase())

// Cliente HTTP para una API: manda la cookie (credentials) y la cabecera anti-CSRF al modificar datos.
// `req` devuelve el JSON (null en 204) y lanza Error(mensaje) si la respuesta no es correcta.
// Con `onNoAutorizado`, un 401 lo llama (cerrar sesión) y devuelve null; sin él, el 401 se trata como cualquier error (login).
export function crearCliente({ base, onNoAutorizado }) {
  const fetchConSesion = (ruta, opciones = {}) => {
    const headers = {
      ...(modifica(opciones.method) ? { [CABECERA_CSRF.nombre]: CABECERA_CSRF.valor } : {}),
      ...opciones.headers,
    }
    return fetchSeguro(`${base}${ruta}`, { ...opciones, credentials: 'include', headers })
  }

  async function req(ruta, opciones = {}) {
    // Con FormData el navegador pone el Content-Type (multipart + boundary)
    const headers = { ...(opciones.body instanceof FormData ? {} : { 'Content-Type': 'application/json' }), ...opciones.headers }
    const res = await fetchConSesion(ruta, { ...opciones, headers })
    if (res.status === 401 && onNoAutorizado) { onNoAutorizado(); return null }
    if (!res.ok) throw new Error(await extraerError(res))
    return res.status === 204 ? null : res.json()
  }

  return { req, fetchConSesion }
}
