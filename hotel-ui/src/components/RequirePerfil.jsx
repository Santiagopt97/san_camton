import Alert from './Alert.jsx'

// Muestra los hijos solo si el perfil actual está permitido
export default function RequirePerfil({ perfiles, actual, children }) {
  return perfiles.includes(actual) ? children : <Alert>No tienes permiso para ver esta pantalla.</Alert>
}
