import axiosClient from '../axiosClient'

export interface UserListItem {
  id: string
  fullName: string
  email: string
  role: string
}

export const usersApi = {
  getByRole: (role: string) =>
    axiosClient.get<UserListItem[]>('/users', { params: { role } }).then((r) => r.data)
}
