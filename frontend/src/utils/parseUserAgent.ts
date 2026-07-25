// Lightweight, best-effort UA parsing for display purposes only ("Chrome on Windows") —
// not a substitute for a real UA-parsing library if you need this to be authoritative.
export function parseUserAgent(ua?: string): string {
  if (!ua) return 'Unknown device'

  const browser =
    ua.includes('Edg/') ? 'Edge' :
    ua.includes('Chrome/') ? 'Chrome' :
    ua.includes('Firefox/') ? 'Firefox' :
    ua.includes('Safari/') && !ua.includes('Chrome') ? 'Safari' :
    'Unknown browser'

  const os =
    ua.includes('Windows') ? 'Windows' :
    ua.includes('Mac OS') ? 'macOS' :
    ua.includes('Linux') && !ua.includes('Android') ? 'Linux' :
    ua.includes('Android') ? 'Android' :
    ua.includes('iPhone') || ua.includes('iPad') ? 'iOS' :
    'Unknown OS'

  return `${browser} on ${os}`
}
