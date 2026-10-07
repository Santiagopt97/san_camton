import { useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { Alert, Button, Card, Field, Input } from 'hotel-ui'
import { rutaPorPerfil, useSesion } from '../SesionContext.jsx'

export default function Login() {
  const nav = useNavigate()
  const { entrar } = useSesion()
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [error, setError] = useState('')
  const [loading, setLoading] = useState(false)

  const enviar = async (e) => {
    e.preventDefault()
    setLoading(true); setError('')
    try {
      const usuario = await entrar(email.trim(), password)
      if (!usuario) throw new Error('No se pudo iniciar la sesión. Revisa que el navegador permita las cookies.')
      nav(rutaPorPerfil(usuario.perfil), { replace: true })
    } catch (err) { setError(err.message) } finally { setLoading(false) }
  }

  return (
    <div className="login-wrap">
      <Card as="form" className="login" onSubmit={enviar}>
        <h1>Hotel</h1>
        <p className="sub">Ingresa con tu cuenta</p>
        <Alert>{error}</Alert>
        <Field label="Correo"><Input type="email" required autoFocus value={email} onChange={(e) => setEmail(e.target.value)} /></Field>
        <Field label="Contraseña"><Input type="password" required value={password} onChange={(e) => setPassword(e.target.value)} /></Field>
        <Button disabled={loading}>{loading ? 'Ingresando…' : 'Ingresar'}</Button>
        <p className="sub"><Link to="/registro">¿Eres huésped? Crea tu cuenta</Link></p>
      </Card>
    </div>
  )
}
