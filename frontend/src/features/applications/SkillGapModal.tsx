import { useQuery } from '@tanstack/react-query'
import { motion, AnimatePresence } from 'framer-motion'
import { X, CheckCircle, XCircle, AlertTriangle, BookOpen, Loader2 } from 'lucide-react'
import { aiAssistantApi } from '../../api/endpoints/aiAssistant.api'

interface SkillGapModalProps {
  applicationId: string
  isOpen: boolean
  onClose: () => void
}

function SeverityAlert({ severity }: { severity: 1 | 2 | 3 }) {
  const config = {
    1: { label: 'Minor Gap', cls: 'bg-emerald-500/10 border-emerald-500/30 text-emerald-400', icon: CheckCircle },
    2: { label: 'Moderate Gap', cls: 'bg-amber-500/10 border-amber-500/30 text-amber-400', icon: AlertTriangle },
    3: { label: 'Critical Gap', cls: 'bg-red-500/10 border-red-500/30 text-red-400', icon: XCircle },
  }[severity]
  const Icon = config.icon
  return (
    <div className={`flex items-center gap-2 border rounded-xl px-4 py-3 ${config.cls}`}>
      <Icon className="w-4 h-4 shrink-0" />
      <div>
        <span className="text-xs font-bold uppercase tracking-wider">{config.label}</span>
        <p className="text-[11px] opacity-70 mt-0.5">
          {severity === 1 && 'Candidate is a strong fit with minor skill gaps.'}
          {severity === 2 && 'Some important skills are missing — consider training.'}
          {severity === 3 && 'Significant skill gaps found — carefully assess fit.'}
        </p>
      </div>
    </div>
  )
}

function SkillTag({ label, variant }: { label: string; variant: 'has' | 'requires' | 'gap' | 'bonus' }) {
  const cls = {
    has: 'bg-emerald-500/10 text-emerald-400 border-emerald-500/20',
    requires: 'bg-indigo-500/10 text-indigo-400 border-indigo-500/20',
    gap: 'bg-red-500/10 text-red-400 border-red-500/20',
    bonus: 'bg-emerald-500/10 text-emerald-300 border-emerald-500/20',
  }[variant]
  const icon = {
    has: '✓',
    requires: '✓',
    gap: '✗',
    bonus: '+',
  }[variant]
  return (
    <span className={`inline-flex items-center gap-1 text-[11px] font-medium border rounded-lg px-2.5 py-1 ${cls}`}>
      <span className="font-bold">{icon}</span>
      {label}
    </span>
  )
}

export function SkillGapModal({ applicationId, isOpen, onClose }: SkillGapModalProps) {
  const { data, isLoading, isError } = useQuery({
    queryKey: ['skill-gap', applicationId],
    queryFn: () => aiAssistantApi.getSkillGap(applicationId),
    enabled: isOpen && !!applicationId,
  })

  return (
    <AnimatePresence>
      {isOpen && (
        <>
          {/* Backdrop */}
          <motion.div
            initial={{ opacity: 0 }}
            animate={{ opacity: 1 }}
            exit={{ opacity: 0 }}
            className="fixed inset-0 bg-black/60 backdrop-blur-sm z-50"
            onClick={onClose}
          />

          {/* Modal */}
          <motion.div
            initial={{ opacity: 0, scale: 0.92, y: 20 }}
            animate={{ opacity: 1, scale: 1, y: 0 }}
            exit={{ opacity: 0, scale: 0.92, y: 20 }}
            transition={{ type: 'spring', stiffness: 400, damping: 28 }}
            className="fixed inset-0 flex items-center justify-center z-50 p-4 pointer-events-none"
          >
            <div
              className="w-full max-w-2xl max-h-[85vh] overflow-y-auto rounded-2xl border border-white/10 bg-[#0d1426]/95 backdrop-blur-xl shadow-2xl pointer-events-auto"
              onClick={(e) => e.stopPropagation()}
            >
              {/* Header */}
              <div className="flex items-center justify-between px-6 py-4 border-b border-white/5 bg-gradient-to-r from-indigo-600/10 to-purple-600/5 sticky top-0">
                <div>
                  <h2 className="text-base font-bold text-white">Skill Gap Analysis</h2>
                  <p className="text-xs text-white/40 mt-0.5">Application #{applicationId.slice(-6)}</p>
                </div>
                <button
                  onClick={onClose}
                  className="w-8 h-8 rounded-xl bg-white/5 hover:bg-white/10 flex items-center justify-center transition-colors"
                >
                  <X className="w-4 h-4 text-white/60" />
                </button>
              </div>

              {/* Content */}
              <div className="p-6 space-y-6">
                {isLoading && (
                  <div className="flex flex-col items-center justify-center py-16 gap-3">
                    <Loader2 className="w-8 h-8 text-indigo-400 animate-spin" />
                    <p className="text-white/40 text-sm">Analyzing skill gaps…</p>
                  </div>
                )}

                {isError && (
                  <div className="flex flex-col items-center justify-center py-16 gap-3">
                    <XCircle className="w-8 h-8 text-red-400" />
                    <p className="text-white/40 text-sm">Could not load skill gap data</p>
                  </div>
                )}

                {data && (
                  <>
                    {/* Severity alert */}
                    <SeverityAlert severity={data.gapSeverity} />

                    {/* Two-column skill comparison */}
                    <div className="grid grid-cols-2 gap-4">
                      <div className="space-y-3">
                        <h3 className="text-xs font-bold text-emerald-400 uppercase tracking-wider flex items-center gap-1.5">
                          <CheckCircle className="w-3.5 h-3.5" />
                          Candidate Has ({data.candidateHas.length})
                        </h3>
                        <div className="flex flex-wrap gap-2">
                          {data.candidateHas.length > 0 ? (
                            data.candidateHas.map((s) => (
                              <SkillTag key={s} label={s} variant="has" />
                            ))
                          ) : (
                            <p className="text-xs text-white/25">No skills listed</p>
                          )}
                        </div>
                      </div>

                      <div className="space-y-3">
                        <h3 className="text-xs font-bold text-indigo-400 uppercase tracking-wider flex items-center gap-1.5">
                          <CheckCircle className="w-3.5 h-3.5" />
                          Job Requires ({data.jobRequires.length})
                        </h3>
                        <div className="flex flex-wrap gap-2">
                          {data.jobRequires.length > 0 ? (
                            data.jobRequires.map((s) => (
                              <SkillTag key={s} label={s} variant="requires" />
                            ))
                          ) : (
                            <p className="text-xs text-white/25">No requirements listed</p>
                          )}
                        </div>
                      </div>
                    </div>

                    {/* Divider */}
                    <div className="h-px bg-white/5" />

                    {/* Gap skills */}
                    {data.gapSkills.length > 0 && (
                      <div className="space-y-3">
                        <h3 className="text-xs font-bold text-red-400 uppercase tracking-wider flex items-center gap-1.5">
                          <XCircle className="w-3.5 h-3.5" />
                          Missing Skills ({data.gapSkills.length})
                        </h3>
                        <div className="flex flex-wrap gap-2">
                          {data.gapSkills.map((s) => (
                            <SkillTag key={s} label={s} variant="gap" />
                          ))}
                        </div>
                      </div>
                    )}

                    {/* Bonus skills */}
                    {data.bonusSkills.length > 0 && (
                      <div className="space-y-3">
                        <h3 className="text-xs font-bold text-emerald-300 uppercase tracking-wider flex items-center gap-1.5">
                          <CheckCircle className="w-3.5 h-3.5" />
                          Bonus Skills ({data.bonusSkills.length})
                        </h3>
                        <div className="flex flex-wrap gap-2">
                          {data.bonusSkills.map((s) => (
                            <SkillTag key={s} label={s} variant="bonus" />
                          ))}
                        </div>
                      </div>
                    )}

                    {/* Learning recommendations */}
                    {data.learningRecommendations && (
                      <div className="space-y-3 border-t border-white/5 pt-4">
                        <h3 className="text-xs font-bold text-white/50 uppercase tracking-wider flex items-center gap-1.5">
                          <BookOpen className="w-3.5 h-3.5" />
                          AI Learning Recommendations
                        </h3>
                        <div className="bg-white/[0.04] border border-white/8 rounded-xl p-4">
                          <p className="text-xs text-white/60 leading-relaxed whitespace-pre-wrap">
                            {data.learningRecommendations}
                          </p>
                        </div>
                      </div>
                    )}
                  </>
                )}
              </div>
            </div>
          </motion.div>
        </>
      )}
    </AnimatePresence>
  )
}

export default SkillGapModal
