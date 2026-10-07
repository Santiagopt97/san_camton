import { useState, useCallback } from 'react'
import { Link } from 'react-router-dom'
import { Alert, Badge, Button, Card, Checkbox, ConfirmModal, DataTable, Input, Pager, Toolbar, useDebouncedEffect } from 'hotel-ui'
import { clientesApi, descargarReporte } from '../api.js'

export default function ClientesList() {
  const [q, setQ] = useState('')
  const [soloActivos, setSoloActivos] = useState(false)
  const [page, setPage] = useState(1)
  const [data, setData] = useState({ total: 0, items: [] })
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')
  const [aDesactivar, setADesactivar] = useState(null)
  const size = 10

  const cargar = useCallback(async () => {
    setLoading(true)
    try { setError(''); setData(await clientesApi.listar({ q, page, size, soloActivos })) }
    catch (e) { setError(e.message) } finally { setLoading(false) }
  }, [q, page, soloActivos])
  useDebouncedEffect(cargar, [cargar])

  const confirmar = async () => {
    try { await clientesApi.desactivar(aDesactivar.id); setADesactivar(null); cargar() }
    catch (e) { setError(e.message); setADesactivar(null) }
  }
  const reporte = (tipo) => descargarReporte(tipo, { q, soloActivos }).catch((e) => setError(e.message))

  const columns = [
    { header: 'Documento', cell: (c) => `${c.tipoDocumento} ${c.numeroDocumento}` },
    { header: 'Nombre', cell: (c) => `${c.nombres} ${c.apellidos}` },
    { header: 'Email', cell: (c) => c.email || '—' },
    { header: 'Teléfono', cell: (c) => c.telefono || '—' },
    { header: 'Estado', cell: (c) => <Badge tone={c.activo ? 'ok' : 'off'}>{c.activo ? 'Activo' : 'Inactivo'}</Badge> },
    { header: '', className: 'actions', cell: (c) => (<>
      <Link to={`/clientes/${c.id}`}>Editar</Link>
      {c.activo && <button className="link danger" onClick={() => setADesactivar(c)}>Desactivar</button>}
    </>) },
  ]

  return (
    <Card>
      <Toolbar>
        <Input placeholder="Buscar por nombre, documento o email…" value={q} onChange={(e) => { setQ(e.target.value); setPage(1) }} />
        <Checkbox label="Solo activos" checked={soloActivos} onChange={(e) => { setSoloActivos(e.target.checked); setPage(1) }} />
        <Button variant="ghost" onClick={() => reporte('csv')}>Descargar CSV</Button>
        <Button variant="ghost" onClick={() => reporte('pdf')}>Descargar PDF</Button>
        <Button as={Link} to="/clientes/nuevo">+ Nuevo</Button>
      </Toolbar>
      <Alert>{error}</Alert>
      <DataTable columns={columns} rows={data.items} loading={loading} />
      <Pager page={page} size={size} total={data.total} onPage={setPage} noun="clientes" />
      <ConfirmModal open={!!aDesactivar} danger title="Desactivar cliente" confirmText="Desactivar"
        message={aDesactivar && `¿Desactivar a ${aDesactivar.nombres} ${aDesactivar.apellidos}?`}
        onConfirm={confirmar} onCancel={() => setADesactivar(null)} />
    </Card>
  )
}
