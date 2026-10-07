import { createSession } from './session.js'
import { CABECERA_CSRF } from './http.js'

const respuesta = (estado, cuerpo) => ({ ok: estado >= 200 && estado < 300, status: estado, json: async () => cuerpo })
let fetchFalso
let irA
let sesion

beforeEach(() => {
  fetchFalso = vi.fn()
  vi.stubGlobal('fetch', fetchFalso)
  irA = vi.fn()
  sesion = createSession({ authApiUrl: 'http://auth', loginUrl: 'http://login', irA })
})
afterEach(() => vi.unstubAllGlobals())

describe('createSession', () => {
  test('initSession pregunta a /me con credenciales y guarda nombre y perfil en memoria', async () => {
    fetchFalso.mockResolvedValue(respuesta(200, { nombre: 'Ana', perfil: 'admin', correo: 'no-debe-guardarse' }))
    expect(await sesion.initSession()).toBe(true)
    const [url, opciones] = fetchFalso.mock.calls[0]
    expect(url).toBe('http://auth/api/auth/me')
    expect(opciones.credentials).toBe('include')
    expect(sesion.usuarioActual()).toEqual({ nombre: 'Ana', perfil: 'admin' })
  })

  // Review Focus 2
  test('un 401 significa sin sesión', async () => {
    fetchFalso.mockResolvedValue(respuesta(401))
    expect(await sesion.initSession()).toBe(false)
    expect(sesion.usuarioActual()).toBeNull()
  })

  test('un fallo de red significa sin sesión (no lanza)', async () => {
    fetchFalso.mockRejectedValue(new TypeError('fail'))
    expect(await sesion.initSession()).toBe(false)
    expect(sesion.usuarioActual()).toBeNull()
  })

  test('tienePerfil usa el perfil de la memoria', async () => {
    fetchFalso.mockResolvedValue(respuesta(200, { nombre: 'Ana', perfil: 'recepcion' }))
    await sesion.initSession()
    expect(sesion.tienePerfil('admin', 'recepcion')).toBe(true)
    expect(sesion.tienePerfil('admin')).toBe(false)
  })

  test('antes de iniciar no hay usuario ni perfil', () => {
    expect(sesion.usuarioActual()).toBeNull()
    expect(sesion.tienePerfil('admin')).toBe(false)
  })

  test('logout llama a /logout con credenciales y cabecera, borra la memoria y va al login', async () => {
    fetchFalso.mockResolvedValueOnce(respuesta(200, { nombre: 'Ana', perfil: 'admin' }))
    await sesion.initSession()
    fetchFalso.mockResolvedValueOnce(respuesta(204))
    await sesion.logout()
    const [url, opciones] = fetchFalso.mock.calls[1]
    expect(url).toBe('http://auth/api/auth/logout')
    expect(opciones.method).toBe('POST')
    expect(opciones.credentials).toBe('include')
    expect(opciones.headers[CABECERA_CSRF.nombre]).toBe(CABECERA_CSRF.valor)
    expect(sesion.usuarioActual()).toBeNull()
    expect(irA).toHaveBeenCalledWith('http://login/login')
  })

  test('logout va al login aunque el servidor no responda', async () => {
    fetchFalso.mockRejectedValue(new TypeError('fail'))
    await sesion.logout()
    expect(irA).toHaveBeenCalledWith('http://login/login')
  })

  test('nunca guarda nada en sessionStorage ni en localStorage', async () => {
    // Con Node 26 el localStorage nativo puede no existir en jsdom: se comprueba solo el que exista y cualquier escritura
    const escritura = vi.spyOn(Storage.prototype, 'setItem')
    fetchFalso.mockResolvedValue(respuesta(200, { nombre: 'Ana', perfil: 'admin' }))
    await sesion.initSession()
    await sesion.logout()
    expect(escritura).not.toHaveBeenCalled()
    expect([sessionStorage, globalThis.localStorage].filter(Boolean).every((a) => a.length === 0)).toBe(true)
  })
})
