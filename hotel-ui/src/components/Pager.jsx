import Button from './Button.jsx'

export default function Pager({ page, size, total, onPage, noun = 'registros' }) {
  const paginas = Math.max(1, Math.ceil(total / size))
  return (
    <div className="pager">
      <Button type="button" variant="ghost" disabled={page <= 1} onClick={() => onPage(page - 1)}>Anterior</Button>
      <span>Página {page} de {paginas} · {total} {noun}</span>
      <Button type="button" variant="ghost" disabled={page >= paginas} onClick={() => onPage(page + 1)}>Siguiente</Button>
    </div>
  )
}
