import {
  DndContext,
  DragOverlay,
  KeyboardSensor,
  MouseSensor,
  TouchSensor,
  useDraggable,
  useDroppable,
  useSensor,
  useSensors,
  type DragEndEvent,
  type DragStartEvent,
} from '@dnd-kit/core'
import { useState, type KeyboardEventHandler } from 'react'
import { Link } from 'react-router-dom'
import { StatusMenu } from '../../components/StatusMenu'
import { StatusMark } from '../../components/ui/StatusMark'
import { formatShortDate } from '../../lib/format'
import { JOB_STATUSES, type JobStatus, type JobSummary } from '../../lib/types'
import { isStatus, resolveDrop, type JobColumns } from './boardModel'

interface JobBoardProps {
  columns: JobColumns
  /** Statuses to render; the rest are hidden. */
  visible: readonly JobStatus[]
  pending: ReadonlySet<string>
  onMove: (jobId: string, status: JobStatus) => void
}

function CardBody({ job }: { job: JobSummary }) {
  return (
    <>
      <span className="board-card-company">{job.company}</span>
      <span className="board-card-title">{job.title}</span>
      <span className="board-card-date">{formatShortDate(job.dateApplied ?? job.updatedAt)}</span>
    </>
  )
}

function BoardCard({
  job,
  busy,
  onMove,
}: {
  job: JobSummary
  busy: boolean
  onMove: (status: JobStatus) => void
}) {
  const { attributes, listeners, setNodeRef, setActivatorNodeRef, isDragging } = useDraggable({
    id: job.id,
    data: { status: job.status },
  })
  // Pointer/touch drag can start anywhere on the card; keyboard drag only from the handle.
  const { onKeyDown, ...pointerListeners } = listeners ?? {}

  return (
    <li
      ref={setNodeRef}
      className={`board-card${isDragging ? ' is-dragging' : ''}${busy ? ' is-busy' : ''}`}
      {...pointerListeners}
    >
      <div className="board-card-main">
        <Link to={`/jobs/${job.id}`} className="board-card-link">
          <CardBody job={job} />
        </Link>
      </div>
      <div className="board-card-tools">
        <button
          ref={setActivatorNodeRef}
          type="button"
          className="board-card-handle"
          aria-label={`Drag ${job.title} at ${job.company}`}
          onKeyDown={onKeyDown as KeyboardEventHandler<HTMLButtonElement> | undefined}
          {...attributes}
        >
          <svg width="12" height="12" viewBox="0 0 12 12" fill="currentColor" aria-hidden="true">
            <circle cx="4" cy="3" r="1" />
            <circle cx="8" cy="3" r="1" />
            <circle cx="4" cy="6" r="1" />
            <circle cx="8" cy="6" r="1" />
            <circle cx="4" cy="9" r="1" />
            <circle cx="8" cy="9" r="1" />
          </svg>
        </button>
        <StatusMenu
          compact
          value={job.status}
          onChange={onMove}
          disabled={busy}
          label={`${job.title} at ${job.company}`}
        />
      </div>
    </li>
  )
}

function BoardColumn({
  status,
  jobs,
  pending,
  onMove,
}: {
  status: JobStatus
  jobs: JobSummary[]
  pending: ReadonlySet<string>
  onMove: (jobId: string, status: JobStatus) => void
}) {
  const { setNodeRef, isOver } = useDroppable({ id: status })
  return (
    <section
      className={`board-column board-column--${status.toLowerCase()}${isOver ? ' is-over' : ''}`}
      aria-label={`${status}, ${jobs.length} ${jobs.length === 1 ? 'job' : 'jobs'}`}
    >
      <header className="board-column-head">
        <StatusMark status={status} />
        <span className="board-column-count">{jobs.length}</span>
      </header>
      <ul ref={setNodeRef} className="board-column-body">
        {jobs.map((job) => (
          <BoardCard
            key={job.id}
            job={job}
            busy={pending.has(job.id)}
            onMove={(next) => onMove(job.id, next)}
          />
        ))}
        {jobs.length === 0 ? (
          <li className="board-empty">{isOver ? 'Drop to move here' : 'Nothing here yet'}</li>
        ) : null}
      </ul>
    </section>
  )
}

export function JobBoard({ columns, visible, pending, onMove }: JobBoardProps) {
  const [activeJob, setActiveJob] = useState<JobSummary | null>(null)
  const sensors = useSensors(
    useSensor(MouseSensor, { activationConstraint: { distance: 6 } }),
    useSensor(TouchSensor, { activationConstraint: { delay: 220, tolerance: 8 } }),
    useSensor(KeyboardSensor),
  )

  function findJob(id: string | number): JobSummary | null {
    for (const status of JOB_STATUSES) {
      const found = columns[status].find((j) => j.id === id)
      if (found) return found
    }
    return null
  }

  function handleDragStart(event: DragStartEvent) {
    setActiveJob(findJob(event.active.id))
  }

  function handleDragEnd(event: DragEndEvent) {
    setActiveJob(null)
    const drop = resolveDrop(event.active.id, event.over?.id)
    if (drop) onMove(drop.jobId, drop.status)
  }

  const announcements = {
    onDragStart: ({ active }: DragStartEvent) => {
      const job = findJob(active.id)
      return job ? `Picked up ${job.title} at ${job.company}. Currently ${job.status}.` : undefined
    },
    onDragOver: ({ over }: { over: DragEndEvent['over'] }) =>
      isStatus(over?.id) ? `Over ${over?.id}.` : undefined,
    onDragEnd: ({ active, over }: DragEndEvent) => {
      const job = findJob(active.id)
      return job && isStatus(over?.id)
        ? `Moved ${job.title} to ${over?.id}.`
        : 'Dropped. No change.'
    },
    onDragCancel: () => 'Move cancelled.',
  }

  return (
    <DndContext
      sensors={sensors}
      onDragStart={handleDragStart}
      onDragEnd={handleDragEnd}
      onDragCancel={() => setActiveJob(null)}
      accessibility={{ announcements }}
    >
      <div className="board">
        {visible.map((status) => (
          <BoardColumn
            key={status}
            status={status}
            jobs={columns[status]}
            pending={pending}
            onMove={onMove}
          />
        ))}
      </div>
      <DragOverlay dropAnimation={{ duration: 180, easing: 'cubic-bezier(0.2, 0, 0, 1)' }}>
        {activeJob ? (
          <div className="board-card board-card--overlay">
            <div className="board-card-main">
              <span className="board-card-link">
                <CardBody job={activeJob} />
              </span>
            </div>
          </div>
        ) : null}
      </DragOverlay>
    </DndContext>
  )
}
