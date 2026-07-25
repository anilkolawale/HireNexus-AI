import { describe, it, expect, vi, beforeEach } from 'vitest'
import { screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { renderWithProviders } from '../../../test/testUtils'
import LoginPage from '../LoginPage'
import axiosClient from '../../../api/axiosClient'

// Mock the shared axios instance so no real HTTP call happens; each test configures
// what post() resolves/rejects with.
vi.mock('../../../api/axiosClient', () => ({
  default: { post: vi.fn(), get: vi.fn(), interceptors: { request: { use: vi.fn() }, response: { use: vi.fn() } } }
}))

describe('LoginPage', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    localStorage.clear()
  })

  it('renders email, password fields and a submit button', () => {
    renderWithProviders(<LoginPage />)

    expect(screen.getByLabelText('Email address')).toBeInTheDocument()
    expect(screen.getByLabelText('Password')).toBeInTheDocument()
    expect(screen.getByRole('button', { name: /sign in/i })).toBeInTheDocument()
  })

  it('submits credentials and stores them in the auth store on success', async () => {
    const user = userEvent.setup()
    const mockResponse = {
      data: {
        accessToken: 'access-123',
        refreshToken: 'refresh-456',
        user: { id: 'u1', firstName: 'Jane', lastName: 'Doe', email: 'jane@ats.local', role: 'Recruiter', isEmailVerified: true }
      }
    }
    vi.mocked(axiosClient.post).mockResolvedValueOnce(mockResponse)

    const { store } = renderWithProviders(<LoginPage />)

    await user.type(screen.getByLabelText('Email address'), 'jane@ats.local')
    await user.type(screen.getByLabelText('Password'), 'Password123!')
    await user.click(screen.getByRole('button', { name: /sign in/i }))

    await waitFor(() => {
      expect(store.getState().auth.accessToken).toBe('access-123')
    })
    expect(axiosClient.post).toHaveBeenCalledWith('/auth/login', {
      email: 'jane@ats.local',
      password: 'Password123!'
    })
  })

  it('shows a disabled "Signing in..." state while the request is in flight', async () => {
    const user = userEvent.setup()
    let resolveRequest: (value: any) => void = () => {}
    vi.mocked(axiosClient.post).mockReturnValueOnce(
      new Promise((resolve) => { resolveRequest = resolve })
    )

    renderWithProviders(<LoginPage />)

    await user.type(screen.getByLabelText('Email address'), 'jane@ats.local')
    await user.type(screen.getByLabelText('Password'), 'Password123!')
    await user.click(screen.getByRole('button', { name: /sign in/i }))

    expect(screen.getByRole('button', { name: /signing in/i })).toBeDisabled()

    resolveRequest({ data: { accessToken: 'a', refreshToken: 'r', user: {} } })
  })

  it('does not store credentials when the login request fails', async () => {
    const user = userEvent.setup()
    vi.mocked(axiosClient.post).mockRejectedValueOnce({
      response: { data: { message: 'Invalid email or password.' } }
    })

    const { store } = renderWithProviders(<LoginPage />)

    await user.type(screen.getByLabelText('Email address'), 'jane@ats.local')
    await user.type(screen.getByLabelText('Password'), 'wrong-password')
    await user.click(screen.getByRole('button', { name: /sign in/i }))

    await waitFor(() => {
      expect(screen.getByRole('button', { name: /sign in/i })).not.toBeDisabled()
    })
    expect(store.getState().auth.accessToken).toBeNull()
  })
})
