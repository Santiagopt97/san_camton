import { useEffect, useState } from 'react'
import { Alert, Card, Carousel, SIN_FOTO, Spinner } from 'hotel-ui'
import { listarHabitaciones } from './api.js'
import { cop } from './formato.js'

export default function Habitaciones() {
  const [estado, setEstado] = useState({ cargando: true, error: false, tipos: [] })

  useEffect(() => {
    let vivo = true
    listarHabitaciones()
      .then((tipos) => { if (vivo) setEstado({ cargando: false, error: false, tipos }) })
      .catch(() => { if (vivo) setEstado({ cargando: false, error: true, tipos: [] }) })
    return () => { vivo = false }
  }, [])

  return (
    <section id="habitaciones" className="ln-seccion" aria-labelledby="titulo-habitaciones">
      <h2 id="titulo-habitaciones">Nuestras habitaciones</h2>
      {estado.cargando && <Spinner />}
      {estado.error && <Alert>No pudimos cargar las habitaciones. Intenta de nuevo más tarde.</Alert>}
      {!estado.cargando && !estado.error && estado.tipos.length === 0 && <p className="ln-vacio">Próximamente</p>}
      <div className="ln-habitaciones">
        {estado.tipos.map((t) => (
          <Card as="article" key={t.tipo} className="ln-habitacion">
            <Carousel images={t.imagenes} alt={`Habitación ${t.tipo}`} fallback={SIN_FOTO} />
            <h3>{t.tipo}</h3>
            <p>Hasta {t.capacidad} {t.capacidad === 1 ? 'persona' : 'personas'}</p>
            <p className="ln-precio">Desde <strong>{cop(t.precioDesde)}</strong> / noche</p>
          </Card>
        ))}
      </div>
    </section>
  )
}
