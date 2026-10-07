import { render, screen } from '@testing-library/react'
import Button from './Button.jsx'
import Card from './Card.jsx'

describe('Button', () => {
  test('por defecto es un botón con clase btn', () => {
    render(<Button>Guardar</Button>)
    const b = screen.getByRole('button', { name: 'Guardar' })
    expect(b).toHaveClass('btn')
  })
  test('variantes ghost, danger y tamaño', () => {
    render(<Button variant="ghost" size="sm">X</Button>)
    expect(screen.getByRole('button')).toHaveClass('btn', 'ghost', 'sm')
  })
  test('la variante link no usa el aspecto de botón', () => {
    render(<Button variant="link danger">Borrar</Button>)
    const b = screen.getByRole('button', { name: 'Borrar' })
    expect(b).toHaveClass('link', 'danger')
    expect(b).not.toHaveClass('btn')
  })
  test('reenvía props como disabled y title', () => {
    render(<Button disabled title="Ayuda">X</Button>)
    expect(screen.getByRole('button')).toBeDisabled()
    expect(screen.getByTitle('Ayuda')).toBeInTheDocument()
  })
  test('permite renderizar otra etiqueta con as', () => {
    render(<Button as="a" href="/x">Ir</Button>)
    expect(screen.getByRole('link', { name: 'Ir' })).toHaveClass('btn')
  })
})

describe('Card', () => {
  test('por defecto es un div con clase card', () => {
    const { container } = render(<Card>Hola</Card>)
    expect(container.firstChild.tagName).toBe('DIV')
    expect(container.firstChild).toHaveClass('card')
  })
  test('con as="button" renderiza un botón con clase card', () => {
    render(<Card as="button" className="stat">Tile</Card>)
    expect(screen.getByRole('button', { name: 'Tile' })).toHaveClass('card', 'stat')
  })
})
