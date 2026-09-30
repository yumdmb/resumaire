import { useCallback, useState } from 'react'

/** useState backed by localStorage. Storage failures are ignored; invalid stored values fall back to `initial`. */
export function usePersistedState<T extends string>(
  key: string,
  initial: T,
  isValid: (value: string) => value is T,
): [T, (value: T) => void] {
  const [value, setValue] = useState<T>(() => {
    try {
      const stored = window.localStorage.getItem(key)
      if (stored !== null && isValid(stored)) return stored
    } catch {
      // storage unavailable
    }
    return initial
  })

  const set = useCallback(
    (next: T) => {
      setValue(next)
      try {
        window.localStorage.setItem(key, next)
      } catch {
        // storage unavailable: the choice still applies for this page view
      }
    },
    [key],
  )

  return [value, set]
}
