import Encabezado from './Encabezado.jsx'
import Portada from './Portada.jsx'
import Habitaciones from './Habitaciones.jsx'
import Servicios from './Servicios.jsx'
import Contacto from './Contacto.jsx'
import Pie from './Pie.jsx'

export default function App() {
  return (
    <>
      <Encabezado />
      <main className="ln-main">
        <Portada />
        <Habitaciones />
        <Servicios />
        <Contacto />
      </main>
      <Pie />
    </>
  )
}
