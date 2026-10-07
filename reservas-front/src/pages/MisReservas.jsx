import { useCallback, useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { Alert, Badge, Button, Card, ConfirmModal, DataTable } from 'hotel-ui'
import { miApi, cop } from '../api.js'

const tono = { Confirmada: 'info', 'Check-in': 'ok', 'Check-out': 'off', Cancelada: 'danger' }

export default function MisReservas() {
  const [rows, setRows] = useState([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')
  const [aCancelar, setACancelar] = useState(null)

  const cargar = useCallback(async () => {
    try { setError(''); setRows(await miApi.listar()) } catch (e) { setError(e.message) } finally { setLoading(false) }
  }, [])
  useEffect(() => { cargar() }, [cargar])

  const cancelar = async () => {
    try { await miApi.cancelar(aCancelar.id); cargar() } catch (e) { setError(e.message) }
    setACancelar(null)
  }

  const columns = [
    { header: 'Habitación', cell: (r) => <>{r.habitacion} <small>({r.tipo})</small></> },
    { header: 'Entrada', cell: (r) => r.fechaEntrada },
    { header: 'Salida', cell: (r) => r.fechaSalida },
    { header: 'Huéspedes', cell: (r) => r.huespedes },
    { header: 'Total', cell: (r) => cop(r.total) },
    { header: 'Estado', cell: (r) => <Badge tone={tono[r.estado]}>{r.estado}</Badge> },
    { header: '', className: 'actions', cell: (r) => r.estado === 'Confirmada'
      && <Button variant="link danger" onClick={() => setACancelar(r)}>Cancelar</Button> },
  ]

  return (
    <Card>
      <div className="toolbar"><h2 style={{ margin: 0, flex: 1 }}>Mis reservas</h2><Button as={Link} to="/reservar">+ Nueva reserva</Button></div>
      <Alert>{error}</Alert>
      <DataTable columns={columns} rows={rows} loading={loading} empty="Aún no tienes reservas" />
      <ConfirmModal open={!!aCancelar} danger title="Cancelar reserva" confirmText="Cancelar reserva"
        message={aCancelar && `¿Cancelar tu reserva de la habitación ${aCancelar.habitacion} del ${aCancelar.fechaEntrada}?`}
        onConfirm={cancelar} onCancel={() => setACancelar(null)} />
    </Card>
  )
}
