import { cop } from './formato.js'

const normal = (s) => s.replace(/\s/g, ' ')

describe('cop', () => {
  test('formatea pesos colombianos sin decimales', () => {
    expect(normal(cop(180000))).toBe('$ 180.000')
    expect(normal(cop(1500000))).toBe('$ 1.500.000')
  })
  test('redondea los centavos', () => {
    expect(normal(cop(120000.4))).toBe('$ 120.000')
  })
})
