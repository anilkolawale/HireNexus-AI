import { useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { motion, AnimatePresence } from 'framer-motion'
import toast from 'react-hot-toast'
import {
  Calendar, Clock, Video, Star, ChevronRight,
  CheckCircle2, XCircle, AlertCircle, Sparkles,
  MessageSquare, X, Loader2, ExternalLink, Download, Send
} from 'lucide-react'
import { interviewsApi } from '../../api/endpoints/interviews.api'
import api from '../../api/axiosClient'
import type { Interview } from '../../types/interview.types'

/* ─── Helpers ────────────────────────────────────────────── */
function statusConfig(result: string) {
  switch (result) {
    case 'Passed':  return { icon: CheckCircle2, color: 'text-emerald-400', bg: 'bg-emerald-500/10 border-emerald-500/30', label: 'Passed' }
    case 'Failed':  return { icon: XCircle,      color: 'text-red-400',     bg: 'bg-red-500/10 border-red-500/30',     label: 'Failed' }
    default:        return { icon: AlertCircle,  color: 'text-amber-400',   bg: 'bg-amber-500/10 border-amber-500/30', label: 'Pending' }
  }
}

function StarRating({ value, onChange }: { value: number; onChange?: (v: number) => void }) {
  const [hover, setHover] = useState(0)
  return (
    <div className="flex gap-1">
      {[1, 2, 3, 4, 5].map(s => (
        <button
          key={s}
          type="button"
          onClick={() => onChange?.(s)}
          onMouseEnter={() => onChange && setHover(s)}
          onMouseLeave={() => onChange && setHover(0)}
          className={`transition-all ${onChange ? 'cursor-pointer' : 'cursor-default'}`}
        >
          <Star
            className={`w-5 h-5 transition-all ${
              s <= (hover || value)
                ? 'text-amber-400 fill-amber-400'
                : 'text-white/20'
            }`}
          />
        </button>
      ))}
    </div>
  )
}

/* ─── Feedback Modal ─────────────────────────────────────── */
function FeedbackModal({ interview, onClose }: { interview: Interview; onClose: () => void }) {
  const qc = useQueryClient()
  const [rating,     setRating]     = useState(interview.feedback?.rating ?? 3)
  const [strengths,  setStrengths]  = useState(interview.feedback?.strengths ?? '')
  const [weaknesses, setWeaknesses] = useState(interview.feedback?.weaknesses ?? '')
  const [comments,   setComments]   = useState(interview.feedback?.comments ?? '')
  const [recommend,  setRecommend]  = useState(interview.feedback?.recommend ?? true)

  const { mutate: submit, isPending } = useMutation({
    mutationFn: () => interviewsApi.submitFeedback(interview.id, {
      rating, strengths, weaknesses, comments, recommend,
      result: recommend ? 'Passed' : 'Failed',
    }),
    onSuccess: () => {
      toast.success('Feedback submitted!')
      qc.invalidateQueries({ queryKey: ['interviews', 'my'] })
      onClose()
    },
    onError: () => toast.error('Could not submit feedback'),
  })

  return (
    <AnimatePresence>
      <motion.div
        initial={{ opacity: 0 }}
        animate={{ opacity: 1 }}
        exit={{ opacity: 0 }}
        className="fixed inset-0 bg-black/60 backdrop-blur-sm z-50 flex items-center justify-center p-4"
        onClick={e => e.target === e.currentTarget && onClose()}
      >
        <motion.div
          initial={{ opacity: 0, scale: 0.92, y: 16 }}
          animate={{ opacity: 1, scale: 1, y: 0 }}
          exit={{ opacity: 0, scale: 0.92, y: 16 }}
          transition={{ type: 'spring', stiffness: 300, damping: 30 }}
          className="w-full max-w-lg rounded-2xl overflow-hidden"
          style={{
            background: 'linear-gradient(135deg, rgba(15,20,40,0.99) 0%, rgba(10,15,30,0.99) 100%)',
            border: '1px solid rgba(99,102,241,0.2)',
          }}
        >
          {/* Header */}
          <div className="flex items-center justify-between px-6 py-4 border-b border-white/5 bg-gradient-to-r from-indigo-600/10 to-transparent">
            <div className="flex items-center gap-2">
              <MessageSquare className="w-4 h-4 text-indigo-400" />
              <p className="text-sm font-bold text-white">Interview Feedback</p>
            </div>
            <button onClick={onClose} className="w-7 h-7 rounded-lg bg-white/5 hover:bg-white/10 flex items-center justify-center transition-colors">
              <X className="w-3.5 h-3.5 text-white/50" />
            </button>
          </div>

          <div className="p-6 space-y-5">
            <div>
              <p className="text-xs text-white/40 mb-2 font-semibold uppercase tracking-wider">Overall Rating</p>
              <StarRating value={rating} onChange={setRating} />
            </div>

            <div className="grid grid-cols-2 gap-4">
              <div className="space-y-1.5">
                <label className="text-xs text-emerald-400 font-semibold uppercase tracking-wider">Strengths</label>
                <textarea
                  value={strengths}
                  onChange={e => setStrengths(e.target.value)}
                  placeholder="What stood out positively..."
                  rows={3}
                  className="w-full bg-white/5 border border-white/10 rounded-xl px-3 py-2.5 text-xs text-white placeholder-white/25 focus:outline-none focus:border-emerald-500/50 resize-none"
                />
              </div>
              <div className="space-y-1.5">
                <label className="text-xs text-red-400 font-semibold uppercase tracking-wider">Concerns</label>
                <textarea
                  value={weaknesses}
                  onChange={e => setWeaknesses(e.target.value)}
                  placeholder="Areas of concern..."
                  rows={3}
                  className="w-full bg-white/5 border border-white/10 rounded-xl px-3 py-2.5 text-xs text-white placeholder-white/25 focus:outline-none focus:border-red-500/50 resize-none"
                />
              </div>
            </div>

            <div className="space-y-1.5">
              <label className="text-xs text-white/40 font-semibold uppercase tracking-wider">Additional Comments</label>
              <textarea
                value={comments}
                onChange={e => setComments(e.target.value)}
                placeholder="Any other observations..."
                rows={2}
                className="w-full bg-white/5 border border-white/10 rounded-xl px-3 py-2.5 text-xs text-white placeholder-white/25 focus:outline-none focus:border-indigo-500/50 resize-none"
              />
            </div>

            {/* Recommendation toggle */}
            <div className="flex items-center justify-between bg-white/[0.04] rounded-xl px-4 py-3 border border-white/8">
              <p className="text-sm text-white/70 font-medium">Recommend for next round?</p>
              <button
                type="button"
                onClick={() => setRecommend(r => !r)}
                className={`relative w-11 h-6 rounded-full transition-all ${recommend ? 'bg-emerald-500' : 'bg-white/20'}`}
              >
                <span className={`absolute top-0.5 left-0.5 w-5 h-5 rounded-full bg-white shadow transition-all ${recommend ? 'translate-x-5' : 'translate-x-0'}`} />
              </button>
            </div>

            <motion.button
              whileHover={{ scale: 1.01 }}
              whileTap={{ scale: 0.99 }}
              onClick={() => submit()}
              disabled={isPending}
              className="w-full flex items-center justify-center gap-2 bg-gradient-to-r from-indigo-600 to-indigo-500 hover:from-indigo-500 hover:to-indigo-400 disabled:opacity-50 text-white font-semibold text-sm py-3 rounded-xl shadow-lg transition-all"
            >
              {isPending ? <><Loader2 className="w-4 h-4 animate-spin" /> Submitting...</> : 'Submit Feedback'}
            </motion.button>
          </div>
        </motion.div>
      </motion.div>
    </AnimatePresence>
  )
}

/* ─── Interview card ─────────────────────────────────────── */
function InterviewCard({ interview }: { interview: Interview }) {
  const [feedbackOpen, setFeedbackOpen] = useState(false)
  const [sendingInvite, setSendingInvite] = useState(false)
  const { icon: StatusIcon, color, bg, label } = statusConfig(interview.result ?? 'Pending')
  const isPast    = new Date(interview.scheduledAtUtc) < new Date()
  const hasFeedback = !!interview.feedback

  const scheduledDate = new Date(interview.scheduledAtUtc)
  const dateStr = scheduledDate.toLocaleDateString('en-US', { weekday: 'short', month: 'short', day: 'numeric' })
  const timeStr = scheduledDate.toLocaleTimeString('en-US', { hour: '2-digit', minute: '2-digit' })

  async function downloadIcs() {
    try {
      const res = await api.get(`/interviews/${interview.id}/ics`, { responseType: 'blob' })
      const url = URL.createObjectURL(new Blob([res.data], { type: 'text/calendar' }))
      const a = document.createElement('a')
      a.href = url
      a.download = `interview-${interview.id.slice(-8)}.ics`
      a.click()
      URL.revokeObjectURL(url)
      toast.success('Calendar file downloaded — open it to add to your calendar')
    } catch {
      toast.error('Failed to download calendar file')
    }
  }

  async function sendCalendarInvite() {
    setSendingInvite(true)
    try {
      await api.post(`/interviews/${interview.id}/send-invite`)

      toast.success('📅 Calendar invite sent to candidate and interviewer!')
    } catch {
      toast.error('Failed to send calendar invite')
    } finally {
      setSendingInvite(false)
    }
  }

  return (
    <>
      <motion.div
        initial={{ opacity: 0, y: 12 }}
        animate={{ opacity: 1, y: 0 }}
        className="group rounded-2xl border border-white/8 hover:border-indigo-500/30 p-5 transition-all"
        style={{ background: 'linear-gradient(135deg, rgba(255,255,255,0.04) 0%, rgba(255,255,255,0.02) 100%)' }}
      >
        <div className="flex items-start justify-between gap-4">
          {/* Left: Date block */}
          <div className="flex items-start gap-4">
            <div className={`w-14 text-center rounded-xl p-2 border ${isPast ? 'border-white/10 bg-white/5' : 'border-indigo-500/30 bg-indigo-500/10'}`}>
              <p className="text-[10px] text-white/40 uppercase tracking-wider font-bold">
                {scheduledDate.toLocaleDateString('en-US', { month: 'short' })}
              </p>
              <p className={`text-2xl font-bold leading-none ${isPast ? 'text-white/50' : 'text-indigo-300'}`}>
                {scheduledDate.getDate()}
              </p>
            </div>

            <div className="space-y-1">
              <p className="text-sm font-bold text-white">{interview.candidateName ?? `Interview #${interview.id.slice(-6)}`}</p>
              <div className="flex items-center gap-3 text-xs text-white/40">
                <span className="flex items-center gap-1"><Clock className="w-3 h-3" />{timeStr}</span>
                <span className="flex items-center gap-1"><Calendar className="w-3 h-3" />{dateStr}</span>
                <span>{interview.durationMinutes}min</span>
              </div>
              {interview.meetingLink && (
                <a
                  href={interview.meetingLink}
                  target="_blank"
                  rel="noreferrer"
                  className="inline-flex items-center gap-1 text-[11px] text-indigo-400 hover:text-indigo-300 transition-colors"
                >
                  <Video className="w-3 h-3" /> Join Meeting <ExternalLink className="w-2.5 h-2.5" />
                </a>
              )}
            </div>
          </div>

          {/* Right: Status + actions */}
          <div className="flex flex-col items-end gap-2">
            <span className={`flex items-center gap-1.5 text-[11px] font-semibold border rounded-full px-3 py-1 ${bg} ${color}`}>
              <StatusIcon className="w-3 h-3" />
              {label}
            </span>

            {/* Calendar actions */}
            <div className="flex gap-1.5">
              <motion.button
                whileHover={{ scale: 1.04 }}
                whileTap={{ scale: 0.96 }}
                onClick={downloadIcs}
                title="Download .ics calendar file"
                className="flex items-center gap-1 text-[11px] text-white/50 bg-white/5 hover:bg-indigo-500/15 border border-white/10 hover:border-indigo-500/30 hover:text-indigo-300 rounded-lg px-2.5 py-1.5 transition-all"
              >
                <Download className="w-3 h-3" /> .ics
              </motion.button>
              {!isPast && (
                <motion.button
                  whileHover={{ scale: 1.04 }}
                  whileTap={{ scale: 0.96 }}
                  onClick={sendCalendarInvite}
                  disabled={sendingInvite}
                  title="Email calendar invite to candidate & interviewer"
                  className="flex items-center gap-1 text-[11px] text-white/50 bg-white/5 hover:bg-indigo-500/15 border border-white/10 hover:border-indigo-500/30 hover:text-indigo-300 rounded-lg px-2.5 py-1.5 transition-all disabled:opacity-50"
                >
                  {sendingInvite ? <Loader2 className="w-3 h-3 animate-spin" /> : <Send className="w-3 h-3" />}
                  {sendingInvite ? '' : 'Invite'}
                </motion.button>
              )}
            </div>

            {isPast && !hasFeedback && (
              <motion.button
                whileHover={{ scale: 1.03 }}
                whileTap={{ scale: 0.97 }}
                onClick={() => setFeedbackOpen(true)}
                className="flex items-center gap-1 text-[11px] text-white/50 bg-white/5 hover:bg-white/10 border border-white/10 hover:border-indigo-500/30 hover:text-indigo-300 rounded-lg px-3 py-1.5 transition-all"
              >
                <MessageSquare className="w-3 h-3" /> Add Feedback
              </motion.button>
            )}

            {hasFeedback && (
              <div className="flex items-center gap-1">
                <StarRating value={interview.feedback!.rating} />
              </div>
            )}
          </div>
        </div>

        {/* Feedback summary if exists */}
        {hasFeedback && interview.feedback!.strengths && (
          <div className="mt-4 pt-4 border-t border-white/5">
            <p className="text-xs text-white/30 font-semibold mb-1">Your feedback</p>
            <p className="text-xs text-white/50 line-clamp-2">{interview.feedback!.strengths}</p>
          </div>
        )}
      </motion.div>

      {feedbackOpen && (
        <FeedbackModal interview={interview} onClose={() => setFeedbackOpen(false)} />
      )}
    </>
  )
}

/* ─── Main page ──────────────────────────────────────────── */
export default function MySchedulePage() {
  const [filter, setFilter] = useState<'all' | 'upcoming' | 'past'>('all')

  const { data: interviews = [], isLoading, refetch } = useQuery({
    queryKey: ['interviews', 'my'],
    queryFn:  () => interviewsApi.getMySchedule(),
    staleTime: 0,
    refetchOnWindowFocus: true,
  })

  const now = new Date()
  const filtered = (interviews as Interview[]).filter((i: Interview) => {
    const d = new Date(i.scheduledAtUtc)
    if (filter === 'upcoming') return d >= now
    if (filter === 'past')     return d < now
    return true
  }).sort((a: Interview, b: Interview) => new Date(b.scheduledAtUtc).getTime() - new Date(a.scheduledAtUtc).getTime())

  const upcoming = (interviews as Interview[]).filter((i: Interview) => new Date(i.scheduledAtUtc) >= now).length

  return (
    <div className="min-h-full space-y-6">
      {/* Header */}
      <div className="flex items-start justify-between flex-wrap gap-4">
        <div>
          <h1 className="text-2xl font-bold text-white tracking-tight flex items-center gap-2">
            <Calendar className="w-6 h-6 text-indigo-400" />
            My Interview Schedule
          </h1>
          <p className="text-white/40 text-sm mt-1">
            {interviews.length} total · {upcoming} upcoming
          </p>
        </div>

        <button
          onClick={() => refetch()}
          className="flex items-center gap-2 text-xs bg-white/5 border border-white/10 hover:bg-white/10 text-white/70 px-3 py-2 rounded-xl transition-all"
        >
          <Sparkles className="w-3.5 h-3.5 text-indigo-400" /> Refresh Schedule
        </button>
      </div>


      {/* Filter tabs */}
      <div className="flex gap-2">
        {(['all', 'upcoming', 'past'] as const).map(f => (
          <button
            key={f}
            onClick={() => setFilter(f)}
            className={`px-4 py-2 rounded-xl text-sm font-medium border transition-all ${
              filter === f
                ? 'bg-indigo-500/15 border-indigo-500/40 text-indigo-300'
                : 'bg-white/[0.03] border-white/10 text-white/50 hover:text-white/70'
            }`}
          >
            {f.charAt(0).toUpperCase() + f.slice(1)}
          </button>
        ))}
      </div>

      {/* Interview list */}
      {isLoading ? (
        <div className="flex items-center justify-center py-20">
          <div className="w-8 h-8 rounded-full border-2 border-indigo-500/30 border-t-indigo-500 animate-spin" />
        </div>
      ) : filtered.length === 0 ? (
        <div className="flex flex-col items-center justify-center py-24 gap-3">
          <div className="w-16 h-16 rounded-2xl bg-white/5 flex items-center justify-center">
            <Calendar className="w-8 h-8 text-white/20" />
          </div>
          <p className="text-white/40 font-medium">No interviews {filter !== 'all' ? filter : ''} yet</p>
        </div>
      ) : (
        <div className="space-y-3">
          {filtered.map(i => <InterviewCard key={i.id} interview={i} />)}
        </div>
      )}
    </div>
  )
}
