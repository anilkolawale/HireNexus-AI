import { useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { motion, AnimatePresence } from 'framer-motion'
import toast from 'react-hot-toast'
import {
  Kanban, ChevronRight, X, ExternalLink,
  Clock, Star, Users, Plus, GitCompareArrows
} from 'lucide-react'
import { applicationsApi } from '../../api/endpoints/applications.api'
import type { ApplicationDetail, ApplicationStatus } from '../../types/candidate.types'
import { SkillGapModal } from './SkillGapModal'
import { CandidateComparisonModal } from './CandidateComparisonModal'

/* ─── Column configuration ──────────────────────────────── */
interface Column {
  id: ApplicationStatus
  label: string
  color: string
  headerGradient: string
}

const COLUMNS: Column[] = [
  { id: 'Applied',           label: 'Applied',       color: 'border-blue-500/40',   headerGradient: 'from-blue-600/20 to-blue-600/5' },
  { id: 'Screening',         label: 'Screening',     color: 'border-purple-500/40', headerGradient: 'from-purple-600/20 to-purple-600/5' },
  { id: 'Shortlisted',       label: 'Interview',     color: 'border-indigo-500/40', headerGradient: 'from-indigo-600/20 to-indigo-600/5' },
  { id: 'TechnicalInterview',label: 'Technical',     color: 'border-amber-500/40',  headerGradient: 'from-amber-600/20 to-amber-600/5' },
  { id: 'HRInterview',       label: 'HR Round',      color: 'border-orange-500/40', headerGradient: 'from-orange-600/20 to-orange-600/5' },
  { id: 'Offer',             label: 'Offer',         color: 'border-emerald-500/40',headerGradient: 'from-emerald-600/20 to-emerald-600/5' },
  { id: 'Hired',             label: 'Hired',         color: 'border-green-500/40',  headerGradient: 'from-green-600/20 to-green-600/5' },
  { id: 'Rejected',          label: 'Rejected',      color: 'border-red-500/40',    headerGradient: 'from-red-600/20 to-red-600/5' },
]

const STATUS_ORDER: ApplicationStatus[] = [
  'Applied', 'Screening', 'Shortlisted', 'TechnicalInterview',
  'HRInterview', 'Offer', 'Hired',
]

function nextStatus(current: ApplicationStatus): ApplicationStatus | null {
  const idx = STATUS_ORDER.indexOf(current)
  if (idx === -1 || idx >= STATUS_ORDER.length - 1) return null
  return STATUS_ORDER[idx + 1]
}

/* ─── Score badge ───────────────────────────────────────── */
function ScoreBadge({ score }: { score?: number }) {
  if (score == null) return null
  const cls =
    score >= 80 ? 'bg-emerald-500/20 text-emerald-400 border-emerald-500/30' :
    score >= 60 ? 'bg-amber-500/20 text-amber-400 border-amber-500/30' :
                  'bg-red-500/20 text-red-400 border-red-500/30'
  return (
    <span className={`flex items-center gap-1 text-[10px] font-bold border rounded-full px-2 py-0.5 ${cls}`}>
      <Star className="w-2.5 h-2.5" />
      {score}%
    </span>
  )
}

/* ─── Avatar initials ───────────────────────────────────── */
function Avatar({ name }: { name: string }) {
  const parts = name.split(' ')
  const initials = parts.length >= 2
    ? `${parts[0][0]}${parts[parts.length - 1][0]}`.toUpperCase()
    : name.substring(0, 2).toUpperCase()
  const hue = (name.charCodeAt(0) * 37) % 360
  return (
    <div
      className="w-8 h-8 rounded-full flex items-center justify-center text-xs font-bold text-white shrink-0"
      style={{ background: `hsl(${hue}, 65%, 45%)` }}
    >
      {initials}
    </div>
  )
}

/* ─── Candidate card ────────────────────────────────────── */
interface CandidateCardProps {
  app: ApplicationDetail
  onMoveForward: () => void
  onReject: () => void
  onViewSkillGap: () => void
  isMoving: boolean
}

function CandidateCard({ app, onMoveForward, onReject, onViewSkillGap, isMoving }: CandidateCardProps) {
  const daysInStage = Math.floor(
    (Date.now() - new Date(app.createdAtUtc).getTime()) / 86400000
  )
  const canMove = nextStatus(app.status) !== null && app.status !== 'Hired' && app.status !== 'Rejected'

  return (
    <motion.div
      layout
      initial={{ opacity: 0, scale: 0.95 }}
      animate={{ opacity: 1, scale: 1 }}
      exit={{ opacity: 0, scale: 0.9 }}
      whileHover={{ y: -2 }}
      className="group bg-white/[0.04] border border-white/8 hover:border-indigo-500/30 rounded-xl p-3 space-y-3 transition-all duration-200 hover:shadow-lg hover:shadow-indigo-500/10 cursor-default"
    >
      {/* Top row */}
      <div className="flex items-start gap-2">
        <Avatar name={(app as any).candidateName || app.jobTitle} />
        <div className="flex-1 min-w-0">
          <p className="text-xs font-semibold text-white/90 truncate">{(app as any).candidateName || `Candidate #${app.id.slice(-6)}`}</p>
          <p className="text-[10px] text-white/40 truncate">{app.jobTitle}</p>
        </div>
        <ScoreBadge score={app.matchScore} />
      </div>


      {/* Time in stage */}
      <div className="flex items-center gap-1 text-[10px] text-white/30">
        <Clock className="w-3 h-3" />
        <span>{daysInStage === 0 ? 'Today' : `${daysInStage}d in stage`}</span>
      </div>

      {/* Missing skills preview */}
      {app.missingSkills.length > 0 && (
        <div className="flex flex-wrap gap-1">
          {app.missingSkills.slice(0, 3).map((s) => (
            <span key={s} className="text-[9px] bg-red-500/10 text-red-400/70 border border-red-500/20 rounded px-1.5 py-0.5">
              -{s}
            </span>
          ))}
          {app.missingSkills.length > 3 && (
            <span className="text-[9px] text-white/20">+{app.missingSkills.length - 3} more</span>
          )}
        </div>
      )}

      {/* Action buttons */}
      <div className="flex items-center gap-1.5 opacity-0 group-hover:opacity-100 transition-opacity">
        {canMove && (
          <button
            onClick={onMoveForward}
            disabled={isMoving}
            title="Move to next stage"
            className="flex-1 flex items-center justify-center gap-1 text-[10px] text-indigo-400 bg-indigo-500/10 hover:bg-indigo-500/20 border border-indigo-500/20 rounded-lg px-2 py-1.5 transition-colors disabled:opacity-40"
          >
            <ChevronRight className="w-3 h-3" /> Move
          </button>
        )}
        <button
          onClick={onViewSkillGap}
          title="Skill gap analysis"
          className="flex items-center justify-center gap-1 text-[10px] text-white/40 bg-white/5 hover:bg-white/10 border border-white/10 rounded-lg px-2 py-1.5 transition-colors"
        >
          <ExternalLink className="w-3 h-3" />
        </button>
        {app.status !== 'Rejected' && app.status !== 'Hired' && (
          <button
            onClick={onReject}
            disabled={isMoving}
            title="Reject candidate"
            className="flex items-center justify-center gap-1 text-[10px] text-red-400/60 bg-red-500/5 hover:bg-red-500/15 border border-red-500/10 rounded-lg px-2 py-1.5 transition-colors disabled:opacity-40"
          >
            <X className="w-3 h-3" />
          </button>
        )}
      </div>
    </motion.div>
  )
}

/* ─── Column component ──────────────────────────────────── */
interface KanbanColumnProps {
  column: Column
  apps: ApplicationDetail[]
  onMoveForward: (app: ApplicationDetail) => void
  onReject: (app: ApplicationDetail) => void
  onViewSkillGap: (app: ApplicationDetail) => void
  movingId: string | null
}

function KanbanColumn({ column, apps, onMoveForward, onReject, onViewSkillGap, movingId }: KanbanColumnProps) {
  return (
    <div className="flex-shrink-0 w-64 flex flex-col gap-2">
      {/* Column header */}
      <div className={`rounded-xl border ${column.color} bg-gradient-to-b ${column.headerGradient} px-3 py-2.5 flex items-center justify-between`}>
        <span className="text-xs font-bold text-white/80 uppercase tracking-wider">{column.label}</span>
        <span className="w-5 h-5 rounded-full bg-white/10 flex items-center justify-center text-[10px] font-bold text-white/60">
          {apps.length}
        </span>
      </div>

      {/* Cards */}
      <div className="flex flex-col gap-2 min-h-[120px]">
        <AnimatePresence mode="popLayout">
          {apps.length === 0 ? (
            <motion.div
              initial={{ opacity: 0 }}
              animate={{ opacity: 1 }}
              className="flex flex-col items-center justify-center py-8 gap-2 border border-dashed border-white/10 rounded-xl text-center"
            >
              <Users className="w-5 h-5 text-white/15" />
              <p className="text-[10px] text-white/20">No candidates</p>
              <button className="flex items-center gap-1 text-[10px] text-indigo-400/50 hover:text-indigo-400 transition-colors">
                <Plus className="w-3 h-3" /> Add
              </button>
            </motion.div>
          ) : (
            apps.map((app) => (
              <CandidateCard
                key={app.id}
                app={app}
                onMoveForward={() => onMoveForward(app)}
                onReject={() => onReject(app)}
                onViewSkillGap={() => onViewSkillGap(app)}
                isMoving={movingId === app.id}
              />
            ))
          )}
        </AnimatePresence>
      </div>
    </div>
  )
}

/* ─── Main Pipeline Page ────────────────────────────────── */
export default function PipelinePage() {
  const queryClient = useQueryClient()
  const [movingId, setMovingId] = useState<string | null>(null)
  const [skillGapApp, setSkillGapApp] = useState<ApplicationDetail | null>(null)
  const [compareOpen, setCompareOpen] = useState(false)

  const { data: applications = [], isLoading } = useQuery({
    queryKey: ['applications', 'pipeline'],
    queryFn: () => applicationsApi.getAllPipeline(),
    staleTime: 0,
    refetchOnWindowFocus: true,
  })

  const statusMutation = useMutation({
    mutationFn: ({ id, status }: { id: string; status: string }) =>
      applicationsApi.changeStatus(id, status),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['applications', 'pipeline'] })
    },
    onError: () => toast.error('Could not update status'),
    onSettled: () => setMovingId(null),
  })


  const handleMoveForward = (app: ApplicationDetail) => {
    const next = nextStatus(app.status)
    if (!next) return
    setMovingId(app.id)
    statusMutation.mutate({ id: app.id, status: next })
    toast.success(`Moved to ${next}`)
  }

  const handleReject = (app: ApplicationDetail) => {
    setMovingId(app.id)
    statusMutation.mutate({ id: app.id, status: 'Rejected' })
    toast.success('Candidate rejected')
  }

  const grouped = COLUMNS.reduce<Record<string, ApplicationDetail[]>>((acc, col) => {
    acc[col.id] = applications.filter((a) => a.status === col.id)
    return acc
  }, {})

  return (
    <div className="min-h-full flex flex-col">
      {/* Header */}
      <div className="mb-6 shrink-0">
        <div className="flex items-center gap-3">
          <div className="w-9 h-9 rounded-xl bg-gradient-to-br from-indigo-500 to-purple-600 flex items-center justify-center shadow-lg shadow-indigo-500/25">
            <Kanban className="w-5 h-5 text-white" />
          </div>
          <div>
            <h1 className="text-2xl font-bold text-white tracking-tight">Application Pipeline</h1>
            <p className="text-white/40 text-sm mt-0.5">
              {applications.length} total candidates across {COLUMNS.length} stages
            </p>
          </div>
          <motion.button
            whileHover={{ scale: 1.03 }}
            whileTap={{ scale: 0.97 }}
            onClick={() => setCompareOpen(true)}
            disabled={applications.length < 2}
            className="ml-auto flex items-center gap-2 bg-white/5 hover:bg-white/10 border border-white/10 hover:border-indigo-500/30 text-white/60 hover:text-indigo-300 px-4 py-2 rounded-xl text-sm font-medium transition-all disabled:opacity-30"
          >
            <GitCompareArrows className="w-4 h-4" />
            Compare Candidates
          </motion.button>
        </div>
      </div>

      {/* Loading */}
      {isLoading && (
        <div className="flex items-center justify-center py-20">
          <div className="flex flex-col items-center gap-3">
            <div className="w-10 h-10 rounded-full border-2 border-indigo-500/30 border-t-indigo-500 animate-spin" />
            <p className="text-white/30 text-sm">Loading pipeline…</p>
          </div>
        </div>
      )}

      {/* Kanban board */}
      {!isLoading && (
        <div className="flex-1 overflow-x-auto pb-6">
          <div className="flex gap-3 min-w-max">
            {COLUMNS.map((column) => (
              <KanbanColumn
                key={column.id}
                column={column}
                apps={grouped[column.id] ?? []}
                onMoveForward={handleMoveForward}
                onReject={handleReject}
                onViewSkillGap={(app) => setSkillGapApp(app)}
                movingId={movingId}
              />
            ))}
          </div>
        </div>
      )}

      {/* Skill Gap Modal */}
      {skillGapApp && (
        <SkillGapModal
          applicationId={skillGapApp.id}
          isOpen={!!skillGapApp}
          onClose={() => setSkillGapApp(null)}
        />
      )}

      {/* Candidate Comparison Modal */}
      <CandidateComparisonModal
        jobId=""
        jobTitle="All Active Positions"
        candidates={applications.map(a => ({
          applicationId: a.id,
          name: `Candidate #${a.id.slice(-6)}`,
          matchScore: a.matchScore,
        }))}
        isOpen={compareOpen}
        onClose={() => setCompareOpen(false)}
      />
    </div>
  )
}
