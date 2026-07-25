import { describe, it, expect } from 'vitest'
import jobsReducer, { fetchJobs } from '../jobsSlice'
import type { JobListItem, PaginatedResult } from '../../../types/job.types'

const mockJob: JobListItem = {
  id: 'job-1',
  title: 'Senior .NET Developer',
  department: 'Engineering',
  location: 'Remote',
  employmentType: 'FullTime',
  status: 'Published',
  applicationCount: 3,
  createdAtUtc: '2026-01-01T00:00:00Z'
}

const mockResult: PaginatedResult<JobListItem> = {
  items: [mockJob],
  pageNumber: 1,
  totalPages: 1,
  totalCount: 1,
  hasPreviousPage: false,
  hasNextPage: false
}

describe('jobsSlice', () => {
  it('starts in idle state with no jobs', () => {
    const state = jobsReducer(undefined, { type: '@@INIT' })
    expect(state.status).toBe('idle')
    expect(state.items).toEqual([])
  })

  it('sets status to loading on fetchJobs.pending', () => {
    const state = jobsReducer(undefined, { type: fetchJobs.pending.type })
    expect(state.status).toBe('loading')
  })

  it('populates items and pagination on fetchJobs.fulfilled', () => {
    const state = jobsReducer(undefined, {
      type: fetchJobs.fulfilled.type,
      payload: mockResult
    })

    expect(state.status).toBe('succeeded')
    expect(state.items).toEqual([mockJob])
    expect(state.totalCount).toBe(1)
    expect(state.pageNumber).toBe(1)
  })

  it('sets status to failed with an error message on fetchJobs.rejected', () => {
    const state = jobsReducer(undefined, {
      type: fetchJobs.rejected.type,
      error: { message: 'Network error' }
    })

    expect(state.status).toBe('failed')
    expect(state.error).toBe('Network error')
  })
})
