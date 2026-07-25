import axiosClient from '../axiosClient'

export interface Offer {
  id: string
  applicationId: string
  jobTitle: string
  candidateName: string
  offeredSalary: number
  joiningDate: string
  notes?: string
  isAccepted: boolean
  respondedAtUtc?: string
  createdAtUtc: string
}

export const offersApi = {
  create: (applicationId: string, offeredSalary: number, joiningDate: string, notes?: string) =>
    axiosClient.post<Offer>('/offers', { applicationId, offeredSalary, joiningDate, notes }).then((r) => r.data),

  respond: (offerId: string, accept: boolean) =>
    axiosClient.post<Offer>(`/offers/${offerId}/respond`, accept, {
      headers: { 'Content-Type': 'application/json' }
    }).then((r) => r.data),

  getMyOffers: () => axiosClient.get<Offer[]>('/offers/my').then((r) => r.data)
}
