import { useRef, useState } from 'react'
import Alert from './Alert.jsx'
import Button from './Button.jsx'

const MB = 1024 * 1024
const NOMBRES = { 'image/jpeg': 'JPG', 'image/png': 'PNG', 'image/webp': 'WebP' }
const FORMATOS = Object.keys(NOMBRES)

// Gestor presentacional: no conoce la API. Cada front le pasa onUpload / onDelete / onReorder (pueden ser async).
export default function ImageManager({
  images = [], onUpload, onDelete, onReorder, max = 6, maxBytes = 5 * MB, accept = FORMATOS, disabled = false,
}) {
  const [error, setError] = useState('')
  const [ocupado, setOcupado] = useState(false)
  const input = useRef(null)
  const bloqueado = disabled || ocupado
  const lleno = images.length >= max

  const ejecutar = async (accion) => {
    setError(''); setOcupado(true)
    try { await accion() } catch (e) { setError(e?.message || 'No se pudo completar la acción.') } finally { setOcupado(false) }
  }

  const elegir = (e) => {
    const archivo = e.target.files?.[0]
    e.target.value = '' // permite volver a elegir el mismo archivo
    if (!archivo) return
    if (!accept.includes(archivo.type)) {
      setError(`Formato no permitido. Usa ${accept.map((t) => NOMBRES[t] ?? t).join(', ')}.`)
      return
    }
    if (archivo.size > maxBytes) {
      setError(`La imagen supera ${+(maxBytes / MB).toFixed(1)} MB.`)
      return
    }
    ejecutar(() => onUpload(archivo))
  }

  const mover = (i, delta) => {
    const ids = images.map((img) => img.id)
    const j = i + delta
    ;[ids[i], ids[j]] = [ids[j], ids[i]]
    ejecutar(() => onReorder(ids))
  }

  return (
    <div className="image-manager">
      <Alert>{error}</Alert>
      <ul className="image-manager-list">
        {images.map((img, i) => (
          <li key={img.id}>
            <img src={img.url} alt={`Imagen ${i + 1}`} />
            <div className="image-manager-actions">
              <Button type="button" variant="ghost" size="sm" disabled={bloqueado || i === 0}
                aria-label={`Mover imagen ${i + 1} a la izquierda`} onClick={() => mover(i, -1)}>←</Button>
              <Button type="button" variant="ghost" size="sm" disabled={bloqueado || i === images.length - 1}
                aria-label={`Mover imagen ${i + 1} a la derecha`} onClick={() => mover(i, 1)}>→</Button>
              <Button type="button" variant="danger" size="sm" disabled={bloqueado}
                aria-label={`Eliminar imagen ${i + 1}`} onClick={() => ejecutar(() => onDelete(img.id))}>Eliminar</Button>
            </div>
          </li>
        ))}
      </ul>
      <div>
        <Button type="button" disabled={bloqueado || lleno} onClick={() => input.current?.click()}>Agregar imagen</Button>
        <span className="image-manager-count">{images.length} de {max} imágenes</span>
        <input ref={input} type="file" hidden accept={accept.join(',')} aria-label="Seleccionar imagen" onChange={elegir} />
      </div>
    </div>
  )
}
