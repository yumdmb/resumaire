# Resumaire - Tailored Resume Builder & Job Tracker

## Core idea

A user saves jobs, pastes the job description, and the app helps them generate a tailored resume as **HTML preview → PDF export**.

## MVP features

### 1. Job tracker

Each job should have only necessary fields:

| Field           | Purpose                                    |
| --------------- | ------------------------------------------ |
| Company         | Where you applied                          |
| Job title       | Role name                                  |
| Job link        | Original posting                           |
| Job description | Used for tailoring                         |
| Status          | Saved, Applied, Interview, Rejected, Offer |
| Date applied    | Optional but useful                        |
| Notes           | Small free-text area                       |
| Resume version  | Which tailored resume was used             |

Avoid reminders, calendar sync, analytics, kanban complexity, etc.

## 2. Resume profile

User creates one “base resume”:

* Personal info
* Summary
* Skills
* Work/project experience
* Education
* Certifications
* Links: GitHub, LinkedIn, portfolio

This is the source content for tailoring.

## 3. Resume tailoring flow

Simple flow:

1. User adds job description.
2. App extracts important keywords:

   * required skills
   * tools/frameworks
   * responsibilities
   * seniority level
3. App compares job description with base resume.
4. App suggests tailored edits:

   * improved summary
   * reordered skills
   * adjusted bullet points
   * missing keywords to include honestly
5. User can edit the resume.
6. Export as PDF.

Important: the app should **not fabricate experience**. It should only rephrase, reorder, and emphasize relevant existing experience.

## Recommended MVP pages

### Dashboard

Shows all jobs with status filters.

### Add Job

Paste job link/title/company/description.

### Job Detail

Shows:

* job description
* status
* notes
* tailored resume button
* saved resume versions

### Resume Builder

Edit base resume.

### Tailored Resume Preview

HTML resume preview with:

* editable sections
* regenerate suggestions
* export PDF

## Simple tech stack

For a small SaaS:

**Frontend + backend:** Next.js
**Database:** PostgreSQL with Supabase
**Auth:** Supabase Auth or Clerk
**AI tailoring:** OpenAI API
**PDF export:** Playwright or Puppeteer
**Deployment:** Vercel
**Payments later:** Stripe, not needed for MVP

## Basic database tables

```txt
users
- id
- email
- created_at

jobs
- id
- user_id
- company
- title
- job_link
- job_description
- status
- date_applied
- notes
- created_at

base_resumes
- id
- user_id
- content_json
- created_at
- updated_at

tailored_resumes
- id
- user_id
- job_id
- resume_html
- resume_json
- ai_notes
- created_at
```

## Best MVP scope

Start with this:

1. User signs in.
2. User creates base resume.
3. User adds job descriptions.
4. User clicks “Tailor Resume”.
5. App generates tailored resume draft.
6. User edits it.
7. User downloads PDF.
8. Job status can be updated manually.

That is enough for a useful SaaS.

## Nice positioning

You could describe it as:

> A lightweight job tracker that helps job seekers manage applications and generate tailored resumes from job descriptions without overcomplicating the process.


---

Yes — the best version is to support **both manual tailoring and AI-assisted tailoring**.

That gives users control while still saving time.

## Recommended approach

### 1. Manual tailoring (must-have)

Users can:

* edit resume sections directly
* reorder skills
* rewrite bullet points
* add/remove projects
* tweak summary

Think of it like:

> “Resume editor optimized for job applications.”

This is important because:

* people don’t fully trust AI
* recruiters dislike obviously AI-generated resumes
* users want control

---

## 2. AI-assisted tailoring (your differentiator)

AI should act like a smart assistant, not auto-generate fake resumes.

### Example flow

User pastes JD:

```txt
Looking for React developer with TypeScript, REST API, Tailwind...
```

AI suggests:

### Summary

Before:

> Fresh graduate passionate about web development.

After:

> Fresh graduate developer with experience building React and TypeScript web applications using REST APIs and modern UI frameworks.

---

### Skills reorder

Move:

* React
* TypeScript
* Tailwind
* API integration

to top.

---

### Experience bullet rewrite

Before:

> Built university project using React.

After:

> Developed responsive React application with reusable components and API integration for university final-year project.

---

## Key idea

AI should:

* rephrase
* optimize wording
* improve ATS keywords
* highlight relevant experience

AI should NOT:

* invent companies
* fake experience
* add fake metrics
* create fake projects

---

# Best UX for your SaaS

Instead of:

> “Generate Resume”

Do this:

## “Suggest Tailored Changes”

Then show:

* original version
* AI suggestion
* accept/reject buttons

This feels more professional and trustworthy.

---

# Smart MVP architecture

## Resume stored as structured JSON

Not raw text.

Example:

```json
{
  "summary": "...",
  "skills": ["React", "Node.js"],
  "experience": [
    {
      "company": "...",
      "bullets": []
    }
  ]
}
```

Then AI modifies sections individually.

This is MUCH easier than editing entire documents.

---

# PDF generation approach

Recommended flow:

```txt
Resume JSON
   ↓
HTML template
   ↓
PDF export
```

This is the cleanest architecture.

Use:

* React HTML template
* Puppeteer/Playwright → PDF

Avoid:

* generating PDFs directly from AI text

---

# Feature priority

## Phase 1 (best MVP)

* Job tracker
* Resume editor
* AI suggestions
* PDF export

