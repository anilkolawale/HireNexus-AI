import { publicAxios } from '../publicAxiosClient'

export interface PublicJob {
  id: string
  title: string
  description: string
  department?: string
  location?: string
  employmentType: number
  salaryMin?: number
  salaryMax?: number
  closingDate?: string
  createdAtUtc: string
  company: { name: string; logoUrl?: string; website?: string }
  skills?: string[]
}

export interface PublicJobsResponse {
  total: number
  page: number
  pageSize: number
  jobs: PublicJob[]
}

export interface PublicApplyPayload {
  jobId: string
  fullName: string
  email: string
  phone?: string
  coverLetter?: string
  resumeUrl?: string
}

export const publicApi = {
  getJobs: (params?: { keyword?: string; department?: string; page?: number }) =>
    publicAxios.get<PublicJobsResponse>('/public/jobs', { params }).then(r => r.data),

  getJob: (id: string) =>
    publicAxios.get<PublicJob>(`/public/jobs/${id}`).then(r => r.data),

  apply: (payload: PublicApplyPayload) =>
    publicAxios.post<{ message: string; reference: string }>('/public/apply', payload).then(r => r.data),
}
