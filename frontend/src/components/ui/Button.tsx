import type { ButtonHTMLAttributes } from 'react'
import { Link, type LinkProps } from 'react-router-dom'

type Variant = 'primary' | 'secondary' | 'ghost' | 'danger'

function classes(variant: Variant, small: boolean | undefined, extra?: string) {
  return ['btn', `btn-${variant}`, small ? 'btn-sm' : '', extra ?? ''].filter(Boolean).join(' ')
}

interface ButtonProps extends ButtonHTMLAttributes<HTMLButtonElement> {
  variant?: Variant
  small?: boolean
}

export function Button({ variant = 'secondary', small, className, type = 'button', ...rest }: ButtonProps) {
  return <button type={type} className={classes(variant, small, className)} {...rest} />
}

interface ButtonLinkProps extends LinkProps {
  variant?: Variant
  small?: boolean
}

export function ButtonLink({ variant = 'secondary', small, className, ...rest }: ButtonLinkProps) {
  return <Link className={classes(variant, small, className)} {...rest} />
}
