import type { CSSProperties } from 'react'

interface SkeletonProps {
  variant?: 'line' | 'heading' | 'pill' | 'card'
  width?: number | string
  style?: CSSProperties
}

export function Skeleton({ variant = 'line', width, style }: SkeletonProps) {
  return <div className={`skeleton skeleton-${variant}`} style={{ width, ...style }} aria-hidden="true" />
}
