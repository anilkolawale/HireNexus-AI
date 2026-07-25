import { useState, useEffect, useRef, useCallback } from 'react'
import api from '../../api/axiosClient'

interface Assessment {
  id: string
  templateName: string
  assessmentType: string
  jobTitle: string
  status: string
  sentAtUtc: string
  expiresAtUtc?: string
  completedAtUtc?: string
  hackerRankInviteUrl?: string
  durationMinutes: number
  instructions?: string
  questions: Question[]
}

interface Question {
  id: string
  questionText: string
  thinkTimeSecs: number
  recordingTimeSecs: number
  order: number
  isAnswered: boolean
}

export default function CandidateAssessmentPage() {
  const [assessments, setAssessments] = useState<Assessment[]>([])
  const [isLoading, setIsLoading] = useState(true)
  const [activeAssessment, setActiveAssessment] = useState<Assessment | null>(null)
  const [activeQuestion, setActiveQuestion] = useState<Question | null>(null)
  const [phase, setPhase] = useState<'thinking' | 'recording' | 'done'>('thinking')
  const [countdown, setCountdown] = useState(0)
  const [isRecording, setIsRecording] = useState(false)
  const [videoBlob, setVideoBlob] = useState<Blob | null>(null)
  const [uploading, setUploading] = useState(false)

  const videoRef = useRef<HTMLVideoElement>(null)
  const mediaRecorderRef = useRef<MediaRecorder | null>(null)
  const chunksRef = useRef<Blob[]>([])
  const streamRef = useRef<MediaStream | null>(null)
  const timerRef = useRef<ReturnType<typeof setInterval> | null>(null)

  useEffect(() => {
    loadAssessments()
    return () => { if (timerRef.current) clearInterval(timerRef.current) }
  }, [])

  async function loadAssessments() {
    try {
      const res = await api.get('/assessments/my')
      setAssessments(res.data)
    } catch (err) {
      console.error(err)
    } finally {
      setIsLoading(false)
    }
  }


  function startAssessment(assessment: Assessment) {
    const unanswered = assessment.questions.find(q => !q.isAnswered)
    if (!unanswered) {
      alert('You have already answered all questions in this assessment.')
      return
    }
    setActiveAssessment(assessment)
    startQuestion(unanswered)
  }

  function startQuestion(question: Question) {
    setActiveQuestion(question)
    setPhase('thinking')
    setCountdown(question.thinkTimeSecs)
    setVideoBlob(null)

    timerRef.current = setInterval(() => {
      setCountdown(prev => {
        if (prev <= 1) {
          clearInterval(timerRef.current!)
          startRecording(question)
          return 0
        }
        return prev - 1
      })
    }, 1000)
  }

  async function startRecording(question: Question) {
    setPhase('recording')
    setCountdown(question.recordingTimeSecs)
    chunksRef.current = []

    try {
      const stream = await navigator.mediaDevices.getUserMedia({ video: true, audio: true })
      streamRef.current = stream
      if (videoRef.current) videoRef.current.srcObject = stream

      const recorder = new MediaRecorder(stream)
      mediaRecorderRef.current = recorder
      recorder.ondataavailable = e => { if (e.data.size > 0) chunksRef.current.push(e.data) }
      recorder.onstop = () => {
        const blob = new Blob(chunksRef.current, { type: 'video/webm' })
        setVideoBlob(blob)
        stream.getTracks().forEach(t => t.stop())
        streamRef.current = null
        setPhase('done')
      }
      recorder.start()
      setIsRecording(true)

      timerRef.current = setInterval(() => {
        setCountdown(prev => {
          if (prev <= 1) {
            clearInterval(timerRef.current!)
            stopRecording()
            return 0
          }
          return prev - 1
        })
      }, 1000)
    } catch (err) {
      alert('Camera/microphone access denied. Please grant permission and try again.')
      setPhase('thinking')
    }
  }

  function stopRecording() {
    mediaRecorderRef.current?.stop()
    setIsRecording(false)
    if (timerRef.current) clearInterval(timerRef.current)
  }

  async function submitResponse() {
    if (!activeAssessment || !activeQuestion || !videoBlob) return
    setUploading(true)

    // Upload video blob to backend (returns blob URL)
    // For demo, we convert to base64 data URL as a stub
    // In production: upload to Azure Blob Storage via a signed URL endpoint
    const stubBlobUrl = `https://ats-storage.blob.core.windows.net/videos/${activeAssessment.id}/${activeQuestion.id}.webm`

    try {
      await api.post(`/assessments/${activeAssessment.id}/responses`, {
        questionId: activeQuestion.id,
        blobVideoUrl: stubBlobUrl,
        durationSeconds: activeQuestion.recordingTimeSecs,
      })

      // Refresh
      await loadAssessments()
      const updated = (await api.get('/assessments/my')).data.find((a: Assessment) => a.id === activeAssessment.id)


      if (updated) {
        const nextQuestion = updated.questions.find((q: Question) => !q.isAnswered)
        if (nextQuestion) {
          setActiveAssessment(updated)
          startQuestion(nextQuestion)
        } else {
          setActiveAssessment(null)
          setActiveQuestion(null)
          setVideoBlob(null)
          alert('🎉 Assessment completed! All questions answered.')
          await loadAssessments()
        }
      }
    } catch (err) {
      alert('Failed to submit response. Please try again.')
    } finally {
      setUploading(false)
    }
  }

  function formatTime(secs: number) {
    return `${Math.floor(secs / 60)}:${String(secs % 60).padStart(2, '0')}`
  }

  const statusBadge = (status: string) => {
    const styles: Record<string, string> = {
      Pending: 'bg-amber-500/20 text-amber-400 border-amber-500/30',
      InProgress: 'bg-blue-500/20 text-blue-400 border-blue-500/30',
      Completed: 'bg-emerald-500/20 text-emerald-400 border-emerald-500/30',
      Expired: 'bg-slate-700 text-slate-400 border-slate-600',
    }
    return `px-2.5 py-0.5 text-xs rounded-full border ${styles[status] ?? styles.Pending}`
  }

  if (isLoading) {
    return (
      <div className="flex justify-center py-20">
        <div className="w-8 h-8 rounded-full border-2 border-indigo-500/30 border-t-indigo-500 animate-spin" />
      </div>
    )
  }

  // Active recording modal
  if (activeAssessment && activeQuestion) {
    return (
      <div className="fixed inset-0 bg-black flex flex-col items-center justify-center z-50 p-6">
        {/* Progress */}
        <div className="absolute top-6 left-0 right-0 flex justify-center gap-2">
          {activeAssessment.questions.map((q, i) => (
            <div key={q.id} className={`h-1.5 w-12 rounded-full transition-all ${q.id === activeQuestion.id ? 'bg-violet-500' : q.isAnswered ? 'bg-emerald-500' : 'bg-slate-700'}`} />
          ))}
        </div>

        <div className="w-full max-w-3xl">
          {/* Question */}
          <div className="text-center mb-8">
            <div className="text-sm text-slate-400 mb-2">Question {activeQuestion.order} of {activeAssessment.questions.length}</div>
            <h2 className="text-2xl font-bold text-white leading-relaxed">{activeQuestion.questionText}</h2>
          </div>

          {/* Video preview / countdown */}
          <div className="relative mx-auto max-w-xl aspect-video bg-slate-900 rounded-2xl overflow-hidden border border-slate-700 mb-6">
            {phase === 'recording' ? (
              <video ref={videoRef} autoPlay muted className="w-full h-full object-cover" />
            ) : phase === 'done' && videoBlob ? (
              <video src={URL.createObjectURL(videoBlob)} controls className="w-full h-full object-cover" />
            ) : (
              <div className="flex flex-col items-center justify-center h-full text-slate-400">
                <div className="text-6xl mb-4">🎙️</div>
                <div className="text-lg">Prepare your answer</div>
                <div className="text-sm mt-1">Recording starts automatically</div>
              </div>
            )}

            {/* Countdown overlay */}
            {phase !== 'done' && (
              <div className="absolute top-4 right-4 bg-black/70 backdrop-blur-sm rounded-xl px-4 py-2 text-white font-mono text-xl font-bold">
                {formatTime(countdown)}
              </div>
            )}

            {phase === 'thinking' && (
              <div className="absolute bottom-4 left-0 right-0 flex justify-center">
                <span className="bg-amber-500/20 border border-amber-500/30 text-amber-300 px-4 py-1.5 rounded-full text-sm">
                  💭 Think Time
                </span>
              </div>
            )}

            {phase === 'recording' && (
              <div className="absolute bottom-4 left-0 right-0 flex justify-center">
                <span className="bg-red-500/20 border border-red-500/30 text-red-400 px-4 py-1.5 rounded-full text-sm flex items-center gap-2">
                  <span className="w-2 h-2 bg-red-500 rounded-full animate-pulse" />
                  Recording
                </span>
              </div>
            )}
          </div>

          {/* Controls */}
          <div className="flex justify-center gap-4">
            {phase === 'recording' && (
              <button
                onClick={stopRecording}
                className="px-6 py-3 bg-red-600 hover:bg-red-500 text-white rounded-xl font-medium transition-all"
              >
                ⏹ Stop Recording
              </button>
            )}
            {phase === 'done' && (
              <>
                <button
                  onClick={() => startQuestion(activeQuestion)}
                  className="px-6 py-3 border border-slate-600 text-slate-300 rounded-xl hover:bg-slate-800 transition-all"
                >
                  🔄 Re-record
                </button>
                <button
                  onClick={submitResponse}
                  disabled={uploading}
                  className="px-6 py-3 bg-gradient-to-r from-violet-600 to-indigo-600 text-white rounded-xl font-medium hover:from-violet-500 hover:to-indigo-500 transition-all disabled:opacity-50"
                >
                  {uploading ? 'Uploading...' : '✅ Submit Response'}
                </button>
              </>
            )}
          </div>

          <button
            onClick={() => { setActiveAssessment(null); setActiveQuestion(null); streamRef.current?.getTracks().forEach(t => t.stop()) }}
            className="absolute top-6 right-6 text-slate-500 hover:text-white transition-colors"
          >
            ✕
          </button>
        </div>
      </div>
    )
  }

  return (
    <div className="min-h-screen bg-slate-950 text-white p-6 space-y-8">
      <div>
        <h1 className="text-3xl font-bold bg-gradient-to-r from-violet-400 to-cyan-400 bg-clip-text text-transparent">
          🎥 My Assessments
        </h1>
        <p className="text-slate-400 mt-1">Complete video interviews and coding assessments assigned by recruiters</p>
      </div>

      {assessments.length === 0 ? (
        <div className="text-center py-20 text-slate-500">
          <div className="text-5xl mb-4">📋</div>
          <div className="text-lg">No assessments assigned</div>
          <div className="text-sm mt-1">When a recruiter assigns an assessment, it will appear here</div>
        </div>
      ) : (
        <div className="grid grid-cols-1 gap-5">
          {assessments.map(a => (
            <div key={a.id} className="p-6 bg-slate-900 border border-slate-700 rounded-2xl">
              <div className="flex items-start justify-between gap-4 mb-4">
                <div>
                  <div className="flex items-center gap-3 flex-wrap">
                    <h3 className="text-lg font-semibold text-white">{a.templateName}</h3>
                    <span className={statusBadge(a.status)}>{a.status}</span>
                    <span className="text-xs text-slate-500">{a.assessmentType}</span>
                  </div>
                  <p className="text-slate-400 text-sm mt-1">{a.jobTitle}</p>
                </div>
                <div className="text-right text-sm text-slate-400 shrink-0">
                  <div>{a.durationMinutes} min</div>
                  {a.expiresAtUtc && <div className="text-amber-400">Due {new Date(a.expiresAtUtc).toLocaleDateString()}</div>}
                </div>
              </div>

              {a.instructions && (
                <div className="text-sm text-slate-400 bg-slate-800 rounded-lg px-4 py-3 mb-4">{a.instructions}</div>
              )}

              {/* Question progress */}
              {a.questions.length > 0 && (
                <div className="mb-4">
                  <div className="text-xs text-slate-500 mb-2">Questions: {a.questions.filter(q => q.isAnswered).length}/{a.questions.length} answered</div>
                  <div className="flex gap-1">
                    {a.questions.map(q => (
                      <div key={q.id} className={`h-1.5 flex-1 rounded-full ${q.isAnswered ? 'bg-emerald-500' : 'bg-slate-700'}`} />
                    ))}
                  </div>
                </div>
              )}

              <div className="flex gap-3 flex-wrap">
                {a.hackerRankInviteUrl && (
                  <a
                    href={a.hackerRankInviteUrl}
                    target="_blank"
                    rel="noopener noreferrer"
                    className="px-4 py-2 bg-emerald-600 hover:bg-emerald-500 text-white rounded-lg text-sm font-medium transition-all"
                  >
                    💻 Start Coding Test
                  </a>
                )}
                {(a.assessmentType === 'Video' || a.assessmentType === 'Mixed') && a.status !== 'Completed' && a.status !== 'Expired' && (
                  <button
                    onClick={() => startAssessment(a)}
                    className="px-4 py-2 bg-gradient-to-r from-violet-600 to-indigo-600 text-white rounded-lg text-sm font-medium hover:from-violet-500 hover:to-indigo-500 transition-all"
                  >
                    {a.questions.some(q => q.isAnswered) ? '▶️ Continue Video' : '🎬 Start Video Interview'}
                  </button>
                )}
                {a.status === 'Completed' && (
                  <div className="px-4 py-2 bg-emerald-500/10 border border-emerald-500/30 text-emerald-400 rounded-lg text-sm">
                    ✅ Completed {a.completedAtUtc ? new Date(a.completedAtUtc).toLocaleDateString() : ''}
                  </div>
                )}
              </div>
            </div>
          ))}
        </div>
      )}
    </div>
  )
}
