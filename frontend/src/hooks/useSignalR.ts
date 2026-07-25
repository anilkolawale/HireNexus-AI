import { useEffect, useRef, useState } from 'react'
import * as signalR from '@microsoft/signalr'
import { useAppSelector } from '../app/hooks'

export interface LiveNotification {
  id: string
  title: string
  message: string
  createdAtUtc: string
}

// Connects to the NotificationHub once authenticated; auto-reconnects; exposes the
// latest notifications so any component (toast, bell icon, etc.) can consume them.
export function useSignalR() {
  const accessToken = useAppSelector((s) => s.auth.accessToken)
  const [notifications, setNotifications] = useState<LiveNotification[]>([])
  const connectionRef = useRef<signalR.HubConnection | null>(null)

  useEffect(() => {
    if (!accessToken) return

    const baseUrl = (import.meta.env.VITE_API_BASE_URL || 'http://localhost:5000/api').replace(/\/api\/?$/, '')
    const connection = new signalR.HubConnectionBuilder()
      .withUrl(`${baseUrl}/hubs/notifications`, { accessTokenFactory: () => accessToken })
      .withAutomaticReconnect()
      .build()

    connection.on('ReceiveNotification', (notification: LiveNotification) => {
      setNotifications((prev) => [notification, ...prev].slice(0, 50))
    })

    connection.start().catch((err) => console.error('SignalR connection failed:', err))
    connectionRef.current = connection

    return () => {
      connection.stop()
    }
  }, [accessToken])

  return { notifications }
}
