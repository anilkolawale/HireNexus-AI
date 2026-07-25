import { useState, useEffect } from 'react'
import api from '../../api/axiosClient'

interface BoardPosting {
  id: string
  board: string
  status: string
  externalPostingId?: string
  postedAtUtc?: string
  expiresAtUtc?: string
  errorMessage?: string
}

interface BoardInfo {
  board: string
  displayName: string
  isConfigured: boolean
  logoUrl?: string
}

const BOARD_COLORS: Record<string, string> = {
  LinkedIn: 'from-blue-600 to-blue-700',
  Indeed: 'from-violet-600 to-violet-700',
  Glassdoor: 'from-emerald-600 to-emerald-700',
  ZipRecruiter: 'from-orange-600 to-orange-700',
}

const BOARD_ICONS: Record<string, string> = {
  LinkedIn: '🔵',
  Indeed: '🔷',
  Glassdoor: '🟢',
  ZipRecruiter: '🟠',
}

const STATUS_STYLES: Record<string, string> = {
  Active: 'bg-emerald-500/20 text-emerald-400 border-emerald-500/30',
  Pending: 'bg-amber-500/20 text-amber-400 border-amber-500/30',
  Failed: 'bg-red-500/20 text-red-400 border-red-500/30',
  Closed: 'bg-slate-700 text-slate-400 border-slate-600',
}

interface Props {
  jobId: string
}

const DEFAULT_BOARDS: BoardInfo[] = [
  { board: 'LinkedIn', displayName: 'LinkedIn Jobs', isConfigured: true },
  { board: 'Indeed', displayName: 'Indeed', isConfigured: true },
  { board: 'Glassdoor', displayName: 'Glassdoor', isConfigured: true },
  { board: 'ZipRecruiter', displayName: 'ZipRecruiter', isConfigured: true },
]

export default function JobBoardsPanel({ jobId }: Props) {
  const [boards, setBoards] = useState<BoardInfo[]>(DEFAULT_BOARDS)
  const [postings, setPostings] = useState<BoardPosting[]>([])
  const [isLoading, setIsLoading] = useState(true)
  const [publishing, setPublishing] = useState<string | null>(null)

  useEffect(() => {
    loadData()
  }, [jobId])

  async function loadData() {
    setIsLoading(true)
    try {
      const [boardsRes, postingsRes] = await Promise.all([
        api.get('/job-boards/boards'),
        api.get(`/job-boards/${jobId}/postings`),
      ])
      setBoards(boardsRes.data?.length ? boardsRes.data : DEFAULT_BOARDS)
      setPostings(postingsRes.data ?? [])
    } catch (err) {
      console.error(err)
      setBoards(DEFAULT_BOARDS)
    } finally {
      setIsLoading(false)
    }
  }


  async function publish(board: string) {
    setPublishing(board)
    try {
      await api.post(`/job-boards/${jobId}/publish/${board}`)
      await loadData()
    } catch (err: any) {
      alert(err.response?.data?.message ?? `Failed to publish to ${board}`)
    } finally {
      setPublishing(null)
    }
  }

  async function unpublish(board: string) {
    if (!confirm(`Unpublish job from ${board}?`)) return
    setPublishing(board)
    try {
      await api.delete(`/job-boards/${jobId}/postings/${board}`)

      await loadData()
    } catch (err: any) {
      alert(err.response?.data?.message ?? `Failed to unpublish from ${board}`)
    } finally {
      setPublishing(null)
    }
  }

  function getPosting(board: string): BoardPosting | undefined {
    return postings.find(p => p.board === board && p.status !== 'Closed')
  }

  if (isLoading) {
    return (
      <div className="flex justify-center py-8">
        <div className="w-6 h-6 rounded-full border-2 border-indigo-500/30 border-t-indigo-500 animate-spin" />
      </div>
    )
  }

  return (
    <div className="space-y-4">
      <div className="flex items-center gap-2 mb-5">
        <span className="text-xl">🌐</span>
        <h3 className="text-lg font-semibold text-white">Job Board Distribution</h3>
      </div>
      <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
        {boards.map(board => {
          const posting = getPosting(board.board)
          const isPublishing = publishing === board.board

          return (
            <div
              key={board.board}
              className="p-4 bg-slate-900 border border-slate-700 rounded-xl hover:border-slate-600 transition-all"
            >
              <div className="flex items-center justify-between mb-3">
                <div className="flex items-center gap-2">
                  <span className="text-2xl">{BOARD_ICONS[board.board] ?? '📋'}</span>
                  <div>
                    <div className="font-medium text-white">{board.displayName}</div>
                    {!board.isConfigured && (
                      <div className="text-xs text-amber-400">Credentials not configured</div>
                    )}
                  </div>
                </div>
                {posting && (
                  <span className={`px-2 py-0.5 text-xs rounded-full border font-medium ${STATUS_STYLES[posting.status] ?? STATUS_STYLES.Pending}`}>
                    {posting.status}
                  </span>
                )}
              </div>

              {posting?.postedAtUtc && (
                <div className="text-xs text-slate-400 mb-3">
                  Posted {new Date(posting.postedAtUtc).toLocaleDateString()}
                  {posting.externalPostingId && <span className="ml-1 font-mono">#{posting.externalPostingId.slice(-8)}</span>}
                </div>
              )}

              {posting?.errorMessage && (
                <div className="text-xs text-red-400 bg-red-500/10 border border-red-500/20 rounded-lg px-3 py-2 mb-3">
                  {posting.errorMessage}
                </div>
              )}

              {!board.isConfigured && (
                <div className="text-xs text-slate-500 bg-slate-800 rounded-lg px-3 py-2 mb-3">
                  Add credentials in <code className="text-slate-300">appsettings.json</code> → <code className="text-slate-300">JobBoards:{board.board}</code>
                </div>
              )}

              {!posting || posting.status === 'Failed' || posting.status === 'Closed' ? (
                <button
                  onClick={() => publish(board.board)}
                  disabled={isPublishing}
                  className={`w-full py-2 rounded-lg text-sm font-medium transition-all ${isPublishing ? 'bg-slate-700 text-slate-400' : `bg-gradient-to-r ${BOARD_COLORS[board.board] ?? 'from-slate-600 to-slate-700'} text-white hover:opacity-90`}`}
                >
                  {isPublishing ? 'Publishing...' : `Post to ${board.displayName}`}
                </button>
              ) : posting.status === 'Active' ? (
                <button
                  onClick={() => unpublish(board.board)}
                  disabled={isPublishing}
                  className="w-full py-2 rounded-lg text-sm font-medium border border-red-500/30 text-red-400 hover:bg-red-500/10 transition-all disabled:opacity-50"
                >
                  {isPublishing ? 'Removing...' : 'Unpublish'}
                </button>
              ) : (
                <div className="text-center text-sm text-amber-400 py-1">Publishing in progress...</div>
              )}
            </div>
          )
        })}
      </div>
    </div>
  )
}
