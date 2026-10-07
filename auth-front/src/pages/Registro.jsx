import { useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import {
  Alert, Button, Card, Field, Input, Select,
  validar, requerido, documento, letras, email, telefono, password, maxLen,
} from 'hotel-ui'
import { registro } from '../api.js'
import { saveSession, rutaPorPerfil } from '../session.js'

const vacio = { tipoDocumento: 'CC', numeroDocumento: '', nombres: '', apellidos: '', email: '', telefono: '', password: '', confirmar: '' }
const ESQUEMA = {
  numeroDocumento: [requerido(), documento()],
  nombres: [requerido(), letras, maxLen(80)],
  apellidos: [requerido(), letras, maxLen(80)],
  email: [requerido(), email, maxLen(120)],
  telefono: [telefono],
  password: [requerido(), password],
  confirmar: [requerido('Confirma tu contraseña'), (v, f) => (v !== f.password ? 'Las contraseñas no coinciden' : null)],
}

export default function Registro() {
  const nav = useNavigate()
  const [f, setF] = useState(vacio)
  const [error, setError] = useState('')
  const [loading, setLoading] = useState(false)
  const [intento, setIntento] = useState(false)
  const errs = intento ? validar(f, ESQUEMA) : {}
  const set = (k) => (e) => setF({ ...f, [k]: e.target.value })

  const enviar = async (e) => {
    e.preventDefault()
    setIntento(true)
    if (Object.keys(validar(f, ESQUEMA)).length) return
    setLoading(true); setError('')
    try {
      const { confirmar, ...datos } = f
      const s = await registro({ ...datos, email: f.email.trim(), telefono: f.telefono || null })
      saveSession(s)
      nav(rutaPorPerfil(s.usuario.perfil), { replace: true })
    } catch (err) { setError(err.message) } finally { setLoading(false) }
  }

  return (
    <div className="login-wrap">
      <Card as="form" className="login" onSubmit={enviar} noValidate>
        <h1>Crear cuenta</h1>
        <p className="sub">Regístrate para reservar tu estadía</p>
        <Alert>{error}</Alert>
        <Field label="Tipo de documento">
          <Select value={f.tipoDocumento} onChange={set('tipoDocumento')} options={['CC', 'CE', 'TI', 'PA']} />
        </Field>
        <Field label="Número de documento" error={errs.numeroDocumento}><Input value={f.numeroDocumento} onChange={set('numeroDocumento')} /></Field>
        <Field label="Nombres" error={errs.nombres}><Input value={f.nombres} onChange={set('nombres')} /></Field>
        <Field label="Apellidos" error={errs.apellidos}><Input value={f.apellidos} onChange={set('apellidos')} /></Field>
        <Field label="Correo" error={errs.email}><Input type="email" value={f.email} onChange={set('email')} /></Field>
        <Field label="Teléfono (opcional)" error={errs.telefono}><Input value={f.telefono} onChange={set('telefono')} /></Field>
        <Field label="Contraseña (8+ caracteres, mayúscula, minúscula y número)" error={errs.password}><Input type="password" value={f.password} onChange={set('password')} /></Field>
        <Field label="Confirmar contraseña" error={errs.confirmar}><Input type="password" value={f.confirmar} onChange={set('confirmar')} /></Field>
        <Button disabled={loading}>{loading ? 'Creando…' : 'Crear cuenta'}</Button>
        <p className="sub"><Link to="/login">Ya tengo cuenta</Link></p>
      </Card>
    </div>
  )
}
