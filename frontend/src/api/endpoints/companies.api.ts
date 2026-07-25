import axiosClient from '../axiosClient'

export interface Company { id: string; name: string; website?: string; industry?: string; description?: string; logoUrl?: string }
export interface Department { id: string; name: string; companyId: string }
export interface Designation { id: string; title: string; departmentId: string }

export const companiesApi = {
  getAll: () => axiosClient.get<Company[]>('/companies').then((r) => r.data),
  create: (data: { name: string; website?: string; industry?: string; description?: string }) =>
    axiosClient.post<Company>('/companies', data).then((r) => r.data),
  update: (id: string, data: { name: string; website?: string; industry?: string; description?: string }) =>
    axiosClient.put<Company>(`/companies/${id}`, data).then((r) => r.data),
  delete: (id: string) => axiosClient.delete(`/companies/${id}`),

  getDepartments: (companyId: string) =>
    axiosClient.get<Department[]>(`/companies/${companyId}/departments`).then((r) => r.data),
  createDepartment: (name: string, companyId: string) =>
    axiosClient.post<Department>('/companies/departments', { name, companyId }).then((r) => r.data),
  updateDepartment: (id: string, name: string) =>
    axiosClient.put<Department>(`/companies/departments/${id}`, { name }).then((r) => r.data),
  deleteDepartment: (id: string) => axiosClient.delete(`/companies/departments/${id}`),

  getDesignations: (departmentId: string) =>
    axiosClient.get<Designation[]>(`/companies/departments/${departmentId}/designations`).then((r) => r.data),
  createDesignation: (title: string, departmentId: string) =>
    axiosClient.post<Designation>('/companies/designations', { title, departmentId }).then((r) => r.data),
  updateDesignation: (id: string, title: string) =>
    axiosClient.put<Designation>(`/companies/designations/${id}`, { title }).then((r) => r.data),
  deleteDesignation: (id: string) => axiosClient.delete(`/companies/designations/${id}`),

  uploadLogo: (companyId: string, file: File) => {
    const formData = new FormData()
    formData.append('file', file)
    return axiosClient
      .post<{ logoUrl: string }>(`/companies/${companyId}/logo`, formData, {
        headers: { 'Content-Type': 'multipart/form-data' }
      })
      .then((r) => r.data)
  }
}
