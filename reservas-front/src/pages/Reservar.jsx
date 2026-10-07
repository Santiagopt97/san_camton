import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { validar, requerido, entre, hoyLocal, Alert, Button, Card, Carousel, ConfirmModal, Field, FormActions, FormGrid, Input, SIN_FOTO, Spinner } from 'hotel-ui'
import { miApi, cop } from '../api.js'

const hoy = hoyLocal
const ESQUEMA = {
  entrada: [requerido('Indica la fecha de entrada'), (v) => (v < hoyLocal() ? 'No puede ser una fecha pasada' : null)],
  salida: [
    requerido('Indica la fecha de salida'),
    (v, f) => (f.entrada && v <= f.entrada ? 'Debe ser posterior a la entrada' : null),
    (v, f) => (f.entrada && (new Date(v) - new Date(f.entrada)) / 864e5 > 30 ? 'La estadía máxima es de 30 noches' : null),
  ],
  huespedes: [requerido(), entre(1, 20, 'Entre 1 y 20 huéspedes')],
}

export default function Reservar() {
  const nav = useNavigate()
  const [f, setF] = useState({ entrada: '', salida: '', huespedes: 1, notas: '' })
  const [libres, setLibres] = useState(null)
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState('')
  const [elegida, setElegida] = useState(null)
  const [intento, setIntento] = useState(false)
  const errs = intento ? validar(f, ESQUEMA) : {}
  const set = (k) => (e) => setF({ ...f, [k]: e.target.value })

  const buscar = async (e) => {
    e.preventDefault()
    setIntento(true)
    if (Object.keys(validar(f, ESQUEMA)).length) return
    setLoading(true); setError(''); setLibres(null)
    try { setLibres(await miApi.disponibles({ ...f, huespedes: Number(f.huespedes) })) }
    catch (err) { setError(err.message) } finally { setLoading(false) }
  }
  const confirmar = async () => {
    try {
      await miApi.crear({ habitacionId: elegida.id, fechaEntrada: f.entrada, fechaSalida: f.salida,
        huespedes: Number(f.huespedes), notas: f.notas || null })
      nav('/mis-reservas')
    } catch (err) { setError(err.message); setElegida(null) }
  }

  return (
    <>
      <Card className="form">
        <form onSubmit={buscar} noValidate>
          <h2>Reserva tu estadía</h2>
          <Alert>{error}</Alert>
          <FormGrid>
            <Field label="Entrada" error={errs.entrada}><Input type="date" required min={hoy()} value={f.entrada} onChange={set('entrada')} /></Field>
            <Field label="Salida" error={errs.salida}><Input type="date" required min={f.entrada || hoy()} value={f.salida} onChange={set('salida')} /></Field>
            <Field label="Huéspedes" error={errs.huespedes}><Input type="number" min="1" max="20" required value={f.huespedes} onChange={set('huespedes')} /></Field>
            <Field label="Notas (opcional)"><Input maxLength="500" value={f.notas} onChange={set('notas')} /></Field>
          </FormGrid>
          <FormActions><Button disabled={loading}>Buscar disponibilidad</Button></FormActions>
        </form>
      </Card>

      {loading && <Spinner />}
      {libres && (
        <div className="rooms">
          {libres.length === 0 && <Card>No hay habitaciones disponibles para esas fechas. Prueba con otras.</Card>}
          {libres.map((h) => (
            <Card key={h.id} className="room">
              <Carousel images={h.imagenes} alt={`Habitación ${h.numero}`} fallback={SIN_FOTO} />
              <h3>Habitación {h.numero} · {h.tipo}</h3>
              <p>Hasta {h.capacidad} personas</p>
              <p><strong>{cop(h.precioNoche)}</strong> / noche</p>
              <p className="total">{h.noches} noche(s): <strong>{cop(h.total)}</strong></p>
              <Button onClick={() => setElegida(h)}>Reservar</Button>
            </Card>
          ))}
        </div>
      )}
      <ConfirmModal open={!!elegida} title="Confirmar reserva" confirmText="Confirmar reserva"
        message={elegida && `Habitación ${elegida.numero} (${elegida.tipo}) del ${f.entrada} al ${f.salida} por ${cop(elegida.total)}.`}
        onConfirm={confirmar} onCancel={() => setElegida(null)} />
    </>
  )
}
