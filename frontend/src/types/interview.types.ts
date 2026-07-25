export type InterviewResultStatus = 'Pending' | 'Passed' | 'Failed' | 'NoShow'

export interface Feedback {
  id: string
  rating: number
  strengths?: string
  weaknesses?: string
  comments?: string
  recommend: boolean
}

export interface Interview {
  id: string
  interviewRoundId: string
  roundName: string
  applicationId: string
  jobTitle: string
  candidateName: string
  interviewerId: string
  interviewerName: string
  scheduledAtUtc: string
  durationMinutes: number
  meetingLink?: string
  result: InterviewResultStatus
  feedback?: Feedback
}

export interface InterviewRound {
  id: string
  roundName: string
  sequenceOrder: number
  interviews: Interview[]
}

export interface RankedApplication {
  id: string
  jobId: string
  jobTitle: string
  candidateId: string
  candidateName: string
  status: string
  matchScore?: number
  createdAtUtc: string
}
