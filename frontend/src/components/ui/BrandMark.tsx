export function BrandMark({ size = 24 }: { size?: number }) {
  return (
    <svg width={size} height={size} viewBox="0 0 64 64" fill="none" aria-hidden="true" focusable="false">
      <use href={`${import.meta.env.BASE_URL}brand/resumaire-mark.svg#resumaire-mark`} />
    </svg>
  )
}
