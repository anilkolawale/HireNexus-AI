import axiosClient from '../axiosClient'
import type { ApplicationDetail } from '../../types/candidate.types'

export const applicationsApi = {
  applyToJob: (jobId: string) => axiosClient.post(`/applications/apply/${jobId}`).then((r) => r.data),

  getMyApplications: () =>
    axiosClient.get<ApplicationDetail[]>('/applications/my').then((r) => r.data),

  getAllPipeline: () =>
    axiosClient.get<ApplicationDetail[]>('/applications/pipeline').then((r) => r.data),

  getRankedForJob: (jobId: string) =>
    axiosClient.get(`/applications/job/${jobId}/ranked`).then((r) => r.data),

  changeStatus: (applicationId: string, newStatus: string, notes?: string) =>
    axiosClient.patch(`/applications/${applicationId}/status`, { newStatus, notes })
}

