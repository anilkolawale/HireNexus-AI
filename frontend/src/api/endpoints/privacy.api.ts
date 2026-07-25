import axiosClient from '../axiosClient'

export interface DataExport {
  profile: {
    firstName: string
    lastName: string
    email: string
    phoneNumber?: string
    headline?: string
    summary?: string
    currentEmployer?: string
    expectedSalary?: number
    linkedInUrl?: string
    portfolioUrl?: string
    skills: string[]
    education: string[]
    experience: string[]
    certifications: string[]
    accountCreatedAtUtc: string
  }
  applications: { jobTitle: string; companyName: string; status: string; matchScore?: number; appliedAtUtc: string }[]
  resumeHistory: { fileName: string; version: number; uploadedAtUtc: string }[]
  exportedAtUtc: string
}

export const privacyApi = {
  exportMyData: () => axiosClient.get<DataExport>('/privacy/my-data').then((r) => r.data),

  deleteMyAccount: (confirmationPhrase: string) =>
    axiosClient.post('/privacy/delete-my-account', { confirmationPhrase })
}
