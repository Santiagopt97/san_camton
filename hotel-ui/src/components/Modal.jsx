import { useEffect } from 'react'
import Button from './Button.jsx'

export default function Modal({ open, title, onClose, children, actions }) {
  useEffect(() => {
    if (!open) return
    const h = (e) => e.key === 'Escape' && onClose?.()
    window.addEventListener('keydown', h)
    return () => window.removeEventListener('keydown', h)
  }, [open, onClose])
  if (!open) return null
  return (
    <div className="modal-overlay" onClick={onClose}>
      <div className="modal" role="dialog" aria-modal="true" onClick={(e) => e.stopPropagation()}>
        {title && <h3>{title}</h3>}
        <div>{children}</div>
        <div className="row-end">{actions ?? <Button type="button" variant="ghost" onClick={onClose}>Cerrar</Button>}</div>
      </div>
    </div>
  )
}

// Reemplaza a window.confirm
export function ConfirmModal({ open, title = 'Confirmar', message, confirmText = 'Confirmar', danger, onConfirm, onCancel }) {
  return (
    <Modal open={open} title={title} onClose={onCancel} actions={<>
      <Button type="button" variant="ghost" onClick={onCancel}>Cancelar</Button>
      <Button type="button" variant={danger ? 'danger' : ''} onClick={onConfirm}>{confirmText}</Button>
    </>}>
      <p>{message}</p>
    </Modal>
  )
}
