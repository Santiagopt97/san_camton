import { useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { registro } from '../api.js'
import { saveSession, rutaPorPerfil } from '../session.js'
import { validar, requerido, documento, letras, email, telefono, password, maxLen } from '../validators.js'

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

function Campo({ label, error, children }) {
  return (
    <label className={error ? 'has-error' : ''}>{label}{children}
      {error && <small className="field-error" role="alert">{error}</small>}
    </label>
  )
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
      <form className="card login" onSubmit={enviar} noValidate>
        <h1>Crear cuenta</h1>
        <p className="sub">Regístrate para reservar tu estadía</p>
        {error && <p className="error">{error}</p>}
        <Campo label="Tipo de documento">
          <select value={f.tipoDocumento} onChange={set('tipoDocumento')}>
            {['CC', 'CE', 'TI', 'PA'].map((t) => <option key={t}>{t}</option>)}
          </select>
        </Campo>
        <Campo label="Número de documento" error={errs.numeroDocumento}><input value={f.numeroDocumento} onChange={set('numeroDocumento')} /></Campo>
        <Campo label="Nombres" error={errs.nombres}><input value={f.nombres} onChange={set('nombres')} /></Campo>
        <Campo label="Apellidos" error={errs.apellidos}><input value={f.apellidos} onChange={set('apellidos')} /></Campo>
        <Campo label="Correo" error={errs.email}><input type="email" value={f.email} onChange={set('email')} /></Campo>
        <Campo label="Teléfono (opcional)" error={errs.telefono}><input value={f.telefono} onChange={set('telefono')} /></Campo>
        <Campo label="Contraseña (8+ caracteres, mayúscula, minúscula y número)" error={errs.password}><input type="password" value={f.password} onChange={set('password')} /></Campo>
        <Campo label="Confirmar contraseña" error={errs.confirmar}><input type="password" value={f.confirmar} onChange={set('confirmar')} /></Campo>
        <button className="btn" disabled={loading}>{loading ? 'Creando…' : 'Crear cuenta'}</button>
        <p className="sub"><Link to="/login">Ya tengo cuenta</Link></p>
      </form>
    </div>
  )
}
