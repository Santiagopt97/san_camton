import { NavLink, Route, Routes, Navigate } from 'react-router-dom'
import { AppShell } from 'hotel-ui'
import HabitacionesList from './pages/HabitacionesList.jsx'
import HabitacionForm from './pages/HabitacionForm.jsx'
import { logout, usuarioActual } from './auth.js'

export default function App() {
  const u = usuarioActual()
  return (
    <AppShell section="Habitaciones" user={u?.nombre} onLogout={logout} nav={<>
      <NavLink to="/habitaciones" end>Listado</NavLink>
      <NavLink to="/habitaciones/nueva">Nueva habitación</NavLink>
    </>}>
      <Routes>
        <Route path="/" element={<Navigate to="/habitaciones" replace />} />
        <Route path="/habitaciones" element={<HabitacionesList perfil={u?.perfil} />} />
        <Route path="/habitaciones/nueva" element={<HabitacionForm />} />
        <Route path="/habitaciones/:id" element={<HabitacionForm />} />
      </Routes>
    </AppShell>
  )
}
