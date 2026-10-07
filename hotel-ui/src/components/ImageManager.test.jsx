import { fireEvent, render, screen, waitFor } from '@testing-library/react'
import ImageManager from './ImageManager.jsx'

const fotos = [
  { id: 'a', url: '/a.jpg' },
  { id: 'b', url: '/b.jpg' },
  { id: 'c', url: '/c.jpg' },
]
const archivo = (nombre, tipo, bytes = 1000) => {
  const f = new File(['x'], nombre, { type: tipo })
  Object.defineProperty(f, 'size', { value: bytes })
  return f
}
const subir = (f) => fireEvent.change(screen.getByLabelText('Seleccionar imagen'), { target: { files: [f] } })

describe('ImageManager', () => {
  test('muestra las miniaturas y el contador', () => {
    render(<ImageManager images={fotos} max={6} />)
    expect(screen.getAllByRole('img')).toHaveLength(3)
    expect(screen.getByText('3 de 6 imágenes')).toBeInTheDocument()
  })

  test('sube un archivo válido', async () => {
    const onUpload = vi.fn().mockResolvedValue()
    render(<ImageManager images={[]} onUpload={onUpload} />)
    const f = archivo('foto.png', 'image/png')
    subir(f)
    await waitFor(() => expect(onUpload).toHaveBeenCalledWith(f))
  })

  test('rechaza un formato no permitido sin llamar a onUpload', () => {
    const onUpload = vi.fn()
    render(<ImageManager images={[]} onUpload={onUpload} />)
    subir(archivo('doc.pdf', 'application/pdf'))
    expect(screen.getByText('Formato no permitido. Usa JPG, PNG, WebP.')).toBeInTheDocument()
    expect(onUpload).not.toHaveBeenCalled()
  })

  // Review Focus 3
  test('rechaza un archivo sin tipo (algunos navegadores con .webp)', () => {
    const onUpload = vi.fn()
    render(<ImageManager images={[]} onUpload={onUpload} />)
    subir(archivo('foto.webp', ''))
    expect(screen.getByText('Formato no permitido. Usa JPG, PNG, WebP.')).toBeInTheDocument()
    expect(onUpload).not.toHaveBeenCalled()
  })

  test('acepta exactamente maxBytes y rechaza un byte más', async () => {
    const onUpload = vi.fn().mockResolvedValue()
    render(<ImageManager images={[]} onUpload={onUpload} maxBytes={5 * 1024 * 1024} />)
    subir(archivo('justa.jpg', 'image/jpeg', 5 * 1024 * 1024))
    await waitFor(() => expect(onUpload).toHaveBeenCalledTimes(1))
    subir(archivo('grande.jpg', 'image/jpeg', 5 * 1024 * 1024 + 1))
    expect(await screen.findByText('La imagen supera 5 MB.')).toBeInTheDocument()
    expect(onUpload).toHaveBeenCalledTimes(1)
  })

  test('con el máximo de imágenes no se puede agregar otra', () => {
    render(<ImageManager images={fotos} max={3} />)
    expect(screen.getByRole('button', { name: 'Agregar imagen' })).toBeDisabled()
  })

  test('eliminar llama a onDelete con el id', async () => {
    const onDelete = vi.fn().mockResolvedValue()
    render(<ImageManager images={fotos} onDelete={onDelete} />)
    fireEvent.click(screen.getByRole('button', { name: 'Eliminar imagen 2' }))
    await waitFor(() => expect(onDelete).toHaveBeenCalledWith('b'))
  })

  test('mover a la derecha envía el nuevo orden', async () => {
    const onReorder = vi.fn().mockResolvedValue()
    render(<ImageManager images={fotos} onReorder={onReorder} />)
    fireEvent.click(screen.getByRole('button', { name: 'Mover imagen 1 a la derecha' }))
    await waitFor(() => expect(onReorder).toHaveBeenCalledWith(['b', 'a', 'c']))
  })

  test('la primera no se mueve a la izquierda ni la última a la derecha', () => {
    render(<ImageManager images={fotos} />)
    expect(screen.getByRole('button', { name: 'Mover imagen 1 a la izquierda' })).toBeDisabled()
    expect(screen.getByRole('button', { name: 'Mover imagen 3 a la derecha' })).toBeDisabled()
  })

  // Review Focus 4
  test('si onUpload rechaza, muestra el mensaje y permite reintentar', async () => {
    const onUpload = vi.fn()
      .mockRejectedValueOnce(new Error('Máximo 6 imágenes por habitación.'))
      .mockResolvedValueOnce()
    render(<ImageManager images={[]} onUpload={onUpload} />)
    subir(archivo('a.png', 'image/png'))
    expect(await screen.findByText('Máximo 6 imágenes por habitación.')).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Agregar imagen' })).toBeEnabled()
    subir(archivo('b.png', 'image/png'))
    await waitFor(() => expect(onUpload).toHaveBeenCalledTimes(2))
    await waitFor(() => expect(screen.queryByText('Máximo 6 imágenes por habitación.')).not.toBeInTheDocument())
  })

  test('mientras una acción está en curso los controles quedan deshabilitados', async () => {
    let terminar
    const onDelete = vi.fn(() => new Promise((res) => { terminar = res }))
    render(<ImageManager images={fotos} onDelete={onDelete} />)
    fireEvent.click(screen.getByRole('button', { name: 'Eliminar imagen 1' }))
    await waitFor(() => expect(screen.getByRole('button', { name: 'Eliminar imagen 2' })).toBeDisabled())
    terminar()
    await waitFor(() => expect(screen.getByRole('button', { name: 'Eliminar imagen 2' })).toBeEnabled())
  })

  // Review Focus 5
  test('los botones no envían el formulario que los contiene', async () => {
    const onSubmit = vi.fn((e) => e.preventDefault())
    const onDelete = vi.fn().mockResolvedValue()
    render(<form onSubmit={onSubmit}><ImageManager images={fotos} onDelete={onDelete} /></form>)
    fireEvent.click(screen.getByRole('button', { name: 'Eliminar imagen 1' }))
    fireEvent.click(screen.getByRole('button', { name: 'Mover imagen 2 a la derecha' }))
    await waitFor(() => expect(onDelete).toHaveBeenCalled())
    expect(onSubmit).not.toHaveBeenCalled()
  })

  test('disabled deshabilita todos los controles', () => {
    render(<ImageManager images={fotos} disabled />)
    expect(screen.getByRole('button', { name: 'Agregar imagen' })).toBeDisabled()
    expect(screen.getByRole('button', { name: 'Eliminar imagen 1' })).toBeDisabled()
  })
})
