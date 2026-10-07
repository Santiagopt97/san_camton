import { render, screen, waitFor } from '@testing-library/react'
import Habitaciones from './Habitaciones.jsx'

const ok = (cuerpo) => ({ ok: true, status: 200, json: async () => cuerpo })
const normal = (s) => s.replace(/\s+/g, ' ')
let fetchFalso

beforeEach(() => { fetchFalso = vi.fn(); vi.stubGlobal('fetch', fetchFalso) })
afterEach(() => vi.unstubAllGlobals())

const TIPOS = [
  { tipo: 'Sencilla', capacidad: 1, precioDesde: 120000, imagenes: [{ url: '/a.jpg' }, { url: '/b.jpg' }] },
  { tipo: 'Doble', capacidad: 2, precioDesde: 180000, imagenes: [] },
]

describe('Habitaciones', () => {
  test('mientras carga muestra un indicador', () => {
    fetchFalso.mockReturnValue(new Promise(() => {}))
    render(<Habitaciones />)
    expect(screen.getByRole('status', { name: 'Cargando' })).toBeInTheDocument()
  })

  test('muestra una tarjeta por tipo con capacidad y precio', async () => {
    fetchFalso.mockResolvedValue(ok(TIPOS))
    render(<Habitaciones />)
    expect(await screen.findByRole('heading', { name: 'Sencilla' })).toBeInTheDocument()
    expect(screen.getByRole('heading', { name: 'Doble' })).toBeInTheDocument()
    expect(screen.getByText('Hasta 1 persona')).toBeInTheDocument()
    expect(screen.getByText('Hasta 2 personas')).toBeInTheDocument()
    expect(screen.getByText((_, el) => el?.className === 'ln-precio' && normal(el.textContent) === 'Desde $ 120.000 / noche')).toBeInTheDocument()
    expect(screen.getByText((_, el) => el?.className === 'ln-precio' && normal(el.textContent) === 'Desde $ 180.000 / noche')).toBeInTheDocument()
  })

  test('cada tarjeta tiene su carrusel', async () => {
    fetchFalso.mockResolvedValue(ok(TIPOS))
    render(<Habitaciones />)
    await screen.findByRole('heading', { name: 'Sencilla' })
    expect(screen.getAllByRole('region', { name: 'Carrusel de imágenes' })).toHaveLength(2)
  })

  // Review Focus 3
  test('un tipo sin imágenes muestra la imagen de relleno', async () => {
    fetchFalso.mockResolvedValue(ok(TIPOS))
    render(<Habitaciones />)
    const relleno = await screen.findByAltText('Habitación Doble')
    expect(relleno.getAttribute('src')).toMatch(/^data:image\/svg\+xml/)
  })

  // Review Focus 3
  test('sin habitaciones muestra «Próximamente»', async () => {
    fetchFalso.mockResolvedValue(ok([]))
    render(<Habitaciones />)
    expect(await screen.findByText('Próximamente')).toBeInTheDocument()
  })

  // Review Focus 4
  test('si la red falla muestra un mensaje y la sección sigue en pantalla', async () => {
    fetchFalso.mockRejectedValue(new TypeError('fail'))
    render(<Habitaciones />)
    expect(await screen.findByRole('alert')).toHaveTextContent('No pudimos cargar las habitaciones. Intenta de nuevo más tarde.')
    expect(screen.getByRole('heading', { name: 'Nuestras habitaciones' })).toBeInTheDocument()
    expect(screen.queryByText('Próximamente')).not.toBeInTheDocument()
  })

  // Review Focus 4
  test('si el servidor responde con error muestra el mismo mensaje', async () => {
    fetchFalso.mockResolvedValue({ ok: false, status: 500, json: async () => ({}) })
    render(<Habitaciones />)
    expect(await screen.findByRole('alert')).toHaveTextContent('No pudimos cargar las habitaciones.')
  })

  test('la petición es pública: va al endpoint correcto y sin credenciales', async () => {
    fetchFalso.mockResolvedValue(ok([]))
    render(<Habitaciones />)
    await waitFor(() => expect(fetchFalso).toHaveBeenCalledTimes(1))
    const [url, opciones] = fetchFalso.mock.calls[0]
    expect(url).toBe('http://localhost:5003/api/publico/habitaciones')
    expect(opciones?.credentials).toBeUndefined()
  })
})
