import { createSession } from './session.js'

const b64url = (obj) => btoa(JSON.stringify(obj)).replace(/=/g, '').replace(/\+/g, '-').replace(/\//g, '_')
const jwt = (payload) => `${b64url({ alg: 'HS256', typ: 'JWT' })}.${b64url(payload)}.firma`
const enUnaHora = () => Math.floor(Date.now() / 1000) + 3600
const haceUnaHora = () => Math.floor(Date.now() / 1000) - 3600

describe('createSession', () => {
  let sesion
  beforeEach(() => { sesion = createSession('http://localhost:5173') })

  test('initSession guarda el token del fragmento y limpia la URL', () => {
    const token = jwt({ name: 'Ana', role: 'admin', exp: enUnaHora() })
    window.location.hash = `#token=${encodeURIComponent(token)}`
    expect(sesion.initSession()).toBe(true)
    expect(sesion.getToken()).toBe(token)
    expect(window.location.hash).toBe('')
  })

  test('sin token no hay sesión', () => {
    expect(sesion.initSession()).toBe(false)
    expect(sesion.getToken()).toBeNull()
    expect(sesion.usuarioActual()).toBeNull()
  })

  test('un token vencido no cuenta como sesión', () => {
    sessionStorage.setItem('token', jwt({ name: 'Ana', role: 'admin', exp: haceUnaHora() }))
    expect(sesion.initSession()).toBe(false)
    expect(sesion.getToken()).toBeNull()
  })

  test('usuarioActual y tienePerfil leen el perfil del token', () => {
    sessionStorage.setItem('token', jwt({ name: 'Ana', role: 'recepcion', exp: enUnaHora() }))
    expect(sesion.usuarioActual()).toEqual({ nombre: 'Ana', perfil: 'recepcion' })
    expect(sesion.tienePerfil('admin', 'recepcion')).toBe(true)
    expect(sesion.tienePerfil('admin')).toBe(false)
  })

  test('un token con formato inválido se trata como sin sesión', () => {
    sessionStorage.setItem('token', 'basura')
    expect(sesion.getToken()).toBeNull()
    expect(sesion.usuarioActual()).toBeNull()
  })

  test('logout borra el token', () => {
    // jsdom no implementa la navegación y lo avisa por consola; se silencia solo aquí
    vi.spyOn(console, 'error').mockImplementation(() => {})
    sessionStorage.setItem('token', jwt({ name: 'Ana', role: 'admin', exp: enUnaHora() }))
    sesion.logout()
    expect(sessionStorage.getItem('token')).toBeNull()
    vi.restoreAllMocks()
  })
})
