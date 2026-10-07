import { fireEvent, render, screen } from '@testing-library/react'
import Carousel from './Carousel.jsx'

const fotos = [
  { id: 'a', url: '/a.jpg' },
  { id: 'b', url: '/b.jpg' },
  { id: 'c', url: '/c.jpg' },
]
const region = () => screen.getByRole('region', { name: 'Carrusel de imágenes' })

describe('Carousel', () => {
  test('sin imágenes ni relleno muestra un texto', () => {
    render(<Carousel images={[]} />)
    expect(screen.getByText('Sin imágenes')).toBeInTheDocument()
  })

  test('sin imágenes con relleno muestra la imagen de relleno', () => {
    const { container } = render(<Carousel images={[]} fallback="/relleno.png" alt="Habitación" />)
    expect(container.querySelector('img')).toHaveAttribute('src', '/relleno.png')
  })

  test('con una sola imagen no muestra flechas ni puntos', () => {
    render(<Carousel images={[fotos[0]]} />)
    expect(screen.queryByRole('button', { name: 'Imagen siguiente' })).not.toBeInTheDocument()
    expect(screen.queryByRole('button', { name: /Ir a la imagen/ })).not.toBeInTheDocument()
  })

  test('siguiente avanza y anterior desde la primera da la vuelta', () => {
    render(<Carousel images={fotos} />)
    expect(screen.getByText('Imagen 1 de 3')).toBeInTheDocument()
    fireEvent.click(screen.getByRole('button', { name: 'Imagen siguiente' }))
    expect(screen.getByText('Imagen 2 de 3')).toBeInTheDocument()
    fireEvent.click(screen.getByRole('button', { name: 'Imagen anterior' }))
    fireEvent.click(screen.getByRole('button', { name: 'Imagen anterior' }))
    expect(screen.getByText('Imagen 3 de 3')).toBeInTheDocument()
  })

  test('un punto lleva a su imagen y queda marcado como actual', () => {
    render(<Carousel images={fotos} />)
    fireEvent.click(screen.getByRole('button', { name: 'Ir a la imagen 3' }))
    expect(screen.getByText('Imagen 3 de 3')).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Ir a la imagen 3' })).toHaveAttribute('aria-current', 'true')
    expect(screen.getByRole('button', { name: 'Ir a la imagen 1' })).toHaveAttribute('aria-current', 'false')
  })

  test('las flechas del teclado navegan', () => {
    render(<Carousel images={fotos} />)
    fireEvent.keyDown(region(), { key: 'ArrowRight' })
    expect(screen.getByText('Imagen 2 de 3')).toBeInTheDocument()
    fireEvent.keyDown(region(), { key: 'ArrowLeft' })
    expect(screen.getByText('Imagen 1 de 3')).toBeInTheDocument()
  })

  test('deslizar con el dedo cambia de imagen; un roce corto no', () => {
    render(<Carousel images={fotos} />)
    fireEvent.touchStart(region(), { touches: [{ clientX: 200 }] })
    fireEvent.touchEnd(region(), { changedTouches: [{ clientX: 195 }] })
    expect(screen.getByText('Imagen 1 de 3')).toBeInTheDocument()
    fireEvent.touchStart(region(), { touches: [{ clientX: 200 }] })
    fireEvent.touchEnd(region(), { changedTouches: [{ clientX: 100 }] })
    expect(screen.getByText('Imagen 2 de 3')).toBeInTheDocument()
    fireEvent.touchStart(region(), { touches: [{ clientX: 100 }] })
    fireEvent.touchEnd(region(), { changedTouches: [{ clientX: 200 }] })
    expect(screen.getByText('Imagen 1 de 3')).toBeInTheDocument()
  })

  // Review Focus 1
  test('si la lista se acorta mientras se ve la última, muestra una imagen válida', () => {
    const { rerender } = render(<Carousel images={fotos} />)
    fireEvent.click(screen.getByRole('button', { name: 'Ir a la imagen 3' }))
    rerender(<Carousel images={[fotos[0]]} />)
    expect(screen.getByText('Imagen 1 de 1')).toBeInTheDocument()
  })

  // Review Focus 2
  test('una imagen que no carga se omite del carrusel', () => {
    const { container } = render(<Carousel images={fotos} />)
    fireEvent.error(container.querySelectorAll('img')[1])
    expect(screen.getByText('Imagen 1 de 2')).toBeInTheDocument()
    expect(container.querySelectorAll('img')).toHaveLength(2)
  })

  test('si ninguna imagen carga, muestra el relleno', () => {
    const { container } = render(<Carousel images={[fotos[0]]} fallback="/relleno.png" />)
    fireEvent.error(container.querySelector('img'))
    expect(container.querySelector('img')).toHaveAttribute('src', '/relleno.png')
  })

  test('si ninguna imagen carga y no hay relleno, muestra el texto', () => {
    const { container } = render(<Carousel images={[fotos[0]]} />)
    fireEvent.error(container.querySelector('img'))
    expect(screen.getByText('Sin imágenes')).toBeInTheDocument()
  })
})
