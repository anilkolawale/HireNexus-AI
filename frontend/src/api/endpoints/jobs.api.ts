import axiosClient from '../axiosClient'
import type { JobListItem, PaginatedResult } from '../../types/job.types'

export interface GetJobsParams {
  searchTerm?: string
  status?: string
  location?: string
  pageNumber?: number
  pageSize?: number
}

export interface CreateJobPayload {
  title: string
  description: string
  responsibilities?: string
  benefits?: string
  departmentId: string
  companyId: string
  hiringManagerId: string
  createdByRecruiterId: string
  experienceRequired: string
  salaryMin: number
  salaryMax: number
  location: string
  employmentType: string
  skills: string[]
}

export const jobsApi = {
  getJobs: (params: GetJobsParams) =>
    axiosClient.get<PaginatedResult<JobListItem>>('/jobs', { params }).then((r) => r.data),

  createJob: (payload: CreateJobPayload) =>
    axiosClient.post('/jobs', payload).then((r) => r.data),

  generateDescription: (title: string, department: string, experienceLevel: string, keySkills: string) =>
    axiosClient.post<{ description: string }>('/jobs/generate-description', { title, department, experienceLevel, keySkills })
      .then((r) => r.data),

  publishJob: (id: string) => axiosClient.post(`/jobs/${id}/publish`),
  closeJob: (id: string) => axiosClient.post(`/jobs/${id}/close`),
  deleteJob: (id: string) => axiosClient.delete(`/jobs/${id}`),
  duplicateJob: (id: string) => axiosClient.post(`/jobs/${id}/duplicate`)
}
