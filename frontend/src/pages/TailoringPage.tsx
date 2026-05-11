// Tailoring workflow — wired to real data in tasks 6.5 and 6.6

const STEPS = [
  {
    title: 'Select a job',
    desc: 'Choose the job you want to tailor your resume for.',
  },
  {
    title: 'Review keywords',
    desc: 'Keywords extracted from the job description are matched against your base resume.',
  },
  {
    title: 'Review suggestions',
    desc: 'Accept, reject, or edit each suggested change before it becomes part of your resume.',
  },
  {
    title: 'Save version',
    desc: 'Save the tailored resume as a named version linked to this job.',
  },
]

export function TailoringPage() {
  return (
    <div className="page">
      <div className="page-header">
        <div>
          <h1 className="page-title">Tailoring</h1>
          <p className="page-subtitle">Tailor your base resume for a specific role</p>
        </div>
        <button className="btn btn-primary">Start tailoring</button>
      </div>

      <div className="step-list">
        {STEPS.map((step, i) => (
          <div key={step.title} className="step-item">
            <div className="step-num">{i + 1}</div>
            <div className="step-content">
              <span className="step-title">{step.title}</span>
              <span className="step-desc">{step.desc}</span>
            </div>
          </div>
        ))}
      </div>
    </div>
  )
}
