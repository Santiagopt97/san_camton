const vacio = (v) => v === undefined || v === null || String(v).trim() === ''

export const requerido = (msg = 'Este campo es obligatorio') => (v) => (vacio(v) ? msg : null)
export const patron = (re, msg) => (v) => (vacio(v) || re.test(String(v).trim()) ? null : msg)
export const maxLen = (n) => (v) => (vacio(v) || String(v).length <= n ? null : `Máximo ${n} caracteres`)
export const minLen = (n) => (v) => (vacio(v) || String(v).length >= n ? null : `Mínimo ${n} caracteres`)
export const entre = (min, max, msg) => (v) => {
  if (vacio(v)) return null
  const n = Number(v)
  return Number.isFinite(n) && n >= min && n <= max ? null : (msg || `Debe estar entre ${min} y ${max}`)
}
export const email = patron(/^[^\s@]+@[^\s@]+\.[^\s@]{2,}$/, 'Correo no válido')
export const letras = patron(/^\p{L}[\p{L}\s'.-]*$/u, 'Solo se permiten letras')
export const telefono = patron(/^\+?[0-9][0-9\s()-]{6,19}$/, 'Teléfono no válido (7 a 20 dígitos)')

// Documento según el tipo elegido (campo `tipoDocumento` por defecto)
export const documento = (campoTipo = 'tipoDocumento') => (v, vals) => {
  if (vacio(v)) return null
  const d = String(v).trim()
  const tipo = vals[campoTipo]
  if (['CC', 'TI', 'CE'].includes(tipo)) return /^\d{5,12}$/.test(d) ? null : 'Debe tener entre 5 y 12 dígitos'
  if (tipo === 'PA') return /^[A-Za-z0-9]{5,20}$/.test(d) ? null : 'Pasaporte: 5 a 20 letras o números'
  if (tipo === 'NIT') return /^\d{9,10}(-\d)?$/.test(d) ? null : 'NIT no válido (9 o 10 dígitos)'
  return null
}

export const hoyLocal = () => {
  const d = new Date()
  d.setMinutes(d.getMinutes() - d.getTimezoneOffset())
  return d.toISOString().slice(0, 10)
}
export const fechaNoFutura = (v) => (vacio(v) || v <= hoyLocal() ? null : 'No puede ser una fecha futura')
export const edadMinimaSiTipo = (tipo, anios) => (v, vals) => {
  if (vacio(v) || vals.tipoDocumento !== tipo) return null
  const lim = new Date(); lim.setFullYear(lim.getFullYear() - anios)
  return new Date(v) <= lim ? null : `Con ${tipo} debe ser mayor de ${anios} años`
}
export const password = (v) => {
  if (vacio(v)) return null
  if (v.length < 8 || v.length > 72) return 'Debe tener entre 8 y 72 caracteres'
  if (!/[a-z]/.test(v) || !/[A-Z]/.test(v) || !/\d/.test(v)) return 'Debe incluir mayúscula, minúscula y número'
  return null
}

// validar(valores, { campo: [validador, ...] }) → { campo: 'mensaje' } (vacío si todo está bien)
export function validar(valores, esquema) {
  const errores = {}
  for (const [campo, reglas] of Object.entries(esquema)) {
    for (const regla of reglas) {
      const msg = regla(valores[campo], valores)
      if (msg) { errores[campo] = msg; break }
    }
  }
  return errores
}

