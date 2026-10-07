import { NavLink, Route, Routes, Navigate } from 'react-router-dom'
import { AppShell } from 'hotel-ui'
import ClientesList from './pages/ClientesList.jsx'
import ClienteForm from './pages/ClienteForm.jsx'
import { logout, usuarioActual } from './auth.js'

export default function App() {
  const u = usuarioActual()
  return (
    <AppShell section="Clientes" user={u?.nombre} onLogout={logout} nav={<>
      <NavLink to="/clientes" end>Listado</NavLink>
      <NavLink to="/clientes/nuevo">Nuevo cliente</NavLink>
    </>}>
      <Routes>
        <Route path="/" element={<Navigate to="/clientes" replace />} />
        <Route path="/clientes" element={<ClientesList />} />
        <Route path="/clientes/nuevo" element={<ClienteForm />} />
        <Route path="/clientes/:id" element={<ClienteForm />} />
      </Routes>
    </AppShell>
  )
}
