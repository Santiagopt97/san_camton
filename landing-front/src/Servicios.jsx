import { Card } from 'hotel-ui'
import { SERVICIOS } from './contenido.js'

export default function Servicios() {
  return (
    <section id="servicios" className="ln-seccion" aria-labelledby="titulo-servicios">
      <h2 id="titulo-servicios">Servicios</h2>
      <div className="ln-servicios">
        {SERVICIOS.map((s) => (
          <Card as="article" key={s.titulo}>
            <h3>{s.titulo}</h3>
            <p>{s.texto}</p>
          </Card>
        ))}
      </div>
    </section>
  )
}
