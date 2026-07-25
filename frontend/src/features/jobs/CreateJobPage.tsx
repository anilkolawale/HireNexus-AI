import { useState, useEffect } from 'react'
import { useNavigate } from 'react-router-dom'
import { useMutation, useQuery } from '@tanstack/react-query'
import { motion, AnimatePresence } from 'framer-motion'
import toast from 'react-hot-toast'
import {
  Sparkles, Copy, ChevronDown, MapPin, Briefcase,
  DollarSign, Users, Calendar, X, Plus, Loader2,
  CheckCircle, ArrowLeft
} from 'lucide-react'
import { jobsApi } from '../../api/endpoints/jobs.api'
import { companiesApi } from '../../api/endpoints/companies.api'
import { usersApi } from '../../api/endpoints/users.api'
import { useAppSelector } from '../../app/hooks'

const EMPLOYMENT_TYPES = ['FullTime', 'PartTime', 'Contract', 'Internship', 'Remote'] as const
type EmploymentTypeOption = typeof EMPLOYMENT_TYPES[number]

const EMP_LABELS: Record<EmploymentTypeOption, string> = {
  FullTime: 'Full Time',
  PartTime: 'Part Time',
  Contract: 'Contract',
  Internship: 'Internship',
  Remote: 'Remote',
}

const EXPERIENCE_LEVELS = [
  '< 1 year', '1-2 years', '2-3 years', '3-5 years', '5-8 years', '8+ years'
]

interface FormErrors {
  title?: string
  companyId?: string
  departmentId?: string
  location?: string
  skills?: string
  description?: string
  salaryRange?: string
}

function FormField({
  label, required, error, children
}: { label: string; required?: boolean; error?: string; children: React.ReactNode }) {
  return (
    <div className="space-y-1.5">
      <label className="text-xs font-semibold text-white/60 uppercase tracking-wider flex items-center gap-1">
        {label} {required && <span className="text-red-400">*</span>}
      </label>
      {children}
      {error && (
        <motion.p initial={{ opacity: 0, y: -4 }} animate={{ opacity: 1, y: 0 }}
          className="text-xs text-red-400 flex items-center gap-1">
          <X className="w-3 h-3" /> {error}
        </motion.p>
      )}
    </div>
  )
}

const inputCls = "w-full bg-white/5 border border-white/10 rounded-xl px-4 py-2.5 text-sm text-white placeholder-white/30 focus:outline-none focus:border-indigo-500/50 focus:ring-1 focus:ring-indigo-500/30 transition-all"
const selectCls = `${inputCls} appearance-none cursor-pointer`

export default function CreateJobPage() {
  const navigate = useNavigate()
  const user = useAppSelector((s) => s.auth.user)

  // Form state
  const [title, setTitle] = useState('')
  const [companyId, setCompanyId] = useState('')
  const [departmentId, setDepartmentId] = useState('')
  const [hiringManagerId, setHiringManagerId] = useState('')
  const [experienceRequired, setExperienceRequired] = useState('3-5 years')
  const [salaryMin, setSalaryMin] = useState(80000)
  const [salaryMax, setSalaryMax] = useState(120000)
  const [location, setLocation] = useState('')
  const [employmentType, setEmploymentType] = useState<EmploymentTypeOption>('FullTime')
  const [isRemote, setIsRemote] = useState(false)
  const [totalPositions, setTotalPositions] = useState(1)
  const [skills, setSkills] = useState<string[]>([])
  const [skillInput, setSkillInput] = useState('')
  const [description, setDescription] = useState('')
  const [responsibilities, setResponsibilities] = useState('')
  const [benefits, setBenefits] = useState('')
  const [closingDate, setClosingDate] = useState('')
  const [errors, setErrors] = useState<FormErrors>({})

  // AI panel state
  const [aiTitle, setAiTitle] = useState('')
  const [aiDepartment, setAiDepartment] = useState('')
  const [aiExperience, setAiExperience] = useState('3-5 years')
  const [aiSkills, setAiSkills] = useState('')
  const [generatedDesc, setGeneratedDesc] = useState('')

  // Data queries
  const { data: companies = [] } = useQuery({
    queryKey: ['companies'],
    queryFn: () => companiesApi.getAll(),
  })

  const { data: departments = [] } = useQuery({
    queryKey: ['departments', companyId],
    queryFn: () => companiesApi.getDepartments(companyId),
    enabled: !!companyId,
  })

  const { data: hiringManagers = [] } = useQuery({
    queryKey: ['hiringManagers'],
    queryFn: () => usersApi.getByRole('HRManager'),
  })

  // Auto-select: prefer the logged-in user's own company, fallback to first if only 1
  useEffect(() => {
    if (companies.length === 0) return
    const userCompany = user?.companyId
      ? companies.find((c: { id: string }) => c.id === user.companyId)
      : null
    if (userCompany) {
      setCompanyId(userCompany.id)
    } else if (companies.length === 1) {
      setCompanyId(companies[0].id)
    }
  }, [companies])

  // Clear department when company changes
  useEffect(() => {
    setDepartmentId('')
  }, [companyId])

  // AI generate mutation
  const generateMutation = useMutation({
    mutationFn: () =>
      jobsApi.generateDescription(aiTitle, aiDepartment, aiExperience, aiSkills),
    onSuccess: (data) => {
      setGeneratedDesc(data.description)
      toast.success('AI description generated!')
    },
    onError: () => toast.error('Could not generate description'),
  })

  // Submit mutation
  const createMutation = useMutation({
    mutationFn: () =>
      jobsApi.createJob({
        title,
        description,
        responsibilities,
        benefits,
        companyId,
        departmentId,
        hiringManagerId: hiringManagerId || user!.id,
        createdByRecruiterId: user!.id,
        experienceRequired,
        salaryMin,
        salaryMax,
        location: isRemote ? `${location} (Remote)` : location,
        employmentType,
        skills,
      }),
    onSuccess: () => {
      toast.success('Job created as draft!')
      navigate('/jobs')
    },
    onError: (err: any) => {
      const serverMsg = err.response?.data?.message
      const valErrors = err.response?.data?.errors
        ? Object.values(err.response.data.errors).flat().join(' | ')
        : null
      toast.error(serverMsg || valErrors || 'Could not create job. Please check all fields.')
    },
  })

  const validate = (): boolean => {
    const e: FormErrors = {}
    if (!title) e.title = 'Title is required'
    if (!companyId) e.companyId = 'Company is required'
    if (!departmentId) e.departmentId = 'Department is required'
    if (!location) e.location = 'Location is required'
    if (skills.length === 0) e.skills = 'Add at least one skill'
    if (!description) e.description = 'Description is required'
    if (salaryMin >= salaryMax) e.salaryRange = 'Min salary must be less than max'
    setErrors(e)
    return Object.keys(e).length === 0
  }

  const handleSubmit = () => {
    if (validate()) createMutation.mutate()
  }

  const addSkill = (s: string) => {
    const trimmed = s.trim()
    if (trimmed && !skills.includes(trimmed)) setSkills([...skills, trimmed])
    setSkillInput('')
  }

  const removeSkill = (s: string) => setSkills(skills.filter((sk) => sk !== s))

  const copyToForm = () => {
    setDescription(generatedDesc)
    setTitle(aiTitle || title)
    toast.success('Copied to form!')
  }

  return (
    <div className="min-h-full">
      {/* Header */}
      <div className="mb-8 flex items-center gap-4">
        <button
          onClick={() => navigate('/jobs')}
          className="w-9 h-9 rounded-xl bg-white/5 hover:bg-white/10 flex items-center justify-center transition-colors"
        >
          <ArrowLeft className="w-4 h-4 text-white/60" />
        </button>
        <div>
          <h1 className="text-2xl font-bold text-white tracking-tight">Create Job Posting</h1>
          <p className="text-white/40 text-sm mt-0.5">Fill in the details or use AI to generate content</p>
        </div>
      </div>

      {/* Two-column layout */}
      <div className="flex gap-6 items-start">
        {/* LEFT: Form (60%) */}
        <div className="flex-[3] min-w-0 space-y-6">
          <div className="rounded-2xl border border-white/5 bg-white/[0.04] backdrop-blur-sm p-6 space-y-5">
            <h2 className="text-sm font-semibold text-white/60 uppercase tracking-wider border-b border-white/5 pb-3">
              Basic Information
            </h2>

            <FormField label="Job Title" required error={errors.title}>
              <input
                value={title}
                onChange={(e) => setTitle(e.target.value)}
                placeholder="e.g. Senior Frontend Engineer"
                className={inputCls}
              />
            </FormField>

            <div className="grid grid-cols-2 gap-4">
              <FormField label="Company" required error={errors.companyId}>
                <div className="relative">
                  <select value={companyId} onChange={(e) => setCompanyId(e.target.value)} className={selectCls}>
                    <option value="" className="bg-[#0f1829]">Select company</option>
                    {companies.map((c) => <option key={c.id} value={c.id} className="bg-[#0f1829]">{c.name}</option>)}
                  </select>
                  <ChevronDown className="absolute right-3 top-1/2 -translate-y-1/2 w-4 h-4 text-white/30 pointer-events-none" />
                </div>
              </FormField>

              <FormField label="Department" required error={errors.departmentId}>
                <div className="relative">
                  <select value={departmentId} onChange={(e) => setDepartmentId(e.target.value)} className={selectCls} disabled={!companyId}>
                    <option value="" className="bg-[#0f1829]">Select department</option>
                    {departments.map((d) => <option key={d.id} value={d.id} className="bg-[#0f1829]">{d.name}</option>)}
                  </select>
                  <ChevronDown className="absolute right-3 top-1/2 -translate-y-1/2 w-4 h-4 text-white/30 pointer-events-none" />
                </div>
              </FormField>
            </div>

            <div className="grid grid-cols-2 gap-4">
              <FormField label="Location" required error={errors.location}>
                <div className="relative">
                  <MapPin className="absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-white/30" />
                  <input value={location} onChange={(e) => setLocation(e.target.value)} placeholder="City, Country" className={`${inputCls} pl-9`} />
                </div>
              </FormField>

              <FormField label="Experience Level">
                <div className="relative">
                  <select value={experienceRequired} onChange={(e) => setExperienceRequired(e.target.value)} className={selectCls}>
                    {EXPERIENCE_LEVELS.map((l) => <option key={l} value={l} className="bg-[#0f1829]">{l}</option>)}
                  </select>
                  <ChevronDown className="absolute right-3 top-1/2 -translate-y-1/2 w-4 h-4 text-white/30 pointer-events-none" />
                </div>
              </FormField>
            </div>

            {/* Employment type radio cards */}
            <FormField label="Employment Type">
              <div className="flex flex-wrap gap-2">
                {EMPLOYMENT_TYPES.map((t) => (
                  <button
                    key={t}
                    type="button"
                    onClick={() => setEmploymentType(t)}
                    className={`px-3 py-1.5 rounded-xl text-xs font-semibold border transition-all duration-200 ${
                      employmentType === t
                        ? 'bg-indigo-600 border-indigo-500 text-white shadow-lg shadow-indigo-500/25'
                        : 'bg-white/5 border-white/10 text-white/50 hover:border-white/20 hover:text-white/70'
                    }`}
                  >
                    {EMP_LABELS[t]}
                  </button>
                ))}
              </div>
            </FormField>

            {/* Remote toggle */}
            <div className="flex items-center justify-between py-2">
              <div>
                <p className="text-sm font-medium text-white/80">Remote Work</p>
                <p className="text-xs text-white/40">Allow fully remote work for this role</p>
              </div>
              <button
                type="button"
                onClick={() => setIsRemote(!isRemote)}
                className={`relative w-11 h-6 rounded-full transition-all duration-300 ${isRemote ? 'bg-indigo-600' : 'bg-white/10'}`}
              >
                <motion.div
                  animate={{ x: isRemote ? 20 : 2 }}
                  transition={{ type: 'spring', stiffness: 500, damping: 30 }}
                  className="absolute top-1 w-4 h-4 rounded-full bg-white shadow-sm"
                />
              </button>
            </div>
          </div>

          {/* Compensation & Details */}
          <div className="rounded-2xl border border-white/5 bg-white/[0.04] backdrop-blur-sm p-6 space-y-5">
            <h2 className="text-sm font-semibold text-white/60 uppercase tracking-wider border-b border-white/5 pb-3">
              Compensation &amp; Details
            </h2>

            <FormField label="Salary Range" error={errors.salaryRange}>
              <div className="flex items-center gap-3">
                <div className="relative flex-1">
                  <DollarSign className="absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-white/30" />
                  <input type="number" value={salaryMin} onChange={(e) => setSalaryMin(+e.target.value)}
                    placeholder="Min" className={`${inputCls} pl-9`} />
                </div>
                <span className="text-white/20">—</span>
                <div className="relative flex-1">
                  <DollarSign className="absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-white/30" />
                  <input type="number" value={salaryMax} onChange={(e) => setSalaryMax(+e.target.value)}
                    placeholder="Max" className={`${inputCls} pl-9`} />
                </div>
              </div>
            </FormField>

            <div className="grid grid-cols-2 gap-4">
              <FormField label="Total Positions">
                <div className="relative">
                  <Users className="absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-white/30" />
                  <input type="number" value={totalPositions} min={1}
                    onChange={(e) => setTotalPositions(+e.target.value)}
                    className={`${inputCls} pl-9`} />
                </div>
              </FormField>

              <FormField label="Closing Date">
                <div className="relative">
                  <Calendar className="absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-white/30" />
                  <input type="date" value={closingDate} onChange={(e) => setClosingDate(e.target.value)}
                    className={`${inputCls} pl-9 [color-scheme:dark]`} />
                </div>
              </FormField>
            </div>

            {hiringManagers.length > 0 && (
              <FormField label="Hiring Manager">
                <div className="relative">
                  <select value={hiringManagerId} onChange={(e) => setHiringManagerId(e.target.value)} className={selectCls}>
                    <option value="" className="bg-[#0f1829]">Defaults to you</option>
                    {hiringManagers.map((m) => <option key={m.id} value={m.id} className="bg-[#0f1829]">{m.fullName}</option>)}
                  </select>
                  <ChevronDown className="absolute right-3 top-1/2 -translate-y-1/2 w-4 h-4 text-white/30 pointer-events-none" />
                </div>
              </FormField>
            )}
          </div>

          {/* Skills & Content */}
          <div className="rounded-2xl border border-white/5 bg-white/[0.04] backdrop-blur-sm p-6 space-y-5">
            <h2 className="text-sm font-semibold text-white/60 uppercase tracking-wider border-b border-white/5 pb-3">
              Skills &amp; Content
            </h2>

            <FormField label="Required Skills" required error={errors.skills}>
              <div className="space-y-2">
                <div className="flex gap-2">
                  <input
                    value={skillInput}
                    onChange={(e) => setSkillInput(e.target.value)}
                    onKeyDown={(e) => {
                      if (e.key === 'Enter' || e.key === ',') { e.preventDefault(); addSkill(skillInput) }
                    }}
                    placeholder="Type a skill and press Enter"
                    className={inputCls}
                  />
                  <button
                    type="button"
                    onClick={() => addSkill(skillInput)}
                    className="shrink-0 w-10 h-10 rounded-xl bg-indigo-600 hover:bg-indigo-500 flex items-center justify-center transition-colors"
                  >
                    <Plus className="w-4 h-4 text-white" />
                  </button>
                </div>
                {skills.length > 0 && (
                  <div className="flex flex-wrap gap-2">
                    <AnimatePresence>
                      {skills.map((s) => (
                        <motion.span
                          key={s}
                          initial={{ opacity: 0, scale: 0.8 }}
                          animate={{ opacity: 1, scale: 1 }}
                          exit={{ opacity: 0, scale: 0.8 }}
                          className="flex items-center gap-1 text-xs bg-indigo-500/20 text-indigo-300 border border-indigo-500/30 rounded-lg px-2.5 py-1"
                        >
                          {s}
                          <button onClick={() => removeSkill(s)} className="hover:text-white ml-0.5">
                            <X className="w-3 h-3" />
                          </button>
                        </motion.span>
                      ))}
                    </AnimatePresence>
                  </div>
                )}
              </div>
            </FormField>

            <FormField label="Job Description" required error={errors.description}>
              <textarea
                value={description}
                onChange={(e) => setDescription(e.target.value)}
                rows={6}
                placeholder="Describe the role — or use the AI Generator on the right →"
                className={`${inputCls} resize-none`}
              />
            </FormField>

            <FormField label="Responsibilities">
              <textarea
                value={responsibilities}
                onChange={(e) => setResponsibilities(e.target.value)}
                rows={4}
                placeholder="Key responsibilities and duties…"
                className={`${inputCls} resize-none`}
              />
            </FormField>

            <FormField label="Benefits">
              <textarea
                value={benefits}
                onChange={(e) => setBenefits(e.target.value)}
                rows={3}
                placeholder="Health insurance, equity, remote work…"
                className={`${inputCls} resize-none`}
              />
            </FormField>
          </div>

          {/* Submit */}
          <div className="flex items-center gap-4 pb-8">
            <motion.button
              whileHover={{ scale: 1.02 }}
              whileTap={{ scale: 0.98 }}
              onClick={handleSubmit}
              disabled={createMutation.isPending}
              className="flex items-center gap-2 bg-gradient-to-r from-indigo-600 to-indigo-500 hover:from-indigo-500 hover:to-indigo-400 disabled:opacity-50 text-white px-8 py-3 rounded-xl font-semibold text-sm shadow-lg shadow-indigo-500/25 transition-all"
            >
              {createMutation.isPending ? (
                <><Loader2 className="w-4 h-4 animate-spin" /> Creating…</>
              ) : (
                <><CheckCircle className="w-4 h-4" /> Create Job</>
              )}
            </motion.button>
            <button
              onClick={() => navigate('/jobs')}
              className="px-6 py-3 rounded-xl text-sm text-white/50 hover:text-white/80 hover:bg-white/5 transition-all"
            >
              Cancel
            </button>
          </div>
        </div>

        {/* RIGHT: AI Assistant Panel (40%) */}
        <div className="flex-[2] min-w-[320px] sticky top-6">
          <div className="rounded-2xl border border-indigo-500/20 bg-gradient-to-br from-indigo-950/60 to-purple-950/40 backdrop-blur-sm overflow-hidden">
            {/* Panel header */}
            <div className="p-5 border-b border-indigo-500/20 bg-gradient-to-r from-indigo-600/10 to-purple-600/10">
              <div className="flex items-center gap-2 mb-1">
                <Sparkles className="w-5 h-5 text-indigo-400" />
                <h3 className="font-bold text-white text-sm">AI Job Description Generator</h3>
              </div>
              <span className="text-[10px] bg-gradient-to-r from-indigo-500 to-purple-500 text-white px-2.5 py-0.5 rounded-full font-semibold">
                ✨ Powered by Gemini
              </span>
            </div>

            <div className="p-5 space-y-4">
              <p className="text-xs text-white/40 leading-relaxed">
                Let AI craft a compelling job description. Fill in the fields below and click Generate.
              </p>

              <div className="space-y-3">
                <input
                  value={aiTitle}
                  onChange={(e) => setAiTitle(e.target.value)}
                  placeholder="Job Title (e.g. Senior React Developer)"
                  className={inputCls}
                />
                <input
                  value={aiDepartment}
                  onChange={(e) => setAiDepartment(e.target.value)}
                  placeholder="Department (e.g. Engineering)"
                  className={inputCls}
                />
                <div className="relative">
                  <select
                    value={aiExperience}
                    onChange={(e) => setAiExperience(e.target.value)}
                    className={selectCls}
                  >
                    {EXPERIENCE_LEVELS.map((l) => (
                      <option key={l} value={l} className="bg-[#0f1829]">{l}</option>
                    ))}
                  </select>
                  <ChevronDown className="absolute right-3 top-1/2 -translate-y-1/2 w-4 h-4 text-white/30 pointer-events-none" />
                </div>
                <input
                  value={aiSkills}
                  onChange={(e) => setAiSkills(e.target.value)}
                  placeholder="Key Skills (e.g. React, TypeScript, Node.js)"
                  className={inputCls}
                />
              </div>

              <motion.button
                whileHover={{ scale: 1.02 }}
                whileTap={{ scale: 0.98 }}
                onClick={() => generateMutation.mutate()}
                disabled={generateMutation.isPending || !aiTitle}
                className="w-full flex items-center justify-center gap-2 bg-gradient-to-r from-indigo-600 to-purple-600 hover:from-indigo-500 hover:to-purple-500 disabled:opacity-50 text-white py-2.5 rounded-xl font-semibold text-sm shadow-lg shadow-indigo-500/25 transition-all"
              >
                {generateMutation.isPending ? (
                  <><Loader2 className="w-4 h-4 animate-spin" /> Generating…</>
                ) : (
                  <><Sparkles className="w-4 h-4" /> Generate with AI</>
                )}
              </motion.button>

              <AnimatePresence>
                {generatedDesc && (
                  <motion.div
                    initial={{ opacity: 0, y: 10 }}
                    animate={{ opacity: 1, y: 0 }}
                    exit={{ opacity: 0, y: -10 }}
                    className="space-y-3"
                  >
                    <div className="h-px bg-indigo-500/20" />
                    <div className="text-xs text-white/40 font-semibold uppercase tracking-wider">
                      Generated Description
                    </div>
                    <div className="bg-white/5 border border-white/10 rounded-xl p-3 max-h-56 overflow-y-auto">
                      <p className="text-xs text-white/70 leading-relaxed whitespace-pre-wrap">{generatedDesc}</p>
                    </div>
                    <motion.button
                      whileHover={{ scale: 1.02 }}
                      whileTap={{ scale: 0.98 }}
                      onClick={copyToForm}
                      className="w-full flex items-center justify-center gap-2 bg-emerald-600 hover:bg-emerald-500 text-white py-2.5 rounded-xl font-semibold text-sm transition-all"
                    >
                      <Copy className="w-4 h-4" /> Copy to Form
                    </motion.button>
                  </motion.div>
                )}
              </AnimatePresence>

              {/* Tips */}
              <div className="pt-2 border-t border-white/5">
                <p className="text-[10px] text-white/25 leading-relaxed">
                  💡 Tip: Be specific with skills for better AI results. Review and edit the generated content before publishing.
                </p>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  )
}
