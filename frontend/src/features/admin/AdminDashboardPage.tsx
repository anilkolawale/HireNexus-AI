import { useEffect, useState } from 'react'
import { Doughnut } from 'react-chartjs-2'
import { Chart as ChartJS, ArcElement, Tooltip, Legend } from 'chart.js'
import { adminApi, type AdminDashboard } from '../../api/endpoints/admin.api'

ChartJS.register(ArcElement, Tooltip, Legend)

function KpiCard({ label, value }: { label: string; value: number }) {
  return (
    <div className="bg-white dark:bg-gray-800 rounded-xl p-4 shadow-sm">
      <p className="text-2xl font-semibold">{value}</p>
      <p className="text-xs text-gray-500">{label}</p>
    </div>
  )
}

export default function AdminDashboardPage() {
  const [data, setData] = useState<AdminDashboard | null>(null)

  useEffect(() => {
    adminApi.getDashboard().then(setData)
  }, [])

  if (!data) return <div className="p-6 text-sm text-gray-500">Loading...</div>

  return (
    <div className="p-6 space-y-6">
      <h1 className="text-xl font-semibold">System Admin Dashboard</h1>

      <div className="grid grid-cols-2 md:grid-cols-4 gap-3">
        <KpiCard label="Total Users" value={data.totalUsers} />
        <KpiCard label="Companies" value={data.totalCompanies} />
        <KpiCard label="Jobs Posted" value={data.totalJobs} />
        <KpiCard label="Applications" value={data.totalApplications} />
      </div>

      <div className="bg-white dark:bg-gray-800 rounded-xl p-4 shadow-sm max-w-md">
        <h2 className="text-sm font-medium mb-3">Users by Role</h2>
        <Doughnut
          data={{
            labels: data.usersByRole.map((r) => r.role),
            datasets: [{
              data: data.usersByRole.map((r) => r.count),
              backgroundColor: ['#4F46E5', '#818CF8', '#A5B4FC', '#F59E0B', '#10B981']
            }]
          }}
        />
      </div>
    </div>
  )
}
