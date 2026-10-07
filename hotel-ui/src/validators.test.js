import {
  requerido, email, letras, telefono, documento, password, maxLen, entre,
  fechaNoFutura, hoyLocal, validar, fetchSeguro, extraerError,
} from './validators.js'

describe('validadores simples', () => {
  test('requerido rechaza vacío y espacios', () => {
    expect(requerido()('')).toBe('Este campo es obligatorio')
    expect(requerido()('   ')).toBe('Este campo es obligatorio')
    expect(requerido()('a')).toBeNull()
  })
  test('requerido acepta mensaje propio', () => {
    expect(requerido('Falta')('')).toBe('Falta')
  })
  test('email', () => {
    expect(email('ana@hotel.com')).toBeNull()
    expect(email('ana@hotel')).toBe('Correo no válido')
    expect(email('')).toBeNull()
  })
  test('letras permite tildes y rechaza números', () => {
    expect(letras('María José')).toBeNull()
    expect(letras('Ana2')).toBe('Solo se permiten letras')
  })
  test('telefono', () => {
    expect(telefono('+57 300 123 4567')).toBeNull()
    expect(telefono('123')).toBe('Teléfono no válido (7 a 20 dígitos)')
  })
  test('maxLen y entre', () => {
    expect(maxLen(3)('abcd')).toBe('Máximo 3 caracteres')
    expect(maxLen(3)('abc')).toBeNull()
    expect(entre(1, 10)('11')).toBe('Debe estar entre 1 y 10')
    expect(entre(1, 10)('5')).toBeNull()
  })
})

describe('documento según tipo', () => {
  test('CC exige 5 a 12 dígitos', () => {
    expect(documento()('12345', { tipoDocumento: 'CC' })).toBeNull()
    expect(documento()('1234', { tipoDocumento: 'CC' })).toBe('Debe tener entre 5 y 12 dígitos')
  })
  test('pasaporte admite letras y números', () => {
    expect(documento()('AB12345', { tipoDocumento: 'PA' })).toBeNull()
    expect(documento()('AB-1', { tipoDocumento: 'PA' })).toBe('Pasaporte: 5 a 20 letras o números')
  })
})

describe('password', () => {
  test('exige mayúscula, minúscula, número y 8 caracteres', () => {
    expect(password('Abcdef12')).toBeNull()
    expect(password('abcdef12')).toBe('Debe incluir mayúscula, minúscula y número')
    expect(password('Ab1')).toBe('Debe tener entre 8 y 72 caracteres')
  })
})

describe('fechas', () => {
  test('fechaNoFutura', () => {
    expect(fechaNoFutura(hoyLocal())).toBeNull()
    expect(fechaNoFutura('2999-01-01')).toBe('No puede ser una fecha futura')
  })
})

describe('validar', () => {
  test('devuelve solo el primer error de cada campo', () => {
    const errores = validar({ nombre: '', correo: 'x' }, {
      nombre: [requerido(), maxLen(5)],
      correo: [requerido(), email],
    })
    expect(errores).toEqual({ nombre: 'Este campo es obligatorio', correo: 'Correo no válido' })
  })
  test('devuelve objeto vacío cuando todo es válido', () => {
    expect(validar({ nombre: 'Ana' }, { nombre: [requerido()] })).toEqual({})
  })
})

describe('utilidades de red', () => {
  afterEach(() => vi.restoreAllMocks())

  test('fetchSeguro traduce el fallo de red a un mensaje claro', async () => {
    vi.stubGlobal('fetch', vi.fn().mockRejectedValue(new TypeError('fail')))
    await expect(fetchSeguro('/x')).rejects.toThrow('No se pudo conectar con el servidor')
    vi.unstubAllGlobals()
  })
  test('extraerError usa el mensaje del cuerpo', async () => {
    const res = { status: 409, json: async () => ({ message: 'Ya existe' }) }
    expect(await extraerError(res)).toBe('Ya existe')
  })
  test('extraerError usa el texto por estado si no hay cuerpo', async () => {
    const res = { status: 403, json: async () => { throw new Error('sin cuerpo') } }
    expect(await extraerError(res)).toBe('No tienes permiso para esta acción.')
  })
})
