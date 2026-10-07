import { useState, useCallback } from 'react'
import { Link } from 'react-router-dom'
import { Alert, Button, Card, Carousel, Checkbox, ConfirmModal, DataTable, Input, Pager, SIN_FOTO, Select, Toolbar, useDebouncedEffect } from 'hotel-ui'
import { habitacionesApi, ESTADOS, TIPOS } from '../api.js'

const cop = (n) => new Intl.NumberFormat('es-CO', { style: 'currency', currency: 'COP', maximumFractionDigits: 0 }).format(n)

export default function HabitacionesList({ perfil }) {
  const [q, setQ] = useState('')
  const [estado, setEstado] = useState('')
  const [tipo, setTipo] = useState('')
  const [soloActivas, setSoloActivas] = useState(true)
  const [page, setPage] = useState(1)
  const [data, setData] = useState({ total: 0, items: [] })
  const [resumen, setResumen] = useState([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')
  const [aBaja, setABaja] = useState(null)
  const size = 12

  const cargar = useCallback(async () => {
    setLoading(true)
    try {
      setError('')
      const [lista, res] = await Promise.all([
        habitacionesApi.listar({ q, estado, tipo, page, size, soloActivas }),
        habitacionesApi.resumen(),
      ])
      setData(lista); setResumen(res)
    } catch (e) { setError(e.message) } finally { setLoading(false) }
  }, [q, estado, tipo, page, soloActivas])
  useDebouncedEffect(cargar, [cargar])

  const cambiarEstado = async (h, nuevo) => {
    try { await habitacionesApi.cambiarEstado(h.id, nuevo); cargar() } catch (e) { setError(e.message) }
  }
  const confirmarBaja = async () => {
    try { await habitacionesApi.desactivar(aBaja.id); cargar() } catch (e) { setError(e.message) }
    setABaja(null)
  }
  const filtro = (setter) => (e) => { setter(e.target.value); setPage(1) }

  const columns = [
    { header: '', cell: (h) => (
      <div className="thumb"><Carousel images={h.imagenes} alt={`Habitación ${h.numero}`} fallback={SIN_FOTO} /></div>
    ) },
    { header: 'N.º', cell: (h) => <strong>{h.numero}</strong> },
    { header: 'Tipo', cell: (h) => h.tipo },
    { header: 'Piso', cell: (h) => h.piso },
    { header: 'Cap.', cell: (h) => h.capacidad },
    { header: 'Precio / noche', cell: (h) => cop(h.precioNoche) },
    { header: 'Estado', cell: (h) => (
      <Select className={`estado ${h.estado}`} value={h.estado} disabled={!h.activo} onChange={(e) => cambiarEstado(h, e.target.value)} options={ESTADOS} />
    ) },
    { header: '', className: 'actions', cell: (h) => (<>
      <Link to={`/habitaciones/${h.id}`}>Editar</Link>
      {perfil === 'admin' && h.activo && <Button variant="link danger" disabled={h.estado === 'Ocupada'} title={h.estado === 'Ocupada' ? 'No se puede dar de baja una habitación ocupada' : undefined} onClick={() => setABaja(h)}>Dar de baja</Button>}
    </>) },
  ]

  return (
    <>
      <div className="summary">
        {resumen.map((r) => (
          <Card as="button" key={r.estado} className={`stat ${estado === r.estado ? 'sel' : ''}`}
            onClick={() => { setEstado(estado === r.estado ? '' : r.estado); setPage(1) }}>
            <span className={`dot ${r.estado}`} /> <strong>{r.cantidad}</strong> <small>{r.estado}</small>
          </Card>
        ))}
      </div>
      <Card>
        <Toolbar>
          <Input placeholder="Buscar por número, tipo o descripción…" value={q} onChange={filtro(setQ)} />
          <Select value={tipo} onChange={filtro(setTipo)} placeholder="Todos los tipos" options={TIPOS} />
          <Checkbox label="Solo activas" checked={soloActivas} onChange={(e) => { setSoloActivas(e.target.checked); setPage(1) }} />
          <Button as={Link} to="/habitaciones/nueva">+ Nueva</Button>
        </Toolbar>
        <Alert>{error}</Alert>
        <DataTable columns={columns} rows={data.items} loading={loading} rowClass={(h) => (h.activo ? '' : 'inactive')} />
        <Pager page={page} size={size} total={data.total} onPage={setPage} noun="habitaciones" />
      </Card>
      <ConfirmModal open={!!aBaja} danger title="Dar de baja" confirmText="Dar de baja"
        message={aBaja && `¿Dar de baja la habitación ${aBaja.numero}?`} onConfirm={confirmarBaja} onCancel={() => setABaja(null)} />
    </>
  )
}
