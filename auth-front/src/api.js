import { crearCliente } from 'hotel-ui'

// Sin onNoAutorizado: un 401 en el login es un error normal («Correo o contraseña incorrectos»)
const { req } = crearCliente({ base: import.meta.env.VITE_API_URL })

export const login = (email, password) => req('/api/auth/login', { method: 'POST', body: JSON.stringify({ email, password }) })
export const registro = (datos) => req('/api/auth/registro', { method: 'POST', body: JSON.stringify(datos) })
export const yo = () => req('/api/auth/me')
export const salirDelServidor = () => req('/api/auth/logout', { method: 'POST' })
