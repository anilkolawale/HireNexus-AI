import axiosClient from '../axiosClient'

export interface ChatMessage {
  role: 'user' | 'assistant'
  content: string
}

export interface SkillGapResult {
  candidateHas: string[]
  jobRequires: string[]
  gapSkills: string[]
  bonusSkills: string[]
  learningRecommendations: string
  gapSeverity: 1 | 2 | 3
}

export interface CandidateRanking {
  candidateName: string
  rank: number
  strengths: string
  weaknesses: string
  hiringRecommendation: string
}

export interface CandidateComparisonResult {
  bestCandidateName: string
  summary: string
  rankings: CandidateRanking[]
}

export const aiAssistantApi = {
  chat: (message: string, history: ChatMessage[]) =>
    axiosClient.post<{ reply: string }>('/aiassistant/chat', { message, history }).then((r) => r.data),

  generateJobDescription: (params: {
    title: string
    department: string
    experienceLevel: string
    keySkills: string
  }): Promise<{ description: string }> =>
    axiosClient
      .post<{ description: string }>('/aiassistant/generate-job-description', params)
      .then((r) => r.data),

  getSkillGap: (applicationId: string): Promise<SkillGapResult> =>
    axiosClient
      .get<SkillGapResult>(`/applications/${applicationId}/skill-gap`)
      .then((r) => r.data),

  compareCandidates: (
    applicationIds: string[],
    jobId: string
  ): Promise<CandidateComparisonResult> =>
    axiosClient
      .post<CandidateComparisonResult>('/aiassistant/compare-candidates', { applicationIds, jobId })
      .then((r) => r.data),
}
