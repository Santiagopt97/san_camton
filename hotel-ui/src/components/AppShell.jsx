// Barra superior + contenedor. `nav` recibe los NavLink del front.
export default function AppShell({ section, nav, user, onLogout, children }) {
  return (
    <>
      <header className="topbar">
        <h1>Hotel <span>· {section}</span></h1>
        <nav>{nav}</nav>
        <div className="user">{user} <button className="link" onClick={onLogout}>Salir</button></div>
      </header>
      <main>{children}</main>
    </>
  )
}
