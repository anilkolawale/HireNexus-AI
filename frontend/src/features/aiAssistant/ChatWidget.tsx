import { useEffect, useRef, useState } from 'react'
import { motion, AnimatePresence } from 'framer-motion'
import { X, Send, Bot, Sparkles, ChevronRight, MessageSquare } from 'lucide-react'
import { aiAssistantApi, type ChatMessage } from '../../api/endpoints/aiAssistant.api'

const SUGGESTIONS = [
  'Find top React devs',
  'Compare finalists',
  'Draft offer letter',
  'Weekly hiring summary',
]

/* ─── Typing indicator ─────────────────────────────────── */
function TypingIndicator() {
  return (
    <div className="flex items-end gap-2 px-1">
      <div className="w-7 h-7 rounded-full bg-indigo-500/20 border border-indigo-500/30 flex items-center justify-center shrink-0">
        <Bot className="w-3.5 h-3.5 text-indigo-400" />
      </div>
      <div className="flex items-center gap-1 bg-white/[0.06] border border-white/10 rounded-2xl rounded-bl-sm px-4 py-3">
        {[0, 0.15, 0.3].map((delay, i) => (
          <motion.div
            key={i}
            className="w-1.5 h-1.5 rounded-full bg-indigo-400"
            animate={{ y: [0, -5, 0], opacity: [0.4, 1, 0.4] }}
            transition={{ duration: 0.8, delay, repeat: Infinity, ease: 'easeInOut' }}
          />
        ))}
      </div>
    </div>
  )
}

/* ─── Message bubble ───────────────────────────────────── */
function MessageBubble({ msg }: { msg: ChatMessage }) {
  const isUser = msg.role === 'user'
  return (
    <motion.div
      initial={{ opacity: 0, y: 10 }}
      animate={{ opacity: 1, y: 0 }}
      className={`flex items-end gap-2 ${isUser ? 'flex-row-reverse' : ''}`}
    >
      {!isUser && (
        <div className="w-7 h-7 rounded-full bg-indigo-500/20 border border-indigo-500/30 flex items-center justify-center shrink-0">
          <Bot className="w-3.5 h-3.5 text-indigo-400" />
        </div>
      )}
      <div
        className={`max-w-[80%] text-xs leading-relaxed whitespace-pre-wrap rounded-2xl px-4 py-3 ${
          isUser
            ? 'bg-gradient-to-br from-indigo-600 to-indigo-500 text-white rounded-br-sm shadow-lg shadow-indigo-500/20'
            : 'bg-white/[0.06] border border-white/10 text-white/80 rounded-bl-sm'
        }`}
      >
        {msg.content}
      </div>
    </motion.div>
  )
}

/* ─── Chat Panel ───────────────────────────────────────── */
interface ChatWidgetProps {
  open: boolean
  onClose: () => void
  onNewMessage?: () => void
}

export function ChatWidget({ open, onClose, onNewMessage }: ChatWidgetProps) {
  const [messages, setMessages] = useState<ChatMessage[]>([])
  const [input, setInput] = useState('')
  const [sending, setSending] = useState(false)
  const scrollRef = useRef<HTMLDivElement>(null)
  const inputRef = useRef<HTMLInputElement>(null)

  useEffect(() => {
    if (open) {
      setTimeout(() => inputRef.current?.focus(), 300)
    }
  }, [open])

  useEffect(() => {
    scrollRef.current?.scrollTo({ top: scrollRef.current.scrollHeight, behavior: 'smooth' })
  }, [messages, sending])

  const send = async (text: string) => {
    if (!text.trim() || sending) return
    const userMsg: ChatMessage = { role: 'user', content: text }
    const nextMessages = [...messages, userMsg]
    setMessages(nextMessages)
    setInput('')
    setSending(true)
    try {
      const { reply } = await aiAssistantApi.chat(text, messages)
      setMessages([...nextMessages, { role: 'assistant', content: reply }])
      onNewMessage?.()
    } catch {
      setMessages([...nextMessages, { role: 'assistant', content: "Sorry, I couldn't process that. Please try again." }])
    } finally {
      setSending(false)
    }
  }

  const handleKeyDown = (e: React.KeyboardEvent<HTMLInputElement>) => {
    if ((e.key === 'Enter' && (e.ctrlKey || e.metaKey)) || (e.key === 'Enter' && !e.shiftKey)) {
      e.preventDefault()
      send(input)
    }
  }

  return (
    <AnimatePresence>
      {open && (
        <>
          {/* Backdrop */}
          <motion.div
            initial={{ opacity: 0 }}
            animate={{ opacity: 1 }}
            exit={{ opacity: 0 }}
            className="fixed inset-0 bg-black/20 backdrop-blur-[2px] z-40"
            onClick={onClose}
          />

          {/* Panel */}
          <motion.div
            initial={{ x: 440, opacity: 0 }}
            animate={{ x: 0, opacity: 1 }}
            exit={{ x: 440, opacity: 0 }}
            transition={{ type: 'spring', stiffness: 300, damping: 30 }}
            className="fixed top-16 right-0 bottom-0 w-[420px] z-50 flex flex-col overflow-hidden"
            style={{
              background: 'linear-gradient(135deg, rgba(10,15,30,0.97) 0%, rgba(15,20,45,0.97) 100%)',
              borderLeft: '1px solid rgba(99,102,241,0.15)',
              backdropFilter: 'blur(24px)',
            }}
          >
            {/* Header */}
            <div className="flex items-center justify-between px-5 py-4 border-b border-white/5 bg-gradient-to-r from-indigo-600/10 to-purple-600/5 shrink-0">
              <div className="flex items-center gap-3">
                <div className="w-9 h-9 rounded-xl bg-gradient-to-br from-indigo-500 to-purple-600 flex items-center justify-center shadow-lg shadow-indigo-500/30">
                  <Bot className="w-5 h-5 text-white" />
                </div>
                <div>
                  <p className="text-sm font-bold text-white">🤖 AI Hiring Copilot</p>
                  <span className="text-[10px] bg-gradient-to-r from-indigo-500 to-purple-500 text-white px-2 py-0.5 rounded-full font-semibold">
                    ✨ Powered by Gemini
                  </span>
                </div>
              </div>
              <button
                onClick={onClose}
                className="w-8 h-8 rounded-xl bg-white/5 hover:bg-white/10 flex items-center justify-center transition-colors"
              >
                <X className="w-4 h-4 text-white/60" />
              </button>
            </div>

            {/* Suggestion chips */}
            {messages.length === 0 && (
              <div className="px-4 py-3 border-b border-white/5 shrink-0">
                <p className="text-[10px] text-white/30 uppercase tracking-wider mb-2 font-semibold">Quick Actions</p>
                <div className="flex gap-2 overflow-x-auto pb-1 scrollbar-hide">
                  {SUGGESTIONS.map((s) => (
                    <button
                      key={s}
                      onClick={() => send(s)}
                      className="shrink-0 flex items-center gap-1 text-[11px] text-indigo-300 bg-indigo-500/10 border border-indigo-500/20 rounded-full px-3 py-1.5 hover:bg-indigo-500/20 transition-colors whitespace-nowrap"
                    >
                      <Sparkles className="w-3 h-3" />
                      {s}
                    </button>
                  ))}
                </div>
              </div>
            )}

            {/* Messages */}
            <div ref={scrollRef} className="flex-1 overflow-y-auto px-4 py-4 space-y-3 scrollbar-hide">
              {messages.length === 0 && (
                <div className="flex flex-col items-center justify-center h-full gap-4 py-8">
                  <div className="w-16 h-16 rounded-2xl bg-indigo-500/10 flex items-center justify-center">
                    <Bot className="w-8 h-8 text-indigo-400/60" />
                  </div>
                  <div className="text-center">
                    <p className="text-white/50 text-sm font-medium">Hi! I'm your AI Hiring Copilot</p>
                    <p className="text-white/25 text-xs mt-1">Ask me about candidates, jobs, or hiring strategies</p>
                  </div>
                  <div className="space-y-2 w-full">
                    {SUGGESTIONS.map((s) => (
                      <button
                        key={s}
                        onClick={() => send(s)}
                        className="w-full flex items-center justify-between text-sm text-white/40 bg-white/5 hover:bg-white/10 rounded-xl px-4 py-2.5 transition-colors"
                      >
                        <span>{s}</span>
                        <ChevronRight className="w-4 h-4 text-white/20" />
                      </button>
                    ))}
                  </div>
                </div>
              )}

              {messages.map((m, i) => (
                <MessageBubble key={i} msg={m} />
              ))}

              {sending && <TypingIndicator />}
            </div>

            {/* Input area */}
            <div className="px-4 py-4 border-t border-white/5 shrink-0 bg-gradient-to-t from-black/20 to-transparent">
              <div className="flex items-center gap-2 bg-white/5 border border-white/10 rounded-xl px-3 py-2 focus-within:border-indigo-500/50 focus-within:ring-1 focus-within:ring-indigo-500/30 transition-all">
                <input
                  ref={inputRef}
                  value={input}
                  onChange={(e) => setInput(e.target.value)}
                  onKeyDown={handleKeyDown}
                  placeholder="Ask about hiring…"
                  className="flex-1 bg-transparent text-sm text-white placeholder-white/25 focus:outline-none"
                  disabled={sending}
                />
                <div className="flex items-center gap-2 shrink-0">
                  <span className="text-[10px] text-white/20 hidden sm:block">Ctrl+Enter</span>
                  <button
                    onClick={() => send(input)}
                    disabled={sending || !input.trim()}
                    className="w-7 h-7 rounded-lg bg-indigo-600 hover:bg-indigo-500 disabled:opacity-40 flex items-center justify-center transition-all"
                  >
                    <Send className="w-3.5 h-3.5 text-white" />
                  </button>
                </div>
              </div>
            </div>
          </motion.div>
        </>
      )}
    </AnimatePresence>
  )
}

/* ─── Toggle Button for Topbar ─────────────────────────── */
interface ChatToggleButtonProps {
  onClick: () => void
  hasUnread?: boolean
}

export function ChatToggleButton({ onClick, hasUnread }: ChatToggleButtonProps) {
  return (
    <motion.button
      whileHover={{ scale: 1.05 }}
      whileTap={{ scale: 0.95 }}
      onClick={onClick}
      className="relative btn-ghost p-2 rounded-xl flex items-center gap-1.5"
      aria-label="Open AI Copilot"
      title="AI Hiring Copilot"
    >
      <div className="relative">
        <MessageSquare className="w-4 h-4" />
        <AnimatePresence>
          {hasUnread && (
            <motion.div
              initial={{ scale: 0 }}
              animate={{ scale: 1 }}
              exit={{ scale: 0 }}
              className="absolute -top-1 -right-1 w-2 h-2 rounded-full bg-indigo-400 shadow-lg shadow-indigo-400/50"
            />
          )}
        </AnimatePresence>
      </div>
    </motion.button>
  )
}

/* ─── Legacy default export (kept for backwards compat) ── */
export default ChatWidget
