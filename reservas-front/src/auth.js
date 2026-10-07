import { createSession } from 'hotel-ui'

export const { initSession, logout, usuarioActual, tienePerfil } = createSession({
  authApiUrl: import.meta.env.VITE_AUTH_API_URL,
  loginUrl: import.meta.env.VITE_AUTH_URL,
})
