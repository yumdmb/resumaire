import type { HTMLAttributes } from 'react'

export type PageWidth = 'narrow' | 'standard' | 'full'

interface PageProps extends HTMLAttributes<HTMLDivElement> {
  /** narrow: forms and reading. standard: two-column pages. full: boards and dense views. */
  width?: PageWidth
}

/** The single page container. Pages pick a width tier and never set their own. */
export function Page({ width = 'standard', className, ...rest }: PageProps) {
  return <div className={`page page--${width}${className ? ` ${className}` : ''}`} {...rest} />
}
