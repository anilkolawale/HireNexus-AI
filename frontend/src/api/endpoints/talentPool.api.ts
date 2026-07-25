import axiosClient from '../axiosClient'

export interface TalentPoolRow {
  candidateId: string
  fullName: string
  email: string
  headline?: string
  currentEmployer?: string
  skills: string[]
  totalApplications: number
  bestMatchScore?: number
  profileCreatedAtUtc: string
}

export interface PaginatedResult<T> {
  items: T[]
  pageNumber: number
  totalPages: number
  totalCount: number
}

export interface TalentPoolSearchParams {
  searchTerm?: string
  skills?: string
  minExperienceYears?: number
  pageNumber?: number
  pageSize?: number
}

export const talentPoolApi = {
  search: (params: TalentPoolSearchParams) =>
    axiosClient.get<PaginatedResult<TalentPoolRow>>('/candidates/talent-pool', { params }).then((r) => r.data)
}
