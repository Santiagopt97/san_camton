import { Button } from 'hotel-ui'
import { APP_URL } from './api.js'
import { HOTEL } from './contenido.js'

export default function Portada() {
  return (
    <section id="inicio" className="ln-portada">
      <h1>{HOTEL.nombre} · {HOTEL.eslogan}</h1>
      <p>{HOTEL.descripcion}</p>
      <Button as="a" href={`${APP_URL}/registro`}>Reservar ahora</Button>
    </section>
  )
}
