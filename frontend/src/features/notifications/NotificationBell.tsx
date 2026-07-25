import { useEffect, useRef, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { motion, AnimatePresence } from 'framer-motion'
import {
  Bell, X, Check, CheckCheck, Briefcase, Calendar,
  Gift, User, AlertCircle, ChevronRight
} from 'lucide-react'
import { notificationsApi, type NotificationRow } from '../../api/endpoints/notifications.api'
import { useSignalR } from '../../hooks/useSignalR'

function getNotificationIcon(title: string) {
  const t = title.toLowerCase()
  if (t.includes('interview') || t.includes('schedule')) return { icon: Calendar, color: 'text-indigo-400', bg: 'bg-indigo-500/15' }
  if (t.includes('offer'))     return { icon: Gift,     color: 'text-emerald-400', bg: 'bg-emerald-500/15' }
  if (t.includes('job') || t.includes('application')) return { icon: Briefcase, color: 'text-blue-400', bg: 'bg-blue-500/15' }
  if (t.includes('profile') || t.includes('candidate')) return { icon: User, color: 'text-purple-400', bg: 'bg-purple-500/15' }
  return { icon: AlertCircle, color: 'text-amber-400', bg: 'bg-amber-500/15' }
}

function timeAgo(dateStr: string) {
  const diff = Date.now() - new Date(dateStr).getTime()
  const mins = Math.floor(diff / 60000)
  if (mins < 1)  return 'Just now'
  if (mins < 60) return `${mins}m ago`
  const hrs = Math.floor(mins / 60)
  if (hrs < 24)  return `${hrs}h ago`
  return `${Math.floor(hrs / 24)}d ago`
}

function NotificationItem({
  n, onClick
}: { n: NotificationRow; onClick: (n: NotificationRow) => void }) {
  const { icon: Icon, color, bg } = getNotificationIcon(n.title)
  return (
    <motion.button
      initial={{ opacity: 0, x: 8 }}
      animate={{ opacity: 1, x: 0 }}
      onClick={() => onClick(n)}
      className={`
        w-full flex items-start gap-3 px-4 py-3 text-left
        hover:bg-white/5 transition-colors border-b border-white/5 last:border-0
        ${!n.isRead ? 'bg-indigo-500/5' : ''}
      `}
    >
      <div className={`w-8 h-8 rounded-xl ${bg} flex items-center justify-center shrink-0 mt-0.5`}>
        <Icon className={`w-4 h-4 ${color}`} />
      </div>
      <div className="flex-1 min-w-0">
        <div className="flex items-start justify-between gap-2">
          <p className={`text-xs font-semibold leading-snug ${!n.isRead ? 'text-white' : 'text-white/70'}`}>
            {n.title}
          </p>
          {!n.isRead && (
            <div className="w-2 h-2 rounded-full bg-indigo-400 shrink-0 mt-1 shadow-lg shadow-indigo-400/50" />
          )}
        </div>
        <p className="text-[11px] text-white/40 mt-0.5 leading-relaxed line-clamp-2">{n.message}</p>
        <p className="text-[10px] text-white/25 mt-1">{timeAgo(n.createdAtUtc)}</p>
      </div>
      {n.linkUrl && <ChevronRight className="w-3 h-3 text-white/20 shrink-0 mt-2" />}
    </motion.button>
  )
}

export default function NotificationBell() {
  const navigate  = useNavigate()
  const panelRef  = useRef<HTMLDivElement>(null)
  const { notifications: liveNotifications } = useSignalR()
  const [history, setHistory]     = useState<NotificationRow[]>([])
  const [unreadCount, setUnread]  = useState(0)
  const [open, setOpen]           = useState(false)
  const [marking, setMarking]     = useState(false)
  const [pulse, setPulse]         = useState(false)

  const load = () =>
    notificationsApi.getSummary().then(s => {
      setHistory(s.recent)
      setUnread(s.unreadCount)
    })

  useEffect(() => { load() }, [])

  // Pulse badge on new real-time notification
  useEffect(() => {
    if (liveNotifications.length > 0) {
      load()
      setPulse(true)
      setTimeout(() => setPulse(false), 1500)
    }
  }, [liveNotifications.length])

  // Close on outside click
  useEffect(() => {
    if (!open) return
    const handler = (e: MouseEvent) => {
      if (panelRef.current && !panelRef.current.contains(e.target as Node)) setOpen(false)
    }
    document.addEventListener('mousedown', handler)
    return () => document.removeEventListener('mousedown', handler)
  }, [open])

  const handleClick = async (n: NotificationRow) => {
    if (!n.isRead) { await notificationsApi.markRead(n.id); load() }
    setOpen(false)
    if (n.linkUrl) navigate(n.linkUrl)
  }

  const handleMarkAll = async () => {
    setMarking(true)
    await notificationsApi.markAllRead()
    await load()
    setMarking(false)
  }

  return (
    <div className="relative" ref={panelRef}>
      {/* Bell button */}
      <motion.button
        whileHover={{ scale: 1.05 }}
        whileTap={{ scale: 0.95 }}
        onClick={() => setOpen(o => !o)}
        className="relative w-9 h-9 rounded-xl bg-white/5 hover:bg-white/10 border border-white/10 flex items-center justify-center transition-all"
        aria-label="Notifications"
      >
        <Bell className={`w-4 h-4 ${unreadCount > 0 ? 'text-indigo-300' : 'text-white/50'}`} />
        <AnimatePresence>
          {unreadCount > 0 && (
            <motion.span
              initial={{ scale: 0 }}
              animate={{ scale: pulse ? 1.3 : 1 }}
              exit={{ scale: 0 }}
              transition={{ type: 'spring', stiffness: 400, damping: 15 }}
              className="absolute -top-1 -right-1 min-w-[18px] h-[18px] bg-indigo-500 text-white text-[9px] font-bold rounded-full flex items-center justify-center px-1 shadow-lg shadow-indigo-500/40"
            >
              {unreadCount > 9 ? '9+' : unreadCount}
            </motion.span>
          )}
        </AnimatePresence>
      </motion.button>

      {/* Dropdown panel */}
      <AnimatePresence>
        {open && (
          <motion.div
            initial={{ opacity: 0, y: 8, scale: 0.96 }}
            animate={{ opacity: 1, y: 0, scale: 1 }}
            exit={{ opacity: 0, y: 8, scale: 0.96 }}
            transition={{ duration: 0.18, ease: 'easeOut' }}
            className="absolute right-0 mt-2 w-[360px] z-50 rounded-2xl overflow-hidden"
            style={{
              background: 'linear-gradient(135deg, rgba(15,20,40,0.98) 0%, rgba(10,15,30,0.98) 100%)',
              border: '1px solid rgba(99,102,241,0.2)',
              boxShadow: '0 20px 60px -10px rgba(0,0,0,0.6), 0 0 0 1px rgba(255,255,255,0.05)',
              backdropFilter: 'blur(24px)',
            }}
          >
            {/* Header */}
            <div className="flex items-center justify-between px-4 py-3 border-b border-white/5 bg-gradient-to-r from-indigo-600/10 to-transparent">
              <div className="flex items-center gap-2">
                <Bell className="w-4 h-4 text-indigo-400" />
                <span className="text-sm font-bold text-white">Notifications</span>
                {unreadCount > 0 && (
                  <span className="text-[10px] bg-indigo-500/20 text-indigo-300 border border-indigo-500/30 rounded-full px-2 py-0.5 font-semibold">
                    {unreadCount} new
                  </span>
                )}
              </div>
              <div className="flex items-center gap-2">
                {unreadCount > 0 && (
                  <button
                    onClick={handleMarkAll}
                    disabled={marking}
                    className="flex items-center gap-1 text-[11px] text-indigo-400 hover:text-indigo-300 transition-colors disabled:opacity-40"
                  >
                    <CheckCheck className="w-3 h-3" />
                    {marking ? 'Marking...' : 'Mark all read'}
                  </button>
                )}
                <button
                  onClick={() => setOpen(false)}
                  className="w-6 h-6 rounded-lg bg-white/5 hover:bg-white/10 flex items-center justify-center transition-colors"
                >
                  <X className="w-3 h-3 text-white/50" />
                </button>
              </div>
            </div>

            {/* Notification list */}
            <div className="max-h-[420px] overflow-y-auto scrollbar-hide">
              {history.length === 0 ? (
                <div className="flex flex-col items-center justify-center py-12 gap-3">
                  <div className="w-14 h-14 rounded-2xl bg-white/5 flex items-center justify-center">
                    <Bell className="w-7 h-7 text-white/20" />
                  </div>
                  <p className="text-sm text-white/30 font-medium">You're all caught up!</p>
                  <p className="text-xs text-white/20">No notifications yet</p>
                </div>
              ) : (
                history.map(n => (
                  <NotificationItem key={n.id} n={n} onClick={handleClick} />
                ))
              )}
            </div>

            {/* Footer */}
            {history.length > 0 && (
              <div className="px-4 py-2.5 border-t border-white/5 bg-white/[0.02]">
                <p className="text-[10px] text-white/25 text-center">
                  Showing latest {history.length} notifications
                </p>
              </div>
            )}
          </motion.div>
        )}
      </AnimatePresence>
    </div>
  )
}
