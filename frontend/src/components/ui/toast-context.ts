import { createContext, useContext } from 'react'

export interface ToastOptions {
  message: string
  tone?: 'error' | 'info'
  action?: { label: string; onAction: () => void }
  /** Milliseconds before auto-dismiss. */
  duration?: number
}

export interface ToastApi {
  show: (options: ToastOptions) => number
  dismiss: (id: number) => void
}

export const ToastContext = createContext<ToastApi | null>(null)

export function useToast(): ToastApi {
  const api = useContext(ToastContext)
  if (!api) throw new Error('useToast must be used within ToastProvider')
  return api
}
