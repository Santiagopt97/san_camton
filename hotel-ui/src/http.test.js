import { crearCliente, CABECERA_CSRF } from './http.js'

const respuesta = (estado, cuerpo) => ({ ok: estado >= 200 && estado < 300, status: estado, json: async () => cuerpo })
let fetchFalso

beforeEach(() => { fetchFalso = vi.fn(); vi.stubGlobal('fetch', fetchFalso) })
afterEach(() => vi.unstubAllGlobals())

const ultimaLlamada = () => fetchFalso.mock.calls.at(-1)

describe('crearCliente', () => {
  test('manda las credenciales y no pone la cabecera anti-CSRF en las lecturas', async () => {
    fetchFalso.mockResolvedValue(respuesta(200, { ok: 1 }))
    const { req } = crearCliente({ base: 'http://api' })
    expect(await req('/api/x')).toEqual({ ok: 1 })
    const [url, opciones] = ultimaLlamada()
    expect(url).toBe('http://api/api/x')
    expect(opciones.credentials).toBe('include')
    expect(opciones.headers[CABECERA_CSRF.nombre]).toBeUndefined()
  })

  test.each(['POST', 'PUT', 'PATCH', 'DELETE'])('%s lleva la cabecera anti-CSRF', async (metodo) => {
    fetchFalso.mockResolvedValue(respuesta(204))
    const { req } = crearCliente({ base: 'http://api' })
    await req('/api/x', { method: metodo })
    expect(ultimaLlamada()[1].headers[CABECERA_CSRF.nombre]).toBe(CABECERA_CSRF.valor)
  })

  test('JSON lleva Content-Type y FormData no (lo pone el navegador)', async () => {
    fetchFalso.mockResolvedValue(respuesta(204))
    const { req } = crearCliente({ base: 'http://api' })
    await req('/api/x', { method: 'POST', body: '{}' })
    expect(ultimaLlamada()[1].headers['Content-Type']).toBe('application/json')
    await req('/api/x', { method: 'POST', body: new FormData() })
    expect(ultimaLlamada()[1].headers['Content-Type']).toBeUndefined()
  })

  // Review Focus 2
  test('un 401 con onNoAutorizado cierra la sesión y no lanza', async () => {
    fetchFalso.mockResolvedValue(respuesta(401, { message: 'x' }))
    const onNoAutorizado = vi.fn()
    const { req } = crearCliente({ base: 'http://api', onNoAutorizado })
    expect(await req('/api/x')).toBeNull()
    expect(onNoAutorizado).toHaveBeenCalledTimes(1)
  })

  test('un 401 sin onNoAutorizado lanza el mensaje del servidor (login)', async () => {
    fetchFalso.mockResolvedValue(respuesta(401, { message: 'Correo o contraseña incorrectos.' }))
    const { req } = crearCliente({ base: 'http://api' })
    await expect(req('/api/auth/login', { method: 'POST', body: '{}' })).rejects.toThrow('Correo o contraseña incorrectos.')
  })

  test('una respuesta de error lanza su mensaje', async () => {
    fetchFalso.mockResolvedValue(respuesta(409, { message: 'Ya existe' }))
    const { req } = crearCliente({ base: 'http://api' })
    await expect(req('/api/x', { method: 'POST', body: '{}' })).rejects.toThrow('Ya existe')
  })

  test('un 204 devuelve null', async () => {
    fetchFalso.mockResolvedValue(respuesta(204))
    const { req } = crearCliente({ base: 'http://api' })
    expect(await req('/api/x', { method: 'DELETE' })).toBeNull()
  })

  test('fetchConSesion también manda credenciales (descarga de reportes)', async () => {
    fetchFalso.mockResolvedValue(respuesta(200))
    const { fetchConSesion } = crearCliente({ base: 'http://api' })
    await fetchConSesion('/api/reportes/clientes/csv')
    expect(ultimaLlamada()[1].credentials).toBe('include')
  })

  test('un fallo de red da un mensaje claro', async () => {
    fetchFalso.mockRejectedValue(new TypeError('fail'))
    const { req } = crearCliente({ base: 'http://api' })
    await expect(req('/api/x')).rejects.toThrow('No se pudo conectar con el servidor')
  })

  test('no guarda nada en sessionStorage ni en localStorage', async () => {
    // Con Node 26 el localStorage nativo puede no existir en jsdom: se comprueba solo el que exista y cualquier escritura
    const escritura = vi.spyOn(Storage.prototype, 'setItem')
    fetchFalso.mockResolvedValue(respuesta(200, {}))
    const { req } = crearCliente({ base: 'http://api' })
    await req('/api/x')
    expect(escritura).not.toHaveBeenCalled()
    expect([sessionStorage, globalThis.localStorage].filter(Boolean).every((a) => a.length === 0)).toBe(true)
  })
})
