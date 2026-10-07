import { render, screen } from '@testing-library/react'
import Portada from './Portada.jsx'
import Servicios from './Servicios.jsx'
import Contacto from './Contacto.jsx'
import Pie from './Pie.jsx'
import { CONTACTO, HOTEL, SERVICIOS } from './contenido.js'

describe('secciones de contenido', () => {
  test('la portada tiene el título y un botón de reserva hacia el registro', () => {
    render(<Portada />)
    expect(screen.getByRole('heading', { level: 1 })).toHaveTextContent(HOTEL.nombre)
    expect(screen.getByRole('link', { name: 'Reservar ahora' })).toHaveAttribute('href', 'http://localhost:5173/registro')
  })

  test('los servicios salen de contenido.js', () => {
    render(<Servicios />)
    expect(screen.getByRole('heading', { name: 'Servicios' })).toBeInTheDocument()
    for (const s of SERVICIOS) expect(screen.getByRole('heading', { name: s.titulo })).toBeInTheDocument()
  })

  test('el contacto muestra dirección, teléfono y correo', () => {
    render(<Contacto />)
    expect(screen.getByRole('heading', { name: 'Contacto' })).toBeInTheDocument()
    expect(screen.getByText(CONTACTO.direccion)).toBeInTheDocument()
    expect(screen.getByRole('link', { name: CONTACTO.correo })).toHaveAttribute('href', `mailto:${CONTACTO.correo}`)
    expect(screen.getByRole('link', { name: CONTACTO.telefono })).toHaveAttribute('href', `tel:${CONTACTO.telefono.replace(/\s/g, '')}`)
  })

  test('el pie lleva el año actual', () => {
    render(<Pie />)
    expect(screen.getByRole('contentinfo')).toHaveTextContent(String(new Date().getFullYear()))
  })
})
