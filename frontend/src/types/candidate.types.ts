export interface CandidateProfile {
  id: string
  userId: string
  fullName: string
  email: string
  headline?: string
  summary?: string
  currentEmployer?: string
  expectedSalary?: number
  linkedInUrl?: string
  portfolioUrl?: string
  resumeUrl?: string
  skills: string[]
  education: { id: string; institution: string; degree: string; fieldOfStudy?: string; startYear?: number; endYear?: number }[]
  experience: { id: string; companyName: string; title: string; startDate: string; endDate?: string; description?: string }[]
  certifications: string[]
}

export interface ResumeUploadResult {
  resumeUrl: string
  extractedSkills: string[]
  missingFields: string[]
  aiSummary: string
}

export type ApplicationStatus =
  | 'Applied' | 'Screening' | 'Shortlisted' | 'TechnicalInterview'
  | 'HRInterview' | 'Offer' | 'Hired' | 'Rejected'

export interface ApplicationDetail {
  id: string
  jobId: string
  jobTitle: string
  status: ApplicationStatus
  matchScore?: number
  missingSkills: string[]
  recommendedSkills: string[]
  aiRecommendation?: string
  createdAtUtc: string
}
