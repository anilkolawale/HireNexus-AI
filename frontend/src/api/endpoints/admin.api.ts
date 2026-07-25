import axiosClient from '../axiosClient'

export interface AdminDashboard {
  totalUsers: number
  totalCompanies: number
  totalJobs: number
  totalApplications: number
  usersByRole: { role: string; count: number }[]
}

export interface AuditLogRow {
  id: string
  userId?: string
  userName?: string
  action: string
  entityName: string
  entityId?: string
  timestampUtc: string
}

export interface UserManagementRow {
  id: string
  fullName: string
  email: string
  role: string
  companyName?: string
  isActive: boolean
  isEmailVerified: boolean
  createdAtUtc: string
}

export interface PaginatedResult<T> {
  items: T[]
  pageNumber: number
  totalPages: number
  totalCount: number
}

export const adminApi = {
  getDashboard: () => axiosClient.get<AdminDashboard>('/admin/dashboard').then((r) => r.data),

  getAuditLogs: (pageNumber = 1, entityName?: string) =>
    axiosClient.get<PaginatedResult<AuditLogRow>>('/admin/audit-logs', { params: { pageNumber, entityName } }).then((r) => r.data),

  getAllUsers: (searchTerm?: string) =>
    axiosClient.get<UserManagementRow[]>('/admin/users', { params: { searchTerm } }).then((r) => r.data),

  setUserActiveStatus: (userId: string, isActive: boolean) =>
    axiosClient.patch(`/admin/users/${userId}/active-status`, isActive, {
      headers: { 'Content-Type': 'application/json' }
    })
}
