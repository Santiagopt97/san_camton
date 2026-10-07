import { fireEvent, render, screen } from '@testing-library/react'
import Modal, { ConfirmModal } from './Modal.jsx'
import Pager from './Pager.jsx'

// Los componentes de la librería pueden quedar dentro de un <form> (p. ej. el formulario de habitación):
// sus botones auxiliares no deben enviarlo.
const enFormulario = (hijo) => {
  const onSubmit = vi.fn((e) => e.preventDefault())
  render(<form onSubmit={onSubmit}>{hijo}</form>)
  return onSubmit
}

describe('botones auxiliares dentro de un formulario', () => {
  test('Modal: «Cerrar» no envía el formulario', () => {
    const onClose = vi.fn()
    const onSubmit = enFormulario(<Modal open title="Aviso" onClose={onClose}>Texto</Modal>)
    fireEvent.click(screen.getByRole('button', { name: 'Cerrar' }))
    expect(onClose).toHaveBeenCalledTimes(1)
    expect(onSubmit).not.toHaveBeenCalled()
  })

  test('ConfirmModal: «Cancelar» y «Confirmar» no envían el formulario', () => {
    const onCancel = vi.fn()
    const onConfirm = vi.fn()
    const onSubmit = enFormulario(<ConfirmModal open message="¿Seguro?" onCancel={onCancel} onConfirm={onConfirm} />)
    fireEvent.click(screen.getByRole('button', { name: 'Cancelar' }))
    fireEvent.click(screen.getByRole('button', { name: 'Confirmar' }))
    expect(onCancel).toHaveBeenCalledTimes(1)
    expect(onConfirm).toHaveBeenCalledTimes(1)
    expect(onSubmit).not.toHaveBeenCalled()
  })

  test('Pager: «Anterior» y «Siguiente» no envían el formulario', () => {
    const onPage = vi.fn()
    const onSubmit = enFormulario(<Pager page={2} size={10} total={30} onPage={onPage} />)
    fireEvent.click(screen.getByRole('button', { name: 'Anterior' }))
    fireEvent.click(screen.getByRole('button', { name: 'Siguiente' }))
    expect(onPage).toHaveBeenCalledTimes(2)
    expect(onSubmit).not.toHaveBeenCalled()
  })
})
