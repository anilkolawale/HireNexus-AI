import { useState } from 'react'
import { motion, AnimatePresence } from 'framer-motion'
import { useQuery, useMutation } from '@tanstack/react-query'
import {
  X, FileText, Loader2, Copy, Check, Download,
  Sparkles, Building2, Calendar, DollarSign, User
} from 'lucide-react'
import axiosClient from '../../api/axiosClient'

interface Props {
  applicationId: string
  candidateName: string
  jobTitle: string
  companyName: string
  isOpen: boolean
  onClose: () => void
}

interface OfferLetterData {
  candidateName: string
  jobTitle: string
  companyName: string
  offeredSalary: number
  joiningDate: string
}

async function draftOfferLetter(applicationId: string, data: OfferLetterData): Promise<{ draftLetterText: string }> {
  const response = await axiosClient.post(`/offers/draft-letter`, {
    applicationId,
    ...data,
  })
  return response.data
}

export function OfferLetterModal({ applicationId, candidateName, jobTitle, companyName, isOpen, onClose }: Props) {
  const [salary, setSalary] = useState('')
  const [joiningDate, setJoiningDate] = useState('')
  const [generatedText, setGeneratedText] = useState('')
  const [copied, setCopied] = useState(false)

  const { mutate: generate, isPending } = useMutation({
    mutationFn: () => draftOfferLetter(applicationId, {
      candidateName,
      jobTitle,
      companyName,
      offeredSalary: Number(salary),
      joiningDate,
    }),
    onSuccess: (data) => {
      setGeneratedText(data.draftLetterText)
    },
    onError: () => {
      setGeneratedText('Failed to generate offer letter. Please check your Gemini API key in appsettings.json.')
    },
  })

  const handleCopy = async () => {
    await navigator.clipboard.writeText(generatedText)
    setCopied(true)
    setTimeout(() => setCopied(false), 2000)
  }

  const handleDownload = () => {
    const blob = new Blob([generatedText], { type: 'text/plain' })
    const url = URL.createObjectURL(blob)
    const a = document.createElement('a')
    a.href = url
    a.download = `offer-letter-${candidateName.replace(/\s+/g, '-')}.txt`
    a.click()
    URL.revokeObjectURL(url)
  }

  return (
    <AnimatePresence>
      {isOpen && (
        <>
          {/* Backdrop */}
          <motion.div
            initial={{ opacity: 0 }}
            animate={{ opacity: 1 }}
            exit={{ opacity: 0 }}
            onClick={onClose}
            className="fixed inset-0 bg-black/60 backdrop-blur-sm z-50"
          />

          {/* Modal */}
          <motion.div
            initial={{ opacity: 0, scale: 0.92, y: 20 }}
            animate={{ opacity: 1, scale: 1, y: 0 }}
            exit={{ opacity: 0, scale: 0.92, y: 20 }}
            transition={{ type: 'spring', stiffness: 300, damping: 30 }}
            className="fixed inset-x-4 top-[5%] bottom-[5%] md:inset-x-auto md:left-1/2 md:-translate-x-1/2 md:w-[680px] z-50 flex flex-col overflow-hidden rounded-2xl"
            style={{
              background: 'linear-gradient(135deg, rgba(15,20,40,0.98) 0%, rgba(10,15,30,0.98) 100%)',
              border: '1px solid rgba(99,102,241,0.2)',
            }}
          >
            {/* Header */}
            <div className="flex items-center justify-between px-6 py-4 border-b border-white/5 bg-gradient-to-r from-indigo-600/10 to-emerald-600/5 shrink-0">
              <div className="flex items-center gap-3">
                <div className="w-9 h-9 rounded-xl bg-gradient-to-br from-indigo-500 to-emerald-500 flex items-center justify-center">
                  <FileText className="w-5 h-5 text-white" />
                </div>
                <div>
                  <p className="text-sm font-bold text-white">✨ AI Offer Letter Generator</p>
                  <p className="text-[11px] text-white/40">Powered by Gemini</p>
                </div>
              </div>
              <button
                onClick={onClose}
                className="w-8 h-8 rounded-xl bg-white/5 hover:bg-white/10 flex items-center justify-center transition-colors"
              >
                <X className="w-4 h-4 text-white/60" />
              </button>
            </div>

            <div className="flex-1 overflow-y-auto p-6 space-y-5">
              {/* Candidate info (read-only) */}
              <div className="grid grid-cols-2 gap-3">
                <div className="bg-white/5 rounded-xl p-3 flex items-center gap-2">
                  <User className="w-4 h-4 text-indigo-400 shrink-0" />
                  <div>
                    <p className="text-[10px] text-white/30 uppercase tracking-wider">Candidate</p>
                    <p className="text-sm text-white font-medium">{candidateName}</p>
                  </div>
                </div>
                <div className="bg-white/5 rounded-xl p-3 flex items-center gap-2">
                  <Building2 className="w-4 h-4 text-emerald-400 shrink-0" />
                  <div>
                    <p className="text-[10px] text-white/30 uppercase tracking-wider">Role</p>
                    <p className="text-sm text-white font-medium">{jobTitle}</p>
                  </div>
                </div>
              </div>

              {/* Inputs */}
              <div className="grid grid-cols-2 gap-4">
                <div className="space-y-1.5">
                  <label className="text-xs text-white/50 font-semibold uppercase tracking-wider flex items-center gap-1">
                    <DollarSign className="w-3 h-3" /> Offered Salary
                  </label>
                  <input
                    type="number"
                    value={salary}
                    onChange={e => setSalary(e.target.value)}
                    placeholder="e.g. 1200000"
                    className="w-full bg-white/5 border border-white/10 rounded-xl px-4 py-2.5 text-sm text-white placeholder-white/25 focus:outline-none focus:border-indigo-500/50 focus:ring-1 focus:ring-indigo-500/30 transition-all"
                  />
                </div>
                <div className="space-y-1.5">
                  <label className="text-xs text-white/50 font-semibold uppercase tracking-wider flex items-center gap-1">
                    <Calendar className="w-3 h-3" /> Joining Date
                  </label>
                  <input
                    type="date"
                    value={joiningDate}
                    onChange={e => setJoiningDate(e.target.value)}
                    className="w-full bg-white/5 border border-white/10 rounded-xl px-4 py-2.5 text-sm text-white focus:outline-none focus:border-indigo-500/50 focus:ring-1 focus:ring-indigo-500/30 transition-all [color-scheme:dark]"
                  />
                </div>
              </div>

              {/* Generate button */}
              <motion.button
                whileHover={{ scale: 1.01 }}
                whileTap={{ scale: 0.99 }}
                onClick={() => generate()}
                disabled={isPending || !salary || !joiningDate}
                className="w-full flex items-center justify-center gap-2 bg-gradient-to-r from-indigo-600 to-indigo-500 hover:from-indigo-500 hover:to-indigo-400 disabled:opacity-40 text-white font-semibold text-sm py-3 rounded-xl shadow-lg shadow-indigo-500/25 transition-all"
              >
                {isPending ? (
                  <><Loader2 className="w-4 h-4 animate-spin" /> Generating with Gemini...</>
                ) : (
                  <><Sparkles className="w-4 h-4" /> Generate Offer Letter</>
                )}
              </motion.button>

              {/* Generated letter */}
              <AnimatePresence>
                {generatedText && (
                  <motion.div
                    initial={{ opacity: 0, y: 12 }}
                    animate={{ opacity: 1, y: 0 }}
                    className="space-y-3"
                  >
                    <div className="flex items-center justify-between">
                      <p className="text-xs text-white/50 font-semibold uppercase tracking-wider">Generated Letter</p>
                      <div className="flex gap-2">
                        <button
                          onClick={handleCopy}
                          className="flex items-center gap-1.5 text-[11px] text-indigo-400 hover:text-indigo-300 bg-indigo-500/10 hover:bg-indigo-500/20 border border-indigo-500/20 rounded-lg px-3 py-1.5 transition-all"
                        >
                          {copied ? <Check className="w-3 h-3" /> : <Copy className="w-3 h-3" />}
                          {copied ? 'Copied!' : 'Copy'}
                        </button>
                        <button
                          onClick={handleDownload}
                          className="flex items-center gap-1.5 text-[11px] text-emerald-400 hover:text-emerald-300 bg-emerald-500/10 hover:bg-emerald-500/20 border border-emerald-500/20 rounded-lg px-3 py-1.5 transition-all"
                        >
                          <Download className="w-3 h-3" />
                          Download
                        </button>
                      </div>
                    </div>
                    <div className="bg-white/[0.04] border border-white/10 rounded-xl p-5 text-sm text-white/75 whitespace-pre-wrap leading-relaxed max-h-80 overflow-y-auto scrollbar-hide font-mono text-xs">
                      {generatedText}
                    </div>
                  </motion.div>
                )}
              </AnimatePresence>
            </div>
          </motion.div>
        </>
      )}
    </AnimatePresence>
  )
}
