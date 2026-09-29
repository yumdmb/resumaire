export interface StepperStep {
  label: string
}

interface StepperProps {
  steps: StepperStep[]
  /** Zero-based index of the current step. Earlier steps read as done, later ones as upcoming. */
  current: number
  label?: string
}

export function Stepper({ steps, current, label = 'Progress' }: StepperProps) {
  return (
    <ol className="stepper" aria-label={label}>
      {steps.map((step, i) => {
        const state = i < current ? 'done' : i === current ? 'current' : 'upcoming'
        return (
          <li
            key={step.label}
            className={`stepper-item stepper-item--${state}`}
            aria-current={state === 'current' ? 'step' : undefined}
          >
            <span className="stepper-num" aria-hidden="true">
              {state === 'done' ? '✓' : i + 1}
            </span>
            <span className="stepper-label">
              {step.label}
              {state === 'done' ? <span className="visually-hidden"> (completed)</span> : null}
            </span>
          </li>
        )
      })}
    </ol>
  )
}
