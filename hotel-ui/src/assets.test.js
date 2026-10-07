import { SIN_FOTO } from './assets.js'

test('SIN_FOTO es una imagen SVG incrustada con el texto "Sin foto"', () => {
  expect(SIN_FOTO.startsWith('data:image/svg+xml')).toBe(true)
  expect(decodeURIComponent(SIN_FOTO)).toContain('Sin foto')
})
