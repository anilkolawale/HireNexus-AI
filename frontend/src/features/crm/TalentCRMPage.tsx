import { useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { motion } from 'framer-motion'
import {
  UserPlus, Search, Mail, Phone, Briefcase, Tag,
  Sparkles, Plus, X, Loader2, Copy, Link2
} from 'lucide-react'
import axiosClient from '../../api/axiosClient'
import toast from 'react-hot-toast'

const SOURCE_CONFIG: Record<string, { label: string; color: string }> = {
  1: { label: 'LinkedIn',     color: 'bg-blue-500/10 text-blue-400 border-blue-500/20' },
  2: { label: 'Referral',     color: 'bg-purple-500/10 text-purple-400 border-purple-500/20' },
  3: { label: 'Career Site',  color: 'bg-emerald-500/10 text-emerald-400 border-emerald-500/20' },
  4: { label: 'Agency',       color: 'bg-amber-500/10 text-amber-400 border-amber-500/20' },
  5: { label: 'Cold Outreach', color: 'bg-indigo-500/10 text-indigo-400 border-indigo-500/20' },
  6: { label: 'Job Board',    color: 'bg-cyan-500/10 text-cyan-400 border-cyan-500/20' },
  7: { label: 'Other',        color: 'bg-slate-500/10 text-slate-400 border-slate-500/20' },
}

const STATUS_CONFIG: Record<string, { label: string; color: string }> = {
  1: { label: 'New',           color: 'bg-indigo-500/10 text-indigo-400' },
  2: { label: 'Contacted',     color: 'bg-blue-500/10   text-blue-400' },
  3: { label: 'Interested',    color: 'bg-emerald-500/10 text-emerald-400' },
  4: { label: 'Not Interested', color: 'bg-red-500/10   text-red-400' },
  5: { label: 'Converted',     color: 'bg-green-500/10  text-green-400' },
}

interface Prospect {
  id: string; fullName: string; email: string; phone?: string
  currentTitle?: string; linkedInUrl?: string; skills?: string
  source: number; status: number; lastContactedAtUtc?: string
  createdAtUtc: string; hasAiOutreach: boolean
}

export default function TalentCRMPage() {
  const qc = useQueryClient()
  const [search, setSearch] = useState('')
  const [showAdd, setShowAdd] = useState(false)
  const [outreachEmail, setOutreachEmail] = useState<string | null>(null)
  const [outreachLoading, setOutreachLoading] = useState<string | null>(null)
  const [form, setForm] = useState({ fullName: '', email: '', phone: '', linkedInUrl: '', currentTitle: '', skills: '', source: '7' })

  const { data: prospects = [], isLoading } = useQuery<Prospect[]>({
    queryKey: ['prospects'],
    queryFn: () => axiosClient.get('/prospects').then(r => r.data),
    staleTime: 1000 * 60,
  })

  const addMutation = useMutation({
    mutationFn: () => axiosClient.post('/prospects', { ...form, source: Number(form.source) }),
    onSuccess: () => { toast.success('Prospect added!'); qc.invalidateQueries({ queryKey: ['prospects'] }); setShowAdd(false); setForm({ fullName: '', email: '', phone: '', linkedInUrl: '', currentTitle: '', skills: '', source: '7' }) },
    onError: () => toast.error('Failed to add prospect'),
  })

  const statusMutation = useMutation({
    mutationFn: ({ id, status }: { id: string; status: number }) =>
      axiosClient.patch(`/prospects/${id}/status`, { status }),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['prospects'] }),
  })

  const generateOutreach = async (prospect: Prospect) => {
    setOutreachLoading(prospect.id)
    try {
      const { data } = await axiosClient.post(`/prospects/${prospect.id}/ai-outreach`)
      setOutreachEmail(data.email)
    } catch { toast.error('Failed to generate outreach email') }
    finally { setOutreachLoading(null) }
  }

  const filtered = prospects.filter(p =>
    `${p.fullName} ${p.email} ${p.currentTitle} ${p.skills}`.toLowerCase().includes(search.toLowerCase())
  )

  const stats = {
    total: prospects.length,
    contacted: prospects.filter(p => p.status >= 2).length,
    interested: prospects.filter(p => p.status === 3).length,
    converted: prospects.filter(p => p.status === 5).length,
  }

  return (
    <div className="min-h-full space-y-6 p-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-black text-white">Talent CRM</h1>
          <p className="text-white/40 text-sm mt-0.5">Nurture passive candidates before they apply</p>
        </div>
        <motion.button whileHover={{ scale: 1.03 }} whileTap={{ scale: 0.97 }}
          onClick={() => setShowAdd(true)}
          className="flex items-center gap-2 bg-gradient-to-r from-indigo-600 to-purple-600 text-white font-semibold text-sm px-5 py-2.5 rounded-xl shadow-lg shadow-indigo-500/25">
          <Plus className="w-4 h-4" /> Add Prospect
        </motion.button>
      </div>

      {/* KPI */}
      <div className="grid grid-cols-4 gap-4">
        {[
          { label: 'Total Prospects', value: stats.total, color: 'indigo' },
          { label: 'Contacted', value: stats.contacted, color: 'blue' },
          { label: 'Interested', value: stats.interested, color: 'emerald' },
          { label: 'Converted to Applicants', value: stats.converted, color: 'green' },
        ].map(kpi => (
          <div key={kpi.label} className="bg-white/[0.04] border border-white/[0.08] rounded-2xl p-4">
            <p className="text-3xl font-black text-white mb-1">{kpi.value}</p>
            <p className="text-white/40 text-xs">{kpi.label}</p>
          </div>
        ))}
      </div>

      {/* Search */}
      <div className="relative max-w-md">
        <Search className="absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-white/30" />
        <input value={search} onChange={e => setSearch(e.target.value)}
          placeholder="Search by name, email, skills..."
          className="w-full bg-white/[0.04] border border-white/[0.08] text-white placeholder-white/30 pl-10 pr-4 py-2.5 rounded-xl text-sm outline-none focus:border-indigo-500/50" />
      </div>

      {/* Prospect Cards */}
      {isLoading ? (
        <div className="flex items-center justify-center h-48"><Loader2 className="w-6 h-6 text-indigo-400 animate-spin" /></div>
      ) : (
        <div className="grid grid-cols-1 md:grid-cols-2 xl:grid-cols-3 gap-4">
          {filtered.map(prospect => {
            const sc = SOURCE_CONFIG[prospect.source] ?? SOURCE_CONFIG[7]
            const stc = STATUS_CONFIG[prospect.status] ?? STATUS_CONFIG[1]
            return (
              <motion.div key={prospect.id} layout
                className="bg-white/[0.04] border border-white/[0.08] rounded-2xl p-4 hover:border-indigo-500/20 transition-colors"
              >
                <div className="flex items-start justify-between gap-2 mb-3">
                  <div className="flex-1 min-w-0">
                    <p className="text-white font-semibold truncate">{prospect.fullName}</p>
                    {prospect.currentTitle && <p className="text-white/40 text-xs">{prospect.currentTitle}</p>}
                  </div>
                  <span className={`flex-shrink-0 text-xs px-2 py-0.5 rounded-full border ${sc.color}`}>{sc.label}</span>
                </div>

                <div className="space-y-1 mb-3">
                  <a href={`mailto:${prospect.email}`} className="flex items-center gap-2 text-white/50 text-xs hover:text-indigo-400 transition-colors">
                    <Mail className="w-3 h-3" /> {prospect.email}
                  </a>
                  {prospect.phone && (
                    <p className="flex items-center gap-2 text-white/50 text-xs"><Phone className="w-3 h-3" /> {prospect.phone}</p>
                  )}
                  {prospect.skills && (
                    <p className="flex items-center gap-2 text-white/50 text-xs"><Tag className="w-3 h-3" /> {prospect.skills}</p>
                  )}
                </div>

                <div className="flex items-center justify-between mt-3 pt-3 border-t border-white/[0.06]">
                  <select
                    value={prospect.status}
                    onChange={e => statusMutation.mutate({ id: prospect.id, status: Number(e.target.value) })}
                    className={`text-xs px-2 py-1 rounded-lg border-none outline-none cursor-pointer ${stc.color} bg-transparent`}
                  >
                    {Object.entries(STATUS_CONFIG).map(([k, v]) => (
                      <option key={k} value={k} className="bg-[#0f1629] text-white">{v.label}</option>
                    ))}
                  </select>

                  <motion.button
                    whileHover={{ scale: 1.05 }} whileTap={{ scale: 0.95 }}
                    onClick={() => generateOutreach(prospect)}
                    disabled={outreachLoading === prospect.id}
                    className="flex items-center gap-1.5 bg-indigo-500/10 hover:bg-indigo-500/20 border border-indigo-500/20 text-indigo-400 text-xs font-medium px-3 py-1.5 rounded-lg transition-colors disabled:opacity-50"
                  >
                    {outreachLoading === prospect.id ? <Loader2 className="w-3 h-3 animate-spin" /> : <Sparkles className="w-3 h-3" />}
                    AI Outreach
                  </motion.button>
                </div>
              </motion.div>
            )
          })}

          {filtered.length === 0 && !isLoading && (
            <div className="col-span-3 text-center py-16">
              <UserPlus className="w-10 h-10 text-white/20 mx-auto mb-3" />
              <p className="text-white/30 text-sm">No prospects yet. Add your first passive candidate.</p>
            </div>
          )}
        </div>
      )}

      {/* Add Prospect Modal */}
      {showAdd && (
        <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/60 backdrop-blur-sm">
          <motion.div initial={{ scale: 0.95, y: 20 }} animate={{ scale: 1, y: 0 }}
            className="bg-[#0f1629] border border-white/10 rounded-2xl w-full max-w-md p-6"
          >
            <div className="flex items-center justify-between mb-5">
              <h2 className="text-white font-bold text-lg">Add Prospect</h2>
              <button onClick={() => setShowAdd(false)} className="text-white/40 hover:text-white/70"><X className="w-5 h-5" /></button>
            </div>
            <div className="space-y-3">
              {[
                { key: 'fullName', placeholder: 'Full Name *' },
                { key: 'email', placeholder: 'Email Address *' },
                { key: 'phone', placeholder: 'Phone' },
                { key: 'currentTitle', placeholder: 'Current Job Title' },
                { key: 'linkedInUrl', placeholder: 'LinkedIn URL' },
                { key: 'skills', placeholder: 'Key Skills (comma-separated)' },
              ].map(field => (
                <input key={field.key}
                  value={form[field.key as keyof typeof form]}
                  onChange={e => setForm(f => ({ ...f, [field.key]: e.target.value }))}
                  placeholder={field.placeholder}
                  className="w-full bg-white/5 border border-white/10 text-white placeholder-white/30 px-3 py-2.5 rounded-xl text-sm outline-none focus:border-indigo-500/50 transition-colors" />
              ))}
              <select value={form.source} onChange={e => setForm(f => ({ ...f, source: e.target.value }))}
                className="w-full bg-white/5 border border-white/10 text-white px-3 py-2.5 rounded-xl text-sm outline-none">
                {Object.entries(SOURCE_CONFIG).map(([k, v]) => (
                  <option key={k} value={k} className="bg-[#0f1629]">{v.label}</option>
                ))}
              </select>
            </div>
            <button onClick={() => addMutation.mutate()} disabled={addMutation.isPending || !form.fullName || !form.email}
              className="mt-5 w-full flex items-center justify-center gap-2 bg-gradient-to-r from-indigo-600 to-purple-600 text-white font-semibold py-2.5 rounded-xl disabled:opacity-50">
              {addMutation.isPending ? <Loader2 className="w-4 h-4 animate-spin" /> : <Plus className="w-4 h-4" />}
              Add Prospect
            </button>
          </motion.div>
        </div>
      )}

      {/* AI Outreach Email Modal */}
      {outreachEmail && (
        <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/60 backdrop-blur-sm">
          <motion.div initial={{ scale: 0.95 }} animate={{ scale: 1 }}
            className="bg-[#0f1629] border border-white/10 rounded-2xl w-full max-w-lg p-6"
          >
            <div className="flex items-center justify-between mb-4">
              <div className="flex items-center gap-2">
                <Sparkles className="w-5 h-5 text-indigo-400" />
                <h2 className="text-white font-bold">AI-Generated Outreach Email</h2>
              </div>
              <button onClick={() => setOutreachEmail(null)} className="text-white/40 hover:text-white/70"><X className="w-5 h-5" /></button>
            </div>
            <div className="bg-white/5 border border-white/10 rounded-xl p-4 text-white/70 text-sm whitespace-pre-wrap mb-4 max-h-72 overflow-y-auto">
              {outreachEmail}
            </div>
            <button onClick={() => { navigator.clipboard.writeText(outreachEmail); toast.success('Copied!') }}
              className="w-full flex items-center justify-center gap-2 bg-indigo-500/20 hover:bg-indigo-500/30 border border-indigo-500/30 text-indigo-400 font-semibold py-2.5 rounded-xl transition-colors text-sm">
              <Copy className="w-4 h-4" /> Copy to Clipboard
            </button>
          </motion.div>
        </div>
      )}
    </div>
  )
}
