import { render, screen } from '@testing-library/react'
import Encabezado from './Encabezado.jsx'

describe('Encabezado', () => {
  // Review Focus 5: sin ningún .env los botones llevan a la aplicación por defecto
  test('los botones apuntan al login y al registro de la aplicación', () => {
    render(<Encabezado />)
    expect(screen.getByRole('link', { name: 'Iniciar sesión' })).toHaveAttribute('href', 'http://localhost:5173/login')
    expect(screen.getByRole('link', { name: 'Reservar' })).toHaveAttribute('href', 'http://localhost:5173/registro')
  })

  test('la navegación lleva a las secciones de la página', () => {
    render(<Encabezado />)
    expect(screen.getByRole('link', { name: 'Habitaciones' })).toHaveAttribute('href', '#habitaciones')
    expect(screen.getByRole('link', { name: 'Servicios' })).toHaveAttribute('href', '#servicios')
    expect(screen.getByRole('link', { name: 'Contacto' })).toHaveAttribute('href', '#contacto')
  })
})
