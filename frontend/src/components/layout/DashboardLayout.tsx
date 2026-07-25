import { useState } from 'react'
import { Outlet, NavLink, useNavigate } from 'react-router-dom'
import { motion, AnimatePresence } from 'framer-motion'
import { useAppDispatch, useAppSelector } from '../../app/hooks'
import { logout } from '../../features/auth/authSlice'
import { authApi } from '../../api/endpoints/auth.api'
import { ChatWidget, ChatToggleButton } from '../../features/aiAssistant/ChatWidget'
import NotificationBell from '../../features/notifications/NotificationBell'
import EmailVerificationBanner from '../../features/auth/EmailVerificationBanner'
import {
  LayoutDashboard, Briefcase, PlusSquare, User, FileText,
  Gift, Shield, CalendarDays, Users, BarChart3, Upload,
  Building2, Webhook, KeyRound, Monitor, Settings,
  LogOut, ChevronLeft, Menu, Sparkles, Lock, Kanban,
  ClipboardList, UserPlus, CheckSquare, Zap, Video, Globe
} from 'lucide-react'

interface NavItem {
  to: string
  icon: React.ElementType
  label: string
  roles?: string[]
}

const navItems: NavItem[] = [
  { to: '/dashboard', icon: LayoutDashboard, label: 'Dashboard', roles: ['Recruiter', 'HRManager', 'SuperAdmin', 'Interviewer'] },
  { to: '/jobs', icon: Briefcase, label: 'Jobs', roles: ['Recruiter', 'HRManager', 'SuperAdmin', 'Interviewer'] },
  { to: '/jobs/new', icon: PlusSquare, label: 'Post a Job', roles: ['Recruiter', 'HRManager', 'SuperAdmin'] },
  { to: '/pipeline', icon: Kanban, label: 'Pipeline', roles: ['Recruiter', 'HRManager', 'SuperAdmin'] },
  { to: '/profile', icon: User, label: 'My Profile', roles: ['Candidate'] },
  { to: '/my-applications', icon: FileText, label: 'My Applications', roles: ['Candidate'] },
  { to: '/my-offers', icon: Gift, label: 'My Offers', roles: ['Candidate', 'Recruiter', 'HRManager', 'SuperAdmin'] },

  { to: '/my-schedule', icon: CalendarDays, label: 'My Schedule', roles: ['Interviewer', 'Recruiter', 'HRManager', 'SuperAdmin'] },
  { to: '/talent-pool', icon: Users, label: 'Talent Pool', roles: ['Recruiter', 'HRManager', 'SuperAdmin'] },
  { to: '/talent-crm', icon: UserPlus, label: 'Talent CRM', roles: ['Recruiter', 'HRManager', 'SuperAdmin'] },
  { to: '/requisitions', icon: ClipboardList, label: 'Job Requisitions', roles: ['Recruiter', 'HRManager', 'SuperAdmin'] },
  { to: '/reports', icon: BarChart3, label: 'Reports', roles: ['Recruiter', 'HRManager', 'SuperAdmin'] },
  { to: '/onboarding', icon: CheckSquare, label: 'Onboarding', roles: ['HRManager', 'SuperAdmin'] },
  { to: '/candidates/bulk-import', icon: Upload, label: 'Bulk Import', roles: ['Recruiter', 'HRManager', 'SuperAdmin'] },
  { to: '/admin/company-settings', icon: Building2, label: 'Company Settings', roles: ['HRManager', 'SuperAdmin'] },
  { to: '/webhooks', icon: Webhook, label: 'Webhooks', roles: ['SuperAdmin'] },
  { to: '/admin/users', icon: Users, label: 'User Management', roles: ['SuperAdmin'] },
  { to: '/admin/audit-log', icon: Shield, label: 'Audit Log', roles: ['SuperAdmin'] },

  // Phase 6: Industry Dominance
  { to: '/my-assessments', icon: Video, label: 'My Assessments', roles: ['Candidate'] },
  { to: '/assessments/builder', icon: Video, label: 'Assessments', roles: ['Recruiter', 'HRManager', 'SuperAdmin'] },
  { to: '/settings/automations', icon: Zap, label: 'Automations', roles: ['HRManager', 'SuperAdmin'] },
]

const settingsItems: NavItem[] = [
  { to: '/settings/change-password', icon: Lock, label: 'Change Password' },
  { to: '/settings/sessions', icon: Monitor, label: 'Active Sessions' },
  { to: '/settings/privacy', icon: Settings, label: 'Privacy & Data', roles: ['Candidate'] },
]

export default function DashboardLayout() {
  const user = useAppSelector((s) => s.auth.user)
  const refreshToken = useAppSelector((s) => s.auth.refreshToken)
  const dispatch = useAppDispatch()
  const navigate = useNavigate()
  const [sidebarOpen, setSidebarOpen] = useState(true)
  const [darkMode, setDarkMode] = useState(
    () => document.documentElement.classList.contains('dark')
  )
  const [chatOpen, setChatOpen] = useState(false)
  const [hasUnread, setHasUnread] = useState(false)

  const isRecruiterRole = user?.role === 'Recruiter' || user?.role === 'HRManager' || user?.role === 'SuperAdmin'

  const handleLogout = async () => {
    try {
      if (refreshToken) await authApi.logout(refreshToken)
    } catch {
      // Logout should never block navigation even if token is expired
    } finally {
      dispatch(logout())
      navigate('/login')
    }
  }

  const toggleDark = () => {
    const html = document.documentElement
    const isDark = html.classList.toggle('dark')
    setDarkMode(isDark)
    localStorage.setItem('theme', isDark ? 'dark' : 'light')
  }

  const isAllowed = (roles?: string[]) =>
    !roles || roles.includes(user?.role ?? '')

  const handleChatOpen = () => {
    setChatOpen(true)
    setHasUnread(false)
  }

  const handleNewAIMessage = () => {
    if (!chatOpen) setHasUnread(true)
  }

  return (
    <div className="min-h-screen flex bg-gray-50 dark:bg-[#0a0f1e]">
      {/* Sidebar */}
      <motion.aside
        animate={{ width: sidebarOpen ? 240 : 72 }}
        transition={{ duration: 0.25, ease: 'easeInOut' }}
        className="relative flex-shrink-0 bg-white dark:bg-gray-900 border-r border-gray-100 dark:border-gray-800 flex flex-col overflow-hidden z-20"
      >
        {/* Logo area */}
        <div className="h-16 flex items-center px-4 border-b border-gray-100 dark:border-gray-800 flex-shrink-0">
          <div className="flex items-center gap-3 min-w-0">
            <div className="w-8 h-8 rounded-xl bg-gradient-to-br from-primary-500 to-accent-500 flex items-center justify-center flex-shrink-0 shadow-glow">
              <Sparkles className="w-4 h-4 text-white" />
            </div>
            <AnimatePresence mode="wait">
              {sidebarOpen && (
                <motion.span
                  initial={{ opacity: 0, x: -10 }}
                  animate={{ opacity: 1, x: 0 }}
                  exit={{ opacity: 0, x: -10 }}
                  transition={{ duration: 0.15 }}
                  className="font-bold text-gray-900 dark:text-white text-lg tracking-tight whitespace-nowrap"
                >
                  HireIQ
                </motion.span>
              )}
            </AnimatePresence>
          </div>
        </div>

        {/* Nav items */}
        <nav className="flex-1 p-3 space-y-0.5 overflow-y-auto scrollbar-hide">
          {navItems.filter(item => isAllowed(item.roles)).map(({ to, icon: Icon, label }) => (
            <NavLink
              key={to}
              to={to}
              end={to === '/dashboard'}
              className={({ isActive }) =>
                `nav-link ${isActive ? 'active' : ''}`
              }
              title={!sidebarOpen ? label : undefined}
            >
              <Icon className="w-4 h-4 flex-shrink-0" />
              <AnimatePresence mode="wait">
                {sidebarOpen && (
                  <motion.span
                    initial={{ opacity: 0 }}
                    animate={{ opacity: 1 }}
                    exit={{ opacity: 0 }}
                    transition={{ duration: 0.15 }}
                    className="whitespace-nowrap truncate"
                  >
                    {label}
                  </motion.span>
                )}
              </AnimatePresence>
            </NavLink>
          ))}

          {/* Divider */}
          <div className="divider" />

          {/* Settings items */}
          {settingsItems.filter(item => isAllowed(item.roles)).map(({ to, icon: Icon, label }) => (
            <NavLink
              key={to}
              to={to}
              className={({ isActive }) =>
                `nav-link ${isActive ? 'active' : ''}`
              }
              title={!sidebarOpen ? label : undefined}
            >
              <Icon className="w-4 h-4 flex-shrink-0" />
              <AnimatePresence mode="wait">
                {sidebarOpen && (
                  <motion.span
                    initial={{ opacity: 0 }}
                    animate={{ opacity: 1 }}
                    exit={{ opacity: 0 }}
                    transition={{ duration: 0.15 }}
                    className="whitespace-nowrap truncate"
                  >
                    {label}
                  </motion.span>
                )}
              </AnimatePresence>
            </NavLink>
          ))}
        </nav>

        {/* User section */}
        <div className="border-t border-gray-100 dark:border-gray-800 p-3">
          {sidebarOpen ? (
            <div className="flex items-center gap-3">
              <div className="w-8 h-8 rounded-lg bg-gradient-to-br from-primary-400 to-accent-400 flex items-center justify-center text-white text-xs font-bold flex-shrink-0">
                {user?.firstName?.[0]}{user?.lastName?.[0]}
              </div>
              <div className="min-w-0 flex-1">
                <p className="text-sm font-semibold text-gray-900 dark:text-white truncate">
                  {user?.firstName} {user?.lastName}
                </p>
                <p className="text-xs text-gray-500 dark:text-gray-400 truncate">{user?.role}</p>
              </div>
              <button
                onClick={handleLogout}
                title="Logout"
                className="text-gray-400 hover:text-red-500 transition-colors flex-shrink-0"
              >
                <LogOut className="w-4 h-4" />
              </button>
            </div>
          ) : (
            <button
              onClick={handleLogout}
              title="Logout"
              className="w-full flex items-center justify-center text-gray-400 hover:text-red-500 transition-colors py-2"
            >
              <LogOut className="w-4 h-4" />
            </button>
          )}
        </div>

        {/* Collapse toggle */}
        <button
          onClick={() => setSidebarOpen(!sidebarOpen)}
          className="absolute -right-3 top-20 w-6 h-6 rounded-full bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700 flex items-center justify-center shadow-card hover:shadow-card-hover transition-all z-30"
          aria-label={sidebarOpen ? 'Collapse sidebar' : 'Expand sidebar'}
        >
          <motion.div animate={{ rotate: sidebarOpen ? 0 : 180 }} transition={{ duration: 0.2 }}>
            <ChevronLeft className="w-3 h-3 text-gray-500" />
          </motion.div>
        </button>
      </motion.aside>

      {/* Main content */}
      <div className="flex-1 flex flex-col min-w-0 overflow-hidden">
        {/* Topbar */}
        <header className="h-16 flex items-center justify-between px-6 bg-white dark:bg-gray-900 border-b border-gray-100 dark:border-gray-800 flex-shrink-0 z-10">
          <div className="flex items-center gap-3">
            <button
              onClick={() => setSidebarOpen(!sidebarOpen)}
              className="lg:hidden btn-ghost p-2"
            >
              <Menu className="w-5 h-5" />
            </button>
          </div>

          <div className="flex items-center gap-3">
            {/* Dark mode toggle */}
            <button
              onClick={toggleDark}
              className="btn-ghost p-2 rounded-xl"
              aria-label="Toggle dark mode"
            >
              {darkMode ? (
                <svg className="w-4 h-4" fill="currentColor" viewBox="0 0 24 24">
                  <path d="M12 3v1m0 16v1m9-9h-1M4 12H3m15.364-6.364l-.707.707M6.343 17.657l-.707.707M17.657 17.657l-.707-.707M6.343 6.343l-.707-.707M16 12a4 4 0 11-8 0 4 4 0 018 0z" stroke="currentColor" strokeWidth="2" strokeLinecap="round" />
                </svg>
              ) : (
                <svg className="w-4 h-4" fill="currentColor" viewBox="0 0 24 24">
                  <path d="M20.354 15.354A9 9 0 018.646 3.646 9.003 9.003 0 0012 21a9.003 9.003 0 008.354-5.646z" />
                </svg>
              )}
            </button>

            {/* AI Copilot toggle */}
            {isRecruiterRole && (
              <ChatToggleButton onClick={handleChatOpen} hasUnread={hasUnread} />
            )}

            <NotificationBell />

            <div className="flex items-center gap-2 pl-3 border-l border-gray-100 dark:border-gray-800">
              <div className="w-8 h-8 rounded-lg bg-gradient-to-br from-primary-400 to-accent-400 flex items-center justify-center text-white text-xs font-bold">
                {user?.firstName?.[0]}{user?.lastName?.[0]}
              </div>
              <div className="hidden sm:block">
                <p className="text-sm font-semibold text-gray-900 dark:text-white leading-none">
                  {user?.firstName} {user?.lastName}
                </p>
                <p className="text-xs text-gray-500 dark:text-gray-400 mt-0.5">{user?.role}</p>
              </div>
            </div>
          </div>
        </header>

        {/* Email verification banner */}
        {user && !user.isEmailVerified && <EmailVerificationBanner />}

        {/* Page content */}
        <main className="flex-1 overflow-auto p-6">
          <Outlet />
        </main>
      </div>

      {/* AI Chat Panel */}
      {isRecruiterRole && (
        <ChatWidget
          open={chatOpen}
          onClose={() => setChatOpen(false)}
          onNewMessage={handleNewAIMessage}
        />
      )}
    </div>
  )
}
