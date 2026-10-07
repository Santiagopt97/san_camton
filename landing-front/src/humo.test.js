import { SIN_FOTO } from 'hotel-ui'

test('hotel-ui se resuelve desde landing-front', () => {
  expect(SIN_FOTO.startsWith('data:image/svg+xml')).toBe(true)
})
