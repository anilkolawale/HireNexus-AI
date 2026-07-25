import { useState } from 'react'
import toast from 'react-hot-toast'
import { authApi } from '../../api/endpoints/auth.api'
import { X } from 'lucide-react'

export default function EmailVerificationBanner() {
  const [sending, setSending] = useState(false)
  const [sent, setSent] = useState(false)
  const [dismissed, setDismissed] = useState(false)

  if (dismissed) return null

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
      <span className="text-amber-800 dark:text-amber-200 font-medium">
        Your email address hasn't been verified yet.
      </span>

      <div className="flex items-center gap-3">
        <button
          onClick={handleResend}
          disabled={sending || sent}
          className="text-amber-900 dark:text-amber-100 font-semibold hover:underline disabled:opacity-50 text-xs bg-amber-200/60 dark:bg-amber-800/60 px-2.5 py-1 rounded-md transition-colors"
        >
          {sent ? 'Sent!' : sending ? 'Sending...' : 'Resend Verification Email'}
        </button>

        <button
          onClick={() => setDismissed(true)}
          className="text-amber-700 hover:text-amber-900 dark:text-amber-300 dark:hover:text-amber-100 p-1 rounded-md transition-colors"
          title="Dismiss notification"
        >
          <X className="w-4 h-4" />
        </button>
      </div>
    </div>
  )
}
