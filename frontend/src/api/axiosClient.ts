import axios from 'axios'
import { store } from '../app/store'
import { logout, setCredentials } from '../features/auth/authSlice'

const axiosClient = axios.create({
  baseURL: import.meta.env.VITE_API_BASE_URL || 'http://localhost:5000/api'
})

axiosClient.interceptors.request.use((config) => {
  const token = store.getState().auth.accessToken
  if (token) config.headers.Authorization = `Bearer ${token}`
  return config
})

let isRefreshing = false

axiosClient.interceptors.response.use(
  (response) => response,
  async (error) => {
    const originalRequest = error.config
    if (error.response?.status === 401 && !originalRequest._retry && !isRefreshing) {
      originalRequest._retry = true
      isRefreshing = true
      try {
        const refreshToken = store.getState().auth.refreshToken
        const { data } = await axios.post(
          `${axiosClient.defaults.baseURL}/auth/refresh-token`,
          { refreshToken }
        )
        store.dispatch(setCredentials(data))
        originalRequest.headers.Authorization = `Bearer ${data.accessToken}`
        return axiosClient(originalRequest)
      } catch {
        store.dispatch(logout())
      } finally {
        isRefreshing = false
      }
    }
    return Promise.reject(error)
  }
)

export default axiosClient
