import { useState, useEffect } from 'react'
import { useQuery, useMutation } from '@tanstack/react-query'
import toast from 'react-hot-toast'
import api from '../../api/axiosClient'
import { motion, AnimatePresence } from 'framer-motion'
import { applicationsApi } from '../../api/endpoints/applications.api'
import {
  Video, Code, Plus, Trash2, ChevronDown, ChevronUp,
  Send, Clock, CheckCircle2, Loader2, Eye
} from 'lucide-react'


interface Job {
  id: string
  title: string
}

interface Template {
  id: string
  title: string
  type: string
  durationMinutes: number
  instructions?: string
  hackerRankTestId?: string
  questionCount: number
  questions: Question[]
}

interface Question {
  id: string
  questionText: string
  thinkTimeSecs: number
  recordingTimeSecs: number
  order: number
}

interface Application {
  id: string
  candidateName: string
  jobTitle: string
  status: string
}

const ASSESSMENT_TYPES = [
  { value: 'Video', label: '🎥 Video Interview', description: 'Async video questions with think + recording time' },
  { value: 'CodingTest', label: '💻 Coding Test', description: 'HackerRank technical test with invite link' },
  { value: 'Mixed', label: '🎯 Mixed', description: 'Both video questions and coding test' },
]

const DEFAULT_QUESTIONS = [
  'Tell us about yourself and why you are interested in this role.',
  'Describe a challenging project you worked on and how you overcame obstacles.',
  'Where do you see yourself professionally in the next 3-5 years?',
]

export default function AssessmentBuilderPage() {
  const [selectedJobId, setSelectedJobId] = useState<string | null>(null)
  const [templates, setTemplates] = useState<Template[]>([])
  const [loadingTemplates, setLoadingTemplates] = useState(false)
  const [showBuilder, setShowBuilder] = useState(false)
  const [showAssignModal, setShowAssignModal] = useState<Template | null>(null)
  const [assignApplicationId, setAssignApplicationId] = useState('')
  const [assignExpiry, setAssignExpiry] = useState(7)
  const [assigning, setAssigning] = useState(false)
  const [candidates, setCandidates] = useState<any[]>([])
  const [loadingCandidates, setLoadingCandidates] = useState(false)

  // Builder form
  const [form, setForm] = useState({
    title: '',
    type: 'Video',
    durationMinutes: 30,
    instructions: '',
    hackerRankTestId: '',
    questions: DEFAULT_QUESTIONS.map(q => ({ questionText: q, thinkTimeSecs: 30, recordingTimeSecs: 120 })),
  })
  const [saving, setSaving] = useState(false)

  // Load jobs
  const { data: jobs = [] } = useQuery<Job[]>({
    queryKey: ['jobs-simple'],
    queryFn: async () => {
      const res = await api.get('/jobs')
      return res.data?.items ?? res.data ?? []
    },
  })

  useEffect(() => {
    if (selectedJobId) {
      loadTemplates(selectedJobId)
      loadCandidates(selectedJobId)
    }
  }, [selectedJobId])

  async function loadCandidates(jobId: string) {
    setLoadingCandidates(true)
    try {
      const data = await applicationsApi.getRankedForJob(jobId)
      setCandidates(data as any[])
    } catch {
      setCandidates([])
    } finally {
      setLoadingCandidates(false)
    }
  }


  async function loadTemplates(jobId: string) {

    setLoadingTemplates(true)
    try {
      const res = await api.get(`/assessments/templates/job/${jobId}`)
      setTemplates(res.data)
    } catch (err) {
      console.error(err)
    } finally {
      setLoadingTemplates(false)
    }
  }

  async function saveTemplate() {
    if (!selectedJobId) { toast.error('Select a job first'); return }
    if (!form.title) { toast.error('Template title is required'); return }
    if ((form.type === 'Video' || form.type === 'Mixed') && form.questions.length === 0) {
      toast.error('Add at least one video question'); return
    }

    setSaving(true)
    try {
      await api.post('/assessments/templates', {
        jobId: selectedJobId,
        title: form.title,
        type: form.type,
        durationMinutes: form.durationMinutes,
        instructions: form.instructions || null,
        hackerRankTestId: form.hackerRankTestId || null,
        questions: form.questions,
      })
      toast.success('Assessment template created!')
      setShowBuilder(false)
      setForm({
        title: '',
        type: 'Video',
        durationMinutes: 30,
        instructions: '',
        hackerRankTestId: '',
        questions: DEFAULT_QUESTIONS.map(q => ({ questionText: q, thinkTimeSecs: 30, recordingTimeSecs: 120 })),
      })
      await loadTemplates(selectedJobId)
    } catch (err: any) {
      toast.error(err.response?.data?.message ?? 'Failed to create template')
    } finally {
      setSaving(false)
    }
  }

  async function assignTemplate() {
    if (!showAssignModal || !assignApplicationId) {
      toast.error('Enter the application ID'); return
    }
    setAssigning(true)
    try {
      await api.post('/assessments/assign', {

        applicationId: assignApplicationId,
        templateId: showAssignModal.id,
        expiryDays: assignExpiry,
      })
      toast.success('Assessment assigned — candidate notified by email!')
      setShowAssignModal(null)
      setAssignApplicationId('')
    } catch (err: any) {
      toast.error(err.response?.data?.message ?? 'Failed to assign assessment')
    } finally {
      setAssigning(false)
    }
  }

  function addQuestion() {
    setForm(f => ({ ...f, questions: [...f.questions, { questionText: '', thinkTimeSecs: 30, recordingTimeSecs: 120 }] }))
  }

  function updateQuestion(i: number, field: string, value: string | number) {
    setForm(f => ({ ...f, questions: f.questions.map((q, idx) => idx === i ? { ...q, [field]: value } : q) }))
  }

  function removeQuestion(i: number) {
    setForm(f => ({ ...f, questions: f.questions.filter((_, idx) => idx !== i) }))
  }

  function moveQuestion(i: number, dir: -1 | 1) {
    const qs = [...form.questions]
    const j = i + dir
    if (j < 0 || j >= qs.length) return
    ;[qs[i], qs[j]] = [qs[j], qs[i]]
    setForm(f => ({ ...f, questions: qs }))
  }

  const showVideQuestions = form.type === 'Video' || form.type === 'Mixed'
  const showHackerRank = form.type === 'CodingTest' || form.type === 'Mixed'

  return (
    <div className="min-h-full space-y-6">
      {/* Header */}
      <div className="flex items-start justify-between gap-4 flex-wrap">
        <div>
          <h1 className="text-2xl font-bold text-white tracking-tight flex items-center gap-2">
            <Video className="w-6 h-6 text-violet-400" />
            Assessment Builder
          </h1>
          <p className="text-white/40 text-sm mt-1">
            Create video interview questions and coding tests for your job postings
          </p>
        </div>
        {selectedJobId && (
          <button
            onClick={() => setShowBuilder(true)}
            className="flex items-center gap-2 px-4 py-2.5 bg-gradient-to-r from-violet-600 to-indigo-600 text-white rounded-xl text-sm font-medium hover:from-violet-500 hover:to-indigo-500 transition-all shadow-lg shadow-indigo-500/20"
          >
            <Plus className="w-4 h-4" /> New Template
          </button>
        )}
      </div>

      {/* Job selector */}
      <div>
        <label className="text-sm text-white/40 block mb-2">Select Job to manage assessments</label>
        <select
          className="w-full max-w-sm bg-white/5 border border-white/10 rounded-xl px-4 py-2.5 text-white focus:outline-none focus:border-violet-500/50"
          value={selectedJobId ?? ''}
          onChange={e => setSelectedJobId(e.target.value || null)}
        >
          <option value="">-- Choose a job --</option>
          {jobs.map((j: Job) => <option key={j.id} value={j.id}>{j.title}</option>)}
        </select>
      </div>

      {/* Templates list */}
      {selectedJobId && (
        <>
          {loadingTemplates ? (
            <div className="flex justify-center py-10">
              <div className="w-7 h-7 rounded-full border-2 border-violet-500/30 border-t-violet-500 animate-spin" />
            </div>
          ) : templates.length === 0 ? (
            <div className="text-center py-16 border border-dashed border-white/10 rounded-2xl">
              <Video className="w-10 h-10 text-white/20 mx-auto mb-3" />
              <p className="text-white/40 font-medium">No assessment templates yet</p>
              <p className="text-white/25 text-sm mt-1">Create your first template to start evaluating candidates</p>
              <button
                onClick={() => setShowBuilder(true)}
                className="mt-4 px-4 py-2 bg-violet-600 hover:bg-violet-500 text-white rounded-lg text-sm font-medium transition-all"
              >
                Create First Template
              </button>
            </div>
          ) : (
            <div className="space-y-4">
              {templates.map(t => (
                <motion.div
                  key={t.id}
                  initial={{ opacity: 0, y: 8 }}
                  animate={{ opacity: 1, y: 0 }}
                  className="p-5 rounded-2xl border border-white/8 hover:border-violet-500/30 transition-all"
                  style={{ background: 'linear-gradient(135deg, rgba(255,255,255,0.04) 0%, rgba(255,255,255,0.02) 100%)' }}
                >
                  <div className="flex items-start justify-between gap-4">
                    <div className="flex-1 min-w-0">
                      <div className="flex items-center gap-3 flex-wrap">
                        <h3 className="font-semibold text-white">{t.title}</h3>
                        <span className="px-2.5 py-0.5 text-xs rounded-full bg-violet-500/20 text-violet-300 border border-violet-500/30">
                          {t.type === 'Video' ? '🎥' : t.type === 'CodingTest' ? '💻' : '🎯'} {t.type}
                        </span>
                      </div>
                      <div className="flex items-center gap-4 mt-2 text-xs text-white/40 flex-wrap">
                        <span className="flex items-center gap-1"><Clock className="w-3 h-3" /> {t.durationMinutes} min</span>
                        {t.questionCount > 0 && <span>{t.questionCount} video question{t.questionCount !== 1 ? 's' : ''}</span>}
                        {t.hackerRankTestId && <span className="text-emerald-400">💻 HackerRank: {t.hackerRankTestId}</span>}
                      </div>
                      {t.instructions && (
                        <p className="text-xs text-white/30 mt-2 line-clamp-2">{t.instructions}</p>
                      )}
                    </div>
                    <div className="flex items-center gap-2 shrink-0">
                      <button
                        onClick={() => setShowAssignModal(t)}
                        className="flex items-center gap-1.5 px-3 py-1.5 bg-indigo-600 hover:bg-indigo-500 text-white rounded-lg text-xs font-medium transition-all"
                      >
                        <Send className="w-3 h-3" /> Assign
                      </button>
                    </div>
                  </div>

                  {/* Questions preview */}
                  {t.questions.length > 0 && (
                    <div className="mt-4 pt-4 border-t border-white/5 space-y-2">
                      {t.questions.slice(0, 3).map((q, i) => (
                        <div key={q.id} className="flex items-start gap-2 text-xs text-white/40">
                          <span className="w-5 h-5 rounded-full bg-white/5 flex items-center justify-center text-[10px] flex-shrink-0 mt-0.5">{q.order}</span>
                          <span className="line-clamp-1">{q.questionText}</span>
                        </div>
                      ))}
                      {t.questions.length > 3 && (
                        <p className="text-[11px] text-white/25 pl-7">+{t.questions.length - 3} more questions</p>
                      )}
                    </div>
                  )}
                </motion.div>
              ))}
            </div>
          )}
        </>
      )}

      {/* Template Builder Modal */}
      <AnimatePresence>
        {showBuilder && (
          <motion.div
            initial={{ opacity: 0 }}
            animate={{ opacity: 1 }}
            exit={{ opacity: 0 }}
            className="fixed inset-0 bg-black/60 backdrop-blur-sm z-50 flex items-center justify-center p-4 overflow-y-auto"
            onClick={e => e.target === e.currentTarget && setShowBuilder(false)}
          >
            <motion.div
              initial={{ opacity: 0, scale: 0.94, y: 16 }}
              animate={{ opacity: 1, scale: 1, y: 0 }}
              exit={{ opacity: 0, scale: 0.94, y: 16 }}
              className="w-full max-w-2xl rounded-2xl overflow-hidden my-8"
              style={{
                background: 'linear-gradient(135deg, rgba(15,20,40,0.99) 0%, rgba(10,15,30,0.99) 100%)',
                border: '1px solid rgba(139,92,246,0.25)',
              }}
            >
              <div className="flex items-center justify-between px-6 py-4 border-b border-white/5">
                <h2 className="font-bold text-white flex items-center gap-2">
                  <Video className="w-4 h-4 text-violet-400" /> Create Assessment Template
                </h2>
                <button onClick={() => setShowBuilder(false)} className="text-white/30 hover:text-white transition-colors">✕</button>
              </div>

              <div className="p-6 space-y-5 max-h-[70vh] overflow-y-auto">
                {/* Basic info */}
                <div className="grid grid-cols-2 gap-4">
                  <div className="col-span-2">
                    <label className="text-xs text-white/40 block mb-1.5">Template Name *</label>
                    <input
                      className="w-full bg-white/5 border border-white/10 rounded-xl px-4 py-2.5 text-white text-sm focus:outline-none focus:border-violet-500/50 placeholder-white/25"
                      placeholder="e.g. Frontend Engineer - Video Round 1"
                      value={form.title}
                      onChange={e => setForm(f => ({ ...f, title: e.target.value }))}
                    />
                  </div>
                  <div>
                    <label className="text-xs text-white/40 block mb-1.5">Duration (minutes)</label>
                    <input
                      type="number"
                      min={5}
                      max={120}
                      className="w-full bg-white/5 border border-white/10 rounded-xl px-4 py-2.5 text-white text-sm focus:outline-none focus:border-violet-500/50"
                      value={form.durationMinutes}
                      onChange={e => setForm(f => ({ ...f, durationMinutes: +e.target.value }))}
                    />
                  </div>
                </div>

                {/* Type selector */}
                <div>
                  <label className="text-xs text-white/40 block mb-2">Assessment Type *</label>
                  <div className="grid grid-cols-3 gap-2">
                    {ASSESSMENT_TYPES.map(t => (
                      <button
                        key={t.value}
                        onClick={() => setForm(f => ({ ...f, type: t.value }))}
                        className={`p-3 rounded-xl border text-left transition-all ${form.type === t.value ? 'border-violet-500/50 bg-violet-500/10' : 'border-white/10 bg-white/[0.03] hover:border-white/20'}`}
                      >
                        <div className="text-sm font-medium text-white">{t.label}</div>
                        <div className="text-[11px] text-white/30 mt-0.5">{t.description}</div>
                      </button>
                    ))}
                  </div>
                </div>

                {/* Instructions */}
                <div>
                  <label className="text-xs text-white/40 block mb-1.5">Instructions for candidate (optional)</label>
                  <textarea
                    rows={2}
                    className="w-full bg-white/5 border border-white/10 rounded-xl px-4 py-2.5 text-white text-sm focus:outline-none focus:border-violet-500/50 placeholder-white/25 resize-none"
                    placeholder="Candidate instructions shown before they start..."
                    value={form.instructions}
                    onChange={e => setForm(f => ({ ...f, instructions: e.target.value }))}
                  />
                </div>

                {/* HackerRank */}
                {showHackerRank && (
                  <div>
                    <label className="text-xs text-white/40 block mb-1.5">HackerRank Test ID</label>
                    <input
                      className="w-full bg-white/5 border border-white/10 rounded-xl px-4 py-2.5 text-white text-sm focus:outline-none focus:border-emerald-500/50 placeholder-white/25 font-mono"
                      placeholder="e.g. 12345 (from HackerRank dashboard)"
                      value={form.hackerRankTestId}
                      onChange={e => setForm(f => ({ ...f, hackerRankTestId: e.target.value }))}
                    />
                  </div>
                )}

                {/* Video Questions */}
                {showVideQuestions && (
                  <div>
                    <div className="flex items-center justify-between mb-3">
                      <label className="text-xs text-white/40 font-semibold uppercase tracking-wider">
                        Video Questions ({form.questions.length})
                      </label>
                      <button
                        onClick={addQuestion}
                        className="flex items-center gap-1 text-xs text-violet-400 hover:text-violet-300 transition-colors"
                      >
                        <Plus className="w-3 h-3" /> Add Question
                      </button>
                    </div>
                    <div className="space-y-3">
                      {form.questions.map((q, i) => (
                        <div key={i} className="p-4 bg-white/[0.03] border border-white/8 rounded-xl space-y-3">
                          <div className="flex items-center gap-2">
                            <span className="w-6 h-6 rounded-full bg-violet-500/20 text-violet-300 flex items-center justify-center text-[11px] font-bold flex-shrink-0">{i + 1}</span>
                            <textarea
                              rows={2}
                              className="flex-1 bg-white/5 border border-white/10 rounded-lg px-3 py-2 text-sm text-white placeholder-white/25 focus:outline-none focus:border-violet-500/50 resize-none"
                              placeholder="Enter your question..."
                              value={q.questionText}
                              onChange={e => updateQuestion(i, 'questionText', e.target.value)}
                            />
                            <div className="flex flex-col gap-1">
                              <button onClick={() => moveQuestion(i, -1)} disabled={i === 0} className="text-white/20 hover:text-white/50 disabled:opacity-20 transition-colors"><ChevronUp className="w-3.5 h-3.5" /></button>
                              <button onClick={() => moveQuestion(i, 1)} disabled={i === form.questions.length - 1} className="text-white/20 hover:text-white/50 disabled:opacity-20 transition-colors"><ChevronDown className="w-3.5 h-3.5" /></button>
                              <button onClick={() => removeQuestion(i)} className="text-white/20 hover:text-red-400 transition-colors"><Trash2 className="w-3.5 h-3.5" /></button>
                            </div>
                          </div>
                          <div className="grid grid-cols-2 gap-3 pl-8">
                            <div>
                              <label className="text-[10px] text-white/30 block mb-1">Think time (sec)</label>
                              <input type="number" min={0} max={300} value={q.thinkTimeSecs} onChange={e => updateQuestion(i, 'thinkTimeSecs', +e.target.value)} className="w-full bg-white/5 border border-white/8 rounded-lg px-3 py-1.5 text-sm text-white focus:outline-none" />
                            </div>
                            <div>
                              <label className="text-[10px] text-white/30 block mb-1">Recording time (sec)</label>
                              <input type="number" min={30} max={600} value={q.recordingTimeSecs} onChange={e => updateQuestion(i, 'recordingTimeSecs', +e.target.value)} className="w-full bg-white/5 border border-white/8 rounded-lg px-3 py-1.5 text-sm text-white focus:outline-none" />
                            </div>
                          </div>
                        </div>
                      ))}
                    </div>
                  </div>
                )}
              </div>

              <div className="px-6 py-4 border-t border-white/5 flex justify-end gap-3">
                <button onClick={() => setShowBuilder(false)} className="px-4 py-2 text-white/40 hover:text-white transition-colors text-sm">Cancel</button>
                <button
                  onClick={saveTemplate}
                  disabled={saving}
                  className="flex items-center gap-2 px-5 py-2.5 bg-gradient-to-r from-violet-600 to-indigo-600 text-white rounded-xl text-sm font-medium hover:from-violet-500 hover:to-indigo-500 transition-all disabled:opacity-50"
                >
                  {saving ? <><Loader2 className="w-4 h-4 animate-spin" /> Saving...</> : <><CheckCircle2 className="w-4 h-4" /> Save Template</>}
                </button>
              </div>
            </motion.div>
          </motion.div>
        )}
      </AnimatePresence>

      {/* Assign Modal */}
      <AnimatePresence>
        {showAssignModal && (
          <motion.div
            initial={{ opacity: 0 }}
            animate={{ opacity: 1 }}
            exit={{ opacity: 0 }}
            className="fixed inset-0 bg-black/60 backdrop-blur-sm z-50 flex items-center justify-center p-4"
            onClick={e => e.target === e.currentTarget && setShowAssignModal(null)}
          >
            <motion.div
              initial={{ opacity: 0, scale: 0.94 }}
              animate={{ opacity: 1, scale: 1 }}
              exit={{ opacity: 0, scale: 0.94 }}
              className="w-full max-w-md rounded-2xl overflow-hidden"
              style={{
                background: 'linear-gradient(135deg, rgba(15,20,40,0.99) 0%, rgba(10,15,30,0.99) 100%)',
                border: '1px solid rgba(99,102,241,0.25)',
              }}
            >
              <div className="p-6">
                <h2 className="font-bold text-white mb-1">Assign Assessment</h2>
                <p className="text-sm text-white/40 mb-5">
                  Assign <strong className="text-white/70">{showAssignModal.title}</strong> to a candidate
                </p>
                <div className="space-y-4">
                  <div>
                    <label className="text-xs text-white/40 block mb-1.5">Select Candidate *</label>
                    {loadingCandidates ? (
                      <div className="flex items-center gap-2 text-xs text-white/40 py-2">
                        <Loader2 className="w-3.5 h-3.5 animate-spin text-indigo-400" /> Loading job applicants...
                      </div>
                    ) : candidates.length === 0 ? (
                      <div className="text-xs text-amber-400 bg-amber-500/10 border border-amber-500/20 rounded-xl p-3">
                        No applicants found for this job yet. Candidates can also be assigned from the Ranked Candidates view.
                      </div>
                    ) : (
                      <select
                        className="w-full bg-slate-800 border border-slate-700 rounded-xl px-4 py-2.5 text-white text-sm focus:outline-none focus:border-indigo-500/50"
                        value={assignApplicationId}
                        onChange={e => setAssignApplicationId(e.target.value)}
                      >
                        <option value="">-- Choose Candidate --</option>
                        {candidates.map(c => (
                          <option key={c.id} value={c.id}>
                            👤 {c.candidateName} (Score: {c.matchScore != null ? `${c.matchScore}%` : 'N/A'})
                          </option>
                        ))}
                      </select>
                    )}
                  </div>

                  <div>
                    <label className="text-xs text-white/40 block mb-1.5">Expiry (days)</label>
                    <input
                      type="number"
                      min={1}
                      max={30}
                      value={assignExpiry}
                      onChange={e => setAssignExpiry(+e.target.value)}
                      className="w-full bg-white/5 border border-white/10 rounded-xl px-4 py-2.5 text-white text-sm focus:outline-none"
                    />
                  </div>
                </div>
                <div className="flex justify-end gap-3 mt-6">
                  <button onClick={() => setShowAssignModal(null)} className="px-4 py-2 text-white/40 hover:text-white text-sm transition-colors">Cancel</button>
                  <button
                    onClick={assignTemplate}
                    disabled={assigning || !assignApplicationId}
                    className="flex items-center gap-2 px-5 py-2.5 bg-gradient-to-r from-indigo-600 to-indigo-500 text-white rounded-xl text-sm font-medium hover:from-indigo-500 hover:to-indigo-400 disabled:opacity-50 transition-all"
                  >
                    {assigning ? <><Loader2 className="w-4 h-4 animate-spin" /> Assigning...</> : <><Send className="w-4 h-4" /> Assign & Notify</>}
                  </button>
                </div>
              </div>
            </motion.div>
          </motion.div>
        )}
      </AnimatePresence>
    </div>
  )
}
