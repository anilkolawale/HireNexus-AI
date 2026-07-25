import { useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { motion, AnimatePresence } from 'framer-motion'
import {
  ClipboardList, Plus, ChevronRight, Clock, Users, DollarSign,
  CheckCircle, XCircle, AlertCircle, Loader2, X, Send
} from 'lucide-react'
import axiosClient from '../../api/axiosClient'
import toast from 'react-hot-toast'

const STATUS_CONFIG: Record<string, { label: string; color: string; icon: React.ElementType }> = {
  Draft:                    { label: 'Draft',            color: 'bg-slate-500/10 text-slate-400 border-slate-500/20',  icon: ClipboardList },
  PendingManagerApproval:   { label: 'Pending Manager',  color: 'bg-amber-500/10  text-amber-400  border-amber-500/20', icon: AlertCircle },
  PendingHRApproval:        { label: 'Pending HR',       color: 'bg-blue-500/10   text-blue-400   border-blue-500/20',  icon: AlertCircle },
  PendingFinanceApproval:   { label: 'Pending Finance',  color: 'bg-purple-500/10 text-purple-400 border-purple-500/20', icon: AlertCircle },
  Approved:                 { label: 'Approved',         color: 'bg-emerald-500/10 text-emerald-400 border-emerald-500/20', icon: CheckCircle },
  Rejected:                 { label: 'Rejected',         color: 'bg-red-500/10    text-red-400    border-red-500/20',   icon: XCircle },
}

const STEP_STATUS_CONFIG = {
  Pending:  { color: 'text-amber-400 bg-amber-500/10',  icon: AlertCircle },
  Approved: { color: 'text-emerald-400 bg-emerald-500/10', icon: CheckCircle },
  Rejected: { color: 'text-red-400 bg-red-500/10',      icon: XCircle },
}

interface Requisition {
  id: string; title: string; department?: string; status: string
  headcountRequested: number; budgetMin?: number; budgetMax?: number
  createdAtUtc: string; requestedBy: { name: string }; rejectionReason?: string
  approvalSteps: Array<{
    id: string; stepName: string; stepOrder: number; status: string
    comment?: string; actedAtUtc?: string; approver: { name: string }
  }>
}

function CreateRequisitionModal({ onClose }: { onClose: () => void }) {
  const qc = useQueryClient()
  const [form, setForm] = useState({
    title: '', department: '', description: '',
    budgetMin: '', budgetMax: '', headcountRequested: 1
  })

  const mutation = useMutation({
    mutationFn: async () => {
      const res = await axiosClient.post('/requisitions', {
        ...form, budgetMin: form.budgetMin ? Number(form.budgetMin) : undefined,
        budgetMax: form.budgetMax ? Number(form.budgetMax) : undefined,
      })
      return res.data
    },
    onSuccess: () => {
      toast.success('Requisition created!')
      qc.invalidateQueries({ queryKey: ['requisitions'] })
      onClose()
    },
    onError: () => toast.error('Failed to create requisition'),
  })

  return (
    <motion.div initial={{ opacity: 0 }} animate={{ opacity: 1 }} exit={{ opacity: 0 }}
      className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/60 backdrop-blur-sm"
      onClick={e => e.target === e.currentTarget && onClose()}
    >
      <motion.div initial={{ scale: 0.95, y: 20 }} animate={{ scale: 1, y: 0 }} exit={{ scale: 0.95, y: 20 }}
        className="bg-[#0f1629] border border-white/10 rounded-2xl w-full max-w-lg p-6 shadow-2xl"
      >
        <div className="flex items-center justify-between mb-5">
          <h2 className="text-white font-bold text-lg">New Job Requisition</h2>
          <button onClick={onClose} className="text-white/40 hover:text-white/70 transition-colors"><X className="w-5 h-5" /></button>
        </div>

        <div className="space-y-4">
          <input value={form.title} onChange={e => setForm(f => ({ ...f, title: e.target.value }))}
            placeholder="Job Title *"
            className="w-full bg-white/5 border border-white/10 text-white placeholder-white/30 px-4 py-2.5 rounded-xl text-sm outline-none focus:border-indigo-500/50 transition-colors" />
          <input value={form.department} onChange={e => setForm(f => ({ ...f, department: e.target.value }))}
            placeholder="Department"
            className="w-full bg-white/5 border border-white/10 text-white placeholder-white/30 px-4 py-2.5 rounded-xl text-sm outline-none focus:border-indigo-500/50 transition-colors" />
          <textarea value={form.description} onChange={e => setForm(f => ({ ...f, description: e.target.value }))}
            placeholder="Job description & justification for headcount..." rows={3}
            className="w-full bg-white/5 border border-white/10 text-white placeholder-white/30 px-4 py-2.5 rounded-xl text-sm outline-none focus:border-indigo-500/50 transition-colors resize-none" />
          <div className="grid grid-cols-3 gap-3">
            <input type="number" value={form.budgetMin} onChange={e => setForm(f => ({ ...f, budgetMin: e.target.value }))}
              placeholder="Budget Min" className="bg-white/5 border border-white/10 text-white placeholder-white/30 px-3 py-2.5 rounded-xl text-sm outline-none focus:border-indigo-500/50 transition-colors" />
            <input type="number" value={form.budgetMax} onChange={e => setForm(f => ({ ...f, budgetMax: e.target.value }))}
              placeholder="Budget Max" className="bg-white/5 border border-white/10 text-white placeholder-white/30 px-3 py-2.5 rounded-xl text-sm outline-none focus:border-indigo-500/50 transition-colors" />
            <input type="number" min={1} value={form.headcountRequested} onChange={e => setForm(f => ({ ...f, headcountRequested: Number(e.target.value) }))}
              placeholder="Headcount" className="bg-white/5 border border-white/10 text-white placeholder-white/30 px-3 py-2.5 rounded-xl text-sm outline-none focus:border-indigo-500/50 transition-colors" />
          </div>
        </div>

        <button onClick={() => mutation.mutate()} disabled={mutation.isPending || !form.title}
          className="mt-5 w-full flex items-center justify-center gap-2 bg-gradient-to-r from-indigo-600 to-purple-600 text-white font-semibold py-2.5 rounded-xl disabled:opacity-50 transition-opacity">
          {mutation.isPending ? <Loader2 className="w-4 h-4 animate-spin" /> : <Plus className="w-4 h-4" />}
          Create Requisition
        </button>
      </motion.div>
    </motion.div>
  )
}

function RequisitionDetailModal({ req, onClose }: { req: Requisition; onClose: () => void }) {
  const qc = useQueryClient()
  const [comment, setComment] = useState('')

  const actMutation = useMutation({
    mutationFn: ({ action }: { action: 'approve' | 'reject' }) =>
      axiosClient.post(`/requisitions/${req.id}/${action}`, { comment }).then(r => r.data),
    onSuccess: () => { toast.success('Action recorded!'); qc.invalidateQueries({ queryKey: ['requisitions'] }); onClose() },
    onError: () => toast.error('Failed to process action'),
  })

  const cfg = STATUS_CONFIG[req.status] ?? STATUS_CONFIG.Draft

  return (
    <motion.div initial={{ opacity: 0 }} animate={{ opacity: 1 }} exit={{ opacity: 0 }}
      className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/60 backdrop-blur-sm"
      onClick={e => e.target === e.currentTarget && onClose()}
    >
      <motion.div initial={{ scale: 0.95, y: 20 }} animate={{ scale: 1, y: 0 }} exit={{ scale: 0.95, y: 20 }}
        className="bg-[#0f1629] border border-white/10 rounded-2xl w-full max-w-lg p-6 shadow-2xl max-h-[80vh] overflow-y-auto"
      >
        <div className="flex items-center justify-between mb-1">
          <h2 className="text-white font-bold text-lg truncate">{req.title}</h2>
          <button onClick={onClose} className="text-white/40 hover:text-white/70 ml-4 flex-shrink-0"><X className="w-5 h-5" /></button>
        </div>
        <div className="flex items-center gap-2 mb-5">
          <span className={`inline-flex items-center gap-1 px-2.5 py-1 rounded-full text-xs font-semibold border ${cfg.color}`}>
            <cfg.icon className="w-3 h-3" /> {cfg.label}
          </span>
          {req.department && <span className="text-white/40 text-xs">{req.department}</span>}
        </div>

        <div className="grid grid-cols-2 gap-3 mb-5">
          <div className="bg-white/5 rounded-xl p-3">
            <p className="text-white/40 text-xs">Headcount</p>
            <p className="text-white font-bold">{req.headcountRequested} position{req.headcountRequested > 1 ? 's' : ''}</p>
          </div>
          {(req.budgetMin || req.budgetMax) && (
            <div className="bg-white/5 rounded-xl p-3">
              <p className="text-white/40 text-xs">Budget</p>
              <p className="text-emerald-400 font-bold text-sm">
                {req.budgetMin && req.budgetMax ? `$${(req.budgetMin/1000).toFixed(0)}k–$${(req.budgetMax/1000).toFixed(0)}k`
                  : req.budgetMin ? `From $${(req.budgetMin/1000).toFixed(0)}k`
                  : `Up to $${(req.budgetMax!/1000).toFixed(0)}k`}
              </p>
            </div>
          )}
        </div>

        {/* Approval Timeline */}
        <div className="space-y-3 mb-5">
          <p className="text-white/60 text-xs font-semibold uppercase tracking-wider">Approval Timeline</p>
          {req.approvalSteps.sort((a, b) => a.stepOrder - b.stepOrder).map((step, i) => {
            const sc = STEP_STATUS_CONFIG[step.status as keyof typeof STEP_STATUS_CONFIG] ?? STEP_STATUS_CONFIG.Pending
            return (
              <div key={step.id} className="flex items-start gap-3">
                <div className={`w-7 h-7 rounded-full flex items-center justify-center flex-shrink-0 ${sc.color}`}>
                  <sc.icon className="w-3.5 h-3.5" />
                </div>
                <div className="flex-1 min-w-0">
                  <div className="flex items-center justify-between">
                    <p className="text-white text-sm font-medium">{step.stepName}</p>
                    <p className="text-white/30 text-xs">{step.approver.name}</p>
                  </div>
                  {step.comment && <p className="text-white/50 text-xs mt-0.5 italic">"{step.comment}"</p>}
                  {step.actedAtUtc && <p className="text-white/30 text-xs">{new Date(step.actedAtUtc).toLocaleDateString()}</p>}
                </div>
              </div>
            )
          })}
          {req.approvalSteps.length === 0 && (
            <p className="text-white/30 text-sm">Approval chain not yet configured.</p>
          )}
        </div>

        {/* Action buttons for pending approvers */}
        {['PendingManagerApproval', 'PendingHRApproval', 'PendingFinanceApproval'].includes(req.status) && (
          <div className="border-t border-white/10 pt-4 space-y-3">
            <textarea value={comment} onChange={e => setComment(e.target.value)}
              placeholder="Comment (optional)..." rows={2}
              className="w-full bg-white/5 border border-white/10 text-white placeholder-white/30 px-3 py-2 rounded-xl text-sm outline-none resize-none" />
            <div className="flex gap-2">
              <button onClick={() => actMutation.mutate({ action: 'approve' })} disabled={actMutation.isPending}
                className="flex-1 flex items-center justify-center gap-2 bg-emerald-500/20 hover:bg-emerald-500/30 border border-emerald-500/30 text-emerald-400 font-semibold py-2.5 rounded-xl text-sm transition-colors">
                <CheckCircle className="w-4 h-4" /> Approve
              </button>
              <button onClick={() => actMutation.mutate({ action: 'reject' })} disabled={actMutation.isPending}
                className="flex-1 flex items-center justify-center gap-2 bg-red-500/20 hover:bg-red-500/30 border border-red-500/30 text-red-400 font-semibold py-2.5 rounded-xl text-sm transition-colors">
                <XCircle className="w-4 h-4" /> Reject
              </button>
            </div>
          </div>
        )}
      </motion.div>
    </motion.div>
  )
}

export default function RequisitionsPage() {
  const [showCreate, setShowCreate] = useState(false)
  const [selected, setSelected] = useState<Requisition | null>(null)

  const { data: requisitions = [], isLoading } = useQuery<Requisition[]>({
    queryKey: ['requisitions'],
    queryFn: () => axiosClient.get('/requisitions').then(r => r.data),
    staleTime: 1000 * 60,
  })

  const stats = {
    total: requisitions.length,
    pending: requisitions.filter(r => r.status.includes('Pending')).length,
    approved: requisitions.filter(r => r.status === 'Approved').length,
    rejected: requisitions.filter(r => r.status === 'Rejected').length,
  }

  return (
    <div className="min-h-full space-y-6 p-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-black text-white">Job Requisitions</h1>
          <p className="text-white/40 text-sm mt-0.5">Manage headcount approval workflows</p>
        </div>
        <motion.button whileHover={{ scale: 1.03 }} whileTap={{ scale: 0.97 }}
          onClick={() => setShowCreate(true)}
          className="flex items-center gap-2 bg-gradient-to-r from-indigo-600 to-purple-600 text-white font-semibold text-sm px-5 py-2.5 rounded-xl shadow-lg shadow-indigo-500/25">
          <Plus className="w-4 h-4" /> New Requisition
        </motion.button>
      </div>

      {/* KPI Cards */}
      <div className="grid grid-cols-4 gap-4">
        {[
          { label: 'Total', value: stats.total, color: 'indigo', icon: ClipboardList },
          { label: 'Pending', value: stats.pending, color: 'amber', icon: AlertCircle },
          { label: 'Approved', value: stats.approved, color: 'emerald', icon: CheckCircle },
          { label: 'Rejected', value: stats.rejected, color: 'red', icon: XCircle },
        ].map(kpi => (
          <div key={kpi.label} className="bg-white/[0.04] border border-white/[0.08] rounded-2xl p-4">
            <div className={`w-9 h-9 rounded-xl bg-${kpi.color}-500/10 flex items-center justify-center mb-3`}>
              <kpi.icon className={`w-5 h-5 text-${kpi.color}-400`} />
            </div>
            <p className="text-2xl font-black text-white">{kpi.value}</p>
            <p className="text-white/40 text-xs">{kpi.label}</p>
          </div>
        ))}
      </div>

      {/* Table */}
      <div className="bg-white/[0.04] border border-white/[0.08] rounded-2xl overflow-hidden">
        {isLoading ? (
          <div className="flex items-center justify-center h-48">
            <Loader2 className="w-6 h-6 text-indigo-400 animate-spin" />
          </div>
        ) : requisitions.length === 0 ? (
          <div className="text-center py-16">
            <ClipboardList className="w-10 h-10 text-white/20 mx-auto mb-3" />
            <p className="text-white/30 text-sm">No requisitions yet. Create your first one.</p>
          </div>
        ) : (
          <table className="w-full">
            <thead>
              <tr className="border-b border-white/[0.06]">
                {['Role', 'Department', 'Headcount', 'Budget', 'Status', 'Requested By', ''].map(h => (
                  <th key={h} className="text-left text-white/40 text-xs font-semibold uppercase tracking-wider px-4 py-3">{h}</th>
                ))}
              </tr>
            </thead>
            <tbody className="divide-y divide-white/[0.04]">
              {requisitions.map(req => {
                const cfg = STATUS_CONFIG[req.status] ?? STATUS_CONFIG.Draft
                return (
                  <tr key={req.id} className="hover:bg-white/[0.03] transition-colors">
                    <td className="px-4 py-3.5 text-white font-medium text-sm">{req.title}</td>
                    <td className="px-4 py-3.5 text-white/50 text-sm">{req.department ?? '—'}</td>
                    <td className="px-4 py-3.5">
                      <span className="flex items-center gap-1 text-white/70 text-sm">
                        <Users className="w-3.5 h-3.5" /> {req.headcountRequested}
                      </span>
                    </td>
                    <td className="px-4 py-3.5 text-emerald-400 text-sm">
                      {req.budgetMin ? `$${(req.budgetMin/1000).toFixed(0)}k` : '—'}
                      {req.budgetMax ? `–$${(req.budgetMax/1000).toFixed(0)}k` : ''}
                    </td>
                    <td className="px-4 py-3.5">
                      <span className={`inline-flex items-center gap-1 px-2.5 py-1 rounded-full text-xs font-semibold border ${cfg.color}`}>
                        <cfg.icon className="w-3 h-3" /> {cfg.label}
                      </span>
                    </td>
                    <td className="px-4 py-3.5 text-white/50 text-sm">{req.requestedBy.name}</td>
                    <td className="px-4 py-3.5">
                      <button onClick={() => setSelected(req)}
                        className="text-white/30 hover:text-indigo-400 transition-colors">
                        <ChevronRight className="w-4 h-4" />
                      </button>
                    </td>
                  </tr>
                )
              })}
            </tbody>
          </table>
        )}
      </div>

      <AnimatePresence>
        {showCreate && <CreateRequisitionModal onClose={() => setShowCreate(false)} />}
        {selected && <RequisitionDetailModal req={selected} onClose={() => setSelected(null)} />}
      </AnimatePresence>
    </div>
  )
}
