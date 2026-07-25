import axiosClient from '../axiosClient'

export interface Session {
  id: string
  ipAddress?: string
  userAgent?: string
  createdAtUtc: string
  lastUsedAtUtc: string
  expiresAtUtc: string
  isCurrent: boolean
}

export const sessionsApi = {
  getMine: (currentRefreshToken: string | null) =>
    axiosClient.post<Session[]>('/sessions/mine', { currentRefreshToken }).then((r) => r.data),

  revoke: (sessionId: string) => axiosClient.delete(`/sessions/${sessionId}`),

  revokeOthers: (currentRefreshToken: string) =>
    axiosClient.post<{ revokedCount: number }>('/sessions/revoke-others', { currentRefreshToken }).then((r) => r.data)
}
