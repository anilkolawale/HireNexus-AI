import { createAsyncThunk, createSlice } from '@reduxjs/toolkit'
import { jobsApi, type GetJobsParams } from '../../api/endpoints/jobs.api'
import type { JobListItem } from '../../types/job.types'

interface JobsState {
  items: JobListItem[]
  totalCount: number
  pageNumber: number
  totalPages: number
  status: 'idle' | 'loading' | 'succeeded' | 'failed'
  error: string | null
}

const initialState: JobsState = {
  items: [],
  totalCount: 0,
  pageNumber: 1,
  totalPages: 1,
  status: 'idle',
  error: null
}

export const fetchJobs = createAsyncThunk('jobs/fetchJobs', async (params: GetJobsParams) =>
  jobsApi.getJobs(params)
)

const jobsSlice = createSlice({
  name: 'jobs',
  initialState,
  reducers: {},
  extraReducers: (builder) => {
    builder
      .addCase(fetchJobs.pending, (state) => {
        state.status = 'loading'
      })
      .addCase(fetchJobs.fulfilled, (state, action) => {
        state.status = 'succeeded'
        state.items = action.payload.items
        state.totalCount = action.payload.totalCount
        state.pageNumber = action.payload.pageNumber
        state.totalPages = action.payload.totalPages
      })
      .addCase(fetchJobs.rejected, (state, action) => {
        state.status = 'failed'
        state.error = action.error.message ?? 'Failed to load jobs'
      })
  }
})

export default jobsSlice.reducer
