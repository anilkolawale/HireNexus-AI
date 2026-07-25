import { useEffect, useState } from 'react'
import { Link, useNavigate, useSearchParams } from 'react-router-dom'
import { authApi } from '../../api/endpoints/auth.api'
import { motion } from 'framer-motion'
import toast from 'react-hot-toast'
import { Sparkles, CheckCircle2, ShieldCheck, ArrowRight, Loader2, RefreshCw } from 'lucide-react'

export default function VerifyEmailPage() {
  const [searchParams] = useSearchParams()
  const tokenFromUrl = searchParams.get('token') || ''
  const emailFromUrl = searchParams.get('email') || ''

  const [otpCode, setOtpCode] = useState(tokenFromUrl)
  const [status, setStatus] = useState<'idle' | 'verifying' | 'success' | 'error'>('idle')
  const [errorMessage, setErrorMessage] = useState('')
  const [resending, setResending] = useState(false)

  const navigate = useNavigate()

  useEffect(() => {
    if (tokenFromUrl) {
      handleVerify(tokenFromUrl)
    }
  }, [tokenFromUrl])

  const handleVerify = async (codeToVerify?: string) => {
    const code = codeToVerify || otpCode
    if (!code || code.trim().length < 6) {
      toast.error('Please enter a valid 6-digit verification code')
      return
    }

    setStatus('verifying')
    setErrorMessage('')

    try {
      await authApi.verifyEmail(code.trim())
      setStatus('success')
      toast.success('Email verified successfully! You can now sign in.')
    } catch (err: any) {
      setStatus('error')
      const msg = err.response?.data?.message || 'Verification failed. The code may be invalid or expired.'
      setErrorMessage(msg)
      toast.error(msg)
    }
  }

  const handleResend = async () => {
    setResending(true)
    try {
      await authApi.resendVerification()
      toast.success('A new 6-digit verification code has been sent to your email.')
    } catch {
      toast.error('Could not resend verification email. Please try again.')
    } finally {
      setResending(false)
    }
  }

  return (
    <div className="min-h-screen flex items-center justify-center bg-gray-50 dark:bg-[#0a0f1e] p-6 relative overflow-hidden">
      {/* Background glow orbs */}
      <div className="absolute top-1/4 left-1/4 w-72 h-72 bg-primary-600/20 rounded-full blur-3xl" />
      <div className="absolute bottom-1/4 right-1/4 w-96 h-96 bg-accent-500/10 rounded-full blur-3xl" />

      <motion.div
        initial={{ opacity: 0, y: 20 }}
        animate={{ opacity: 1, y: 0 }}
        transition={{ duration: 0.6 }}
        className="w-full max-w-md bg-white dark:bg-[#121929] border border-gray-200 dark:border-gray-800 p-8 rounded-3xl shadow-2xl relative z-10 text-center"
      >
        {/* Header Logo */}
        <div className="flex items-center justify-center gap-3 mb-6">
          <div className="w-10 h-10 rounded-2xl bg-gradient-to-br from-primary-500 to-accent-500 flex items-center justify-center shadow-glow">
            <Sparkles className="w-5 h-5 text-white" />
          </div>
          <span className="text-gray-900 dark:text-white font-bold text-xl tracking-tight">HireNexus AI</span>
        </div>

        {status === 'success' ? (
          <motion.div
            initial={{ scale: 0.9, opacity: 0 }}
            animate={{ scale: 1, opacity: 1 }}
            className="space-y-5"
          >
            <div className="w-16 h-16 rounded-full bg-emerald-500/20 text-emerald-500 flex items-center justify-center mx-auto">
              <CheckCircle2 className="w-10 h-10" />
            </div>
            <h2 className="text-2xl font-bold text-gray-900 dark:text-white">Email Verified!</h2>
            <p className="text-sm text-gray-500 dark:text-gray-400">
              Your candidate account is now active and fully verified.
            </p>
            <button
              onClick={() => navigate('/login')}
              className="btn-primary w-full justify-center py-3 text-base mt-4"
            >
              Sign In to Your Account <ArrowRight className="w-4 h-4 ml-2" />
            </button>
          </motion.div>
        ) : (
          <div className="space-y-6">
            <div>
              <div className="w-14 h-14 rounded-2xl bg-primary-500/10 text-primary-500 flex items-center justify-center mx-auto mb-4">
                <ShieldCheck className="w-8 h-8 text-primary-500" />
              </div>
              <h2 className="text-2xl font-bold text-gray-900 dark:text-white">Verify Your Email</h2>
              <p className="text-sm text-gray-500 dark:text-gray-400 mt-2">
                We've sent a 6-digit verification code to{' '}
                <span className="font-semibold text-gray-800 dark:text-gray-200">
                  {emailFromUrl || 'your registered email'}
                </span>.
              </p>
            </div>

            {/* 6-digit OTP Form */}
            <form onSubmit={(e) => { e.preventDefault(); handleVerify(); }} className="space-y-4">
              <div>
                <label className="block text-xs font-medium text-gray-400 uppercase tracking-wider mb-2">
                  Enter 6-Digit Code
                </label>
                <input
                  type="text"
                  maxLength={6}
                  placeholder="e.g. 654321"
                  value={otpCode}
                  onChange={(e) => setOtpCode(e.target.value.replace(/\D/g, ''))}
                  className="w-full text-center text-2xl tracking-[0.4em] font-mono py-3 rounded-xl border border-gray-300 dark:border-gray-700 bg-gray-50 dark:bg-[#0a0f1e] text-gray-900 dark:text-white focus:outline-none focus:ring-2 focus:ring-primary-500"
                  required
                />
              </div>

              {status === 'error' && (
                <p className="text-xs text-red-500 font-medium">{errorMessage}</p>
              )}

              <button
                type="submit"
                disabled={status === 'verifying' || otpCode.length < 6}
                className="btn-primary w-full justify-center py-3 text-base disabled:opacity-50"
              >
                {status === 'verifying' ? (
                  <><Loader2 className="w-5 h-5 animate-spin" /> Verifying Code...</>
                ) : (
                  'Verify Email'
                )}
              </button>
            </form>

            <div className="pt-4 border-t border-gray-100 dark:border-gray-800 flex items-center justify-between text-xs">
              <button
                onClick={handleResend}
                disabled={resending}
                className="text-primary-600 dark:text-primary-400 hover:underline flex items-center gap-1 font-medium disabled:opacity-50"
              >
                <RefreshCw className={`w-3.5 h-3.5 ${resending ? 'animate-spin' : ''}`} />
                Resend Code
              </button>

              <Link to="/login" className="text-gray-500 hover:text-gray-700 dark:hover:text-gray-300">
                Back to Sign In
              </Link>
            </div>
          </div>
        )}
      </motion.div>
    </div>
  )
}
