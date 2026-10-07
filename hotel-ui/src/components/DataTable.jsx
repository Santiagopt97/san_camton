import Spinner from './Spinner.jsx'

// columns: [{ header, cell: (row) => node, className }]
export default function DataTable({ columns, rows, loading, empty = 'Sin resultados', rowKey = (r) => r.id, rowClass }) {
  return (
    <div className="table-wrap">
      <table>
        <thead><tr>{columns.map((c, i) => <th key={i}>{c.header}</th>)}</tr></thead>
        <tbody>
          {loading && rows.length === 0 && <tr><td colSpan={columns.length} className="loading-row"><Spinner /></td></tr>}
          {rows.map((r) => (
            <tr key={rowKey(r)} className={rowClass?.(r) ?? ''}>
              {columns.map((c, i) => <td key={i} className={c.className}>{c.cell(r)}</td>)}
            </tr>
          ))}
          {!loading && rows.length === 0 && <tr><td colSpan={columns.length} className="empty">{empty}</td></tr>}
        </tbody>
      </table>
    </div>
  )
}
