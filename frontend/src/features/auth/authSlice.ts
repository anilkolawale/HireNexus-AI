import { createSlice, type PayloadAction } from '@reduxjs/toolkit'

export interface UserInfo {
  id: string
  firstName: string
  lastName: string
  email: string
  role: string
  isEmailVerified: boolean
  companyId?: string | null
}

interface AuthState {
  accessToken: string | null
  refreshToken: string | null
  user: UserInfo | null
}

const initialState: AuthState = {
  accessToken: localStorage.getItem('accessToken'),
  refreshToken: localStorage.getItem('refreshToken'),
  user: JSON.parse(localStorage.getItem('user') || 'null')
}

const authSlice = createSlice({
  name: 'auth',
  initialState,
  reducers: {
    setCredentials: (state, action: PayloadAction<{ accessToken: string; refreshToken: string; user: UserInfo }>) => {
      state.accessToken = action.payload.accessToken
      state.refreshToken = action.payload.refreshToken
      state.user = action.payload.user
      localStorage.setItem('accessToken', action.payload.accessToken)
      localStorage.setItem('refreshToken', action.payload.refreshToken)
      localStorage.setItem('user', JSON.stringify(action.payload.user))
    },
    markEmailVerified: (state) => {
      if (state.user) {
        state.user.isEmailVerified = true
        localStorage.setItem('user', JSON.stringify(state.user))
      }
    },
    logout: (state) => {
      state.accessToken = null
      state.refreshToken = null
      state.user = null
      localStorage.clear()
    }
  }
})

export const { setCredentials, markEmailVerified, logout } = authSlice.actions
export default authSlice.reducer
