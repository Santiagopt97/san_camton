export default function Alert({ type = 'error', children }) {
  return children ? <p className={`alert ${type}`} role="alert">{children}</p> : null
}
