import { useState } from 'react'
import { useNavigate, useSearchParams, Link } from 'react-router-dom'
import toast from 'react-hot-toast'
import { authApi } from '../../api/endpoints/auth.api'

export default function ResetPasswordPage() {
  const [searchParams] = useSearchParams()
  const token = searchParams.get('token') || ''
  const navigate = useNavigate()

  const [newPassword, setNewPassword] = useState('')
  const [confirmPassword, setConfirmPassword] = useState('')
  const [loading, setLoading] = useState(false)

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    if (newPassword !== confirmPassword) {
      toast.error('Passwords do not match')
      return
    }
    if (!token) {
      toast.error('Missing or invalid reset link')
      return
    }
    setLoading(true)
    try {
      await authApi.resetPassword(token, newPassword)
      toast.success('Password reset. Please sign in.')
      navigate('/login')
    } catch (err: any) {
      toast.error(err.response?.data?.message || 'This reset link is invalid or has expired.')
    } finally {
      setLoading(false)
    }
  }

  return (
    <div className="min-h-screen flex items-center justify-center bg-gray-50 dark:bg-gray-900">
      <form onSubmit={handleSubmit} className="w-full max-w-sm bg-white dark:bg-gray-800 p-8 rounded-xl shadow-md space-y-4">
        <h1 className="text-2xl font-semibold text-center">Reset password</h1>
        <input
          type="password"
          placeholder="New password"
          value={newPassword}
          onChange={(e) => setNewPassword(e.target.value)}
          className="w-full border rounded-lg px-3 py-2 dark:bg-gray-700"
          required
          minLength={8}
        />
        <input
          type="password"
          placeholder="Confirm new password"
          value={confirmPassword}
          onChange={(e) => setConfirmPassword(e.target.value)}
          className="w-full border rounded-lg px-3 py-2 dark:bg-gray-700"
          required
        />
        <p className="text-xs text-gray-400">At least 8 characters, one uppercase letter, one digit.</p>
        <button
          type="submit"
          disabled={loading}
          className="w-full bg-primary hover:bg-primary-dark text-white rounded-lg py-2 font-medium transition"
        >
          {loading ? 'Resetting...' : 'Reset password'}
        </button>
        <Link to="/login" className="block text-center text-sm text-primary hover:underline">
          Back to sign in
        </Link>
      </form>
    </div>
  )
}
