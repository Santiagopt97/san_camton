// tone: ok | off | warn | danger | info
export default function Badge({ tone = 'off', children }) {
  return <span className={`badge ${tone}`}>{children}</span>
}
