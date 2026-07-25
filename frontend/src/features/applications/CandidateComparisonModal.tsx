import { useState } from 'react'
import { motion, AnimatePresence } from 'framer-motion'
import { useMutation } from '@tanstack/react-query'
import {
  X, Loader2, Sparkles, Trophy, ThumbsUp, ThumbsDown,
  Star, Users, ChevronRight
} from 'lucide-react'
import { aiAssistantApi, type CandidateComparisonResult, type CandidateRanking } from '../../api/endpoints/aiAssistant.api'

interface CandidateOption {
  applicationId: string
  name: string
  matchScore?: number
}

interface Props {
  jobId: string
  jobTitle: string
  candidates: CandidateOption[]
  isOpen: boolean
  onClose: () => void
}

function RankCard({ ranking, isBest }: { ranking: CandidateRanking; isBest: boolean }) {
  const rankColors = ['text-amber-400', 'text-slate-400', 'text-orange-600']
  const rankBg = ['border-amber-500/30 bg-amber-500/5', 'border-slate-500/30 bg-slate-500/5', 'border-orange-500/30 bg-orange-500/5']
  const idx = Math.min(ranking.rank - 1, 2)

  return (
    <motion.div
      initial={{ opacity: 0, y: 16 }}
      animate={{ opacity: 1, y: 0 }}
      transition={{ delay: idx * 0.1 }}
      className={`rounded-2xl border p-5 ${isBest ? 'border-amber-500/40 bg-gradient-to-br from-amber-500/10 to-amber-500/5' : 'border-white/10 bg-white/[0.04]'}`}
    >
      {/* Rank header */}
      <div className="flex items-center justify-between mb-3">
        <div className="flex items-center gap-2">
          {isBest && <Trophy className="w-4 h-4 text-amber-400" />}
          <span className={`text-lg font-bold ${rankColors[idx]}`}>#{ranking.rank}</span>
          <span className="text-sm font-semibold text-white">{ranking.candidateName}</span>
        </div>
        <span className="text-[10px] px-2 py-1 rounded-full bg-white/5 text-white/40 border border-white/10">
          {ranking.hiringRecommendation}
        </span>
      </div>

      {/* Strengths */}
      <div className="mb-2.5">
        <div className="flex items-center gap-1.5 mb-1">
          <ThumbsUp className="w-3 h-3 text-emerald-400" />
          <p className="text-[10px] text-emerald-400 font-semibold uppercase tracking-wider">Strengths</p>
        </div>
        <p className="text-xs text-white/60 leading-relaxed">{ranking.strengths}</p>
      </div>

      {/* Weaknesses */}
      <div>
        <div className="flex items-center gap-1.5 mb-1">
          <ThumbsDown className="w-3 h-3 text-red-400" />
          <p className="text-[10px] text-red-400 font-semibold uppercase tracking-wider">Concerns</p>
        </div>
        <p className="text-xs text-white/60 leading-relaxed">{ranking.weaknesses}</p>
      </div>
    </motion.div>
  )
}

export function CandidateComparisonModal({ jobId, jobTitle, candidates, isOpen, onClose }: Props) {
  const [selected, setSelected] = useState<string[]>([])
  const [result, setResult] = useState<CandidateComparisonResult | null>(null)

  const toggleSelect = (id: string) => {
    setSelected(prev =>
      prev.includes(id)
        ? prev.filter(x => x !== id)
        : prev.length < 5 ? [...prev, id] : prev
    )
  }

  const { mutate: compare, isPending } = useMutation({
    mutationFn: () => aiAssistantApi.compareCandidates(selected, jobId),
    onSuccess: setResult,
  })

  const handleClose = () => {
    setResult(null)
    setSelected([])
    onClose()
  }

  return (
    <AnimatePresence>
      {isOpen && (
        <>
          <motion.div
            initial={{ opacity: 0 }}
            animate={{ opacity: 1 }}
            exit={{ opacity: 0 }}
            onClick={handleClose}
            className="fixed inset-0 bg-black/60 backdrop-blur-sm z-50"
          />

          <motion.div
            initial={{ opacity: 0, scale: 0.92, y: 20 }}
            animate={{ opacity: 1, scale: 1, y: 0 }}
            exit={{ opacity: 0, scale: 0.92, y: 20 }}
            transition={{ type: 'spring', stiffness: 300, damping: 30 }}
            className="fixed inset-x-4 top-[5%] bottom-[5%] md:inset-x-auto md:left-1/2 md:-translate-x-1/2 md:w-[700px] z-50 flex flex-col overflow-hidden rounded-2xl"
            style={{
              background: 'linear-gradient(135deg, rgba(15,20,40,0.98) 0%, rgba(10,15,30,0.98) 100%)',
              border: '1px solid rgba(99,102,241,0.2)',
            }}
          >
            {/* Header */}
            <div className="flex items-center justify-between px-6 py-4 border-b border-white/5 bg-gradient-to-r from-purple-600/10 to-indigo-600/5 shrink-0">
              <div className="flex items-center gap-3">
                <div className="w-9 h-9 rounded-xl bg-gradient-to-br from-purple-500 to-indigo-500 flex items-center justify-center">
                  <Users className="w-5 h-5 text-white" />
                </div>
                <div>
                  <p className="text-sm font-bold text-white">🤖 AI Candidate Comparison</p>
                  <p className="text-[11px] text-white/40">For: {jobTitle}</p>
                </div>
              </div>
              <button onClick={handleClose} className="w-8 h-8 rounded-xl bg-white/5 hover:bg-white/10 flex items-center justify-center transition-colors">
                <X className="w-4 h-4 text-white/60" />
              </button>
            </div>

            <div className="flex-1 overflow-y-auto p-6 space-y-5">
              {!result ? (
                <>
                  {/* Candidate selection */}
                  <div>
                    <p className="text-xs text-white/40 mb-3">
                      Select 2–5 candidates to compare <span className="text-indigo-400">({selected.length} selected)</span>
                    </p>
                    <div className="space-y-2">
                      {candidates.map(c => (
                        <button
                          key={c.applicationId}
                          onClick={() => toggleSelect(c.applicationId)}
                          className={`w-full flex items-center justify-between px-4 py-3 rounded-xl border text-left transition-all ${
                            selected.includes(c.applicationId)
                              ? 'border-indigo-500/50 bg-indigo-500/10'
                              : 'border-white/10 bg-white/[0.03] hover:bg-white/[0.06]'
                          }`}
                        >
                          <div className="flex items-center gap-3">
                            <div className={`w-2 h-2 rounded-full transition-all ${selected.includes(c.applicationId) ? 'bg-indigo-400' : 'bg-white/20'}`} />
                            <span className="text-sm text-white font-medium">{c.name}</span>
                          </div>
                          {c.matchScore != null && (
                            <span className={`text-xs font-bold ${c.matchScore >= 80 ? 'text-emerald-400' : c.matchScore >= 60 ? 'text-amber-400' : 'text-red-400'}`}>
                              {c.matchScore}% match
                            </span>
                          )}
                        </button>
                      ))}
                    </div>
                  </div>

                  <motion.button
                    whileHover={{ scale: 1.01 }}
                    whileTap={{ scale: 0.99 }}
                    onClick={() => compare()}
                    disabled={selected.length < 2 || isPending}
                    className="w-full flex items-center justify-center gap-2 bg-gradient-to-r from-purple-600 to-indigo-600 hover:from-purple-500 hover:to-indigo-500 disabled:opacity-40 text-white font-semibold text-sm py-3 rounded-xl shadow-lg transition-all"
                  >
                    {isPending ? (
                      <><Loader2 className="w-4 h-4 animate-spin" /> Comparing with AI...</>
                    ) : (
                      <><Sparkles className="w-4 h-4" /> Compare {selected.length} Candidates</>
                    )}
                  </motion.button>
                </>
              ) : (
                <>
                  {/* Results */}
                  <div className="text-center py-2">
                    <div className="inline-flex items-center gap-2 bg-amber-500/10 border border-amber-500/30 rounded-full px-4 py-2 mb-3">
                      <Trophy className="w-4 h-4 text-amber-400" />
                      <span className="text-sm font-bold text-amber-300">Best candidate: {result.bestCandidateName}</span>
                    </div>
                    <p className="text-sm text-white/60 leading-relaxed max-w-lg mx-auto">{result.summary}</p>
                  </div>

                  <div className="space-y-3">
                    {result.rankings.map(r => (
                      <RankCard key={r.rank} ranking={r} isBest={r.candidateName === result.bestCandidateName} />
                    ))}
                  </div>

                  <button
                    onClick={() => { setResult(null); setSelected([]) }}
                    className="w-full text-sm text-white/40 hover:text-white/70 py-2 transition-colors"
                  >
                    ← Compare different candidates
                  </button>
                </>
              )}
            </div>
          </motion.div>
        </>
      )}
    </AnimatePresence>
  )
}
