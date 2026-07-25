import { useRef, useState } from 'react'
import toast from 'react-hot-toast'
import { candidatesApi } from '../../api/endpoints/candidates.api'
import type { ResumeUploadResult } from '../../types/candidate.types'

export default function ResumeUpload({ onParsed }: { onParsed?: (r: ResumeUploadResult) => void }) {
  const inputRef = useRef<HTMLInputElement>(null)
  const [progress, setProgress] = useState(0)
  const [uploading, setUploading] = useState(false)
  const [result, setResult] = useState<ResumeUploadResult | null>(null)

  const handleFile = async (file: File) => {
    const validExt = ['.pdf', '.doc', '.docx'].some((ext) => file.name.toLowerCase().endsWith(ext))
    if (!validExt) {
      toast.error('Please upload a PDF, DOC, or DOCX file.')
      return
    }
    if (file.size > 10 * 1024 * 1024) {
      toast.error('File must be under 10 MB.')
      return
    }

    setUploading(true)
    setProgress(0)
    try {
      const res = await candidatesApi.uploadResume(file, setProgress)
      setResult(res)
      onParsed?.(res)
      toast.success('Resume parsed successfully')
    } catch (err: any) {
      toast.error(err.response?.data?.message || 'Upload failed')
    } finally {
      setUploading(false)
    }
  }

  return (
    <div className="border-2 border-dashed rounded-xl p-6 text-center bg-white dark:bg-gray-800">
      <input
        ref={inputRef}
        type="file"
        accept=".pdf,.doc,.docx"
        className="hidden"
        onChange={(e) => e.target.files?.[0] && handleFile(e.target.files[0])}
      />
      <button
        onClick={() => inputRef.current?.click()}
        disabled={uploading}
        className="text-primary font-medium hover:underline"
      >
        {uploading ? `Uploading... ${progress}%` : 'Click to upload your resume'}
      </button>
      <p className="text-xs text-gray-400 mt-1">PDF, DOC, or DOCX — max 10MB</p>

      {result && (
        <div className="text-left mt-4 bg-gray-50 dark:bg-gray-700 rounded-lg p-4 text-sm space-y-2">
          <p className="font-medium">AI Summary</p>
          <p className="text-gray-600 dark:text-gray-300">{result.aiSummary}</p>
          <p className="font-medium mt-2">Extracted Skills</p>
          <div className="flex flex-wrap gap-1">
            {result.extractedSkills.map((s) => (
              <span key={s} className="text-xs bg-primary/10 text-primary px-2 py-0.5 rounded-full">{s}</span>
            ))}
          </div>
          {result.missingFields.length > 0 && (
            <>
              <p className="font-medium mt-2 text-amber-600">Missing Info</p>
              <p className="text-gray-500">{result.missingFields.join(', ')}</p>
            </>
          )}
        </div>
      )}
    </div>
  )
}
