import { useEffect, useState } from 'react'
import { applicationsApi } from '../../api/endpoints/applications.api'
import type { ApplicationDetail } from '../../types/candidate.types'

const STAGES = ['Applied', 'Screening', 'Shortlisted', 'TechnicalInterview', 'HRInterview', 'Offer', 'Hired']

function StatusBadge({ status }: { status: string }) {
  const color = status === 'Rejected' ? 'bg-red-100 text-red-600'
    : status === 'Hired' ? 'bg-green-100 text-green-600'
    : 'bg-primary/10 text-primary'
  return <span className={`text-xs px-2 py-0.5 rounded-full ${color}`}>{status}</span>
}

export default function MyApplicationsPage() {
  const [apps, setApps] = useState<ApplicationDetail[]>([])
  const [loading, setLoading] = useState(true)

  useEffect(() => {
    applicationsApi.getMyApplications().then(setApps).finally(() => setLoading(false))
  }, [])

  return (
    <div className="p-6">
      <h1 className="text-xl font-semibold mb-4">My Applications</h1>
      {loading && <p className="text-sm text-gray-500">Loading...</p>}

      <div className="space-y-3">
        {apps.map((app) => (
          <div key={app.id} className="border rounded-lg p-4 bg-white dark:bg-gray-800 shadow-sm">
            <div className="flex justify-between items-start">
              <div>
                <h2 className="font-medium">{app.jobTitle}</h2>
                <p className="text-xs text-gray-400">Applied {new Date(app.createdAtUtc).toLocaleDateString()}</p>
              </div>
              <div className="text-right">
                <StatusBadge status={app.status} />
                {app.matchScore != null && (
                  <p className="text-xs text-gray-500 mt-1">Match score: {app.matchScore}/100</p>
                )}
              </div>
            </div>

            {!STAGES.includes(app.status) ? null : (
              <div className="flex items-center gap-1 mt-3">
                {STAGES.map((stage, i) => (
                  <div key={stage} className="flex-1 flex items-center">
                    <div className={`h-1.5 flex-1 rounded-full ${
                      STAGES.indexOf(app.status) >= i ? 'bg-primary' : 'bg-gray-200 dark:bg-gray-600'
                    }`} />
                  </div>
                ))}
              </div>
            )}

            {app.aiRecommendation && (
              <p className="text-xs text-gray-500 mt-2 italic">"{app.aiRecommendation}"</p>
            )}
            {app.missingSkills.length > 0 && (
              <p className="text-xs text-amber-600 mt-1">Missing: {app.missingSkills.join(', ')}</p>
            )}
          </div>
        ))}
        {!loading && apps.length === 0 && (
          <p className="text-sm text-gray-500">You haven't applied to any jobs yet.</p>
        )}
      </div>
    </div>
  )
}
