import { useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { Link, useNavigate } from 'react-router-dom'
import { motion, AnimatePresence } from 'framer-motion'
import toast from 'react-hot-toast'
import {
  Search, Plus, MapPin, Briefcase, Users, Clock,
  TrendingUp, ChevronRight, Filter, Star
} from 'lucide-react'
import { jobsApi } from '../../api/endpoints/jobs.api'
import { applicationsApi } from '../../api/endpoints/applications.api'
import { useAppSelector } from '../../app/hooks'
import type { JobListItem, JobStatus, EmploymentType } from '../../types/job.types'

const STATUS_OPTIONS: { label: string; value: string }[] = [
  { label: 'All Status', value: '' },
  { label: 'Published', value: 'Published' },
  { label: 'Draft', value: 'Draft' },
  { label: 'Closed', value: 'Closed' },
  { label: 'Paused', value: 'Paused' },
]

const EMP_TYPE_OPTIONS: { label: string; value: string }[] = [
  { label: 'All Types', value: '' },
  { label: 'Full Time', value: 'FullTime' },
  { label: 'Part Time', value: 'PartTime' },
  { label: 'Contract', value: 'Contract' },
  { label: 'Internship', value: 'Internship' },
  { label: 'Remote', value: 'Remote' },
]

function statusConfig(status: JobStatus) {
  switch (status) {
    case 'Published':
      return { label: 'Open', cls: 'bg-emerald-500/15 text-emerald-400 border-emerald-500/30' }
    case 'Draft':
      return { label: 'Draft', cls: 'bg-amber-500/15 text-amber-400 border-amber-500/30' }
    case 'Closed':
      return { label: 'Closed', cls: 'bg-red-500/15 text-red-400 border-red-500/30' }
    default:
      return { label: status, cls: 'bg-gray-500/15 text-gray-400 border-gray-500/30' }
  }
}

function empTypeLabel(type: EmploymentType) {
  return type === 'FullTime' ? 'Full Time' : type === 'PartTime' ? 'Part Time' : type
}

function daysAgo(dateStr: string) {
  const diff = Date.now() - new Date(dateStr).getTime()
  const days = Math.floor(diff / 86400000)
  if (days === 0) return 'Today'
  if (days === 1) return '1d ago'
  return `${days}d ago`
}

function SkeletonCard() {
  return (
    <div className="rounded-2xl border border-white/5 bg-white/5 p-5 animate-pulse">
      <div className="flex justify-between mb-3">
        <div className="h-5 w-40 bg-white/10 rounded-lg" />
        <div className="h-5 w-16 bg-white/10 rounded-full" />
      </div>
      <div className="flex gap-2 mb-4">
        <div className="h-4 w-24 bg-white/10 rounded-md" />
        <div className="h-4 w-20 bg-white/10 rounded-md" />
      </div>
      <div className="h-px bg-white/5 mb-4" />
      <div className="flex justify-between">
        <div className="h-4 w-28 bg-white/10 rounded-md" />
        <div className="h-4 w-20 bg-white/10 rounded-md" />
      </div>
    </div>
  )
}

interface JobCardProps {
  job: JobListItem
  isRecruiter: boolean
  onApply?: () => void
  applying?: boolean
  onAction?: (action: string) => void
  actioning?: boolean
}

function JobCard({ job, isRecruiter, onApply, applying, onAction, actioning }: JobCardProps) {
  const navigate = useNavigate()
  const sc = statusConfig(job.status)

  return (
    <motion.div
      layout
      initial={{ opacity: 0, y: 20 }}
      animate={{ opacity: 1, y: 0 }}
      exit={{ opacity: 0, scale: 0.95 }}
      whileHover={{ scale: 1.018, y: -2 }}
      transition={{ duration: 0.2, ease: 'easeOut' }}
      onClick={() => isRecruiter && navigate(`/jobs/${job.id}/candidates`)}
      className={`
        group relative rounded-2xl border border-white/5 bg-gradient-to-br from-white/[0.06] to-white/[0.02]
        backdrop-blur-sm p-5 flex flex-col gap-3
        hover:border-indigo-500/30 hover:shadow-[0_8px_32px_rgba(99,102,241,0.15)]
        transition-all duration-300
        ${isRecruiter ? 'cursor-pointer' : ''}
      `}
    >
      {/* Accent glow on hover */}
      <div className="absolute inset-0 rounded-2xl bg-gradient-to-br from-indigo-500/0 to-indigo-500/0 group-hover:from-indigo-500/5 group-hover:to-transparent transition-all duration-300 pointer-events-none" />

      {/* Header */}
      <div className="flex items-start justify-between gap-2">
        <div className="flex-1 min-w-0">
          <h3 className="font-semibold text-white text-sm leading-snug line-clamp-2 group-hover:text-indigo-300 transition-colors">
            {job.title}
          </h3>
          <div className="flex items-center gap-1 mt-1">
            <span className="text-xs text-white/40 font-medium">{job.department}</span>
          </div>
        </div>
        <span className={`shrink-0 text-[10px] font-semibold px-2.5 py-1 rounded-full border uppercase tracking-wide ${sc.cls}`}>
          {sc.label}
        </span>
      </div>

      {/* Meta badges */}
      <div className="flex flex-wrap gap-2">
        <span className="flex items-center gap-1 text-[11px] text-white/50 bg-white/5 rounded-lg px-2 py-1">
          <MapPin className="w-3 h-3 text-indigo-400" />
          {job.location}
        </span>
        <span className="flex items-center gap-1 text-[11px] text-white/50 bg-white/5 rounded-lg px-2 py-1">
          <Briefcase className="w-3 h-3 text-emerald-400" />
          {empTypeLabel(job.employmentType)}
        </span>
      </div>

      {/* Divider */}
      <div className="h-px bg-white/5" />

      {/* Footer */}
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-3">
          <span className="flex items-center gap-1 text-[11px] text-white/40">
            <Users className="w-3 h-3" />
            {job.applicationCount} applicants
          </span>
          <span className="flex items-center gap-1 text-[11px] text-white/40">
            <Clock className="w-3 h-3" />
            {daysAgo(job.createdAtUtc)}
          </span>
        </div>

        {isRecruiter ? (
          <div className="flex items-center gap-2" onClick={(e) => e.stopPropagation()}>
            {job.status === 'Draft' && (
              <button
                onClick={() => onAction?.('publish')}
                disabled={actioning}
                className="text-[11px] text-emerald-400 hover:text-emerald-300 font-medium disabled:opacity-50 transition-colors"
              >
                Publish
              </button>
            )}
            {job.status === 'Published' && (
              <button
                onClick={() => onAction?.('close')}
                disabled={actioning}
                className="text-[11px] text-amber-400 hover:text-amber-300 font-medium disabled:opacity-50 transition-colors"
              >
                Close
              </button>
            )}
            <button
              onClick={() => onAction?.('duplicate')}
              disabled={actioning}
              className="text-[11px] text-white/40 hover:text-white/70 font-medium disabled:opacity-50 transition-colors"
            >
              Duplicate
            </button>
            <button
              onClick={() => onAction?.('delete')}
              disabled={actioning}
              className="text-[11px] text-red-400/60 hover:text-red-400 font-medium disabled:opacity-50 transition-colors"
            >
              Delete
            </button>
            <Link
              to={`/jobs/${job.id}/candidates`}
              className="flex items-center gap-0.5 text-[11px] text-indigo-400 hover:text-indigo-300 font-medium transition-colors"
              onClick={(e) => e.stopPropagation()}
            >
              View <ChevronRight className="w-3 h-3" />
            </Link>
          </div>
        ) : (
          <Link
            to={`/jobs/${job.id}/candidates`}
            className="flex items-center gap-0.5 text-[11px] text-indigo-400 hover:text-indigo-300 font-medium transition-colors"
            onClick={(e) => e.stopPropagation()}
          >
            View Details <ChevronRight className="w-3 h-3" />
          </Link>
        )}

      </div>
    </motion.div>
  )
}

function EmptyState() {
  return (
    <motion.div
      initial={{ opacity: 0, y: 20 }}
      animate={{ opacity: 1, y: 0 }}
      className="col-span-full flex flex-col items-center justify-center py-20 gap-4"
    >
      <div className="w-20 h-20 rounded-2xl bg-indigo-500/10 flex items-center justify-center">
        <Briefcase className="w-10 h-10 text-indigo-400/60" />
      </div>
      <div className="text-center">
        <p className="text-white/60 font-medium text-base">No jobs found</p>
        <p className="text-white/30 text-sm mt-1">Try adjusting your filters or create a new job</p>
      </div>
    </motion.div>
  )
}

export default function JobListPage() {
  const queryClient = useQueryClient()
  const user = useAppSelector((s) => s.auth.user)
  const isRecruiter = ['Recruiter', 'HRManager', 'SuperAdmin'].includes(user?.role ?? '')

  const [search, setSearch] = useState('')
  const [statusFilter, setStatusFilter] = useState('')
  const [typeFilter, setTypeFilter] = useState('')
  const [applyingId, setApplyingId] = useState<string | null>(null)
  const [actioningId, setActioningId] = useState<string | null>(null)

  const { data, isLoading } = useQuery({
    queryKey: ['jobs', search, statusFilter, typeFilter],
    queryFn: () =>
      jobsApi.getJobs({
        searchTerm: search || undefined,
        status: statusFilter || undefined,
        pageNumber: 1,
        pageSize: 24,
      }),
  })

  const jobs = data?.items ?? []

  // Filter by employment type client-side (API may not support it)
  const displayed = typeFilter
    ? jobs.filter((j) => j.employmentType === typeFilter)
    : jobs

  const invalidateJobs = () => queryClient.invalidateQueries({ queryKey: ['jobs'] })

  const handleApply = async (jobId: string) => {
    setApplyingId(jobId)
    try {
      await applicationsApi.applyToJob(jobId)
      toast.success('Application submitted — AI match score calculated')
    } catch (err: unknown) {
      const axErr = err as { response?: { data?: { message?: string } } }
      toast.error(axErr.response?.data?.message || 'Could not apply')
    } finally {
      setApplyingId(null)
    }
  }

  const handleAction = async (jobId: string, action: string) => {
    setActioningId(jobId)
    try {
      if (action === 'publish') {
        await jobsApi.publishJob(jobId)
        toast.success('Job published — now visible on the public board')
      } else if (action === 'close') {
        await jobsApi.closeJob(jobId)
        toast.success('Job closed')
      } else if (action === 'duplicate') {
        await jobsApi.duplicateJob(jobId)
        toast.success('Job duplicated as a new draft')
      } else if (action === 'delete') {
        if (!window.confirm('Delete this job? This cannot be undone.')) {
          setActioningId(null)
          return
        }
        await jobsApi.deleteJob(jobId)
        toast.success('Job deleted')
      }
      invalidateJobs()
    } catch (err: unknown) {
      const axErr = err as { response?: { data?: { message?: string } } }
      toast.error(axErr.response?.data?.message || `Could not ${action} job`)
    } finally {
      setActioningId(null)
    }
  }

  return (
    <div className="min-h-full">
      {/* Page header */}
      <div className="mb-8">
        <div className="flex items-center justify-between flex-wrap gap-4">
          <div>
            <h1 className="text-2xl font-bold text-white tracking-tight flex items-center gap-2">
              <TrendingUp className="w-6 h-6 text-indigo-400" />
              Jobs Board
            </h1>
            <p className="text-white/40 text-sm mt-1">
              {data?.totalCount ?? 0} total positions
            </p>
          </div>
          {isRecruiter && (
            <Link to="/jobs/new">
              <motion.button
                whileHover={{ scale: 1.03 }}
                whileTap={{ scale: 0.97 }}
                className="flex items-center gap-2 bg-gradient-to-r from-indigo-600 to-indigo-500 hover:from-indigo-500 hover:to-indigo-400 text-white px-5 py-2.5 rounded-xl font-semibold text-sm shadow-lg shadow-indigo-500/25 transition-all duration-200"
              >
                <Plus className="w-4 h-4" />
                Create Job
              </motion.button>
            </Link>
          )}
        </div>

        {/* Filter bar */}
        <div className="flex flex-wrap items-center gap-3 mt-6">
          <div className="relative flex-1 min-w-[200px] max-w-sm">
            <Search className="absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-white/30" />
            <input
              type="text"
              placeholder="Search jobs…"
              value={search}
              onChange={(e) => setSearch(e.target.value)}
              className="w-full bg-white/5 border border-white/10 rounded-xl pl-9 pr-4 py-2.5 text-sm text-white placeholder-white/30 focus:outline-none focus:border-indigo-500/50 focus:ring-1 focus:ring-indigo-500/30 transition-all"
            />
          </div>

          <div className="flex items-center gap-2">
            <Filter className="w-4 h-4 text-white/30" />
            <select
              value={statusFilter}
              onChange={(e) => setStatusFilter(e.target.value)}
              className="bg-white/5 border border-white/10 rounded-xl px-3 py-2.5 text-sm text-white/70 focus:outline-none focus:border-indigo-500/50 transition-all appearance-none cursor-pointer"
            >
              {STATUS_OPTIONS.map((o) => (
                <option key={o.value} value={o.value} className="bg-[#0f1829] text-white">
                  {o.label}
                </option>
              ))}
            </select>
            <select
              value={typeFilter}
              onChange={(e) => setTypeFilter(e.target.value)}
              className="bg-white/5 border border-white/10 rounded-xl px-3 py-2.5 text-sm text-white/70 focus:outline-none focus:border-indigo-500/50 transition-all appearance-none cursor-pointer"
            >
              {EMP_TYPE_OPTIONS.map((o) => (
                <option key={o.value} value={o.value} className="bg-[#0f1829] text-white">
                  {o.label}
                </option>
              ))}
            </select>
          </div>
        </div>
      </div>

      {/* Jobs grid */}
      <div className="grid grid-cols-1 md:grid-cols-2 xl:grid-cols-3 gap-4">
        <AnimatePresence mode="popLayout">
          {isLoading
            ? Array.from({ length: 6 }).map((_, i) => <SkeletonCard key={i} />)
            : displayed.length === 0
              ? <EmptyState />
              : displayed.map((job) => (
                  <JobCard
                    key={job.id}
                    job={job}
                    isRecruiter={isRecruiter}
                    onApply={() => handleApply(job.id)}
                    applying={applyingId === job.id}
                    onAction={(action) => handleAction(job.id, action)}
                    actioning={actioningId === job.id}
                  />
                ))
          }
        </AnimatePresence>
      </div>

      {/* Pagination hint */}
      {data && data.totalPages > 1 && (
        <p className="text-center text-white/20 text-xs mt-8">
          Page {data.pageNumber} of {data.totalPages}
        </p>
      )}
    </div>
  )
}
