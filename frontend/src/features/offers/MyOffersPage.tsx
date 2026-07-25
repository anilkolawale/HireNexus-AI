import { useEffect, useState } from 'react'
import toast from 'react-hot-toast'
import { motion, AnimatePresence } from 'framer-motion'
import { Gift, Plus, Calendar, DollarSign, CheckCircle2, XCircle, Clock, Send, User, Loader2 } from 'lucide-react'
import { useAppSelector } from '../../app/hooks'
import { offersApi, type Offer } from '../../api/endpoints/offers.api'
import { applicationsApi } from '../../api/endpoints/applications.api'

export default function MyOffersPage() {
  const currentUser = useAppSelector((s) => s.auth.user)
  const isStaff = ['Recruiter', 'HRManager', 'SuperAdmin'].includes(currentUser?.role || '')

  const [offers, setOffers] = useState<Offer[]>([])
  const [loading, setLoading] = useState(true)
  const [responding, setResponding] = useState<string | null>(null)
  
  // Create Offer Modal
  const [showCreateModal, setShowCreateModal] = useState(false)
  const [applications, setApplications] = useState<any[]>([])
  const [loadingApps, setLoadingApps] = useState(false)
  const [selectedAppId, setSelectedAppId] = useState('')
  const [offeredSalary, setOfferedSalary] = useState(120000)
  const [joiningDate, setJoiningDate] = useState('')
  const [notes, setNotes] = useState('')
  const [creating, setCreating] = useState(false)


  const load = () => {
    setLoading(true)
    offersApi.getMyOffers().then(setOffers).catch(() => setOffers([])).finally(() => setLoading(false))
  }

  useEffect(() => {
    load()
  }, [])

  const openCreateModal = async () => {
    setShowCreateModal(true)
    setLoadingApps(true)
    try {
      const data = await applicationsApi.getAllPipeline()
      setApplications(data as any[])
    } catch {
      setApplications([])
    } finally {
      setLoadingApps(false)
    }
  }

  const handleCreateOffer = async () => {
    if (!selectedAppId || !joiningDate) {
      toast.error('Pick a candidate and joining date')
      return
    }
    setCreating(true)
    try {
      await offersApi.create(selectedAppId, offeredSalary, new Date(joiningDate).toISOString(), notes)
      toast.success('🎉 Offer letter created and sent to candidate!')
      setShowCreateModal(false)
      setSelectedAppId('')
      setNotes('')
      load()
    } catch (err: any) {
      toast.error(err.response?.data?.message || 'Could not create offer letter')
    } finally {
      setCreating(false)
    }
  }

  const respond = async (offerId: string, accept: boolean) => {
    setResponding(offerId)
    try {
      await offersApi.respond(offerId, accept)
      toast.success(accept ? '🎉 Offer accepted — candidate marked Hired!' : 'Offer declined')
      load()
    } catch (err: any) {
      toast.error(err.response?.data?.message || 'Could not submit your response')
    } finally {
      setResponding(null)
    }
  }

  return (
    <div className="min-h-full space-y-6 p-6">
      {/* Header */}
      <div className="flex items-center justify-between flex-wrap gap-4">
        <div>
          <h1 className="text-2xl font-bold text-white tracking-tight flex items-center gap-2">
            <Gift className="w-6 h-6 text-emerald-400" />
            Offer Letters & E-Signatures
          </h1>
          <p className="text-white/40 text-sm mt-1">
            {offers.length} active offer{offers.length !== 1 ? 's' : ''} in recruitment pipeline
          </p>
        </div>

        {isStaff && (
          <button
            onClick={openCreateModal}
            className="flex items-center gap-2 px-4 py-2.5 bg-gradient-to-r from-emerald-600 to-teal-600 text-white rounded-xl text-sm font-medium hover:from-emerald-500 hover:to-teal-500 transition-all shadow-lg shadow-emerald-500/20"
          >
            <Plus className="w-4 h-4" /> Create Offer Letter
          </button>
        )}
      </div>


      {loading ? (
        <div className="flex justify-center py-12">
          <div className="w-7 h-7 rounded-full border-2 border-emerald-500/30 border-t-emerald-500 animate-spin" />
        </div>
      ) : offers.length === 0 ? (
        <div className="text-center py-16 border border-dashed border-white/10 rounded-2xl">
          <Gift className="w-12 h-12 text-white/20 mx-auto mb-3" />
          <p className="text-white/50 font-semibold">No offer letters extended yet</p>
          <p className="text-white/30 text-sm mt-1 mb-4">
            {isStaff ? 'Extend official offer letters with salary & joining terms' : 'You will see offer letters here once an employer extends an offer to you.'}
          </p>
          {isStaff && (
            <button
              onClick={openCreateModal}
              className="px-4 py-2 bg-emerald-600 hover:bg-emerald-500 text-white rounded-xl text-sm font-medium transition-all"
            >
              + Create First Offer
            </button>
          )}

        </div>
      ) : (
        <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
          {offers.map((offer) => (
            <motion.div
              key={offer.id}
              initial={{ opacity: 0, y: 8 }}
              animate={{ opacity: 1, y: 0 }}
              className="p-5 rounded-2xl border border-white/8 hover:border-emerald-500/30 transition-all space-y-4"
              style={{ background: 'linear-gradient(135deg, rgba(255,255,255,0.04) 0%, rgba(255,255,255,0.02) 100%)' }}
            >
              <div className="flex items-start justify-between gap-3">
                <div>
                  <h3 className="font-bold text-white text-lg">{offer.jobTitle}</h3>
                  <p className="text-sm text-emerald-400 font-medium flex items-center gap-1.5 mt-0.5">
                    <User className="w-3.5 h-3.5" /> {offer.candidateName || 'Candidate'}
                  </p>
                </div>
                {offer.respondedAtUtc ? (
                  <span className={`flex items-center gap-1 text-xs font-semibold px-3 py-1 rounded-full border ${
                    offer.isAccepted
                      ? 'bg-emerald-500/20 text-emerald-400 border-emerald-500/30'
                      : 'bg-red-500/20 text-red-400 border-red-500/30'
                  }`}>
                    {offer.isAccepted ? <><CheckCircle2 className="w-3.5 h-3.5" /> Accepted</> : <><XCircle className="w-3.5 h-3.5" /> Declined</>}
                  </span>
                ) : (
                  <span className="flex items-center gap-1 text-xs font-semibold px-3 py-1 rounded-full bg-amber-500/20 text-amber-400 border border-amber-500/30">
                    <Clock className="w-3.5 h-3.5" /> Pending Response
                  </span>
                )}
              </div>

              <div className="grid grid-cols-2 gap-3 text-xs bg-white/5 border border-white/8 rounded-xl p-3">
                <div>
                  <span className="text-white/40 block">Offered Salary</span>
                  <span className="text-white font-bold text-sm flex items-center gap-1">
                    <DollarSign className="w-3.5 h-3.5 text-emerald-400" /> ${offer.offeredSalary.toLocaleString()}/yr
                  </span>
                </div>
                <div>
                  <span className="text-white/40 block">Target Joining Date</span>
                  <span className="text-white font-semibold flex items-center gap-1">
                    <Calendar className="w-3.5 h-3.5 text-indigo-400" /> {new Date(offer.joiningDate).toLocaleDateString()}
                  </span>
                </div>
              </div>

              {offer.notes && (
                <p className="text-xs text-white/40 italic bg-white/[0.02] p-2.5 rounded-lg border border-white/5">
                  "{offer.notes}"
                </p>
              )}

              {!offer.respondedAtUtc && (
                <div className="flex gap-2 pt-1">
                  <button
                    onClick={() => respond(offer.id, true)}
                    disabled={responding === offer.id}
                    className="flex-1 py-2 bg-gradient-to-r from-emerald-600 to-emerald-500 text-white rounded-xl text-xs font-semibold hover:from-emerald-500 hover:to-emerald-400 transition-all disabled:opacity-50 flex items-center justify-center gap-1"
                  >
                    {responding === offer.id ? <Loader2 className="w-3.5 h-3.5 animate-spin" /> : <CheckCircle2 className="w-3.5 h-3.5" />}
                    Accept Offer
                  </button>
                  <button
                    onClick={() => respond(offer.id, false)}
                    disabled={responding === offer.id}
                    className="px-4 py-2 border border-white/10 hover:border-red-500/30 text-white/50 hover:text-red-400 rounded-xl text-xs font-semibold transition-all disabled:opacity-50 flex items-center justify-center gap-1"
                  >
                    <XCircle className="w-3.5 h-3.5" /> Decline
                  </button>
                </div>
              )}
            </motion.div>
          ))}
        </div>
      )}

      {/* Create Offer Modal */}
      <AnimatePresence>
        {showCreateModal && (
          <motion.div
            initial={{ opacity: 0 }}
            animate={{ opacity: 1 }}
            exit={{ opacity: 0 }}
            className="fixed inset-0 bg-black/70 backdrop-blur-sm flex items-center justify-center z-50 p-4"
          >
            <motion.div
              initial={{ scale: 0.95, opacity: 0 }}
              animate={{ scale: 1, opacity: 1 }}
              exit={{ scale: 0.95, opacity: 0 }}
              className="bg-slate-900 border border-slate-700 rounded-2xl w-full max-w-md shadow-2xl p-6 space-y-4"
            >
              <h2 className="text-lg font-bold text-white flex items-center gap-2">
                <Gift className="w-5 h-5 text-emerald-400" /> Create Official Offer Letter
              </h2>

              <div>
                <label className="text-xs text-white/40 block mb-1.5">Select Candidate *</label>
                {loadingApps ? (
                  <div className="flex items-center gap-2 text-xs text-white/40 py-2">
                    <Loader2 className="w-3.5 h-3.5 animate-spin text-emerald-400" /> Loading applicants...
                  </div>
                ) : applications.length === 0 ? (
                  <div className="text-xs text-amber-400 bg-amber-500/10 border border-amber-500/20 rounded-xl p-3">
                    No active applications found in pipeline.
                  </div>
                ) : (
                  <select
                    className="w-full bg-slate-800 border border-slate-700 rounded-xl px-4 py-2.5 text-white text-sm focus:outline-none focus:border-emerald-500"
                    value={selectedAppId}
                    onChange={e => setSelectedAppId(e.target.value)}
                  >
                    <option value="">-- Choose Candidate --</option>
                    {applications.map(app => (
                      <option key={app.id} value={app.id}>
                        👤 {app.candidateName || 'Candidate'} — {app.jobTitle} ({app.status})
                      </option>
                    ))}
                  </select>
                )}
              </div>

              <div>
                <label className="text-xs text-white/40 block mb-1.5">Offered Salary ($/year) *</label>
                <input
                  type="number"
                  step={5000}
                  value={offeredSalary}
                  onChange={e => setOfferedSalary(+e.target.value)}
                  className="w-full bg-slate-800 border border-slate-700 rounded-xl px-4 py-2.5 text-white text-sm focus:outline-none focus:border-emerald-500"
                />
              </div>

              <div>
                <label className="text-xs text-white/40 block mb-1.5">Joining Date *</label>
                <input
                  type="date"
                  value={joiningDate}
                  onChange={e => setJoiningDate(e.target.value)}
                  className="w-full bg-slate-800 border border-slate-700 rounded-xl px-4 py-2.5 text-white text-sm focus:outline-none focus:border-emerald-500"
                />
              </div>

              <div>
                <label className="text-xs text-white/40 block mb-1.5">Notes / Equity & Signing Bonus</label>
                <textarea
                  rows={2}
                  value={notes}
                  onChange={e => setNotes(e.target.value)}
                  placeholder="e.g. Includes $10k signing bonus + standard equity option grant."
                  className="w-full bg-slate-800 border border-slate-700 rounded-xl px-4 py-2.5 text-white text-sm focus:outline-none focus:border-emerald-500"
                />
              </div>

              <div className="flex justify-end gap-3 pt-2">
                <button onClick={() => setShowCreateModal(false)} className="px-4 py-2 text-sm text-slate-400 hover:text-white transition-colors">Cancel</button>
                <button
                  onClick={handleCreateOffer}
                  disabled={creating || !selectedAppId || !joiningDate}
                  className="flex items-center gap-2 px-5 py-2.5 bg-gradient-to-r from-emerald-600 to-teal-600 text-white rounded-xl text-sm font-medium hover:from-emerald-500 hover:to-teal-500 disabled:opacity-50 transition-all shadow-lg shadow-emerald-500/20"
                >
                  {creating ? <><Loader2 className="w-4 h-4 animate-spin" /> Creating...</> : <><Send className="w-4 h-4" /> Send Offer Letter</>}
                </button>
              </div>
            </motion.div>
          </motion.div>
        )}
      </AnimatePresence>
    </div>
  )
}

