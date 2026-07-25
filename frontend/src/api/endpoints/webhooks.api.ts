import axiosClient from '../axiosClient'

export interface WebhookSubscription {
  id: string
  url: string
  eventTypes: string[]
  isActive: boolean
  createdAtUtc: string
}

export interface CreatedWebhookSubscription {
  id: string
  url: string
  secret: string
  eventTypes: string[]
}

export interface WebhookDeliveryLog {
  id: string
  eventType: string
  responseStatusCode?: number
  success: boolean
  errorMessage?: string
  attemptedAtUtc: string
}

export const webhooksApi = {
  getEventTypes: () => axiosClient.get<string[]>('/webhooks/event-types').then((r) => r.data),

  getAll: () => axiosClient.get<WebhookSubscription[]>('/webhooks').then((r) => r.data),

  create: (url: string, eventTypes: string[]) =>
    axiosClient.post<CreatedWebhookSubscription>('/webhooks', { url, eventTypes }).then((r) => r.data),

  delete: (id: string) => axiosClient.delete(`/webhooks/${id}`),

  getDeliveries: (id: string) => axiosClient.get<WebhookDeliveryLog[]>(`/webhooks/${id}/deliveries`).then((r) => r.data)
}
