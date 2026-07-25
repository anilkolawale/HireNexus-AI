import { useState, useEffect } from 'react'
import api from '../../api/axiosClient'

interface AutomationRule {
  id: string
  name: string
  description?: string
  isEnabled: boolean
  trigger: string
  triggerConfigJson: string
  action: string
  actionConfigJson: string
  executionCount: number
  lastFiredAtUtc?: string
  createdAtUtc: string
}

interface MetaItem {
  name: string
  description: string
  exampleConfig: string
}

interface AutomationMeta {
  triggers: MetaItem[]
  actions: MetaItem[]
  stages: string[]
}

const TRIGGER_ICONS: Record<string, string> = {
  MatchScoreAbove: '🎯',
  ApplicationStatusChanged: '🔄',
  DaysInStageExceeds: '⏰',
  ApplicationReceived: '📥',
  CandidateHired: '🎉',
}

const ACTION_ICONS: Record<string, string> = {
  SendEmail: '📧',
  SendNotification: '🔔',
  MoveToStage: '➡️',
  AssignToRecruiter: '👤',
  CreateTask: '✅',
}

const PRESET_RULES = [
  {
    name: 'Auto-shortlist high scorers',
    description: 'Automatically move candidates scoring above 85% to Shortlisted stage',
    trigger: 'MatchScoreAbove',
    triggerConfig: '{"minScore": 85}',
    action: 'MoveToStage',
    actionConfig: '{"stage": "Shortlisted"}',
  },
  {
    name: 'SLA breach alert',
    description: 'Notify team when application is stuck in Applied stage for 3+ days',
    trigger: 'DaysInStageExceeds',
    triggerConfig: '{"days": 3, "stage": "Applied"}',
    action: 'SendNotification',
    actionConfig: '{"message": "Application SLA breached — candidate has been in Applied stage for over 3 days."}',
  },
  {
    name: 'Welcome email on apply',
    description: 'Send a welcome email to every new applicant automatically',
    trigger: 'ApplicationReceived',
    triggerConfig: '{}',
    action: 'SendEmail',
    actionConfig: '{"subject": "Thank you for applying!", "body": "<p>Thank you for your application. We will review it shortly and be in touch.</p>"}',
  },
]

export default function AutomationRulesPage() {
  const [rules, setRules] = useState<AutomationRule[]>([])
  const [meta, setMeta] = useState<AutomationMeta | null>(null)
  const [isLoading, setIsLoading] = useState(true)
  const [showBuilder, setShowBuilder] = useState(false)
  const [form, setForm] = useState({
    name: '',
    description: '',
    trigger: '',
    triggerConfigJson: '{}',
    action: '',
    actionConfigJson: '{}',
    isEnabled: true,
  })
  const [saving, setSaving] = useState(false)
  const [error, setError] = useState('')

  useEffect(() => {
    loadData()
  }, [])

  async function loadData() {
    setIsLoading(true)
    try {
      const [rulesRes, metaRes] = await Promise.all([
        api.get('/automations'),
        api.get('/automations/meta'),
      ])
      setRules(rulesRes.data)
      setMeta(metaRes.data)
    } catch (err) {
      console.error(err)
    } finally {
      setIsLoading(false)
    }
  }

  async function toggleRule(id: string) {
    try {
      const res = await api.patch(`/automations/${id}/toggle`)
      setRules(rules.map(r => r.id === id ? { ...r, isEnabled: res.data.isEnabled } : r))
    } catch (err) {
      console.error(err)
    }
  }

  async function deleteRule(id: string) {
    if (!confirm('Delete this automation rule?')) return
    try {
      await api.delete(`/automations/${id}`)
      setRules(rules.filter(r => r.id !== id))
    } catch (err) {
      console.error(err)
    }
  }

  async function saveRule() {
    if (!form.name || !form.trigger || !form.action) {
      setError('Name, trigger, and action are required.')
      return
    }
    setSaving(true)
    setError('')
    try {
      const res = await api.post('/automations', form)

      setShowBuilder(false)
      setForm({ name: '', description: '', trigger: '', triggerConfigJson: '{}', action: '', actionConfigJson: '{}', isEnabled: true })
      await loadData()
    } catch (err: any) {
      setError(err.response?.data?.message ?? 'Failed to create rule.')
    } finally {
      setSaving(false)
    }
  }

  function applyPreset(preset: typeof PRESET_RULES[0]) {
    setForm({
      name: preset.name,
      description: preset.description,
      trigger: preset.trigger,
      triggerConfigJson: preset.triggerConfig,
      action: preset.action,
      actionConfigJson: preset.actionConfig,
      isEnabled: true,
    })
    setShowBuilder(true)
  }

  return (
    <div className="min-h-screen bg-slate-950 text-white p-6 space-y-8">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-3xl font-bold bg-gradient-to-r from-violet-400 to-cyan-400 bg-clip-text text-transparent">
            ⚙️ Workflow Automation Engine
          </h1>
          <p className="text-slate-400 mt-1">Configure trigger-action rules to automate your recruitment pipeline</p>
        </div>
        <button
          onClick={() => setShowBuilder(true)}
          className="px-5 py-2.5 bg-gradient-to-r from-violet-600 to-indigo-600 text-white rounded-xl font-medium hover:from-violet-500 hover:to-indigo-500 transition-all shadow-lg shadow-indigo-500/20"
        >
          + New Rule
        </button>
      </div>

      {/* Preset Templates */}
      <div>
        <h2 className="text-lg font-semibold text-slate-300 mb-3">🚀 Quick Start Templates</h2>
        <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
          {PRESET_RULES.map(preset => (
            <button
              key={preset.name}
              onClick={() => applyPreset(preset)}
              className="text-left p-4 bg-slate-900 border border-slate-700 rounded-xl hover:border-violet-500/50 hover:bg-slate-800 transition-all group"
            >
              <div className="font-medium text-white group-hover:text-violet-300 transition-colors">{preset.name}</div>
              <div className="text-sm text-slate-400 mt-1">{preset.description}</div>
              <div className="mt-3 flex gap-2 text-xs">
                <span className="px-2 py-0.5 bg-violet-900/50 text-violet-300 rounded">{TRIGGER_ICONS[preset.trigger]} {preset.trigger}</span>
                <span className="px-2 py-0.5 bg-cyan-900/50 text-cyan-300 rounded">{ACTION_ICONS[preset.action]} {preset.action}</span>
              </div>
            </button>
          ))}
        </div>
      </div>

      {/* Rule Builder */}
      {showBuilder && (
        <div className="fixed inset-0 bg-black/60 backdrop-blur-sm flex items-center justify-center z-50 p-4">
          <div className="bg-slate-900 border border-slate-700 rounded-2xl w-full max-w-2xl shadow-2xl">
            <div className="p-6 border-b border-slate-700">
              <h2 className="text-xl font-bold text-white">⚡ Create Automation Rule</h2>
            </div>
            <div className="p-6 space-y-5">
              <div>
                <label className="text-sm text-slate-400 block mb-1.5">Rule Name *</label>
                <input
                  className="w-full bg-slate-800 border border-slate-700 rounded-lg px-4 py-2.5 text-white focus:outline-none focus:border-violet-500"
                  placeholder="e.g. Auto-shortlist high scorers"
                  value={form.name}
                  onChange={e => setForm({ ...form, name: e.target.value })}
                />
              </div>
              <div>
                <label className="text-sm text-slate-400 block mb-1.5">Description</label>
                <input
                  className="w-full bg-slate-800 border border-slate-700 rounded-lg px-4 py-2.5 text-white focus:outline-none focus:border-violet-500"
                  placeholder="What does this rule do?"
                  value={form.description}
                  onChange={e => setForm({ ...form, description: e.target.value })}
                />
              </div>
              <div className="grid grid-cols-2 gap-4">
                <div>
                  <label className="text-sm text-slate-400 block mb-1.5">🎯 Trigger *</label>
                  <select
                    className="w-full bg-slate-800 border border-slate-700 rounded-lg px-4 py-2.5 text-white focus:outline-none focus:border-violet-500"
                    value={form.trigger}
                    onChange={e => setForm({ ...form, trigger: e.target.value })}
                  >
                    <option value="">Select trigger...</option>
                    {meta?.triggers.map(t => (
                      <option key={t.name} value={t.name}>{TRIGGER_ICONS[t.name]} {t.name}</option>
                    ))}
                  </select>
                </div>
                <div>
                  <label className="text-sm text-slate-400 block mb-1.5">⚡ Action *</label>
                  <select
                    className="w-full bg-slate-800 border border-slate-700 rounded-lg px-4 py-2.5 text-white focus:outline-none focus:border-violet-500"
                    value={form.action}
                    onChange={e => setForm({ ...form, action: e.target.value })}
                  >
                    <option value="">Select action...</option>
                    {meta?.actions.map(a => (
                      <option key={a.name} value={a.name}>{ACTION_ICONS[a.name]} {a.name}</option>
                    ))}
                  </select>
                </div>
              </div>
              <div>
                <label className="text-sm text-slate-400 block mb-1.5">
                  Trigger Config (JSON)
                  {form.trigger && meta && (
                    <span className="ml-2 text-xs text-slate-500">
                      e.g. {meta.triggers.find(t => t.name === form.trigger)?.exampleConfig}
                    </span>
                  )}
                </label>
                <textarea
                  className="w-full bg-slate-800 border border-slate-700 rounded-lg px-4 py-2.5 text-white font-mono text-sm focus:outline-none focus:border-violet-500 h-20"
                  value={form.triggerConfigJson}
                  onChange={e => setForm({ ...form, triggerConfigJson: e.target.value })}
                />
              </div>
              <div>
                <label className="text-sm text-slate-400 block mb-1.5">
                  Action Config (JSON)
                  {form.action && meta && (
                    <span className="ml-2 text-xs text-slate-500">
                      e.g. {meta.actions.find(a => a.name === form.action)?.exampleConfig}
                    </span>
                  )}
                </label>
                <textarea
                  className="w-full bg-slate-800 border border-slate-700 rounded-lg px-4 py-2.5 text-white font-mono text-sm focus:outline-none focus:border-violet-500 h-20"
                  value={form.actionConfigJson}
                  onChange={e => setForm({ ...form, actionConfigJson: e.target.value })}
                />
              </div>
              {error && <p className="text-red-400 text-sm">{error}</p>}
            </div>
            <div className="p-6 border-t border-slate-700 flex justify-end gap-3">
              <button onClick={() => setShowBuilder(false)} className="px-4 py-2 text-slate-400 hover:text-white transition-colors">Cancel</button>
              <button
                onClick={saveRule}
                disabled={saving}
                className="px-5 py-2.5 bg-gradient-to-r from-violet-600 to-indigo-600 text-white rounded-xl font-medium hover:from-violet-500 hover:to-indigo-500 transition-all disabled:opacity-50"
              >
                {saving ? 'Saving...' : 'Create Rule'}
              </button>
            </div>
          </div>
        </div>
      )}

      {/* Rules List */}
      {isLoading ? (
        <div className="flex justify-center py-16">
          <div className="w-8 h-8 rounded-full border-2 border-violet-500/30 border-t-violet-500 animate-spin" />
        </div>
      ) : rules.length === 0 ? (
        <div className="text-center py-16 text-slate-500">
          <div className="text-5xl mb-4">⚙️</div>
          <div className="text-lg">No automation rules yet</div>
          <div className="text-sm mt-1">Use a template above or create your first rule</div>
        </div>
      ) : (
        <div className="space-y-3">
          {rules.map(rule => (
            <div
              key={rule.id}
              className={`p-5 rounded-xl border transition-all ${rule.isEnabled ? 'bg-slate-900 border-slate-700' : 'bg-slate-900/50 border-slate-800 opacity-60'}`}
            >
              <div className="flex items-start justify-between gap-4">
                <div className="flex-1 min-w-0">
                  <div className="flex items-center gap-3 flex-wrap">
                    <span className="font-semibold text-white">{rule.name}</span>
                    {rule.isEnabled ? (
                      <span className="px-2 py-0.5 text-xs bg-emerald-500/20 text-emerald-400 rounded-full border border-emerald-500/30">Active</span>
                    ) : (
                      <span className="px-2 py-0.5 text-xs bg-slate-700 text-slate-400 rounded-full">Disabled</span>
                    )}
                  </div>
                  {rule.description && <p className="text-sm text-slate-400 mt-1">{rule.description}</p>}
                  <div className="flex gap-3 mt-3 flex-wrap text-xs">
                    <span className="px-2.5 py-1 bg-violet-900/40 text-violet-300 rounded-lg border border-violet-700/30">
                      {TRIGGER_ICONS[rule.trigger]} {rule.trigger}
                    </span>
                    <span className="text-slate-500">→</span>
                    <span className="px-2.5 py-1 bg-cyan-900/40 text-cyan-300 rounded-lg border border-cyan-700/30">
                      {ACTION_ICONS[rule.action]} {rule.action}
                    </span>
                    <span className="px-2.5 py-1 bg-slate-800 text-slate-400 rounded-lg">
                      Fired {rule.executionCount}x
                      {rule.lastFiredAtUtc && ` · Last: ${new Date(rule.lastFiredAtUtc).toLocaleDateString()}`}
                    </span>
                  </div>
                </div>
                <div className="flex items-center gap-3 shrink-0">
                  {/* Toggle */}
                  <button
                    onClick={() => toggleRule(rule.id)}
                    className={`relative w-10 h-5 rounded-full transition-colors ${rule.isEnabled ? 'bg-violet-600' : 'bg-slate-700'}`}
                  >
                    <div className={`absolute top-0.5 w-4 h-4 bg-white rounded-full shadow transition-transform ${rule.isEnabled ? 'translate-x-5' : 'translate-x-0.5'}`} />
                  </button>
                  <button
                    onClick={() => deleteRule(rule.id)}
                    className="text-slate-500 hover:text-red-400 transition-colors text-sm px-2 py-1 rounded"
                  >
                    🗑
                  </button>
                </div>
              </div>
            </div>
          ))}
        </div>
      )}
    </div>
  )
}
