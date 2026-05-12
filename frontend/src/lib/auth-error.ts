export class AuthError extends Error {
  readonly status: number
  readonly fieldErrors: Record<string, string[]>

  constructor(
    message: string,
    status: number,
    fieldErrors?: Record<string, string[]>,
  ) {
    super(message)
    this.name = 'AuthError'
    this.status = status
    this.fieldErrors = fieldErrors ?? {}
  }
}
