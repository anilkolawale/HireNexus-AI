import { useState } from 'react'
import { motion, AnimatePresence } from 'framer-motion'
import { X, User, Mail, Phone, FileText, Send, CheckCircle, Loader2 } from 'lucide-react'
import { useMutation } from '@tanstack/react-query'
import { publicApi, type PublicJob, type PublicApplyPayload } from '../../api/endpoints/public.api'
import toast from 'react-hot-toast'

interface Props { job: PublicJob; onClose: () => void }

export default function JobApplyModal({ job, onClose }: Props) {
  const [form, setForm] = useState({ fullName: '', email: '', phone: '', coverLetter: '' })
  const [submitted, setSubmitted] = useState<{ message: string; reference: string } | null>(null)

  const mutation = useMutation({
    mutationFn: (payload: PublicApplyPayload) => publicApi.apply(payload),
    onSuccess: data => setSubmitted(data),
    onError: () => toast.error('Failed to submit application. Please try again.'),
  })

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault()
    if (!form.fullName || !form.email) return toast.error('Name and email are required.')
    mutation.mutate({ jobId: job.id, ...form })
  }

  return (
    <motion.div
      initial={{ opacity: 0 }}
      animate={{ opacity: 1 }}
      exit={{ opacity: 0 }}
      className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/50 backdrop-blur-sm"
      onClick={e => e.target === e.currentTarget && onClose()}
    >
      <motion.div
        initial={{ scale: 0.95, opacity: 0, y: 20 }}
        animate={{ scale: 1, opacity: 1, y: 0 }}
        exit={{ scale: 0.95, opacity: 0, y: 20 }}
        className="bg-white rounded-2xl shadow-2xl w-full max-w-lg overflow-hidden"
      >
        {/* Header */}
        <div className="relative p-6 pb-4"
          style={{ background: 'linear-gradient(135deg, #4f46e5, #7c3aed)' }}
        >
          <button onClick={onClose} className="absolute top-4 right-4 text-white/70 hover:text-white transition-colors">
            <X className="w-5 h-5" />
          </button>
          <p className="text-indigo-200 text-sm font-medium mb-1">{job.company.name}</p>
          <h2 className="text-white text-xl font-bold">{job.title}</h2>
          {job.location && <p className="text-indigo-200 text-sm mt-1">📍 {job.location}</p>}
        </div>

        <div className="p-6">
          <AnimatePresence mode="wait">
            {submitted ? (
              <motion.div
                key="success"
                initial={{ opacity: 0, scale: 0.9 }}
                animate={{ opacity: 1, scale: 1 }}
                className="text-center py-8"
              >
                <div className="w-16 h-16 rounded-full bg-emerald-100 flex items-center justify-center mx-auto mb-4">
                  <CheckCircle className="w-8 h-8 text-emerald-500" />
                </div>
                <h3 className="text-slate-800 text-lg font-bold mb-2">Application Submitted!</h3>
                <p className="text-slate-500 text-sm mb-4">{submitted.message}</p>
                <div className="bg-slate-50 rounded-xl p-3 font-mono text-sm text-indigo-600 font-bold">
                  {submitted.reference}
                </div>
                <p className="text-slate-400 text-xs mt-2">Save this reference number for your records.</p>
                <button
                  onClick={onClose}
                  className="mt-6 bg-indigo-600 text-white px-6 py-2.5 rounded-xl text-sm font-semibold hover:bg-indigo-500 transition-colors"
                >
                  Close
                </button>
              </motion.div>
            ) : (
              <motion.form key="form" onSubmit={handleSubmit} className="space-y-4">
                <div className="grid grid-cols-2 gap-4">
                  <div>
                    <label className="text-xs font-semibold text-slate-500 uppercase tracking-wide mb-1.5 flex items-center gap-1">
                      <User className="w-3 h-3" /> Full Name *
                    </label>
                    <input
                      value={form.fullName}
                      onChange={e => setForm(f => ({ ...f, fullName: e.target.value }))}
                      placeholder="John Smith"
                      required
                      className="w-full px-3 py-2.5 rounded-xl border border-slate-200 text-slate-800 text-sm focus:border-indigo-400 focus:ring-2 focus:ring-indigo-100 outline-none transition-all"
                    />
                  </div>
                  <div>
                    <label className="text-xs font-semibold text-slate-500 uppercase tracking-wide mb-1.5 flex items-center gap-1">
                      <Phone className="w-3 h-3" /> Phone
                    </label>
                    <input
                      value={form.phone}
                      onChange={e => setForm(f => ({ ...f, phone: e.target.value }))}
                      placeholder="+1 555 000 0000"
                      className="w-full px-3 py-2.5 rounded-xl border border-slate-200 text-slate-800 text-sm focus:border-indigo-400 focus:ring-2 focus:ring-indigo-100 outline-none transition-all"
                    />
                  </div>
                </div>

                <div>
                  <label className="text-xs font-semibold text-slate-500 uppercase tracking-wide mb-1.5 flex items-center gap-1">
                    <Mail className="w-3 h-3" /> Email Address *
                  </label>
                  <input
                    type="email"
                    value={form.email}
                    onChange={e => setForm(f => ({ ...f, email: e.target.value }))}
                    placeholder="john@example.com"
                    required
                    className="w-full px-3 py-2.5 rounded-xl border border-slate-200 text-slate-800 text-sm focus:border-indigo-400 focus:ring-2 focus:ring-indigo-100 outline-none transition-all"
                  />
                </div>

                <div>
                  <label className="text-xs font-semibold text-slate-500 uppercase tracking-wide mb-1.5 flex items-center gap-1">
                    <FileText className="w-3 h-3" /> Cover Letter
                  </label>
                  <textarea
                    value={form.coverLetter}
                    onChange={e => setForm(f => ({ ...f, coverLetter: e.target.value }))}
                    placeholder="Tell us why you're excited about this role..."
                    rows={4}
                    className="w-full px-3 py-2.5 rounded-xl border border-slate-200 text-slate-800 text-sm focus:border-indigo-400 focus:ring-2 focus:ring-indigo-100 outline-none transition-all resize-none"
                  />
                </div>

                <p className="text-xs text-slate-400">
                  By applying, you agree to our privacy policy. Your data is processed securely per GDPR guidelines.
                </p>

                <button
                  type="submit"
                  disabled={mutation.isPending}
                  className="w-full flex items-center justify-center gap-2 bg-gradient-to-r from-indigo-600 to-purple-600 hover:from-indigo-500 hover:to-purple-500 text-white font-bold py-3 rounded-xl shadow-lg shadow-indigo-500/30 transition-all disabled:opacity-60"
                >
                  {mutation.isPending ? (
                    <><Loader2 className="w-4 h-4 animate-spin" /> Submitting...</>
                  ) : (
                    <><Send className="w-4 h-4" /> Submit Application</>
                  )}
                </button>
              </motion.form>
            )}
          </AnimatePresence>
        </div>
      </motion.div>
    </motion.div>
  )
}
