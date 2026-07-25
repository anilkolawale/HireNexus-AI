import axiosClient from '../axiosClient'
import type { Interview, InterviewRound } from '../../types/interview.types'

export interface ScheduleInterviewPayload {
  applicationId: string
  roundName: string
  sequenceOrder: number
  interviewerId: string
  scheduledAtUtc: string
  durationMinutes: number
  meetingLink?: string
}

export interface SubmitFeedbackPayload {
  rating: number
  strengths?: string
  weaknesses?: string
  comments?: string
  recommend: boolean
  result: string
}

export const interviewsApi = {
  schedule: (payload: ScheduleInterviewPayload) =>
    axiosClient.post<Interview>('/interviews/schedule', payload).then((r) => r.data),

  getForApplication: (applicationId: string) =>
    axiosClient.get<InterviewRound[]>(`/interviews/application/${applicationId}`).then((r) => r.data),

  getMySchedule: () => axiosClient.get<Interview[]>('/interviews/my-schedule').then((r) => r.data),

  submitFeedback: (interviewId: string, payload: SubmitFeedbackPayload) =>
    axiosClient.post(`/interviews/${interviewId}/feedback`, payload).then((r) => r.data),

  getAiQuestions: (applicationId: string) =>
    axiosClient.get<string[]>(`/interviews/application/${applicationId}/ai-questions`).then((r) => r.data)
}
