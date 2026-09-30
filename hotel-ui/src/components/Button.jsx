// <Button variant="ghost|danger" size="sm" as={Link} to="/x">Texto</Button>
export default function Button({ as: Tag = 'button', variant = '', size = '', className = '', ...rest }) {
  return <Tag className={`btn ${variant} ${size} ${className}`.trim()} {...rest} />
}
