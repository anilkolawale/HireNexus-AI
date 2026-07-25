import { useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { motion } from 'framer-motion'
import {
  CheckSquare, Plus, ChevronDown, ChevronRight,
  Loader2, Check, User, Monitor, Briefcase, UserCheck, X
} from 'lucide-react'
import axiosClient from '../../api/axiosClient'
import toast from 'react-hot-toast'

const ASSIGNEE_CONFIG: Record<string, { label: string; icon: React.ElementType; color: string }> = {
  HR:      { label: 'HR',         icon: User,       color: 'text-indigo-400 bg-indigo-500/10' },
  IT:      { label: 'IT',         icon: Monitor,    color: 'text-cyan-400 bg-cyan-500/10' },
  Manager: { label: 'Manager',    icon: Briefcase,  color: 'text-amber-400 bg-amber-500/10' },
  NewHire: { label: 'New Hire',   icon: UserCheck,  color: 'text-emerald-400 bg-emerald-500/10' },
}

const STATUS_CONFIG = {
  NotStarted: { label: 'Not Started', color: 'text-slate-400', bar: 'bg-slate-500' },
  InProgress:  { label: 'In Progress',  color: 'text-amber-400',  bar: 'bg-amber-500' },
  Completed:   { label: 'Completed',    color: 'text-emerald-400', bar: 'bg-emerald-500' },
}

interface OnboardingChecklist {
  id: string; status: string; startDate?: string; createdAtUtc: string
  totalTasks: number; completedTasks: number
  candidate: { name: string; email: string }; job: { title: string }
}

interface ChecklistDetail {
  id: string; status: string; startDate?: string
  candidate: { name: string }
  tasks: Array<{
    id: string; title: string; description?: string; assignedTo: string
    dueDate?: string; isCompleted: boolean; completedAtUtc?: string; order: number
  }>
}

export default function OnboardingPage() {
  const qc = useQueryClient()
  const [selected, setSelected] = useState<string | null>(null)
  const [showAddTask, setShowAddTask] = useState(false)
  const [newTask, setNewTask] = useState({ title: '', description: '', assignedTo: 'HR', dueDate: '' })

  const { data: checklists = [], isLoading } = useQuery<OnboardingChecklist[]>({
    queryKey: ['onboarding'],
    queryFn: () => axiosClient.get('/onboarding').then(r => r.data),
    staleTime: 1000 * 60,
  })

  const { data: detail } = useQuery<ChecklistDetail>({
    queryKey: ['onboarding', selected],
    queryFn: () => axiosClient.get(`/onboarding/${selected}`).then(r => r.data),
    enabled: !!selected,
  })

  const completeMutation = useMutation({
    mutationFn: (taskId: string) => axiosClient.patch(`/onboarding/tasks/${taskId}/complete`),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['onboarding', selected] }),
  })

  const addTaskMutation = useMutation({
    mutationFn: () => axiosClient.post(`/onboarding/${selected}/tasks`, {
      ...newTask, dueDate: newTask.dueDate || undefined
    }),
    onSuccess: () => {
      toast.success('Task added!')
      qc.invalidateQueries({ queryKey: ['onboarding', selected] })
      setNewTask({ title: '', description: '', assignedTo: 'HR', dueDate: '' })
      setShowAddTask(false)
    },
    onError: () => toast.error('Failed to add task'),
  })

  const selectedChecklist = checklists.find(c => c.id === selected)

  return (
    <div className="min-h-full p-6">
      <div className="flex items-center justify-between mb-6">
        <div>
          <h1 className="text-2xl font-black text-white">Onboarding</h1>
          <p className="text-white/40 text-sm mt-0.5">Manage new hire onboarding checklists</p>
        </div>
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-5 gap-6 h-[calc(100vh-200px)]">
        {/* Left: checklist list */}
        <div className="lg:col-span-2 space-y-3 overflow-y-auto pr-1">
          {isLoading ? (
            <div className="flex items-center justify-center h-32"><Loader2 className="w-5 h-5 text-indigo-400 animate-spin" /></div>
          ) : checklists.length === 0 ? (
            <div className="text-center py-16">
              <CheckSquare className="w-10 h-10 text-white/20 mx-auto mb-3" />
              <p className="text-white/30 text-sm">No onboarding checklists yet.</p>
              <p className="text-white/20 text-xs mt-1">Checklists are created when a candidate is hired.</p>
            </div>
          ) : (
            checklists.map(cl => {
              const progress = cl.totalTasks > 0 ? (cl.completedTasks / cl.totalTasks) * 100 : 0
              const sc = STATUS_CONFIG[cl.status as keyof typeof STATUS_CONFIG] ?? STATUS_CONFIG.NotStarted
              return (
                <motion.button key={cl.id}
                  onClick={() => setSelected(cl.id === selected ? null : cl.id)}
                  className={`w-full text-left bg-white/[0.04] border rounded-2xl p-4 transition-all ${selected === cl.id ? 'border-indigo-500/40' : 'border-white/[0.08] hover:border-white/20'}`}
                >
                  <div className="flex items-start justify-between gap-2 mb-2">
                    <div>
                      <p className="text-white font-semibold text-sm">{cl.candidate.name}</p>
                      <p className="text-white/40 text-xs">{cl.job.title}</p>
                    </div>
                    <span className={`text-xs font-medium ${sc.color}`}>{sc.label}</span>
                  </div>
                  <div className="flex items-center gap-2">
                    <div className="flex-1 h-1.5 bg-white/10 rounded-full overflow-hidden">
                      <motion.div initial={{ width: 0 }} animate={{ width: `${progress}%` }} transition={{ duration: 0.6 }}
                        className={`h-full rounded-full ${sc.bar}`} />
                    </div>
                    <span className="text-white/30 text-xs flex-shrink-0">{cl.completedTasks}/{cl.totalTasks}</span>
                  </div>
                </motion.button>
              )
            })
          )}
        </div>

        {/* Right: task detail */}
        <div className="lg:col-span-3 bg-white/[0.04] border border-white/[0.08] rounded-2xl overflow-hidden">
          {!selected ? (
            <div className="flex flex-col items-center justify-center h-full text-center p-8">
              <CheckSquare className="w-12 h-12 text-white/10 mb-3" />
              <p className="text-white/30 text-sm">Select a checklist to view tasks</p>
            </div>
          ) : !detail ? (
            <div className="flex items-center justify-center h-full">
              <Loader2 className="w-6 h-6 text-indigo-400 animate-spin" />
            </div>
          ) : (
            <div className="flex flex-col h-full">
              {/* Header */}
              <div className="p-5 border-b border-white/[0.06]">
                <p className="text-white font-bold text-lg">{detail.candidate.name}</p>
                <p className="text-white/40 text-sm">
                  {detail.startDate ? `Start date: ${new Date(detail.startDate).toLocaleDateString()}` : 'Start date not set'}
                </p>
              </div>

              {/* Tasks */}
              <div className="flex-1 overflow-y-auto p-5 space-y-2">
                {detail.tasks.sort((a, b) => a.order - b.order).map(task => {
                  const ac = ASSIGNEE_CONFIG[task.assignedTo] ?? ASSIGNEE_CONFIG.HR
                  return (
                    <motion.div key={task.id} layout
                      className={`flex items-start gap-3 p-3 rounded-xl transition-colors ${task.isCompleted ? 'bg-emerald-500/5 border border-emerald-500/10' : 'bg-white/[0.03] border border-white/[0.06]'}`}
                    >
                      <button
                        onClick={() => completeMutation.mutate(task.id)}
                        disabled={completeMutation.isPending}
                        className={`flex-shrink-0 mt-0.5 w-5 h-5 rounded-full border flex items-center justify-center transition-all ${
                          task.isCompleted
                            ? 'border-emerald-500 bg-emerald-500'
                            : 'border-white/20 hover:border-emerald-500/50'
                        }`}
                      >
                        {task.isCompleted && <Check className="w-3 h-3 text-white" />}
                      </button>

                      <div className="flex-1 min-w-0">
                        <p className={`text-sm font-medium ${task.isCompleted ? 'line-through text-white/30' : 'text-white'}`}>
                          {task.title}
                        </p>
                        {task.description && (
                          <p className="text-white/30 text-xs mt-0.5">{task.description}</p>
                        )}
                        <div className="flex items-center gap-3 mt-1.5">
                          <span className={`inline-flex items-center gap-1 text-xs px-2 py-0.5 rounded-full ${ac.color}`}>
                            <ac.icon className="w-2.5 h-2.5" /> {ac.label}
                          </span>
                          {task.dueDate && (
                            <span className="text-white/20 text-xs">Due {new Date(task.dueDate).toLocaleDateString()}</span>
                          )}
                        </div>
                      </div>
                    </motion.div>
                  )
                })}
              </div>

              {/* Add task */}
              <div className="p-5 border-t border-white/[0.06]">
                {!showAddTask ? (
                  <button onClick={() => setShowAddTask(true)}
                    className="flex items-center gap-2 text-white/30 hover:text-indigo-400 text-sm transition-colors">
                    <Plus className="w-4 h-4" /> Add custom task
                  </button>
                ) : (
                  <div className="space-y-2">
                    <input value={newTask.title} onChange={e => setNewTask(t => ({ ...t, title: e.target.value }))}
                      placeholder="Task title *"
                      className="w-full bg-white/5 border border-white/10 text-white placeholder-white/30 px-3 py-2 rounded-xl text-sm outline-none" />
                    <div className="grid grid-cols-2 gap-2">
                      <select value={newTask.assignedTo} onChange={e => setNewTask(t => ({ ...t, assignedTo: e.target.value }))}
                        className="bg-white/5 border border-white/10 text-white px-3 py-2 rounded-xl text-sm outline-none">
                        {Object.keys(ASSIGNEE_CONFIG).map(k => <option key={k} className="bg-[#0f1629]">{k}</option>)}
                      </select>
                      <input type="date" value={newTask.dueDate} onChange={e => setNewTask(t => ({ ...t, dueDate: e.target.value }))}
                        className="bg-white/5 border border-white/10 text-white px-3 py-2 rounded-xl text-sm outline-none" />
                    </div>
                    <div className="flex gap-2">
                      <button onClick={() => addTaskMutation.mutate()} disabled={!newTask.title || addTaskMutation.isPending}
                        className="flex-1 bg-indigo-500/20 hover:bg-indigo-500/30 border border-indigo-500/30 text-indigo-400 font-medium py-2 rounded-xl text-sm transition-colors disabled:opacity-50">
                        {addTaskMutation.isPending ? <Loader2 className="w-4 h-4 animate-spin mx-auto" /> : 'Add Task'}
                      </button>
                      <button onClick={() => setShowAddTask(false)} className="bg-white/5 text-white/40 hover:text-white/70 px-4 py-2 rounded-xl text-sm transition-colors">
                        Cancel
                      </button>
                    </div>
                  </div>
                )}
              </div>
            </div>
          )}
        </div>
      </div>
    </div>
  )
}
