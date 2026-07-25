import { useEffect, useState } from 'react'
import { dashboardApi, type CandidateDashboard as CandidateDashboardData } from '../../api/endpoints/dashboard.api'

function KpiCard({ label, value }: { label: string; value: number }) {
  return (
    <div className="bg-white dark:bg-gray-800 rounded-xl p-4 shadow-sm">
      <p className="text-2xl font-semibold">{value}</p>
      <p className="text-xs text-gray-500">{label}</p>
    </div>
  )
}

export default function CandidateDashboard() {
  const [data, setData] = useState<CandidateDashboardData | null>(null)

  useEffect(() => {
    dashboardApi.getCandidateDashboard().then(setData)
  }, [])

  if (!data) return <div className="p-6 text-sm text-gray-500">Loading...</div>

  return (
    <div className="p-6 space-y-6">
      <h1 className="text-xl font-semibold">My Dashboard</h1>
      <div className="grid grid-cols-2 md:grid-cols-4 gap-3">
        <KpiCard label="Total Applications" value={data.totalApplications} />
        <KpiCard label="Active Applications" value={data.activeApplications} />
        <KpiCard label="Interviews Scheduled" value={data.interviewsScheduled} />
        <KpiCard label="Offers Received" value={data.offersReceived} />
      </div>
    </div>
  )
}
