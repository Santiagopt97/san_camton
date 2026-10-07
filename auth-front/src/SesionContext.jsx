import { createContext, useCallback, useContext, useEffect, useMemo, useState } from 'react'
import * as api from './api.js'

const Contexto = createContext(null)
export const useSesion = () => useContext(Contexto)

// Cada perfil aterriza en su propia pantalla
const RUTAS = { admin: '/admin', recepcion: '/recepcion', huesped: '/huesped' }
export const rutaPorPerfil = (perfil) => RUTAS[perfil] ?? '/login'

// La sesión es una cookie HttpOnly: aquí solo se guarda, en memoria, quién es el usuario (nombre y perfil)
export function SesionProvider({ children }) {
  const [estado, setEstado] = useState({ cargando: true, usuario: null })

  const refrescar = useCallback(async () => {
    try {
      const datos = await api.yo()
      const usuario = datos ? { nombre: datos.nombre, perfil: datos.perfil } : null
      setEstado({ cargando: false, usuario })
      return usuario
    } catch {
      setEstado({ cargando: false, usuario: null })
      return null
    }
  }, [])

  useEffect(() => { refrescar() }, [refrescar])

  const entrar = useCallback(async (email, password) => { await api.login(email, password); return refrescar() }, [refrescar])
  const registrar = useCallback(async (datos) => { await api.registro(datos); return refrescar() }, [refrescar])
  const salir = useCallback(async () => {
    try { await api.salirDelServidor() } catch { /* sin conexión: igual se cierra la sesión en pantalla */ }
    setEstado({ cargando: false, usuario: null })
  }, [])

  const valor = useMemo(() => ({ ...estado, entrar, registrar, salir }), [estado, entrar, registrar, salir])
  return <Contexto.Provider value={valor}>{children}</Contexto.Provider>
}
