import { useEffect, useState } from 'react'
import toast from 'react-hot-toast'
import { webhooksApi, type WebhookSubscription, type WebhookDeliveryLog } from '../../api/endpoints/webhooks.api'

function DeliveryLogModal({ subscriptionId, onClose }: { subscriptionId: string; onClose: () => void }) {
  const [logs, setLogs] = useState<WebhookDeliveryLog[]>([])
  const [loading, setLoading] = useState(true)

  useEffect(() => {
    webhooksApi.getDeliveries(subscriptionId).then(setLogs).finally(() => setLoading(false))
  }, [subscriptionId])

  return (
    <div className="fixed inset-0 bg-black/40 flex items-center justify-center z-50">
      <div className="bg-white dark:bg-gray-800 rounded-xl p-6 w-full max-w-lg max-h-[70vh] overflow-y-auto space-y-2">
        <div className="flex justify-between items-center mb-2">
          <h2 className="font-semibold">Recent Deliveries</h2>
          <button onClick={onClose} className="text-gray-400 hover:text-gray-600">✕</button>
        </div>
        {loading && <p className="text-sm text-gray-500">Loading...</p>}
        {!loading && logs.length === 0 && <p className="text-sm text-gray-500">No deliveries yet.</p>}
        {logs.map((log) => (
          <div key={log.id} className="flex justify-between items-center text-xs border-b last:border-0 py-2">
            <div>
              <span className="font-medium">{log.eventType}</span>
              <span className="text-gray-400 ml-2">{new Date(log.attemptedAtUtc).toLocaleString()}</span>
              {log.errorMessage && <p className="text-red-500 mt-0.5">{log.errorMessage}</p>}
            </div>
            <span className={`px-2 py-0.5 rounded-full ${log.success ? 'bg-green-100 text-green-600' : 'bg-red-100 text-red-600'}`}>
              {log.responseStatusCode ?? 'Failed'}
            </span>
          </div>
        ))}
      </div>
    </div>
  )
}

export default function WebhooksPage() {
  const [subscriptions, setSubscriptions] = useState<WebhookSubscription[]>([])
  const [eventTypes, setEventTypes] = useState<string[]>([])
  const [loading, setLoading] = useState(true)

  const [newUrl, setNewUrl] = useState('')
  const [selectedEvents, setSelectedEvents] = useState<string[]>([])
  const [creating, setCreating] = useState(false)
  const [newSecret, setNewSecret] = useState<string | null>(null)
  const [viewingDeliveriesFor, setViewingDeliveriesFor] = useState<string | null>(null)

  const load = () => {
    setLoading(true)
    webhooksApi.getAll().then(setSubscriptions).finally(() => setLoading(false))
  }

  useEffect(() => {
    load()
    webhooksApi.getEventTypes().then(setEventTypes)
  }, [])

  const toggleEvent = (evt: string) => {
    setSelectedEvents((prev) => prev.includes(evt) ? prev.filter((e) => e !== evt) : [...prev, evt])
  }

  const handleCreate = async () => {
    if (!newUrl.startsWith('https://')) {
      toast.error('Webhook URL must be HTTPS')
      return
    }
    if (selectedEvents.length === 0) {
      toast.error('Select at least one event type')
      return
    }
    setCreating(true)
    try {
      const created = await webhooksApi.create(newUrl, selectedEvents)
      setNewSecret(created.secret)
      setNewUrl('')
      setSelectedEvents([])
      load()
    } catch (err: any) {
      toast.error(err.response?.data?.message || 'Could not create webhook')
    } finally {
      setCreating(false)
    }
  }

  const handleDelete = async (id: string) => {
    try {
      await webhooksApi.delete(id)
      toast.success('Webhook deleted')
      load()
    } catch {
      toast.error('Could not delete webhook')
    }
  }

  return (
    <div className="p-6 max-w-2xl space-y-6">
      <div>
        <h1 className="text-xl font-semibold">Webhooks</h1>
        <p className="text-sm text-gray-500">
          Get notified in real time when jobs are published, applications change status, or candidates are hired —
          point your own integration (Zapier, a job board syndication tool, a custom listener) at a URL below.
        </p>
      </div>

      {newSecret && (
        <div className="bg-amber-50 dark:bg-amber-900/30 border border-amber-200 dark:border-amber-800 rounded-lg p-4 text-sm">
          <p className="font-medium text-amber-800 dark:text-amber-200">Save this signing secret now — it won't be shown again:</p>
          <code className="block mt-2 bg-white dark:bg-gray-900 rounded px-3 py-2 text-xs break-all">{newSecret}</code>
          <button onClick={() => setNewSecret(null)} className="text-xs text-amber-700 dark:text-amber-300 mt-2 hover:underline">Dismiss</button>
        </div>
      )}

      <div className="bg-white dark:bg-gray-800 rounded-xl p-4 shadow-sm space-y-3">
        <h2 className="text-sm font-medium">Add a webhook</h2>
        <input
          placeholder="https://your-integration.example.com/webhook"
          value={newUrl}
          onChange={(e) => setNewUrl(e.target.value)}
          className="w-full border rounded-lg px-3 py-2 text-sm dark:bg-gray-700"
        />
        <div className="flex flex-wrap gap-2">
          {eventTypes.map((evt) => (
            <button
              key={evt}
              onClick={() => toggleEvent(evt)}
              className={`text-xs px-3 py-1 rounded-full border ${
                selectedEvents.includes(evt) ? 'bg-primary text-white border-primary' : 'text-gray-600 dark:text-gray-300'
              }`}
            >
              {evt}
            </button>
          ))}
        </div>
        <button onClick={handleCreate} disabled={creating} className="text-sm bg-primary text-white rounded-lg px-4 py-2 disabled:opacity-50">
          {creating ? 'Creating...' : 'Add webhook'}
        </button>
      </div>

      {loading && <p className="text-sm text-gray-500">Loading...</p>}

      <div className="space-y-2">
        {subscriptions.map((sub) => (
          <div key={sub.id} className="border rounded-lg p-4 bg-white dark:bg-gray-800 shadow-sm">
            <div className="flex justify-between items-start">
              <div>
                <p className="text-sm font-medium break-all">{sub.url}</p>
                <div className="flex flex-wrap gap-1 mt-1">
                  {sub.eventTypes.map((e) => (
                    <span key={e} className="text-xs bg-primary/10 text-primary px-2 py-0.5 rounded-full">{e}</span>
                  ))}
                </div>
              </div>
              <div className="flex gap-2 text-xs">
                <button onClick={() => setViewingDeliveriesFor(sub.id)} className="text-primary hover:underline">Deliveries</button>
                <button onClick={() => handleDelete(sub.id)} className="text-red-500 hover:underline">Delete</button>
              </div>
            </div>
          </div>
        ))}
        {!loading && subscriptions.length === 0 && (
          <p className="text-sm text-gray-500">No webhooks configured yet.</p>
        )}
      </div>

      {viewingDeliveriesFor && (
        <DeliveryLogModal subscriptionId={viewingDeliveriesFor} onClose={() => setViewingDeliveriesFor(null)} />
      )}
    </div>
  )
}
