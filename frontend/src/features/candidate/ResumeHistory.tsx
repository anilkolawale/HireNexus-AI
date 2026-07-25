import { useEffect, useState } from 'react'
import { candidatesApi, type ResumeHistoryRow } from '../../api/endpoints/candidates.api'

export default function ResumeHistory({ refreshKey }: { refreshKey?: number }) {
  const [history, setHistory] = useState<ResumeHistoryRow[]>([])
  const [loading, setLoading] = useState(true)

  useEffect(() => {
    setLoading(true)
    candidatesApi.getResumeHistory().then(setHistory).finally(() => setLoading(false))
  }, [refreshKey])

  if (loading) return null
  if (history.length === 0) return null

  return (
    <div className="bg-white dark:bg-gray-800 rounded-xl p-4 shadow-sm">
      <h2 className="text-sm font-medium mb-2">Resume Version History</h2>
      <div className="space-y-1">
        {history.map((h) => (
          <div key={h.id} className="flex justify-between items-center text-xs py-1.5 border-b last:border-0">
            <div>
              <span className="font-medium">v{h.version}</span>
              <span className="text-gray-500 ml-2">{h.fileName}</span>
              {h.isCurrent && <span className="ml-2 bg-green-100 text-green-600 px-2 py-0.5 rounded-full">Current</span>}
            </div>
            <div className="flex items-center gap-3">
              <span className="text-gray-400">{new Date(h.uploadedAtUtc).toLocaleDateString()}</span>
              <a href={h.blobUrl} target="_blank" rel="noreferrer" className="text-primary hover:underline">Download</a>
            </div>
          </div>
        ))}
      </div>
    </div>
  )
}
