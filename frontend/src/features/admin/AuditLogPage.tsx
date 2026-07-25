import { useEffect, useState } from 'react'
import { adminApi, type AuditLogRow } from '../../api/endpoints/admin.api'

export default function AuditLogPage() {
  const [logs, setLogs] = useState<AuditLogRow[]>([])
  const [page, setPage] = useState(1)
  const [totalPages, setTotalPages] = useState(1)
  const [entityFilter, setEntityFilter] = useState('')
  const [loading, setLoading] = useState(true)

  useEffect(() => {
    setLoading(true)
    adminApi.getAuditLogs(page, entityFilter || undefined)
      .then((res) => {
        setLogs(res.items)
        setTotalPages(res.totalPages)
      })
      .finally(() => setLoading(false))
  }, [page, entityFilter])

  return (
    <div className="p-6">
      <div className="flex justify-between items-center mb-4">
        <h1 className="text-xl font-semibold">Audit Log</h1>
        <input
          placeholder="Filter by entity (e.g. Job, Application)"
          value={entityFilter}
          onChange={(e) => { setEntityFilter(e.target.value); setPage(1) }}
          className="border rounded-lg px-3 py-1.5 text-sm dark:bg-gray-700"
        />
      </div>

      {loading && <p className="text-sm text-gray-500">Loading...</p>}

      <div className="overflow-x-auto bg-white dark:bg-gray-800 rounded-xl shadow-sm">
        <table className="w-full text-sm">
          <thead>
            <tr className="border-b text-left text-xs text-gray-500">
              <th className="px-4 py-2 font-medium">Timestamp</th>
              <th className="px-4 py-2 font-medium">User</th>
              <th className="px-4 py-2 font-medium">Action</th>
              <th className="px-4 py-2 font-medium">Entity</th>
            </tr>
          </thead>
          <tbody>
            {logs.map((log) => (
              <tr key={log.id} className="border-b last:border-0">
                <td className="px-4 py-2 text-xs text-gray-500">{new Date(log.timestampUtc).toLocaleString()}</td>
                <td className="px-4 py-2">{log.userName || '—'}</td>
                <td className="px-4 py-2"><span className="text-xs bg-primary/10 text-primary px-2 py-0.5 rounded-full">{log.action}</span></td>
                <td className="px-4 py-2 text-xs text-gray-500">{log.entityName}</td>
              </tr>
            ))}
          </tbody>
        </table>
        {!loading && logs.length === 0 && <p className="p-4 text-sm text-gray-400">No audit entries.</p>}
      </div>

      <div className="flex justify-between items-center mt-3 text-xs text-gray-500">
        <button disabled={page <= 1} onClick={() => setPage((p) => p - 1)} className="disabled:opacity-30">← Previous</button>
        <span>Page {page} of {totalPages}</span>
        <button disabled={page >= totalPages} onClick={() => setPage((p) => p + 1)} className="disabled:opacity-30">Next →</button>
      </div>
    </div>
  )
}
