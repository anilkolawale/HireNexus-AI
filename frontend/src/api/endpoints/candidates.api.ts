import axiosClient from '../axiosClient'
import type { CandidateProfile, ResumeUploadResult } from '../../types/candidate.types'

export interface ResumeHistoryRow {
  id: string
  fileName: string
  version: number
  blobUrl: string
  uploadedAtUtc: string
  isCurrent: boolean
}

export interface BulkImportRowResult { rowNumber: number; email: string; success: boolean; error?: string }
export interface BulkImportResult { totalRows: number; succeeded: number; failed: number; rows: BulkImportRowResult[] }

export const candidatesApi = {
  getMyProfile: () => axiosClient.get<CandidateProfile>('/candidates/me').then((r) => r.data),

  updateMyProfile: (data: Partial<CandidateProfile>) =>
    axiosClient.put<CandidateProfile>('/candidates/me', data).then((r) => r.data),

  uploadResume: (file: File, onProgress?: (pct: number) => void) => {
    const formData = new FormData()
    formData.append('file', file)
    return axiosClient
      .post<ResumeUploadResult>('/resumes/upload', formData, {
        headers: { 'Content-Type': 'multipart/form-data' },
        onUploadProgress: (e) => {
          if (onProgress && e.total) onProgress(Math.round((e.loaded * 100) / e.total))
        }
      })
      .then((r) => r.data)
  },

  getResumeHistory: () => axiosClient.get<ResumeHistoryRow[]>('/candidates/me/resume-history').then((r) => r.data),

  bulkImport: (file: File) => {
    const formData = new FormData()
    formData.append('file', file)
    return axiosClient
      .post<BulkImportResult>('/candidates/bulk-import', formData, { headers: { 'Content-Type': 'multipart/form-data' } })
      .then((r) => r.data)
  }
}
