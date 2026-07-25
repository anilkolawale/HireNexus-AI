import { useEffect, useState } from 'react'
import toast from 'react-hot-toast'
import { candidatesApi } from '../../api/endpoints/candidates.api'
import type { CandidateProfile } from '../../types/candidate.types'
import ResumeUpload from './ResumeUpload'
import ResumeHistory from './ResumeHistory'

export default function CandidateProfilePage() {
  const [profile, setProfile] = useState<CandidateProfile | null>(null)
  const [saving, setSaving] = useState(false)
  const [resumeRefreshKey, setResumeRefreshKey] = useState(0)

  const load = () => {
    candidatesApi.getMyProfile()
      .then(setProfile)
      .catch(() => setProfile({
        id: '', userId: '', fullName: '', email: '',
        skills: [], education: [], experience: [], certifications: []
      }))
  }

  useEffect(load, [])

  const handleSave = async () => {
    if (!profile) return
    setSaving(true)
    try {
      await candidatesApi.updateMyProfile({
        headline: profile.headline,
        summary: profile.summary,
        currentEmployer: profile.currentEmployer,
        expectedSalary: profile.expectedSalary,
        linkedInUrl: profile.linkedInUrl,
        portfolioUrl: profile.portfolioUrl
      })
      toast.success('Profile saved')
    } catch {
      toast.error('Failed to save profile')
    } finally {
      setSaving(false)
    }
  }

  if (!profile) return <div className="p-6">Loading...</div>

  return (
    <div className="p-6 max-w-2xl space-y-6">
      <h1 className="text-xl font-semibold">My Profile</h1>

      <ResumeUpload onParsed={() => { load(); setResumeRefreshKey((k) => k + 1) }} />

      <ResumeHistory refreshKey={resumeRefreshKey} />

      <div className="bg-white dark:bg-gray-800 rounded-xl p-5 space-y-3 shadow-sm">
        <input
          placeholder="Headline (e.g. Senior React Developer)"
          value={profile.headline || ''}
          onChange={(e) => setProfile({ ...profile, headline: e.target.value })}
          className="w-full border rounded-lg px-3 py-2 dark:bg-gray-700"
        />
        <textarea
          placeholder="Summary"
          value={profile.summary || ''}
          onChange={(e) => setProfile({ ...profile, summary: e.target.value })}
          className="w-full border rounded-lg px-3 py-2 dark:bg-gray-700"
          rows={3}
        />
        <input
          placeholder="Current employer"
          value={profile.currentEmployer || ''}
          onChange={(e) => setProfile({ ...profile, currentEmployer: e.target.value })}
          className="w-full border rounded-lg px-3 py-2 dark:bg-gray-700"
        />
        <button
          onClick={handleSave}
          disabled={saving}
          className="bg-primary hover:bg-primary-dark text-white rounded-lg px-4 py-2 text-sm font-medium"
        >
          {saving ? 'Saving...' : 'Save Profile'}
        </button>
      </div>

      {profile.skills.length > 0 && (
        <div>
          <h2 className="font-medium mb-2">Skills</h2>
          <div className="flex flex-wrap gap-1">
            {profile.skills.map((s) => (
              <span key={s} className="text-xs bg-primary/10 text-primary px-2 py-0.5 rounded-full">{s}</span>
            ))}
          </div>
        </div>
      )}
    </div>
  )
}
