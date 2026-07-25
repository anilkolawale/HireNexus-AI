import { useEffect, useState } from 'react'
import toast from 'react-hot-toast'
import { sessionsApi, type Session } from '../../api/endpoints/sessions.api'
import { parseUserAgent } from '../../utils/parseUserAgent'
import { useAppSelector } from '../../app/hooks'

export default function SessionsPage() {
  const refreshToken = useAppSelector((s) => s.auth.refreshToken)
  const [sessions, setSessions] = useState<Session[]>([])
  const [loading, setLoading] = useState(true)
  const [revokingId, setRevokingId] = useState<string | null>(null)
  const [revokingOthers, setRevokingOthers] = useState(false)

  const load = () => {
    setLoading(true)
    sessionsApi.getMine(refreshToken).then(setSessions).finally(() => setLoading(false))
  }

  useEffect(load, [refreshToken])

  const handleRevoke = async (sessionId: string) => {
    setRevokingId(sessionId)
    try {
      await sessionsApi.revoke(sessionId)
      toast.success('Session revoked')
      load()
    } catch {
      toast.error('Could not revoke session')
    } finally {
      setRevokingId(null)
    }
  }

  const handleRevokeOthers = async () => {
    if (!refreshToken) return
    setRevokingOthers(true)
    try {
      const { revokedCount } = await sessionsApi.revokeOthers(refreshToken)
      toast.success(`Signed out of ${revokedCount} other session${revokedCount !== 1 ? 's' : ''}`)
      load()
    } catch {
      toast.error('Could not revoke other sessions')
    } finally {
      setRevokingOthers(false)
    }
  }

  const otherSessionsCount = sessions.filter((s) => !s.isCurrent).length

  return (
    <div className="p-6 max-w-2xl">
      <div className="flex justify-between items-center mb-4">
        <h1 className="text-xl font-semibold">Active Sessions</h1>
        {otherSessionsCount > 0 && (
          <button
            onClick={handleRevokeOthers}
            disabled={revokingOthers}
            className="text-sm border border-red-300 text-red-600 rounded-lg px-3 py-1.5 hover:bg-red-50 dark:hover:bg-red-900/20 disabled:opacity-50"
          >
            {revokingOthers ? 'Signing out...' : `Sign out of ${otherSessionsCount} other session${otherSessionsCount !== 1 ? 's' : ''}`}
          </button>
        )}
      </div>

      {loading && <p className="text-sm text-gray-500">Loading...</p>}

      <div className="space-y-2">
        {sessions.map((s) => (
          <div key={s.id} className="border rounded-lg p-4 bg-white dark:bg-gray-800 shadow-sm flex justify-between items-center">
            <div>
              <p className="text-sm font-medium">
                {parseUserAgent(s.userAgent)}
                {s.isCurrent && <span className="ml-2 text-xs bg-green-100 text-green-600 px-2 py-0.5 rounded-full">This device</span>}
              </p>
              <p className="text-xs text-gray-500">{s.ipAddress || 'Unknown location'}</p>
              <p className="text-xs text-gray-400">Last active {new Date(s.lastUsedAtUtc).toLocaleString()}</p>
            </div>
            {!s.isCurrent && (
              <button
                onClick={() => handleRevoke(s.id)}
                disabled={revokingId === s.id}
                className="text-xs text-red-500 hover:underline disabled:opacity-50"
              >
                {revokingId === s.id ? 'Revoking...' : 'Sign out'}
              </button>
            )}
          </div>
        ))}
        {!loading && sessions.length === 0 && (
          <p className="text-sm text-gray-500">No active sessions found.</p>
        )}
      </div>
    </div>
  )
}
