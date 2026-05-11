// Section list — replaced by real resume data in task 5.3

const SECTIONS = [
  { id: 'personal', name: 'Personal info', meta: 'Name, contact, location' },
  { id: 'summary', name: 'Summary', meta: 'Professional summary' },
  { id: 'skills', name: 'Skills', meta: 'Technical and soft skills' },
  { id: 'experience', name: 'Experience', meta: '0 entries' },
  { id: 'education', name: 'Education', meta: '0 entries' },
  { id: 'certifications', name: 'Certifications', meta: '0 entries' },
]

export function ResumeBuilderPage() {
  return (
    <div className="page">
      <div className="page-header">
        <div>
          <h1 className="page-title">Resume</h1>
          <p className="page-subtitle">Base resume — used as source for all tailored versions</p>
        </div>
        <div style={{ display: 'flex', gap: 8 }}>
          <button className="btn btn-secondary">Preview</button>
          <button className="btn btn-primary">Save</button>
        </div>
      </div>

      <div className="section-list" role="list">
        {SECTIONS.map((section) => (
          <div key={section.id} className="section-row" role="listitem">
            <span className="section-row-name">{section.name}</span>
            <span className="section-row-meta">{section.meta}</span>
          </div>
        ))}
      </div>
    </div>
  )
}
