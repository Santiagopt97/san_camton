import { render, screen } from '@testing-library/react'
import App from './App.jsx'

beforeEach(() => vi.stubGlobal('fetch', vi.fn().mockResolvedValue({ ok: true, status: 200, json: async () => [] })))
afterEach(() => vi.unstubAllGlobals())

describe('App', () => {
  test('la página tiene encabezado, contenido principal y pie', async () => {
    render(<App />)
    expect(screen.getByRole('banner')).toBeInTheDocument()
    expect(screen.getByRole('main')).toBeInTheDocument()
    expect(screen.getByRole('contentinfo')).toBeInTheDocument()
    await screen.findByText('Próximamente')
  })

  test('tiene las secciones de habitaciones, servicios y contacto', async () => {
    render(<App />)
    expect(screen.getByRole('heading', { name: 'Nuestras habitaciones' })).toBeInTheDocument()
    expect(screen.getByRole('heading', { name: 'Servicios' })).toBeInTheDocument()
    expect(screen.getByRole('heading', { name: 'Contacto' })).toBeInTheDocument()
    await screen.findByText('Próximamente')
  })
})
