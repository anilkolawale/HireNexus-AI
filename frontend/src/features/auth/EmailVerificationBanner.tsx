import { useState } from 'react'
import toast from 'react-hot-toast'
import { authApi } from '../../api/endpoints/auth.api'

export default function EmailVerificationBanner() {
  const [sending, setSending] = useState(false)
  const [sent, setSent] = useState(false)

  const handleResend = async () => {
    setSending(true)
    try {
      await authApi.resendVerification()
      setSent(true)
      toast.success('Verification email sent — check your inbox')
    } catch {
      toast.error('Could not send verification email')
    } finally {
      setSending(false)
    }
  }

  return (
    <div className="bg-amber-50 dark:bg-amber-900/30 border-b border-amber-200 dark:border-amber-800 px-4 py-2 flex items-center justify-between text-sm">
      <span className="text-amber-800 dark:text-amber-200">
        Your email address hasn't been verified yet.
      </span>
      <button
        onClick={handleResend}
        disabled={sending || sent}
        className="text-amber-900 dark:text-amber-100 font-medium hover:underline disabled:opacity-50"
      >
        {sent ? 'Sent!' : sending ? 'Sending...' : 'Resend verification email'}
      </button>
    </div>
  )
}
