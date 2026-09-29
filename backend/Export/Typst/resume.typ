// Resumaire resume template (Typst).
// Layout ported from the Jake-style base-cv.typ. All content is read from data.json
// and inserted as plain strings via text/link, never evaluated as markup.

#let data = json("data.json")

#set document(title: data.name, author: data.name)

#set page(
  paper: "us-letter",
  margin: (top: 0.3in, bottom: 0.7in, x: 0.5in),
)

#set text(
  font: "New Computer Modern",
  size: 11pt,
  lang: "en",
  hyphenate: false,
)
#set par(leading: 0.65em, spacing: 0.65em, justify: false)

#show link: set text(fill: blue)
#show link: underline.with(evade: false)

#show heading: set text(size: 12pt, weight: "regular")
#show heading: it => {
  block(above: 12pt, below: 8pt, sticky: true)[
    #pad(top: 0pt, bottom: -10pt, [#smallcaps(it.body)])
    #line(length: 100%, stroke: 0.4pt)
  ]
}

#let small = 10pt

#let resume-subheading(title, right-top, subtitle, right-bottom) = {
  block(sticky: true, above: 8pt, below: 3pt)[
    #pad(left: 0.15in)[
      #block(width: 97%)[
        #grid(
          columns: (1fr, auto),
          align: (left, right),
          row-gutter: 3pt,
          [*#title*], [#right-top],
          [#text(size: small, style: "italic", subtitle)],
          [#text(size: small, style: "italic", right-bottom)],
        )
      ]
    ]
  ]
}

#let resume-project(name, url, techs) = {
  let label = if url != "" { strong(link(url, name)) } else { strong(name) }
  block(sticky: true, above: 8pt, below: 3pt)[
    #pad(left: 0.15in)[
      #block(width: 97%)[
        #set text(size: small)
        #if techs != "" [#label $|$ #emph(techs)] else [#label]
      ]
    ]
  ]
}

#let resume-items(items) = {
  if items.len() > 0 {
    set text(size: small)
    set par(leading: 0.55em, spacing: 0.55em)
    set list(
      indent: 0.15in,
      body-indent: 0.5em,
      marker: [•],
      tight: true,
      spacing: 4pt,
    )
    pad(top: 2pt, list(..items.map(item => [#item])))
  }
}

#let skill-line(category, items) = {
  if category != "" [*#category*: #items] else [*Skills*: #items]
}

// ---------- HEADER ----------
#block[
  #align(left)[
    #text(size: 20pt, weight: 700)[#data.name]
  ]

  #if data.contact.len() > 0 [
    #pad(top: 0.25em)[
      #set text(size: small)
      #data.contact.map(c => if c.url != "" { link(c.url)[#c.text] } else [#c.text]).join("  |  ")
    ]
  ]

  #if data.summary != "" [
    #v(6pt)
    #set text(size: small)
    #set par(justify: true)
    #data.summary
  ]
]

// ---------- EDUCATION ----------
#if data.education.len() > 0 [
  = Education
  #for e in data.education [
    #resume-subheading(e.institution, e.location, e.degree, e.dates)
    #resume-items(e.bullets)
  ]
]

// ---------- EXPERIENCE ----------
#if data.experience.len() > 0 [
  = Work Experience
  #for e in data.experience [
    #resume-subheading(e.title, e.dates, e.organization, e.location)
    #resume-items(e.bullets)
  ]
]

// ---------- PROJECTS ----------
#if data.projects.len() > 0 [
  = Projects
  #for p in data.projects [
    #resume-project(p.name, p.url, p.technologies)
    #resume-items(p.bullets)
  ]
]

// ---------- SKILLS ----------
#if data.skills.len() > 0 [
  = Technical Skills
  #pad(left: 0.15in)[
    #set text(size: small)
    #set par(leading: 0.55em, spacing: 0.55em)
    #for (i, s) in data.skills.enumerate() [
      #skill-line(s.category, s.items)#if i < data.skills.len() - 1 [ \ ]
    ]
  ]
]

// ---------- CERTIFICATIONS ----------
#if data.certifications.len() > 0 [
  = Certifications
  #for c in data.certifications [
    #resume-subheading(c.name, c.date, c.issuer, "")
  ]
]

// ---------- ACTIVITIES ----------
#if data.activities.len() > 0 [
  = Leadership & Activities
  #for a in data.activities [
    #resume-subheading(a.title, a.location, a.role, a.date)
    #resume-items(a.bullets)
  ]
]
