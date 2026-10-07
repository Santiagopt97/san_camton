import { useState, useCallback } from 'react'
import { Link } from 'react-router-dom'
import { Alert, Button, Card, DataTable, Input, Pager, Select, Toolbar, useDebouncedEffect } from 'hotel-ui'
import { reservasApi, ESTADOS, cop } from '../api.js'

export default function ReservasList() {
  const [q, setQ] = useState('')
  const [estado, setEstado] = useState('')
  const [page, setPage] = useState(1)
  const [data, setData] = useState({ total: 0, items: [] })
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')
  const size = 10

  const cargar = useCallback(async () => {
    setLoading(true)
    try { setError(''); setData(await reservasApi.listar({ q, estado, page, size })) }
    catch (e) { setError(e.message) } finally { setLoading(false) }
  }, [q, estado, page])
  useDebouncedEffect(cargar, [cargar])

  const cambiar = async (r, nuevo) => {
    try { await reservasApi.cambiarEstado(r.id, nuevo); cargar() } catch (e) { setError(e.message) }
  }

  const columns = [
    { header: 'Cliente', cell: (r) => r.cliente },
    { header: 'Hab.', cell: (r) => <>{r.habitacion} <small>({r.tipo})</small></> },
    { header: 'Entrada', cell: (r) => r.fechaEntrada },
    { header: 'Salida', cell: (r) => r.fechaSalida },
    { header: 'Total', cell: (r) => cop(r.total) },
    { header: 'Estado', cell: (r) => (
      <select className="estado" value={r.estado} onChange={(e) => cambiar(r, e.target.value)}>
        {ESTADOS.map((s) => <option key={s}>{s}</option>)}
      </select>
    ) },
    { header: '', className: 'actions', cell: (r) => <Link to={`/reservas/${r.id}`}>Editar</Link> },
  ]

  return (
    <Card>
      <Toolbar>
        <Input placeholder="Buscar por cliente, documento o habitación…" value={q} onChange={(e) => { setQ(e.target.value); setPage(1) }} />
        <Select value={estado} onChange={(e) => { setEstado(e.target.value); setPage(1) }} placeholder="Todos los estados" options={ESTADOS} />
        <Button as={Link} to="/reservas/nueva">+ Nueva</Button>
      </Toolbar>
      <Alert>{error}</Alert>
      <DataTable columns={columns} rows={data.items} loading={loading} />
      <Pager page={page} size={size} total={data.total} onPage={setPage} noun="reservas" />
    </Card>
  )
}
