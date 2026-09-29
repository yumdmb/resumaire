import type { ReactNode } from 'react'

interface CardProps {
  title?: string
  actions?: ReactNode
  children: ReactNode
  className?: string
}

export function Card({ title, actions, children, className }: CardProps) {
  return (
    <section className={`card${className ? ` ${className}` : ''}`}>
      {title || actions ? (
        <div className="card-header">
          {title ? <h2 className="card-title">{title}</h2> : <span />}
          {actions}
        </div>
      ) : null}
      <div className="card-body">{children}</div>
    </section>
  )
}
