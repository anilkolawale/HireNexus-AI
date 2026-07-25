export type EmploymentType = 'FullTime' | 'PartTime' | 'Contract' | 'Internship' | 'Remote'
export type JobStatus = 'Draft' | 'Published' | 'Closed'

export interface JobListItem {
  id: string
  title: string
  department: string
  location: string
  employmentType: EmploymentType
  status: JobStatus
  applicationCount: number
  createdAtUtc: string
}

export interface PaginatedResult<T> {
  items: T[]
  pageNumber: number
  totalPages: number
  totalCount: number
  hasPreviousPage: boolean
  hasNextPage: boolean
}
