import { useEffect, useState } from 'react'

// Ejecuta `fn` tras una pausa cuando cambian las dependencias (búsquedas)
export function useDebouncedEffect(fn, deps, ms = 250) {
  useEffect(() => { const t = setTimeout(fn, ms); return () => clearTimeout(t) }, deps) // eslint-disable-line
}
export function useLoading(initial = false) {
  const [loading, setLoading] = useState(initial)
  return [loading, setLoading]
}
