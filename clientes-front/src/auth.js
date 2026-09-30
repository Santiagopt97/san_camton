import { createSession } from 'hotel-ui'

export const { initSession, getToken, logout, usuarioActual, tienePerfil } = createSession(import.meta.env.VITE_AUTH_URL)
