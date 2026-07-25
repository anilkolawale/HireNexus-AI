import { useState } from 'react'
import { useQuery } from '@tanstack/react-query'
import { motion, AnimatePresence } from 'framer-motion'
import {
  BarChart, Bar, AreaChart, Area, PieChart, Pie, Cell,
  ResponsiveContainer, XAxis, YAxis, Tooltip, CartesianGrid, Legend
} from 'recharts'
import {
  BarChart3, Download, RefreshCw, TrendingUp,
  Users, Briefcase, Target, Award
} from 'lucide-react'
import toast from 'react-hot-toast'
import { reportsApi, type ReportType } from '../../api/endpoints/reports.api'

/* ─── Config ─────────────────────────────────────────────── */
const TABS: { type: ReportType; label: string; icon: React.ElementType }[] = [
  { type: 'hiring',               label: 'Hiring',       icon: TrendingUp },
  { type: 'recruiter-performance', label: 'Recruiters',  icon: Users },
  { type: 'candidates',           label: 'Candidates',   icon: Target },
  { type: 'departments',          label: 'Departments',  icon: Briefcase },
  { type: 'jobs',                 label: 'Jobs',         icon: Award },
]

const CHART_COLORS = ['#6366f1', '#10b981', '#f59e0b', '#ef4444', '#8b5cf6', '#06b6d4']

const tooltipStyle = {
  backgroundColor: '#0f172a',
  border: '1px solid rgba(99,102,241,0.3)',
  borderRadius: '12px',
  color: '#f1f5f9',
  fontSize: '12px',
}

/* ─── Skeleton ───────────────────────────────────────────── */
function ChartSkeleton() {
  return (
    <div className="animate-pulse space-y-3">
      <div className="h-64 bg-white/5 rounded-2xl" />
    </div>
  )
}

/* ─── Dynamic chart renderer ──────────────────────────────── */
function SmartChart({ rows }: { rows: Record<string, any>[] }) {
  if (!rows.length) return (
    <div className="flex items-center justify-center h-48 text-white/25 text-sm">
      No data available for this report.
    </div>
  )

  const keys    = Object.keys(rows[0])
  const nameKey = keys[0]
  const numKeys = keys.filter(k => k !== nameKey && typeof rows[0][k] === 'number').slice(0, 4)

  // Pie chart for ≤ 8 items with 1 value key
  if (rows.length <= 8 && numKeys.length === 1) {
    return (
      <ResponsiveContainer width="100%" height={280}>
        <PieChart>
          <Pie
            data={rows}
            dataKey={numKeys[0]}
            nameKey={nameKey}
            cx="50%"
            cy="50%"
            outerRadius={100}
            innerRadius={55}
            paddingAngle={3}
            label={({ name, percent }) => `${name} ${((percent ?? 0) * 100).toFixed(0)}%`}
            labelLine={false}
          >
            {rows.map((_, i) => (
              <Cell key={i} fill={CHART_COLORS[i % CHART_COLORS.length]} />
            ))}
          </Pie>
          <Tooltip contentStyle={tooltipStyle} />
        </PieChart>
      </ResponsiveContainer>
    )
  }

  // Area chart for time-series style data
  if (nameKey.toLowerCase().includes('month') || nameKey.toLowerCase().includes('date')) {
    return (
      <ResponsiveContainer width="100%" height={280}>
        <AreaChart data={rows} margin={{ top: 8, right: 8, bottom: 0, left: -10 }}>
          <defs>
            {numKeys.map((k, i) => (
              <linearGradient key={k} id={`grad-${i}`} x1="0" y1="0" x2="0" y2="1">
                <stop offset="5%"  stopColor={CHART_COLORS[i]} stopOpacity={0.3} />
                <stop offset="95%" stopColor={CHART_COLORS[i]} stopOpacity={0} />
              </linearGradient>
            ))}
          </defs>
          <CartesianGrid strokeDasharray="3 3" stroke="rgba(255,255,255,0.05)" />
          <XAxis dataKey={nameKey} tick={{ fill: '#64748b', fontSize: 11 }} axisLine={false} tickLine={false} />
          <YAxis tick={{ fill: '#64748b', fontSize: 11 }} axisLine={false} tickLine={false} />
          <Tooltip contentStyle={tooltipStyle} />
          {numKeys.map((k, i) => (
            <Area key={k} type="monotone" dataKey={k} stroke={CHART_COLORS[i]}
              strokeWidth={2} fill={`url(#grad-${i})`} />
          ))}
        </AreaChart>
      </ResponsiveContainer>
    )
  }

  // Default: Grouped bar chart
  return (
    <ResponsiveContainer width="100%" height={280}>
      <BarChart data={rows} margin={{ top: 8, right: 8, bottom: 0, left: -10 }}>
        <CartesianGrid strokeDasharray="3 3" stroke="rgba(255,255,255,0.05)" />
        <XAxis dataKey={nameKey} tick={{ fill: '#64748b', fontSize: 11 }} axisLine={false} tickLine={false} />
        <YAxis tick={{ fill: '#64748b', fontSize: 11 }} axisLine={false} tickLine={false} />
        <Tooltip contentStyle={tooltipStyle} cursor={{ fill: 'rgba(99,102,241,0.1)' }} />
        <Legend wrapperStyle={{ fontSize: '12px', color: '#94a3b8' }} />
        {numKeys.map((k, i) => (
          <Bar key={k} dataKey={k} fill={CHART_COLORS[i]} radius={[4, 4, 0, 0]} />
        ))}
      </BarChart>
    </ResponsiveContainer>
  )
}

/* ─── Data table ─────────────────────────────────────────── */
function DataTable({ rows }: { rows: Record<string, any>[] }) {
  if (!rows.length) return null
  const cols = Object.keys(rows[0])
  return (
    <div className="overflow-x-auto rounded-2xl border border-white/5">
      <table className="w-full text-xs">
        <thead>
          <tr className="border-b border-white/10">
            {cols.map(c => (
              <th key={c} className="px-4 py-3 text-left text-white/40 font-semibold uppercase tracking-wider">
                {c.replace(/([a-z])([A-Z])/g, '$1 $2')}
              </th>
            ))}
          </tr>
        </thead>
        <tbody>
          {rows.map((row, i) => (
            <tr key={i} className="border-b border-white/5 last:border-0 hover:bg-white/[0.03] transition-colors">
              {cols.map(c => (
                <td key={c} className="px-4 py-3 text-white/70">
                  {typeof row[c] === 'number' && c.toLowerCase().includes('score')
                    ? row[c].toFixed(1)
                    : typeof row[c] === 'number'
                    ? row[c].toLocaleString()
                    : String(row[c] ?? '—')}
                </td>
              ))}
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  )
}

/* ─── Main page ──────────────────────────────────────────── */
export default function ReportsPage() {
  const [active, setActive]   = useState<ReportType>('hiring')
  const [exporting, setExp]   = useState<'excel' | 'pdf' | null>(null)
  const [view, setView]       = useState<'chart' | 'table'>('chart')

  const { data, isLoading, refetch, isFetching } = useQuery({
    queryKey: ['reports', active],
    queryFn:  () => reportsApi.get<Record<string, any>>(active),
    staleTime: 1000 * 60 * 5,
  })

  const rows = data?.rows ?? []

  const handleExport = async (format: 'excel' | 'pdf') => {
    setExp(format)
    try {
      await reportsApi.export(active, format)
      toast.success(`Exported as ${format.toUpperCase()}`)
    } catch {
      toast.error('Export failed')
    } finally {
      setExp(null)
    }
  }

  return (
    <div className="min-h-full space-y-6">
      {/* Header */}
      <div className="flex items-start justify-between flex-wrap gap-4">
        <div>
          <h1 className="text-2xl font-bold text-white tracking-tight flex items-center gap-2">
            <BarChart3 className="w-6 h-6 text-indigo-400" />
            Analytics & Reports
          </h1>
          <p className="text-white/40 text-sm mt-1">
            {data ? `Generated ${new Date(data.generatedAtUtc).toLocaleString()} · ${rows.length} rows` : 'Loading report data...'}
          </p>
        </div>

        <div className="flex items-center gap-2">
          <button
            onClick={() => refetch()}
            className="w-9 h-9 rounded-xl bg-white/5 hover:bg-white/10 border border-white/10 flex items-center justify-center transition-all"
          >
            <RefreshCw className={`w-4 h-4 text-white/50 ${isFetching ? 'animate-spin' : ''}`} />
          </button>
          <button
            onClick={() => handleExport('excel')}
            disabled={exporting !== null}
            className="flex items-center gap-1.5 text-xs bg-emerald-500/10 hover:bg-emerald-500/20 border border-emerald-500/20 text-emerald-400 hover:text-emerald-300 rounded-xl px-4 py-2 transition-all disabled:opacity-40"
          >
            <Download className="w-3.5 h-3.5" />
            {exporting === 'excel' ? 'Exporting...' : 'Excel'}
          </button>
          <button
            onClick={() => handleExport('pdf')}
            disabled={exporting !== null}
            className="flex items-center gap-1.5 text-xs bg-red-500/10 hover:bg-red-500/20 border border-red-500/20 text-red-400 hover:text-red-300 rounded-xl px-4 py-2 transition-all disabled:opacity-40"
          >
            <Download className="w-3.5 h-3.5" />
            {exporting === 'pdf' ? 'Exporting...' : 'PDF'}
          </button>
        </div>
      </div>

      {/* Tab bar */}
      <div className="flex gap-2 flex-wrap">
        {TABS.map(tab => {
          const Icon = tab.icon
          const isActive = active === tab.type
          return (
            <motion.button
              key={tab.type}
              whileHover={{ scale: 1.02 }}
              whileTap={{ scale: 0.98 }}
              onClick={() => setActive(tab.type)}
              className={`flex items-center gap-2 px-4 py-2 rounded-xl text-sm font-medium border transition-all ${
                isActive
                  ? 'bg-indigo-500/15 border-indigo-500/40 text-indigo-300'
                  : 'bg-white/[0.03] border-white/10 text-white/50 hover:text-white/70 hover:bg-white/[0.06]'
              }`}
            >
              <Icon className="w-3.5 h-3.5" />
              {tab.label}
            </motion.button>
          )
        })}
      </div>

      {/* Chart / Table toggle */}
      <div
        className="rounded-2xl border border-white/8 overflow-hidden"
        style={{ background: 'linear-gradient(135deg, rgba(255,255,255,0.04) 0%, rgba(255,255,255,0.02) 100%)' }}
      >
        {/* View switcher */}
        <div className="flex items-center justify-between px-5 py-4 border-b border-white/5">
          <p className="text-sm font-semibold text-white/70 capitalize">{active.replace(/-/g, ' ')} Report</p>
          <div className="flex bg-white/5 rounded-lg p-0.5">
            {(['chart', 'table'] as const).map(v => (
              <button
                key={v}
                onClick={() => setView(v)}
                className={`px-3 py-1 rounded-md text-xs font-medium transition-all ${
                  view === v ? 'bg-indigo-600 text-white shadow' : 'text-white/40 hover:text-white/70'
                }`}
              >
                {v.charAt(0).toUpperCase() + v.slice(1)}
              </button>
            ))}
          </div>
        </div>

        <div className="p-5">
          <AnimatePresence mode="wait">
            {isLoading ? (
              <ChartSkeleton key="skeleton" />
            ) : view === 'chart' ? (
              <motion.div key="chart" initial={{ opacity: 0 }} animate={{ opacity: 1 }}>
                <SmartChart rows={rows} />
              </motion.div>
            ) : (
              <motion.div key="table" initial={{ opacity: 0 }} animate={{ opacity: 1 }}>
                <DataTable rows={rows} />
              </motion.div>
            )}
          </AnimatePresence>
        </div>
      </div>
    </div>
  )
}
