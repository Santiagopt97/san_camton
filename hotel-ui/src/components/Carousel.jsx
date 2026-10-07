import { useRef, useState } from 'react'

const UMBRAL_TACTIL = 40 // px mínimos de deslizamiento para cambiar de imagen

// <Carousel images={[{ id, url, alt? }]} aspectRatio="4 / 3" fallback="/sin-foto.png" alt="Habitación 101" />
export default function Carousel({ images = [], aspectRatio = '4 / 3', fallback, alt = 'Imagen', className = '' }) {
  const [actual, setActual] = useState(0)
  const [rotas, setRotas] = useState({}) // imágenes cuya URL no cargó
  const inicioX = useRef(null)

  const clave = (img) => img.id ?? img.url
  const visibles = images.filter((img) => !rotas[clave(img)])
  const total = visibles.length
  const indice = total ? Math.min(actual, total - 1) : 0 // evita índices fuera de rango si la lista se acorta

  const ir = (n) => setActual(((n % total) + total) % total)
  const marcarRota = (img) => setRotas((r) => ({ ...r, [clave(img)]: true }))

  const alTeclado = (e) => {
    if (total < 2) return
    if (e.key === 'ArrowRight') { e.preventDefault(); ir(indice + 1) }
    if (e.key === 'ArrowLeft') { e.preventDefault(); ir(indice - 1) }
  }
  const alTocar = (e) => { inicioX.current = e.touches[0].clientX }
  const alSoltar = (e) => {
    if (inicioX.current === null || total < 2) return
    const dx = e.changedTouches[0].clientX - inicioX.current
    inicioX.current = null
    if (Math.abs(dx) >= UMBRAL_TACTIL) ir(dx < 0 ? indice + 1 : indice - 1)
  }

  return (
    <div
      className={`carousel ${className}`.trim()}
      style={{ aspectRatio }}
      role="region"
      aria-roledescription="carrusel"
      aria-label="Carrusel de imágenes"
      tabIndex={0}
      onKeyDown={alTeclado}
      onTouchStart={alTocar}
      onTouchEnd={alSoltar}
    >
      {total === 0 ? (
        fallback
          ? <img className="carousel-fallback" src={fallback} alt={alt} />
          : <div className="carousel-empty">Sin imágenes</div>
      ) : (
        <>
          <div className="carousel-track" style={{ transform: `translateX(-${indice * 100}%)` }}>
            {visibles.map((img, i) => (
              <div className="carousel-slide" key={clave(img)} aria-hidden={i !== indice}>
                <img
                  src={img.url}
                  alt={img.alt || `${alt} ${i + 1}`}
                  loading={i === 0 ? 'eager' : 'lazy'}
                  onError={() => marcarRota(img)}
                />
              </div>
            ))}
          </div>
          <span className="sr-only" aria-live="polite">Imagen {indice + 1} de {total}</span>
          {total > 1 && (
            <>
              <button type="button" className="carousel-btn carousel-prev" aria-label="Imagen anterior" onClick={() => ir(indice - 1)}>‹</button>
              <button type="button" className="carousel-btn carousel-next" aria-label="Imagen siguiente" onClick={() => ir(indice + 1)}>›</button>
              <div className="carousel-dots">
                {visibles.map((img, i) => (
                  <button
                    type="button"
                    key={clave(img)}
                    className="carousel-dot"
                    aria-label={`Ir a la imagen ${i + 1}`}
                    aria-current={i === indice}
                    onClick={() => ir(i)}
                  />
                ))}
              </div>
            </>
          )}
        </>
      )}
    </div>
  )
}
