import { useState } from 'react'
import { useQuery } from '@tanstack/react-query'
import { motion, AnimatePresence } from 'framer-motion'
import { Search, MapPin, Briefcase, Clock, ChevronRight, Building2, Star, ArrowRight } from 'lucide-react'
import { publicApi, type PublicJob } from '../../api/endpoints/public.api'
import JobApplyModal from './JobApplyModal'

const EMPLOYMENT_LABELS: Record<number, string> = {
  1: 'Full-time', 2: 'Part-time', 3: 'Contract', 4: 'Internship', 5: 'Remote'
}

function JobCard({ job, onApply }: { job: PublicJob; onApply: (job: PublicJob) => void }) {
  const posted = new Date(job.createdAtUtc)
  const daysAgo = Math.floor((Date.now() - posted.getTime()) / 86400000)

  return (
    <motion.div
      layout
      initial={{ opacity: 0, y: 20 }}
      animate={{ opacity: 1, y: 0 }}
      className="group relative bg-white rounded-2xl border border-slate-100 p-6 shadow-sm hover:shadow-xl hover:-translate-y-1 transition-all duration-300"
    >
      {/* Company logo placeholder */}
      <div className="flex items-start gap-4 mb-4">
        <div className="w-12 h-12 rounded-xl bg-gradient-to-br from-indigo-500 to-purple-600 flex items-center justify-center text-white font-bold text-lg flex-shrink-0">
          {job.company.name[0]}
        </div>
        <div className="flex-1 min-w-0">
          <p className="text-sm text-slate-500 font-medium">{job.company.name}</p>
          <h3 className="text-lg font-bold text-slate-800 group-hover:text-indigo-600 transition-colors truncate">
            {job.title}
          </h3>
        </div>
      </div>

      <div className="flex flex-wrap gap-2 mb-4">
        {job.location && (
          <span className="inline-flex items-center gap-1 text-xs text-slate-500 bg-slate-100 px-2.5 py-1 rounded-full">
            <MapPin className="w-3 h-3" /> {job.location}
          </span>
        )}
        <span className="inline-flex items-center gap-1 text-xs text-indigo-600 bg-indigo-50 px-2.5 py-1 rounded-full font-medium">
          <Briefcase className="w-3 h-3" /> {EMPLOYMENT_LABELS[job.employmentType] ?? 'Full-time'}
        </span>
        {job.department && (
          <span className="inline-flex items-center gap-1 text-xs text-purple-600 bg-purple-50 px-2.5 py-1 rounded-full font-medium">
            {job.department}
          </span>
        )}
      </div>

      <p className="text-sm text-slate-500 line-clamp-2 mb-5">
        {job.description.replace(/<[^>]*>/g, '')}
      </p>

      {(job.salaryMin || job.salaryMax) && (
        <p className="text-sm font-semibold text-emerald-600 mb-4">
          {job.salaryMin && job.salaryMax
            ? `$${(job.salaryMin / 1000).toFixed(0)}k – $${(job.salaryMax / 1000).toFixed(0)}k`
            : job.salaryMin ? `From $${(job.salaryMin / 1000).toFixed(0)}k`
            : `Up to $${(job.salaryMax! / 1000).toFixed(0)}k`}
        </p>
      )}

      <div className="flex items-center justify-between">
        <span className="text-xs text-slate-400 flex items-center gap-1">
          <Clock className="w-3 h-3" />
          {daysAgo === 0 ? 'Today' : daysAgo === 1 ? 'Yesterday' : `${daysAgo}d ago`}
        </span>
        <motion.button
          whileHover={{ scale: 1.04 }}
          whileTap={{ scale: 0.96 }}
          onClick={() => onApply(job)}
          className="flex items-center gap-1.5 bg-gradient-to-r from-indigo-600 to-purple-600 hover:from-indigo-500 hover:to-purple-500 text-white text-sm font-semibold px-4 py-2 rounded-xl shadow-md shadow-indigo-500/30 transition-all"
        >
          Apply Now <ArrowRight className="w-3.5 h-3.5" />
        </motion.button>
      </div>
    </motion.div>
  )
}

export default function CareerPortalPage() {
  const [keyword, setKeyword] = useState('')
  const [department, setDepartment] = useState('')
  const [page, setPage] = useState(1)
  const [applyJob, setApplyJob] = useState<PublicJob | null>(null)
  const [searchInput, setSearchInput] = useState('')

  const { data, isLoading } = useQuery({
    queryKey: ['public-jobs', keyword, department, page],
    queryFn: () => publicApi.getJobs({ keyword: keyword || undefined, department: department || undefined, page }),
    staleTime: 1000 * 60 * 2,
  })

  const totalPages = data ? Math.ceil(data.total / data.pageSize) : 0

  const handleSearch = (e: React.FormEvent) => {
    e.preventDefault()
    setKeyword(searchInput)
    setPage(1)
  }

  return (
    <div className="min-h-screen bg-slate-50">
      {/* Hero Section */}
      <div className="relative overflow-hidden"
        style={{ background: 'linear-gradient(135deg, #4f46e5 0%, #7c3aed 50%, #9333ea 100%)' }}
      >
        {/* Decorative blobs */}
        <div className="absolute inset-0 overflow-hidden pointer-events-none">
          <div className="absolute -top-24 -right-24 w-96 h-96 bg-white/5 rounded-full blur-3xl" />
          <div className="absolute -bottom-24 -left-24 w-96 h-96 bg-white/5 rounded-full blur-3xl" />
        </div>

        <div className="relative max-w-5xl mx-auto px-6 py-20 text-center">
          <motion.div
            initial={{ opacity: 0, y: -20 }}
            animate={{ opacity: 1, y: 0 }}
            transition={{ duration: 0.6 }}
          >
            <div className="inline-flex items-center gap-2 bg-white/10 backdrop-blur-sm border border-white/20 px-4 py-1.5 rounded-full text-white/80 text-sm font-medium mb-6">
              <Star className="w-3.5 h-3.5 text-yellow-400" />
              We're hiring — join our world-class team
            </div>
            <h1 className="text-5xl md:text-6xl font-black text-white mb-4 leading-tight">
              Build Your Career<br />
              <span className="text-transparent bg-clip-text" style={{ backgroundImage: 'linear-gradient(to right, #a5b4fc, #f0abfc)' }}>
                With HireIQ
              </span>
            </h1>
            <p className="text-white/70 text-xl max-w-2xl mx-auto mb-10">
              Discover exciting opportunities and join a team that values innovation, growth, and impact.
            </p>

            {/* Search */}
            <form onSubmit={handleSearch}
              className="flex flex-col sm:flex-row gap-3 max-w-2xl mx-auto"
            >
              <div className="flex-1 relative">
                <Search className="absolute left-4 top-1/2 -translate-y-1/2 w-4 h-4 text-slate-400" />
                <input
                  value={searchInput}
                  onChange={e => setSearchInput(e.target.value)}
                  placeholder="Job title, skills, or keywords..."
                  className="w-full pl-11 pr-4 py-3.5 rounded-xl bg-white text-slate-800 placeholder-slate-400 text-sm font-medium shadow-lg outline-none focus:ring-2 focus:ring-indigo-300"
                />
              </div>
              <button
                type="submit"
                className="bg-white/10 border border-white/30 hover:bg-white/20 text-white font-semibold px-6 py-3.5 rounded-xl transition-all backdrop-blur-sm"
              >
                Search Jobs
              </button>
            </form>
          </motion.div>

          {/* Stats bar */}
          <motion.div
            initial={{ opacity: 0, y: 20 }}
            animate={{ opacity: 1, y: 0 }}
            transition={{ delay: 0.3 }}
            className="flex justify-center gap-8 mt-12"
          >
            {[
              { label: 'Open Positions', value: data?.total ?? '...' },
              { label: 'Departments', value: '12+' },
              { label: 'Countries', value: '8' },
            ].map(stat => (
              <div key={stat.label} className="text-center">
                <div className="text-2xl font-black text-white">{stat.value}</div>
                <div className="text-white/60 text-xs font-medium">{stat.label}</div>
              </div>
            ))}
          </motion.div>
        </div>
      </div>

      {/* Filter bar */}
      <div className="bg-white border-b border-slate-100 sticky top-0 z-10 shadow-sm">
        <div className="max-w-5xl mx-auto px-6 py-3 flex items-center gap-3 overflow-x-auto">
          {['', 'Engineering', 'Design', 'Marketing', 'Sales', 'HR', 'Finance'].map(dept => (
            <button
              key={dept}
              onClick={() => { setDepartment(dept); setPage(1) }}
              className={`flex-shrink-0 px-4 py-1.5 rounded-full text-sm font-medium transition-all ${
                department === dept
                  ? 'bg-indigo-600 text-white shadow-md shadow-indigo-500/30'
                  : 'bg-slate-100 text-slate-600 hover:bg-indigo-50 hover:text-indigo-600'
              }`}
            >
              {dept || 'All Roles'}
            </button>
          ))}
        </div>
      </div>

      {/* Job listings */}
      <div className="max-w-5xl mx-auto px-6 py-10">
        {isLoading ? (
          <div className="grid grid-cols-1 md:grid-cols-2 gap-5">
            {[...Array(6)].map((_, i) => (
              <div key={i} className="bg-white rounded-2xl p-6 h-52 animate-pulse">
                <div className="flex gap-4 mb-4">
                  <div className="w-12 h-12 bg-slate-200 rounded-xl" />
                  <div className="flex-1 space-y-2">
                    <div className="h-3 bg-slate-200 rounded w-24" />
                    <div className="h-5 bg-slate-200 rounded w-48" />
                  </div>
                </div>
                <div className="space-y-2">
                  <div className="h-3 bg-slate-200 rounded w-full" />
                  <div className="h-3 bg-slate-200 rounded w-3/4" />
                </div>
              </div>
            ))}
          </div>
        ) : data?.jobs.length === 0 ? (
          <div className="text-center py-20">
            <Building2 className="w-12 h-12 text-slate-300 mx-auto mb-4" />
            <p className="text-slate-500 font-medium">No open positions found. Check back soon!</p>
          </div>
        ) : (
          <>
            <div className="flex items-center justify-between mb-6">
              <p className="text-slate-500 text-sm">
                <span className="font-bold text-slate-800">{data?.total}</span> open positions
                {keyword && <span> for "<span className="text-indigo-600 font-semibold">{keyword}</span>"</span>}
              </p>
            </div>

            <div className="grid grid-cols-1 md:grid-cols-2 gap-5">
              <AnimatePresence>
                {data?.jobs.map(job => (
                  <JobCard key={job.id} job={job} onApply={setApplyJob} />
                ))}
              </AnimatePresence>
            </div>

            {/* Pagination */}
            {totalPages > 1 && (
              <div className="flex justify-center gap-2 mt-10">
                {[...Array(totalPages)].map((_, i) => (
                  <button
                    key={i}
                    onClick={() => setPage(i + 1)}
                    className={`w-9 h-9 rounded-lg text-sm font-semibold transition-all ${
                      page === i + 1
                        ? 'bg-indigo-600 text-white shadow-md'
                        : 'bg-white text-slate-600 border border-slate-200 hover:border-indigo-300'
                    }`}
                  >
                    {i + 1}
                  </button>
                ))}
              </div>
            )}
          </>
        )}
      </div>

      {/* Apply modal */}
      <AnimatePresence>
        {applyJob && (
          <JobApplyModal job={applyJob} onClose={() => setApplyJob(null)} />
        )}
      </AnimatePresence>
    </div>
  )
}
