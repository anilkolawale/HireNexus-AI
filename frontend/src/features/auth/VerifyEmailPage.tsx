import { useEffect, useState } from 'react'
import { Link, useSearchParams } from 'react-router-dom'
import { authApi } from '../../api/endpoints/auth.api'
import { useAppDispatch } from '../../app/hooks'
import { markEmailVerified } from './authSlice'

export default function VerifyEmailPage() {
  const [searchParams] = useSearchParams()
  const token = searchParams.get('token') || ''
  const [status, setStatus] = useState<'verifying' | 'success' | 'error'>('verifying')
  const dispatch = useAppDispatch()

  useEffect(() => {
    if (!token) {
      setStatus('error')
      return
    }
    authApi.verifyEmail(token)
      .then(() => {
        setStatus('success')
        dispatch(markEmailVerified())
      })
      .catch(() => setStatus('error'))
  }, [token, dispatch])

  return (
    <div className="min-h-screen flex items-center justify-center bg-gray-50 dark:bg-gray-900">
      <div className="w-full max-w-sm bg-white dark:bg-gray-800 p-8 rounded-xl shadow-md text-center space-y-4">
        {status === 'verifying' && <p className="text-sm text-gray-500">Verifying your email...</p>}
        {status === 'success' && (
          <>
            <h1 className="text-xl font-semibold text-green-600">Email verified!</h1>
            <p className="text-sm text-gray-500">Your email address has been confirmed.</p>
          </>
        )}
        {status === 'error' && (
          <>
            <h1 className="text-xl font-semibold text-red-600">Verification failed</h1>
            <p className="text-sm text-gray-500">This link is invalid or has expired. You can request a new one from your account settings.</p>
          </>
        )}
        <Link to="/login" className="block text-sm text-primary hover:underline">Back to sign in</Link>
      </div>
    </div>
  )
}
