import { useNavigate } from 'react-router-dom'
import { AppShell, Card } from 'hotel-ui'
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
    <AppShell section="Inicio" user={`${usuario.nombre} · ${ETIQUETAS[usuario.perfil]}`} onLogout={salir}>
      <h2>Bienvenido, {usuario.nombre}</h2>
      <div className="modules">
        {MODULOS.filter((m) => m.perfiles.includes(usuario.perfil)).map((m) => (
          <Card as="button" key={m.nombre} className="module" disabled={!m.url} onClick={() => abrir(m.url)}>
            <strong>{m.nombre}</strong><span>{m.desc}</span>
            {!m.url && <small>Próximamente</small>}
          </Card>
        ))}
      </div>
    </AppShell>
  )
}
