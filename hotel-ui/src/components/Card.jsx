export default function Card({ className = '', ...rest }) {
  return <div className={`card ${className}`.trim()} {...rest} />
}
