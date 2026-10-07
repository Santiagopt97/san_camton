import Button from './Button.jsx'

// Barra superior + contenedor. `nav` recibe los NavLink del front.
export default function AppShell({ section, nav, user, onLogout, children }) {
  return (
    <>
      <header className="topbar">
        <h1>Hotel <span>· {section}</span></h1>
        <nav>{nav}</nav>
        <div className="user">{user} <Button variant="link" onClick={onLogout}>Salir</Button></div>
      </header>
      <main>{children}</main>
    </>
  )
}
