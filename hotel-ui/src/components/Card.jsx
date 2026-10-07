// <Card as="button" className="stat">…</Card>  (por defecto es un <div>)
export default function Card({ as: Tag = 'div', className = '', ...rest }) {
  return <Tag className={`card ${className}`.trim()} {...rest} />
}
