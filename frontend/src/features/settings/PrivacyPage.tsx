import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import toast from 'react-hot-toast'
import { privacyApi } from '../../api/endpoints/privacy.api'
import { useAppDispatch } from '../../app/hooks'
import { logout } from '../auth/authSlice'

const CONFIRMATION_PHRASE = 'DELETE MY ACCOUNT'

export default function PrivacyPage() {
  const [exporting, setExporting] = useState(false)
  const [showDeleteConfirm, setShowDeleteConfirm] = useState(false)
  const [confirmText, setConfirmText] = useState('')
  const [deleting, setDeleting] = useState(false)
  const dispatch = useAppDispatch()
  const navigate = useNavigate()

  const handleExport = async () => {
    setExporting(true)
    try {
      const data = await privacyApi.exportMyData()
      const blob = new Blob([JSON.stringify(data, null, 2)], { type: 'application/json' })
      const url = window.URL.createObjectURL(blob)
      const link = document.createElement('a')
      link.href = url
      link.download = 'my-ats-data.json'
      document.body.appendChild(link)
      link.click()
      link.remove()
      window.URL.revokeObjectURL(url)
      toast.success('Your data export has downloaded')
    } catch {
      toast.error('Could not export your data')
    } finally {
      setExporting(false)
    }
  }

  const handleDelete = async () => {
    if (confirmText !== CONFIRMATION_PHRASE) {
      toast.error(`Type "${CONFIRMATION_PHRASE}" exactly to confirm`)
      return
    }
    setDeleting(true)
    try {
      await privacyApi.deleteMyAccount(confirmText)
      toast.success('Your account has been deleted')
      dispatch(logout())
      navigate('/login')
    } catch (err: any) {
      toast.error(err.response?.data?.message || 'Could not delete your account')
    } finally {
      setDeleting(false)
    }
  }

  return (
    <div className="p-6 max-w-lg space-y-6">
      <h1 className="text-xl font-semibold">Privacy & Data</h1>

      <div className="bg-white dark:bg-gray-800 rounded-xl p-5 shadow-sm space-y-2">
        <h2 className="text-sm font-medium">Download your data</h2>
        <p className="text-xs text-gray-500">
          Get a copy of everything we hold about you — profile, applications, and resume history — as a JSON file.
        </p>
        <button
          onClick={handleExport}
          disabled={exporting}
          className="text-sm bg-primary text-white rounded-lg px-4 py-2 disabled:opacity-50"
        >
          {exporting ? 'Preparing export...' : 'Download my data'}
        </button>
      </div>

      <div className="bg-white dark:bg-gray-800 rounded-xl p-5 shadow-sm space-y-2 border border-red-200 dark:border-red-900">
        <h2 className="text-sm font-medium text-red-600">Delete your account</h2>
        <p className="text-xs text-gray-500">
          This permanently removes your personal information (name, email, resume, profile details).
          Your application history is kept in anonymized form for the companies you applied to, as required
          for their compliance records. This cannot be undone.
        </p>

        {!showDeleteConfirm ? (
          <button
            onClick={() => setShowDeleteConfirm(true)}
            className="text-sm border border-red-300 text-red-600 rounded-lg px-4 py-2 hover:bg-red-50 dark:hover:bg-red-900/20"
          >
            Delete my account
          </button>
        ) : (
          <div className="space-y-2">
            <p className="text-xs text-gray-600 dark:text-gray-300">
              Type <strong>{CONFIRMATION_PHRASE}</strong> below to confirm:
            </p>
            <input
              value={confirmText}
              onChange={(e) => setConfirmText(e.target.value)}
              className="w-full border rounded-lg px-3 py-2 text-sm dark:bg-gray-700"
              placeholder={CONFIRMATION_PHRASE}
            />
            <div className="flex gap-2">
              <button
                onClick={handleDelete}
                disabled={deleting}
                className="text-sm bg-red-600 hover:bg-red-700 text-white rounded-lg px-4 py-2 disabled:opacity-50"
              >
                {deleting ? 'Deleting...' : 'Permanently delete'}
              </button>
              <button
                onClick={() => { setShowDeleteConfirm(false); setConfirmText('') }}
                className="text-sm border rounded-lg px-4 py-2"
              >
                Cancel
              </button>
            </div>
          </div>
        )}
      </div>
    </div>
  )
}
