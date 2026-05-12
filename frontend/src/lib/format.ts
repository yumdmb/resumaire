export function formatShortDate(isoOrDate: string | null): string {
  if (!isoOrDate) return ''
  const date = new Date(isoOrDate)
  if (Number.isNaN(date.getTime())) return ''
  return date.toLocaleDateString(undefined, {
    month: 'short',
    day: 'numeric',
  })
}

export function formatLongDate(isoOrDate: string | null): string {
  if (!isoOrDate) return ''
  const date = new Date(isoOrDate)
  if (Number.isNaN(date.getTime())) return ''
  return date.toLocaleDateString(undefined, {
    month: 'short',
    day: 'numeric',
    year: 'numeric',
  })
}
