import { useQuery } from '@tanstack/react-query'
import { motion } from 'framer-motion'
import {
  AreaChart, Area, BarChart, Bar, PieChart, Pie, Cell,
  XAxis, YAxis, CartesianGrid, Tooltip, ResponsiveContainer, Legend
} from 'recharts'
import {
  Briefcase, Users, CalendarCheck, TrendingUp,
  ArrowUpRight, ArrowDownRight, Clock, Target
} from 'lucide-react'
import { dashboardApi, type RecruiterDashboard as RecruiterDashboardData } from '../../api/endpoints/dashboard.api'

const COLORS = ['#6366f1', '#8b5cf6', '#a78bfa', '#f59e0b', '#fbbf24', '#10b981', '#34d399', '#ef4444']

interface KpiCardProps {
  label: string
  value: number
  icon: React.ElementType
  trend?: number
  color: string
  delay?: number
}

function KpiCard({ label, value, icon: Icon, trend, color, delay = 0 }: KpiCardProps) {
  return (
    <motion.div
      initial={{ opacity: 0, y: 20 }}
      animate={{ opacity: 1, y: 0 }}
      transition={{ duration: 0.5, delay }}
      className="kpi-card group cursor-default"
    >
      <div className="flex items-start justify-between">
        <div>
          <p className="text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wide">{label}</p>
          <motion.p
            initial={{ scale: 0.5, opacity: 0 }}
            animate={{ scale: 1, opacity: 1 }}
            transition={{ duration: 0.6, delay: delay + 0.2, type: 'spring' }}
            className="mt-2 text-3xl font-bold text-gray-900 dark:text-white"
          >
            {value.toLocaleString()}
          </motion.p>
          {trend !== undefined && (
            <div className={`mt-2 flex items-center gap-1 text-xs font-medium ${
              trend >= 0 ? 'text-green-500' : 'text-red-500'
            }`}>
              {trend >= 0
                ? <ArrowUpRight className="w-3 h-3" />
                : <ArrowDownRight className="w-3 h-3" />
              }
              <span>{Math.abs(trend)}% vs last month</span>
            </div>
          )}
        </div>
        <div className={`w-12 h-12 rounded-2xl flex items-center justify-center ${color} transition-transform duration-200 group-hover:scale-110`}>
          <Icon className="w-5 h-5 text-white" />
        </div>
      </div>
    </motion.div>
  )
}

function SkeletonCard() {
  return (
    <div className="kpi-card">
      <div className="skeleton h-3 w-24 mb-3" />
      <div className="skeleton h-9 w-16 mb-2" />
      <div className="skeleton h-3 w-20" />
    </div>
  )
}

export default function RecruiterDashboard() {
  const { data, isLoading, error } = useQuery<RecruiterDashboardData>({
    queryKey: ['recruiterDashboard'],
    queryFn: () => dashboardApi.getRecruiterDashboard(),
    staleTime: 1000 * 60 * 5, // 5 min
  })

  if (error) {
    return (
      <div className="flex items-center justify-center h-64">
        <div className="text-center">
          <p className="text-red-500 font-medium">Failed to load dashboard</p>
          <p className="text-gray-500 text-sm mt-1">Please refresh the page</p>
        </div>
      </div>
    )
  }

  return (
    <div className="space-y-6 animate-fade-in">
      {/* Page header */}
      <div className="page-header">
        <h1 className="page-title">Recruiter Dashboard</h1>
        <p className="page-subtitle">Your hiring pipeline at a glance</p>
      </div>

      {/* KPI Cards */}
      <div className="grid grid-cols-2 lg:grid-cols-4 gap-4">
        {isLoading ? (
          Array.from({ length: 4 }).map((_, i) => <SkeletonCard key={i} />)
        ) : (
          <>
            <KpiCard label="Open Jobs" value={data?.openJobs ?? 0} icon={Briefcase} color="bg-primary-500" delay={0} />
            <KpiCard label="Total Applications" value={data?.totalApplications ?? 0} icon={Users} color="bg-purple-500" delay={0.1} />
            <KpiCard label="Interviews This Week" value={data?.interviewsThisWeek ?? 0} icon={CalendarCheck} color="bg-amber-500" delay={0.2} />
            <KpiCard label="Offers Extended" value={data?.offersExtended ?? 0} icon={Target} color="bg-accent-500" delay={0.3} />
          </>
        )}
      </div>

      {/* Charts Row */}
      <div className="grid lg:grid-cols-2 gap-4">
        {/* Monthly Applications Trend */}
        <motion.div
          initial={{ opacity: 0, x: -20 }}
          animate={{ opacity: 1, x: 0 }}
          transition={{ duration: 0.5, delay: 0.4 }}
          className="card p-6"
        >
          <div className="flex items-center justify-between mb-6">
            <div>
              <h2 className="text-sm font-semibold text-gray-900 dark:text-white">Monthly Recruitment</h2>
              <p className="text-xs text-gray-500 dark:text-gray-400 mt-0.5">Applications over the last 6 months</p>
            </div>
            <TrendingUp className="w-4 h-4 text-primary-500" />
          </div>
          {isLoading ? (
            <div className="skeleton h-48" />
          ) : (
            <ResponsiveContainer width="100%" height={200}>
              <AreaChart data={data?.monthlyApplications ?? []}>
                <defs>
                  <linearGradient id="colorApps" x1="0" y1="0" x2="0" y2="1">
                    <stop offset="5%" stopColor="#6366f1" stopOpacity={0.3} />
                    <stop offset="95%" stopColor="#6366f1" stopOpacity={0} />
                  </linearGradient>
                </defs>
                <CartesianGrid strokeDasharray="3 3" stroke="rgba(99,102,241,0.1)" />
                <XAxis dataKey="month" tick={{ fontSize: 11, fill: '#94a3b8' }} axisLine={false} tickLine={false} />
                <YAxis tick={{ fontSize: 11, fill: '#94a3b8' }} axisLine={false} tickLine={false} />
                <Tooltip
                  contentStyle={{ background: 'var(--color-bg-secondary)', border: '1px solid var(--color-border)', borderRadius: '12px' }}
                  labelStyle={{ color: 'var(--color-text-primary)', fontWeight: 600, fontSize: 12 }}
                  itemStyle={{ color: '#6366f1', fontSize: 12 }}
                />
                <Area
                  type="monotone"
                  dataKey="count"
                  name="Applications"
                  stroke="#6366f1"
                  strokeWidth={2.5}
                  fill="url(#colorApps)"
                  dot={{ fill: '#6366f1', strokeWidth: 0, r: 4 }}
                  activeDot={{ r: 6, strokeWidth: 0 }}
                />
              </AreaChart>
            </ResponsiveContainer>
          )}
        </motion.div>

        {/* Pipeline by stage */}
        <motion.div
          initial={{ opacity: 0, x: 20 }}
          animate={{ opacity: 1, x: 0 }}
          transition={{ duration: 0.5, delay: 0.5 }}
          className="card p-6"
        >
          <div className="flex items-center justify-between mb-6">
            <div>
              <h2 className="text-sm font-semibold text-gray-900 dark:text-white">Candidate Pipeline</h2>
              <p className="text-xs text-gray-500 dark:text-gray-400 mt-0.5">By current stage</p>
            </div>
            <Clock className="w-4 h-4 text-purple-500" />
          </div>
          {isLoading ? (
            <div className="skeleton h-48" />
          ) : (
            <ResponsiveContainer width="100%" height={200}>
              <PieChart>
                <Pie
                  data={data?.pipelineByStage ?? []}
                  dataKey="count"
                  nameKey="stage"
                  cx="50%"
                  cy="50%"
                  outerRadius={75}
                  innerRadius={45}
                  paddingAngle={3}
                >
                  {(data?.pipelineByStage ?? []).map((_, index) => (
                    <Cell key={index} fill={COLORS[index % COLORS.length]} />
                  ))}
                </Pie>
                <Tooltip
                  contentStyle={{ background: 'var(--color-bg-secondary)', border: '1px solid var(--color-border)', borderRadius: '12px' }}
                  labelStyle={{ color: 'var(--color-text-primary)', fontSize: 12 }}
                  itemStyle={{ fontSize: 12 }}
                />
                <Legend
                  formatter={(value) => <span style={{ fontSize: 11, color: 'var(--color-text-secondary)' }}>{value}</span>}
                />
              </PieChart>
            </ResponsiveContainer>
          )}
        </motion.div>
      </div>

      {/* Department Hiring Bar Chart */}
      <motion.div
        initial={{ opacity: 0, y: 20 }}
        animate={{ opacity: 1, y: 0 }}
        transition={{ duration: 0.5, delay: 0.6 }}
        className="card p-6"
      >
        <div className="flex items-center justify-between mb-6">
          <div>
            <h2 className="text-sm font-semibold text-gray-900 dark:text-white">Department-wise Hiring</h2>
            <p className="text-xs text-gray-500 dark:text-gray-400 mt-0.5">Open roles vs hires per department</p>
          </div>
        </div>
        {isLoading ? (
          <div className="skeleton h-48" />
        ) : (
          <ResponsiveContainer width="100%" height={220}>
            <BarChart data={data?.departmentHiring ?? []} barGap={6}>
              <CartesianGrid strokeDasharray="3 3" stroke="rgba(99,102,241,0.1)" vertical={false} />
              <XAxis dataKey="department" tick={{ fontSize: 11, fill: '#94a3b8' }} axisLine={false} tickLine={false} />
              <YAxis tick={{ fontSize: 11, fill: '#94a3b8' }} axisLine={false} tickLine={false} />
              <Tooltip
                contentStyle={{ background: 'var(--color-bg-secondary)', border: '1px solid var(--color-border)', borderRadius: '12px' }}
                itemStyle={{ fontSize: 12 }}
              />
              <Legend formatter={(value) => <span style={{ fontSize: 11, color: 'var(--color-text-secondary)' }}>{value}</span>} />
              <Bar dataKey="openJobs" name="Open Jobs" fill="#6366f1" radius={[6, 6, 0, 0]} />
              <Bar dataKey="hired" name="Hired" fill="#10b981" radius={[6, 6, 0, 0]} />
            </BarChart>
          </ResponsiveContainer>
        )}
      </motion.div>
    </div>
  )
}
