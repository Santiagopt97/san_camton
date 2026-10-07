import { createSession } from 'hotel-ui'

export const { initSession, logout, usuarioActual, tienePerfil } = createSession({
  authApiUrl: import.meta.env.VITE_AUTH_API_URL ?? 'http://localhost:5001',
  loginUrl: import.meta.env.VITE_AUTH_URL,
})
