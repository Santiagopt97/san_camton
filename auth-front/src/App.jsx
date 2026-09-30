import { Navigate, Route, Routes } from 'react-router-dom'
import Login from './pages/Login.jsx'
import Home from './pages/Home.jsx'
import Registro from './pages/Registro.jsx'
import { getSession, rutaPorPerfil } from './session.js'

// Protección de rutas: exige sesión y, si se indica, un perfil concreto
function Protegida({ perfil, children }) {
  const s = getSession()
  if (!s) return <Navigate to="/login" replace />
  if (perfil && s.usuario.perfil !== perfil) return <Navigate to={rutaPorPerfil(s.usuario.perfil)} replace />
  return children
}

export default function App() {
  const s = getSession()
  return (
    <Routes>
      <Route path="/login" element={s ? <Navigate to={rutaPorPerfil(s.usuario.perfil)} replace /> : <Login />} />
      <Route path="/registro" element={s ? <Navigate to={rutaPorPerfil(s.usuario.perfil)} replace /> : <Registro />} />
      <Route path="/admin" element={<Protegida perfil="admin"><Home /></Protegida>} />
      <Route path="/recepcion" element={<Protegida perfil="recepcion"><Home /></Protegida>} />
      <Route path="/huesped" element={<Protegida perfil="huesped"><Home /></Protegida>} />
      <Route path="*" element={<Navigate to="/login" replace />} />
    </Routes>
  )
}
