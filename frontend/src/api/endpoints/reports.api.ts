import axiosClient from '../axiosClient'

export type ReportType = 'hiring' | 'recruiter-performance' | 'candidates' | 'departments' | 'jobs'

export interface ReportResult<T> {
  title: string
  rows: T[]
  generatedAtUtc: string
}

export const reportsApi = {
  get: <T,>(type: ReportType) => axiosClient.get<ReportResult<T>>(`/reports/${type}`).then((r) => r.data),

  // Downloads the file directly via the browser (keeps the JWT header via axios, then saves the blob).
  export: async (type: ReportType, format: 'excel' | 'pdf') => {
    const response = await axiosClient.get(`/reports/${type}/export/${format}`, { responseType: 'blob' })
    const url = window.URL.createObjectURL(new Blob([response.data]))
    const link = document.createElement('a')
    link.href = url
    link.download = `${type}-report.${format === 'excel' ? 'xlsx' : 'pdf'}`
    document.body.appendChild(link)
    link.click()
    link.remove()
    window.URL.revokeObjectURL(url)
  }
}
