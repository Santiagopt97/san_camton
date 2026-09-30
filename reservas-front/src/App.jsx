import { NavLink, Route, Routes, Navigate } from 'react-router-dom'
import { AppShell } from 'hotel-ui'
import ReservasList from './pages/ReservasList.jsx'
import ReservaForm from './pages/ReservaForm.jsx'
import Reservar from './pages/Reservar.jsx'
import MisReservas from './pages/MisReservas.jsx'
import { logout, usuarioActual } from './auth.js'

export default function App() {
  const u = usuarioActual()

  // Portal del huésped: solo puede buscar, reservar y ver/cancelar lo suyo
  if (u?.perfil === 'huesped') {
    return (
      <AppShell section="Mis reservas" user={u.nombre} onLogout={logout} nav={<>
        <NavLink to="/reservar">Reservar</NavLink>
        <NavLink to="/mis-reservas">Mis reservas</NavLink>
      </>}>
        <Routes>
          <Route path="/reservar" element={<Reservar />} />
          <Route path="/mis-reservas" element={<MisReservas />} />
          <Route path="*" element={<Navigate to="/reservar" replace />} />
        </Routes>
      </AppShell>
    )
  }

  return (
    <AppShell section="Reservas" user={u?.nombre} onLogout={logout} nav={<>
      <NavLink to="/reservas" end>Listado</NavLink>
      <NavLink to="/reservas/nueva">Nueva reserva</NavLink>
    </>}>
      <Routes>
        <Route path="/" element={<Navigate to="/reservas" replace />} />
        <Route path="/reservas" element={<ReservasList />} />
        <Route path="/reservas/nueva" element={<ReservaForm />} />
        <Route path="/reservas/:id" element={<ReservaForm />} />
        <Route path="*" element={<Navigate to="/reservas" replace />} />
      </Routes>
    </AppShell>
  )
}
