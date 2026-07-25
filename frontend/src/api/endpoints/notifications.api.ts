import axiosClient from '../axiosClient'

export interface NotificationRow {
  id: string
  title: string
  message: string
  isRead: boolean
  linkUrl?: string
  createdAtUtc: string
}

export interface NotificationsSummary {
  unreadCount: number
  recent: NotificationRow[]
}

export interface PaginatedResult<T> {
  items: T[]
  pageNumber: number
  totalPages: number
  totalCount: number
}

export const notificationsApi = {
  getSummary: () => axiosClient.get<NotificationsSummary>('/notifications/summary').then((r) => r.data),

  getAll: (pageNumber = 1) =>
    axiosClient.get<PaginatedResult<NotificationRow>>('/notifications', { params: { pageNumber } }).then((r) => r.data),

  markRead: (id: string) => axiosClient.patch(`/notifications/${id}/read`),

  markAllRead: () => axiosClient.post('/notifications/mark-all-read')
}
