import { HOTEL } from './contenido.js'

export default function Pie() {
  return <footer className="ln-pie">© {new Date().getFullYear()} {HOTEL.nombre}. Todos los derechos reservados.</footer>
}
