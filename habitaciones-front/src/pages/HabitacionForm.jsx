import { useEffect, useState } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import { validar, requerido, patron, entre, maxLen, Alert, Button, Card, Checkbox, Field, FormActions, FormGrid, Input, Select } from 'hotel-ui'
import { habitacionesApi, ESTADOS, TIPOS } from '../api.js'

const vacio = { numero: '', tipo: 'Sencilla', piso: 1, capacidad: 1, precioNoche: 0, estado: 'Disponible', descripcion: '', activo: true }

const ESQUEMA = {
  numero: [requerido(), patron(/^[A-Za-z0-9-]{1,10}$/, 'Solo letras, números y guion (máx. 10)')],
  piso: [requerido(), entre(0, 50, 'El piso debe estar entre 0 y 50')],
  capacidad: [requerido(), entre(1, 10, 'La capacidad debe estar entre 1 y 10')],
  precioNoche: [requerido(), entre(1000, 50000000, 'El precio debe estar entre $1.000 y $50.000.000')],
  descripcion: [maxLen(500)],
}

export default function HabitacionForm() {
  const { id } = useParams()
  const nav = useNavigate()
  const [f, setF] = useState(vacio)
  const [error, setError] = useState('')
  const [saving, setSaving] = useState(false)
  const [intento, setIntento] = useState(false)
  const errs = intento ? validar(f, ESQUEMA) : {}

  useEffect(() => {
    if (!id) return setF(vacio)
    habitacionesApi.obtener(id)
      .then((h) => setF({ ...vacio, ...Object.fromEntries(Object.entries(h).map(([k, v]) => [k, v ?? ''])) }))
      .catch((e) => setError(e.message))
  }, [id])

  const set = (k) => (e) => setF({ ...f, [k]: e.target.type === 'checkbox' ? e.target.checked : e.target.value })

  const guardar = async (e) => {
    e.preventDefault()
    setIntento(true)
    if (Object.keys(validar(f, ESQUEMA)).length) return
    setSaving(true); setError('')
    const body = {
      ...f, piso: Number(f.piso), capacidad: Number(f.capacidad), precioNoche: Number(f.precioNoche),
      descripcion: f.descripcion || null,
    }
    delete body.id; delete body.creadoEn
    try {
      id ? await habitacionesApi.actualizar(id, body) : await habitacionesApi.crear(body)
      nav('/habitaciones')
    } catch (err) { setError(err.message) } finally { setSaving(false) }
  }

  return (
    <Card className="form">
      <form onSubmit={guardar} noValidate>
        <h2>{id ? 'Editar habitación' : 'Nueva habitación'}</h2>
        <Alert>{error}</Alert>
        <FormGrid>
          <Field label="Número" error={errs.numero}><Input required maxLength="10" value={f.numero} onChange={set('numero')} /></Field>
          <Field label="Tipo"><Select value={f.tipo} onChange={set('tipo')} options={TIPOS} /></Field>
          <Field label="Piso" error={errs.piso}><Input type="number" min="0" required value={f.piso} onChange={set('piso')} /></Field>
          <Field label="Capacidad (personas)" error={errs.capacidad}><Input type="number" min="1" required value={f.capacidad} onChange={set('capacidad')} /></Field>
          <Field label="Precio por noche (COP)" error={errs.precioNoche}><Input type="number" min="0" step="1000" required value={f.precioNoche} onChange={set('precioNoche')} /></Field>
          <Field label="Estado"><Select value={f.estado} onChange={set('estado')} options={ESTADOS} /></Field>
          <Field label="Descripción" full error={errs.descripcion}><Input maxLength="500" value={f.descripcion} onChange={set('descripcion')} /></Field>
          <Checkbox full label="Habitación activa" checked={f.activo} onChange={set('activo')} />
        </FormGrid>
        <FormActions>
          <Button type="button" variant="ghost" onClick={() => nav('/habitaciones')}>Cancelar</Button>
          <Button disabled={saving}>{saving ? 'Guardando…' : 'Guardar'}</Button>
        </FormActions>
      </form>
    </Card>
  )
}
