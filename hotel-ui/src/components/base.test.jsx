import { fireEvent, render, screen } from '@testing-library/react'
import AppShell from './AppShell.jsx'
import Pager from './Pager.jsx'
import DataTable from './DataTable.jsx'

describe('AppShell', () => {
  test('muestra sección, usuario y llama a onLogout', () => {
    const onLogout = vi.fn()
    render(<AppShell section="Clientes" user="Ana" onLogout={onLogout}><p>Contenido</p></AppShell>)
    expect(screen.getByText('· Clientes')).toBeInTheDocument()
    expect(screen.getByText('Contenido')).toBeInTheDocument()
    fireEvent.click(screen.getByRole('button', { name: 'Salir' }))
    expect(onLogout).toHaveBeenCalledTimes(1)
  })
})

describe('Pager', () => {
  test('en la primera página no se puede retroceder', () => {
    render(<Pager page={1} size={10} total={25} onPage={() => {}} />)
    expect(screen.getByRole('button', { name: 'Anterior' })).toBeDisabled()
    expect(screen.getByText('Página 1 de 3 · 25 registros')).toBeInTheDocument()
  })
  test('en la última página no se puede avanzar y navega con onPage', () => {
    const onPage = vi.fn()
    render(<Pager page={3} size={10} total={25} onPage={onPage} />)
    expect(screen.getByRole('button', { name: 'Siguiente' })).toBeDisabled()
    fireEvent.click(screen.getByRole('button', { name: 'Anterior' }))
    expect(onPage).toHaveBeenCalledWith(2)
  })
  test('sin registros muestra una sola página', () => {
    render(<Pager page={1} size={10} total={0} onPage={() => {}} />)
    expect(screen.getByText('Página 1 de 1 · 0 registros')).toBeInTheDocument()
  })
})

describe('DataTable', () => {
  const columns = [{ header: 'Nombre', cell: (r) => r.nombre }]
  test('pinta una fila por registro', () => {
    render(<DataTable columns={columns} rows={[{ id: 1, nombre: 'Ana' }, { id: 2, nombre: 'Luis' }]} />)
    expect(screen.getByText('Ana')).toBeInTheDocument()
    expect(screen.getByText('Luis')).toBeInTheDocument()
  })
  test('sin filas muestra el mensaje vacío', () => {
    render(<DataTable columns={columns} rows={[]} empty="Nada por aquí" />)
    expect(screen.getByText('Nada por aquí')).toBeInTheDocument()
  })
  test('mientras carga y sin filas muestra el spinner', () => {
    render(<DataTable columns={columns} rows={[]} loading />)
    expect(screen.getByRole('status', { name: 'Cargando' })).toBeInTheDocument()
    expect(screen.queryByText('Sin resultados')).not.toBeInTheDocument()
  })
})
