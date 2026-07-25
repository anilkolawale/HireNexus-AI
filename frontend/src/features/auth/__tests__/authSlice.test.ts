import { describe, it, expect, beforeEach } from 'vitest'
import authReducer, { setCredentials, logout, type UserInfo } from '../authSlice'

const mockUser: UserInfo = {
  id: 'user-1',
  firstName: 'Jane',
  lastName: 'Doe',
  email: 'jane@ats.local',
  role: 'Recruiter',
  isEmailVerified: true
}

describe('authSlice', () => {
  beforeEach(() => {
    localStorage.clear()
  })

  it('has null token/user in the initial state when localStorage is empty', () => {
    const state = authReducer(undefined, { type: '@@INIT' })
    expect(state.accessToken).toBeNull()
    expect(state.refreshToken).toBeNull()
    expect(state.user).toBeNull()
  })

  it('setCredentials stores the token and user, and persists them to localStorage', () => {
    const state = authReducer(undefined, setCredentials({
      accessToken: 'access-123',
      refreshToken: 'refresh-456',
      user: mockUser
    }))

    expect(state.accessToken).toBe('access-123')
    expect(state.refreshToken).toBe('refresh-456')
    expect(state.user).toEqual(mockUser)
    expect(localStorage.getItem('accessToken')).toBe('access-123')
    expect(JSON.parse(localStorage.getItem('user')!)).toEqual(mockUser)
  })

  it('logout clears state and localStorage', () => {
    const loggedInState = authReducer(undefined, setCredentials({
      accessToken: 'access-123',
      refreshToken: 'refresh-456',
      user: mockUser
    }))

    const state = authReducer(loggedInState, logout())

    expect(state.accessToken).toBeNull()
    expect(state.refreshToken).toBeNull()
    expect(state.user).toBeNull()
    expect(localStorage.getItem('accessToken')).toBeNull()
  })
})
