// Imagen de relleno para habitaciones sin fotos (SVG incrustado, sin archivos externos)
const svg = `<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 400 300"><rect width="400" height="300" fill="#E8DFD0"/><g fill="none" stroke="#6B7280" stroke-width="8" stroke-linecap="round" stroke-linejoin="round"><path d="M110 190h180M120 190v-50h160v50M135 140v-18a14 14 0 0 1 14-14h38a14 14 0 0 1 14 14v18M213 140v-18a14 14 0 0 1 14-14h24a14 14 0 0 1 14 14v18"/></g><text x="200" y="245" text-anchor="middle" font-family="sans-serif" font-size="22" fill="#6B7280">Sin foto</text></svg>`

export const SIN_FOTO = `data:image/svg+xml;utf8,${encodeURIComponent(svg)}`
