import { Button } from 'hotel-ui'
import { APP_URL } from './api.js'
import { HOTEL } from './contenido.js'

export default function Encabezado() {
  return (
    <header className="ln-encabezado">
      <a className="ln-marca" href="#inicio">{HOTEL.nombre}</a>
      <nav aria-label="Secciones">
        <a href="#habitaciones">Habitaciones</a>
        <a href="#servicios">Servicios</a>
        <a href="#contacto">Contacto</a>
      </nav>
      <div className="ln-acciones">
        <Button as="a" variant="ghost" href={`${APP_URL}/login`}>Iniciar sesión</Button>
        <Button as="a" href={`${APP_URL}/registro`}>Reservar</Button>
      </div>
    </header>
  )
}
