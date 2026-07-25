import { useEffect, useState } from 'react'
import { useParams } from 'react-router-dom'
import toast from 'react-hot-toast'
import { motion } from 'framer-motion'
import { Eye, EyeOff, Calendar } from 'lucide-react'
import { applicationsApi } from '../../api/endpoints/applications.api'
import { interviewsApi } from '../../api/endpoints/interviews.api'
import { usersApi, type UserListItem } from '../../api/endpoints/users.api'
import api from '../../api/axiosClient'
import type { RankedApplication } from '../../types/interview.types'
import JobBoardsPanel from '../jobs/JobBoardsPanel'

interface BlindConfig {
  isEnabled: boolean
  hideName: boolean
  hidePhoto: boolean
}

function ScheduleModal({ applicationId, onClose, onScheduled }: { applicationId: string; onClose: () => void; onScheduled: () => void }) {
  const [interviewers, setInterviewers] = useState<UserListItem[]>([])
  const [interviewerId, setInterviewerId] = useState('')
  const [roundName, setRoundName] = useState('Technical')
  const [dateTime, setDateTime] = useState('')
  const [duration, setDuration] = useState(60)
  const [meetingLink, setMeetingLink] = useState('')
  const [saving, setSaving] = useState(false)

  useEffect(() => {
    usersApi.getByRole('Interviewer').then(setInterviewers)
  }, [])

  const submit = async () => {
    if (!interviewerId || !dateTime) {
      toast.error('Pick an interviewer and a time')
      return
    }
    setSaving(true)
    try {
      await interviewsApi.schedule({
        applicationId,
        roundName,
        sequenceOrder: 1,
        interviewerId,
        scheduledAtUtc: new Date(dateTime).toISOString(),
        durationMinutes: duration,
        meetingLink: meetingLink || undefined
      })
      toast.success('Interview scheduled')
      onScheduled()
      onClose()
    } catch (err: any) {
      toast.error(err.response?.data?.message || 'Could not schedule')
    } finally {
      setSaving(false)
    }
  }

  return (
    <div className="fixed inset-0 bg-black/60 backdrop-blur-sm flex items-center justify-center z-50 p-4">
      <div className="bg-slate-900 border border-slate-700 rounded-2xl p-6 w-full max-w-md space-y-4 shadow-2xl">
        <h2 className="font-semibold text-white text-lg flex items-center gap-2">
          <Calendar className="w-5 h-5 text-indigo-400" /> Schedule Interview
        </h2>
        <input value={roundName} onChange={(e) => setRoundName(e.target.value)} placeholder="Round name (e.g. Technical)"
          className="w-full bg-slate-800 border border-slate-700 rounded-xl px-4 py-2.5 text-white text-sm focus:outline-none focus:border-indigo-500" />
        <select value={interviewerId} onChange={(e) => setInterviewerId(e.target.value)}
          className="w-full bg-slate-800 border border-slate-700 rounded-xl px-4 py-2.5 text-white text-sm focus:outline-none focus:border-indigo-500">
          <option value="">Select interviewer</option>
          {interviewers.map((u) => <option key={u.id} value={u.id}>{u.fullName}</option>)}
        </select>
        <input type="datetime-local" value={dateTime} onChange={(e) => setDateTime(e.target.value)}
          className="w-full bg-slate-800 border border-slate-700 rounded-xl px-4 py-2.5 text-white text-sm focus:outline-none focus:border-indigo-500" />
        <input type="number" value={duration} onChange={(e) => setDuration(+e.target.value)} placeholder="Duration (minutes)"
          className="w-full bg-slate-800 border border-slate-700 rounded-xl px-4 py-2.5 text-white text-sm focus:outline-none focus:border-indigo-500" />
        <input value={meetingLink} onChange={(e) => setMeetingLink(e.target.value)} placeholder="Meeting link (optional)"
          className="w-full bg-slate-800 border border-slate-700 rounded-xl px-4 py-2.5 text-white text-sm focus:outline-none focus:border-indigo-500" />
        <div className="flex justify-end gap-3 pt-2">
          <button onClick={onClose} className="text-sm px-4 py-2 rounded-xl border border-slate-600 text-slate-300 hover:bg-slate-800 transition-all">Cancel</button>
          <button onClick={submit} disabled={saving} className="text-sm px-4 py-2 rounded-xl bg-gradient-to-r from-indigo-600 to-indigo-500 text-white hover:from-indigo-500 hover:to-indigo-400 disabled:opacity-50 transition-all">
            {saving ? 'Scheduling...' : 'Schedule'}
          </button>
        </div>
      </div>
    </div>
  )
}

export default function RankedCandidatesPage() {
  const { jobId } = useParams<{ jobId: string }>()
  const [applications, setApplications] = useState<RankedApplication[]>([])
  const [loading, setLoading] = useState(true)
  const [scheduleFor, setScheduleFor] = useState<string | null>(null)
  const [blindConfig, setBlindConfig] = useState<BlindConfig>({ isEnabled: false, hideName: true, hidePhoto: true })
  const [togglingBlind, setTogglingBlind] = useState(false)
  const [activeTab, setActiveTab] = useState<'candidates' | 'job-boards'>('candidates')

  const load = () => {
    if (!jobId) return
    setLoading(true)
    applicationsApi.getRankedForJob(jobId)
      .then((data) => setApplications(data as RankedApplication[]))
      .finally(() => setLoading(false))
  }

  useEffect(() => {
    load()
    if (jobId) {
      api.get(`/blind-screening/jobs/${jobId}`)
        .then(res => setBlindConfig(res.data))
        .catch(() => {})
    }
  }, [jobId])

  async function toggleBlindScreening() {
    if (!jobId) return
    setTogglingBlind(true)
    try {
      const res = await api.put(`/blind-screening/jobs/${jobId}`, {
        isEnabled: !blindConfig.isEnabled,
        hideName: blindConfig.hideName,
        hidePhoto: blindConfig.hidePhoto,
        hideGender: false,
        hideEthnicity: false,
        hideAge: false,
      })

      setBlindConfig(res.data)
      toast.success(res.data.isEnabled
        ? '🫣 Blind screening ON — candidate names are hidden'
        : '👁 Blind screening OFF — full candidate info visible')
    } catch {
      toast.error('Failed to update blind screening setting')
    } finally {
      setTogglingBlind(false)
    }
  }

  const scoreColor = (score?: number) => {
    if (score == null) return 'bg-slate-700 text-slate-400'
    if (score >= 75) return 'bg-emerald-500/20 text-emerald-400 border border-emerald-500/30'
    if (score >= 50) return 'bg-amber-500/20 text-amber-400 border border-amber-500/30'
    return 'bg-red-500/20 text-red-400 border border-red-500/30'
  }

  const displayName = (app: RankedApplication, idx: number) => {
    if (blindConfig.isEnabled && blindConfig.hideName) {
      return `Candidate ${String.fromCharCode(65 + idx)}`
    }
    return app.candidateName
  }

  return (
    <div className="min-h-full space-y-6 p-6">
      {/* Header */}
      <div className="flex items-start justify-between gap-4 flex-wrap">
        <div>
          <h1 className="text-2xl font-bold text-white tracking-tight">Ranked Candidates</h1>
          <p className="text-white/40 text-sm mt-1">
            {applications.length} applicants · sorted by AI match score
          </p>
        </div>

        {/* Blind screening toggle */}
        <div className="flex items-center gap-3">
          <div className="flex items-center gap-2">
            {blindConfig.isEnabled ? (
              <EyeOff className="w-4 h-4 text-violet-400" />
            ) : (
              <Eye className="w-4 h-4 text-white/40" />
            )}
            <span className="text-sm text-white/60">Blind Screening</span>
          </div>
          <button
            onClick={toggleBlindScreening}
            disabled={togglingBlind}
            className={`relative w-12 h-6 rounded-full transition-all disabled:opacity-50 ${blindConfig.isEnabled ? 'bg-violet-600' : 'bg-slate-700'}`}
          >
            <div className={`absolute top-0.5 w-5 h-5 bg-white rounded-full shadow transition-transform ${blindConfig.isEnabled ? 'translate-x-6' : 'translate-x-0.5'}`} />
          </button>
        </div>
      </div>

      {/* Blind screening notice */}
      {blindConfig.isEnabled && (
        <div className="flex items-center gap-3 px-4 py-3 bg-violet-500/10 border border-violet-500/30 rounded-xl text-sm text-violet-300">
          <EyeOff className="w-4 h-4 flex-shrink-0" />
          <span>Blind screening is <strong>active</strong> — candidate names are anonymized to reduce unconscious bias during initial review.</span>
        </div>
      )}

      {/* Tabs */}
      <div className="flex gap-2">
        {(['candidates', 'job-boards'] as const).map(tab => (
          <button
            key={tab}
            onClick={() => setActiveTab(tab)}
            className={`px-4 py-2 rounded-xl text-sm font-medium border transition-all ${activeTab === tab ? 'bg-indigo-500/15 border-indigo-500/40 text-indigo-300' : 'bg-white/[0.03] border-white/10 text-white/50 hover:text-white/70'}`}
          >
            {tab === 'candidates' ? `👥 Candidates (${applications.length})` : '🌐 Job Boards'}
          </button>
        ))}
      </div>

      {/* Candidates tab */}
      {activeTab === 'candidates' && (
        <>
          {loading ? (
            <div className="flex justify-center py-10">
              <div className="w-7 h-7 rounded-full border-2 border-indigo-500/30 border-t-indigo-500 animate-spin" />
            </div>
          ) : applications.length === 0 ? (
            <div className="text-center py-16 text-white/40">
              <div className="text-4xl mb-3">👤</div>
              No applications yet for this job.
            </div>
          ) : (
            <div className="space-y-3">
              {applications.map((app, idx) => (
                <motion.div
                  key={app.id}
                  initial={{ opacity: 0, y: 8 }}
                  animate={{ opacity: 1, y: 0 }}
                  transition={{ delay: idx * 0.03 }}
                  className="flex items-center justify-between p-5 rounded-2xl border border-white/8 hover:border-indigo-500/30 transition-all"
                  style={{ background: 'linear-gradient(135deg, rgba(255,255,255,0.04) 0%, rgba(255,255,255,0.02) 100%)' }}
                >
                  <div className="flex items-center gap-4">
                    <span className="text-sm text-white/30 w-6 text-center font-mono">#{idx + 1}</span>
                    <div className={`w-10 h-10 rounded-xl flex items-center justify-center text-sm font-bold ${blindConfig.isEnabled && blindConfig.hidePhoto ? 'bg-violet-500/20 text-violet-300' : 'bg-gradient-to-br from-indigo-500 to-violet-500 text-white'}`}>
                      {blindConfig.isEnabled && blindConfig.hidePhoto ? '?' : (app.candidateName?.[0] ?? '?')}
                    </div>
                    <div>
                      <p className="font-semibold text-white">{displayName(app, idx)}</p>
                      <p className="text-xs text-white/40">{app.status} · Applied {new Date(app.createdAtUtc).toLocaleDateString()}</p>
                    </div>
                  </div>

                  <div className="flex items-center gap-3">
                    <span className={`text-sm font-bold px-3 py-1 rounded-full ${scoreColor(app.matchScore)}`}>
                      {app.matchScore != null ? `${app.matchScore}%` : '—'}
                    </span>
                    <button
                      onClick={() => setScheduleFor(app.id)}
                      className="flex items-center gap-1.5 text-xs bg-gradient-to-r from-indigo-600 to-indigo-500 text-white rounded-xl px-3 py-2 hover:from-indigo-500 hover:to-indigo-400 transition-all"
                    >
                      <Calendar className="w-3 h-3" /> Schedule
                    </button>
                  </div>
                </motion.div>
              ))}
            </div>
          )}
        </>
      )}

      {/* Job boards tab */}
      {activeTab === 'job-boards' && jobId && (
        <div className="p-6 rounded-2xl border border-white/8" style={{ background: 'linear-gradient(135deg, rgba(255,255,255,0.04) 0%, rgba(255,255,255,0.02) 100%)' }}>
          <JobBoardsPanel jobId={jobId} />
        </div>
      )}

      {scheduleFor && (
        <ScheduleModal applicationId={scheduleFor} onClose={() => setScheduleFor(null)} onScheduled={load} />
      )}
    </div>
  )
}
