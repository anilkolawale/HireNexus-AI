import axios from 'axios'

// Separate axios instance for public endpoints — no auth interceptor
export const publicAxios = axios.create({
  baseURL: import.meta.env.VITE_API_URL || 'https://localhost:7001/api',
  timeout: 10000,
})
