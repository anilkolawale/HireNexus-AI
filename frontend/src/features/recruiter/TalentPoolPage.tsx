import { useEffect, useState } from 'react'
import { talentPoolApi, type TalentPoolRow } from '../../api/endpoints/talentPool.api'

export default function TalentPoolPage() {
  const [searchTerm, setSearchTerm] = useState('')
  const [skills, setSkills] = useState('')
  const [minExperience, setMinExperience] = useState('')
  const [results, setResults] = useState<TalentPoolRow[]>([])
  const [totalCount, setTotalCount] = useState(0)
  const [loading, setLoading] = useState(true)
  const [pageNumber, setPageNumber] = useState(1)
  const [totalPages, setTotalPages] = useState(1)

  useEffect(() => {
    setLoading(true)
    talentPoolApi.search({
      searchTerm: searchTerm || undefined,
      skills: skills || undefined,
      minExperienceYears: minExperience ? Number(minExperience) : undefined,
      pageNumber
    }).then((res) => {
      setResults(res.items)
      setTotalCount(res.totalCount)
      setTotalPages(res.totalPages)
    }).finally(() => setLoading(false))
  }, [searchTerm, skills, minExperience, pageNumber])

  return (
    <div className="p-6">
      <h1 className="text-xl font-semibold mb-1">Talent Pool</h1>
      <p className="text-sm text-gray-500 mb-4">Search your full candidate database — not just applicants to one job.</p>

      <div className="grid md:grid-cols-3 gap-3 mb-4">
        <input
          placeholder="Search by name, email, headline, employer..."
          value={searchTerm}
          onChange={(e) => { setSearchTerm(e.target.value); setPageNumber(1) }}
          className="border rounded-lg px-3 py-2 text-sm dark:bg-gray-700 md:col-span-2"
        />
        <input
          placeholder="Skills, comma-separated (React, .NET)"
          value={skills}
          onChange={(e) => { setSkills(e.target.value); setPageNumber(1) }}
          className="border rounded-lg px-3 py-2 text-sm dark:bg-gray-700"
        />
      </div>
      <div className="mb-4">
        <input
          type="number"
          placeholder="Min years of experience"
          value={minExperience}
          onChange={(e) => { setMinExperience(e.target.value); setPageNumber(1) }}
          className="border rounded-lg px-3 py-2 text-sm dark:bg-gray-700 w-56"
        />
      </div>

      {loading && <p className="text-sm text-gray-500">Searching...</p>}
      {!loading && <p className="text-xs text-gray-400 mb-3">{totalCount} candidates found</p>}

      <div className="space-y-2">
        {results.map((c) => (
          <div key={c.candidateId} className="border rounded-lg p-4 bg-white dark:bg-gray-800 shadow-sm">
            <div className="flex justify-between items-start">
              <div>
                <p className="font-medium">{c.fullName}</p>
                <p className="text-xs text-gray-500">{c.headline || c.email}</p>
                {c.currentEmployer && <p className="text-xs text-gray-400">Currently at {c.currentEmployer}</p>}
              </div>
              <div className="text-right text-xs text-gray-500">
                <p>{c.totalApplications} application{c.totalApplications !== 1 ? 's' : ''}</p>
                {c.bestMatchScore != null && <p>Best match: {c.bestMatchScore}/100</p>}
              </div>
            </div>
            {c.skills.length > 0 && (
              <div className="flex flex-wrap gap-1 mt-2">
                {c.skills.map((s) => (
                  <span key={s} className="text-xs bg-primary/10 text-primary px-2 py-0.5 rounded-full">{s}</span>
                ))}
              </div>
            )}
          </div>
        ))}
        {!loading && results.length === 0 && (
          <p className="text-sm text-gray-500">No candidates match these filters.</p>
        )}
      </div>

      {totalPages > 1 && (
        <div className="flex justify-between items-center mt-4 text-xs text-gray-500">
          <button disabled={pageNumber <= 1} onClick={() => setPageNumber((p) => p - 1)} className="disabled:opacity-30">← Previous</button>
          <span>Page {pageNumber} of {totalPages}</span>
          <button disabled={pageNumber >= totalPages} onClick={() => setPageNumber((p) => p + 1)} className="disabled:opacity-30">Next →</button>
        </div>
      )}
    </div>
  )
}
