import { useEffect, useState } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import { validar, requerido, documento, letras, email, telefono, maxLen, fechaNoFutura, edadMinimaSiTipo, Alert, Button, Card, Checkbox, Field, FormActions, FormGrid, Input, Select } from 'hotel-ui'
import { clientesApi } from '../api.js'

const vacio = {
  tipoDocumento: 'CC', numeroDocumento: '', nombres: '', apellidos: '', email: '',
  telefono: '', nacionalidad: '', fechaNacimiento: '', direccion: '', activo: true,
}

const ESQUEMA = {
  tipoDocumento: [requerido()],
  numeroDocumento: [requerido(), documento()],
  nombres: [requerido(), letras, maxLen(80)],
  apellidos: [requerido(), letras, maxLen(80)],
  email: [email, maxLen(120)],
  telefono: [telefono],
  nacionalidad: [letras, maxLen(60)],
  fechaNacimiento: [fechaNoFutura, edadMinimaSiTipo('CC', 18)],
  direccion: [maxLen(200)],
}

export default function ClienteForm() {
  const { id } = useParams()
  const nav = useNavigate()
  const [f, setF] = useState(vacio)
  const [error, setError] = useState('')
  const [saving, setSaving] = useState(false)
  const [intento, setIntento] = useState(false)
  const errs = intento ? validar(f, ESQUEMA) : {}

  useEffect(() => {
    if (!id) return setF(vacio)
    clientesApi.obtener(id)
      .then((c) => setF({ ...vacio, ...Object.fromEntries(Object.entries(c).map(([k, v]) => [k, v ?? ''])) }))
      .catch((e) => setError(e.message))
  }, [id])

  const set = (k) => (e) => setF({ ...f, [k]: e.target.type === 'checkbox' ? e.target.checked : e.target.value })

  const guardar = async (e) => {
    e.preventDefault()
    setIntento(true)
    if (Object.keys(validar(f, ESQUEMA)).length) return
    setSaving(true); setError('')
    const body = {
      ...f,
      email: f.email || null, telefono: f.telefono || null, nacionalidad: f.nacionalidad || null,
      direccion: f.direccion || null, fechaNacimiento: f.fechaNacimiento || null,
    }
    delete body.id; delete body.creadoEn
    try {
      id ? await clientesApi.actualizar(id, body) : await clientesApi.crear(body)
      nav('/clientes')
    } catch (err) { setError(err.message) } finally { setSaving(false) }
  }

  return (
    <Card className="form">
      <form onSubmit={guardar} noValidate>
        <h2>{id ? 'Editar cliente' : 'Nuevo cliente'}</h2>
        <Alert>{error}</Alert>
        <FormGrid>
          <Field label="Tipo de documento" error={errs.tipoDocumento}>
            <Select value={f.tipoDocumento} onChange={set('tipoDocumento')} options={['CC', 'CE', 'TI', 'PA', 'NIT']} />
          </Field>
          <Field label="Número de documento" error={errs.numeroDocumento}><Input required value={f.numeroDocumento} onChange={set('numeroDocumento')} /></Field>
          <Field label="Nombres" error={errs.nombres}><Input required value={f.nombres} onChange={set('nombres')} /></Field>
          <Field label="Apellidos" error={errs.apellidos}><Input required value={f.apellidos} onChange={set('apellidos')} /></Field>
          <Field label="Email" error={errs.email}><Input type="email" value={f.email} onChange={set('email')} /></Field>
          <Field label="Teléfono" error={errs.telefono}><Input value={f.telefono} onChange={set('telefono')} /></Field>
          <Field label="Nacionalidad" error={errs.nacionalidad}><Input value={f.nacionalidad} onChange={set('nacionalidad')} /></Field>
          <Field label="Fecha de nacimiento" error={errs.fechaNacimiento}><Input type="date" value={f.fechaNacimiento} onChange={set('fechaNacimiento')} /></Field>
          <Field label="Dirección" full error={errs.direccion}><Input value={f.direccion} onChange={set('direccion')} /></Field>
          <Checkbox full label="Cliente activo" checked={f.activo} onChange={set('activo')} />
        </FormGrid>
        <FormActions>
          <Button type="button" variant="ghost" onClick={() => nav('/clientes')}>Cancelar</Button>
          <Button disabled={saving}>{saving ? 'Guardando…' : 'Guardar'}</Button>
        </FormActions>
      </form>
    </Card>
  )
}
