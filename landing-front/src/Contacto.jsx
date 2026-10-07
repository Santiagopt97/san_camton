import { CONTACTO } from './contenido.js'

export default function Contacto() {
  return (
    <section id="contacto" className="ln-seccion" aria-labelledby="titulo-contacto">
      <h2 id="titulo-contacto">Contacto</h2>
      <div className="ln-contacto">
        <p>{CONTACTO.direccion}</p>
        <p><a href={`tel:${CONTACTO.telefono.replace(/\s/g, '')}`}>{CONTACTO.telefono}</a></p>
        <p><a href={`mailto:${CONTACTO.correo}`}>{CONTACTO.correo}</a></p>
        <p>{CONTACTO.horario}</p>
      </div>
    </section>
  )
}
