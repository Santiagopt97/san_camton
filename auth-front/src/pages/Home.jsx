import { useNavigate } from 'react-router-dom'
import { clearSession, getSession } from '../session.js'

const CLIENTES = import.meta.env.VITE_CLIENTES_URL
const HABITACIONES = import.meta.env.VITE_HABITACIONES_URL
const RESERVAS = import.meta.env.VITE_RESERVAS_URL

const ETIQUETAS = { admin: 'Administrador', recepcion: 'Recepción', huesped: 'Huésped' }

// Módulos visibles por perfil
const MODULOS = [
  { nombre: 'Clientes', desc: 'Registro, consulta y reportes CSV/PDF', url: CLIENTES, perfiles: ['admin', 'recepcion'] },
  { nombre: 'Habitaciones', desc: 'Disponibilidad y gestión de habitaciones', url: HABITACIONES, perfiles: ['admin', 'recepcion'] },
  { nombre: 'Reservas', desc: 'Reservas, check-in y check-out', url: RESERVAS, perfiles: ['admin', 'recepcion'] },
  { nombre: 'Mis reservas', desc: 'Reserva tu estadía y consulta tus reservas', url: RESERVAS, perfiles: ['huesped'] },
  { nombre: 'Usuarios', desc: 'Administración de cuentas y perfiles', url: null, perfiles: ['admin'] },
]

export default function Home() {
  const nav = useNavigate()
  const { usuario, token } = getSession()
  const salir = () => { clearSession(); nav('/login', { replace: true }) }
  // El token viaja en el fragmento (#) para que no llegue a ningún servidor ni log
  const abrir = (url) => { window.location.href = `${url}/#token=${encodeURIComponent(token)}` }

  return (
    <>
      <header className="topbar">
        <h1>Hotel</h1>
        <div>
          <span>{usuario.nombre} · <em>{ETIQUETAS[usuario.perfil]}</em></span>
          <button className="btn ghost light" onClick={salir}>Salir</button>
        </div>
      </header>
      <main>
        <h2>Bienvenido, {usuario.nombre}</h2>
        <div className="modules">
          {MODULOS.filter((m) => m.perfiles.includes(usuario.perfil)).map((m) => (
            <button key={m.nombre} className="card module" disabled={!m.url} onClick={() => abrir(m.url)}>
              <strong>{m.nombre}</strong><span>{m.desc}</span>
              {!m.url && <small>Próximamente</small>}
            </button>
          ))}
        </div>
      </main>
    </>
  )
}
