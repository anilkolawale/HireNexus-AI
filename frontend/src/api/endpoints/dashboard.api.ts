import axiosClient from '../axiosClient'

export interface RecruiterDashboard {
  openJobs: number
  totalApplications: number
  interviewsThisWeek: number
  offersExtended: number
  monthlyApplications: { month: string; count: number }[]
  pipelineByStage: { stage: string; count: number }[]
  departmentHiring: { department: string; openJobs: number; hired: number }[]
}

export interface CandidateDashboard {
  totalApplications: number
  activeApplications: number
  interviewsScheduled: number
  offersReceived: number
}

export const dashboardApi = {
  getRecruiterDashboard: () => axiosClient.get<RecruiterDashboard>('/dashboard/recruiter').then((r) => r.data),
  getCandidateDashboard: () => axiosClient.get<CandidateDashboard>('/dashboard/candidate').then((r) => r.data)
}
