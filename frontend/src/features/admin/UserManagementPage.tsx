import { useEffect, useState } from 'react'
import toast from 'react-hot-toast'
import { adminApi, type UserManagementRow } from '../../api/endpoints/admin.api'

export default function UserManagementPage() {
  const [users, setUsers] = useState<UserManagementRow[]>([])
  const [search, setSearch] = useState('')
  const [loading, setLoading] = useState(true)
  const [updating, setUpdating] = useState<string | null>(null)

  const load = () => {
    setLoading(true)
    adminApi.getAllUsers(search || undefined).then(setUsers).finally(() => setLoading(false))
  }

  useEffect(load, [search])

  const toggleActive = async (user: UserManagementRow) => {
    setUpdating(user.id)
    try {
      await adminApi.setUserActiveStatus(user.id, !user.isActive)
      toast.success(user.isActive ? 'User deactivated' : 'User activated')
      load()
    } catch {
      toast.error('Could not update user')
    } finally {
      setUpdating(null)
    }
  }

  return (
    <div className="p-6">
      <div className="flex justify-between items-center mb-4">
        <h1 className="text-xl font-semibold">User Management</h1>
        <input
          placeholder="Search by name or email..."
          value={search}
          onChange={(e) => setSearch(e.target.value)}
          className="border rounded-lg px-3 py-1.5 text-sm dark:bg-gray-700"
        />
      </div>

      {loading && <p className="text-sm text-gray-500">Loading...</p>}

      <div className="overflow-x-auto bg-white dark:bg-gray-800 rounded-xl shadow-sm">
        <table className="w-full text-sm">
          <thead>
            <tr className="border-b text-left text-xs text-gray-500">
              <th className="px-4 py-2 font-medium">Name</th>
              <th className="px-4 py-2 font-medium">Email</th>
              <th className="px-4 py-2 font-medium">Role</th>
              <th className="px-4 py-2 font-medium">Company</th>
              <th className="px-4 py-2 font-medium">Status</th>
              <th className="px-4 py-2 font-medium"></th>
            </tr>
          </thead>
          <tbody>
            {users.map((u) => (
              <tr key={u.id} className="border-b last:border-0">
                <td className="px-4 py-2">{u.fullName}</td>
                <td className="px-4 py-2 text-gray-500">{u.email}</td>
                <td className="px-4 py-2">
                  <span className="text-xs bg-primary/10 text-primary px-2 py-0.5 rounded-full">{u.role}</span>
                </td>
                <td className="px-4 py-2 text-gray-500">{u.companyName || '—'}</td>
                <td className="px-4 py-2">
                  <span className={`text-xs px-2 py-0.5 rounded-full ${u.isActive ? 'bg-green-100 text-green-600' : 'bg-red-100 text-red-600'}`}>
                    {u.isActive ? 'Active' : 'Inactive'}
                  </span>
                </td>
                <td className="px-4 py-2">
                  <button
                    onClick={() => toggleActive(u)}
                    disabled={updating === u.id}
                    className="text-xs text-primary hover:underline"
                  >
                    {u.isActive ? 'Deactivate' : 'Activate'}
                  </button>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
        {!loading && users.length === 0 && <p className="p-4 text-sm text-gray-400">No users found.</p>}
      </div>
    </div>
  )
}
