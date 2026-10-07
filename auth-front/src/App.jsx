import { Navigate, Route, Routes } from 'react-router-dom'
import { Spinner } from 'hotel-ui'
import Login from './pages/Login.jsx'
import Home from './pages/Home.jsx'
import Registro from './pages/Registro.jsx'
import { rutaPorPerfil, useSesion } from './SesionContext.jsx'

// Protección de rutas: exige sesión y, si se indica, un perfil concreto. Se evalúa en cada render (la sesión es reactiva).
function Protegida({ perfil, children }) {
  const { usuario } = useSesion()
  if (!usuario) return <Navigate to="/login" replace />
  if (perfil && usuario.perfil !== perfil) return <Navigate to={rutaPorPerfil(usuario.perfil)} replace />
  return children
}

// Login y registro: si ya hay sesión, se va a la pantalla del perfil
function Publica({ children }) {
  const { usuario } = useSesion()
  return usuario ? <Navigate to={rutaPorPerfil(usuario.perfil)} replace /> : children
}

export default function App() {
  const { cargando } = useSesion()
  if (cargando) return <div className="login-wrap"><Spinner /></div>
  return (
    <Routes>
      <Route path="/login" element={<Publica><Login /></Publica>} />
      <Route path="/registro" element={<Publica><Registro /></Publica>} />
      <Route path="/admin" element={<Protegida perfil="admin"><Home /></Protegida>} />
      <Route path="/recepcion" element={<Protegida perfil="recepcion"><Home /></Protegida>} />
      <Route path="/huesped" element={<Protegida perfil="huesped"><Home /></Protegida>} />
      <Route path="*" element={<Navigate to="/login" replace />} />
    </Routes>
  )
}
