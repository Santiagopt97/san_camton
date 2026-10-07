// <Button variant="ghost|danger|link|link danger" size="sm" as={Link} to="/x">Texto</Button>
// La variante `link` se ve como enlace y no lleva la clase `btn`.
export default function Button({ as: Tag = 'button', variant = '', size = '', className = '', ...rest }) {
  const base = variant.split(' ').includes('link') ? '' : 'btn'
  const clases = `${base} ${variant} ${size} ${className}`.replace(/\s+/g, ' ').trim()
  return <Tag className={clases} {...rest} />
}
