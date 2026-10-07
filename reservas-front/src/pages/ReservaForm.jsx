import { useEffect, useMemo, useState } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import { validar, requerido, entre, maxLen, Alert, Button, Card, Field, FormActions, FormGrid, Input, Select } from 'hotel-ui'
import { reservasApi, cop } from '../api.js'

const vacio = { clienteId: '', habitacionId: '', fechaEntrada: '', fechaSalida: '', huespedes: 1, notas: '' }

const ESQUEMA = {
  clienteId: [requerido('Selecciona un cliente')],
  habitacionId: [requerido('Selecciona una habitación')],
  fechaEntrada: [requerido('Indica la fecha de entrada')],
  fechaSalida: [
    requerido('Indica la fecha de salida'),
    (v, f) => (f.fechaEntrada && v <= f.fechaEntrada ? 'Debe ser posterior a la entrada' : null),
    (v, f) => (f.fechaEntrada && (new Date(v) - new Date(f.fechaEntrada)) / 864e5 > 30 ? 'La estadía máxima es de 30 noches' : null),
  ],
  huespedes: [requerido(), entre(1, 20)],
  notas: [maxLen(500)],
}

export default function ReservaForm() {
  const { id } = useParams()
  const nav = useNavigate()
  const [f, setF] = useState(vacio)
  const [op, setOp] = useState({ clientes: [], habitaciones: [] })
  const [error, setError] = useState('')
  const [saving, setSaving] = useState(false)
  const [intento, setIntento] = useState(false)

  useEffect(() => {
    reservasApi.opciones().then(setOp).catch((e) => setError(e.message))
    if (id) reservasApi.obtener(id).then((r) => setF({ ...vacio, ...r, notas: r.notas ?? '' })).catch((e) => setError(e.message))
  }, [id])

  const set = (k) => (e) => setF({ ...f, [k]: e.target.value })
  const hab = op.habitaciones.find((h) => h.id === f.habitacionId)
  const noches = useMemo(() => {
    if (!f.fechaEntrada || !f.fechaSalida) return 0
    return Math.max(0, Math.round((new Date(f.fechaSalida) - new Date(f.fechaEntrada)) / 864e5))
  }, [f.fechaEntrada, f.fechaSalida])

  const calcular = () => ({
    ...validar(f, ESQUEMA),
    ...(hab && Number(f.huespedes) > hab.capacidad ? { huespedes: `Esta habitación admite máximo ${hab.capacidad}` } : {}),
  })
  const errs = intento ? calcular() : {}

  const guardar = async (e) => {
    e.preventDefault()
    setIntento(true)
    if (Object.keys(calcular()).length) return
    setSaving(true); setError('')
    const body = { clienteId: f.clienteId, habitacionId: f.habitacionId, fechaEntrada: f.fechaEntrada,
      fechaSalida: f.fechaSalida, huespedes: Number(f.huespedes), notas: f.notas || null }
    try { id ? await reservasApi.actualizar(id, body) : await reservasApi.crear(body); nav('/reservas') }
    catch (err) { setError(err.message) } finally { setSaving(false) }
  }

  const optClientes = op.clientes.map((c) => ({ value: c.id, label: `${c.nombre} · ${c.numeroDocumento}` }))
  const optHabs = op.habitaciones.map((h) => ({ value: h.id, label: `${h.numero} · ${h.tipo} · ${cop(h.precioNoche)}` }))

  return (
    <Card className="form">
      <form onSubmit={guardar} noValidate>
        <h2>{id ? 'Editar reserva' : 'Nueva reserva'}</h2>
        <Alert>{error}</Alert>
        <FormGrid>
          <Field label="Cliente" error={errs.clienteId}><Select required value={f.clienteId} onChange={set('clienteId')} placeholder="Selecciona…" options={optClientes} /></Field>
          <Field label="Habitación" error={errs.habitacionId}><Select required value={f.habitacionId} onChange={set('habitacionId')} placeholder="Selecciona…" options={optHabs} /></Field>
          <Field label="Entrada" error={errs.fechaEntrada}><Input type="date" required value={f.fechaEntrada} onChange={set('fechaEntrada')} /></Field>
          <Field label="Salida" error={errs.fechaSalida}><Input type="date" required min={f.fechaEntrada} value={f.fechaSalida} onChange={set('fechaSalida')} /></Field>
          <Field label="Huéspedes" error={errs.huespedes}><Input type="number" min="1" max={hab?.capacidad || 20} required value={f.huespedes} onChange={set('huespedes')} /></Field>
          <Field label="Total estimado"><Input readOnly value={hab && noches ? `${cop(hab.precioNoche * noches)} (${noches} noches)` : '—'} /></Field>
          <Field label="Notas" full error={errs.notas}><Input maxLength="500" value={f.notas} onChange={set('notas')} /></Field>
        </FormGrid>
        <FormActions>
          <Button type="button" variant="ghost" onClick={() => nav('/reservas')}>Cancelar</Button>
          <Button disabled={saving}>{saving ? 'Guardando…' : 'Guardar'}</Button>
        </FormActions>
      </form>
    </Card>
  )
}
