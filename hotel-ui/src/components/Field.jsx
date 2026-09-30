// <Field label="Nombre" full><Input .../></Field>
export function Field({ label, full, error, children }) {
  return (
    <label className={`${full ? 'full' : ''} ${error ? 'has-error' : ''}`.trim()}>
      {label}{children}
      {error && <small className="field-error" role="alert">{error}</small>}
    </label>
  )
}
export const Input = (props) => <input {...props} />
export const Textarea = (props) => <textarea {...props} />
// <Select value onChange options={['A','B']} /> o con {value,label}
export function Select({ options = [], placeholder, ...rest }) {
  return (
    <select {...rest}>
      {placeholder && <option value="">{placeholder}</option>}
      {options.map((o) => typeof o === 'string'
        ? <option key={o} value={o}>{o}</option>
        : <option key={o.value} value={o.value}>{o.label}</option>)}
    </select>
  )
}
export function Checkbox({ label, full, ...rest }) {
  return <label className={`check ${full ? 'full' : ''}`}><input type="checkbox" {...rest} /> {label}</label>
}
export const FormGrid = ({ children }) => <div className="grid">{children}</div>
export const FormActions = ({ children }) => <div className="row-end">{children}</div>
