import { describe, it, expect } from 'vitest'
import { screen } from '@testing-library/react'
import { Routes, Route } from 'react-router-dom'
import { renderWithProviders } from '../../test/testUtils'
import ProtectedRoute from '../ProtectedRoute'

function TestApp() {
  return (
    <Routes>
      <Route path="/login" element={<div>Login Page</div>} />
      <Route element={<ProtectedRoute />}>
        <Route path="/dashboard" element={<div>Dashboard Page</div>} />
      </Route>
    </Routes>
  )
}

describe('ProtectedRoute', () => {
  it('redirects to /login when there is no access token', () => {
    renderWithProviders(<TestApp />, {
      route: '/dashboard',
      preloadedState: { auth: { accessToken: null, refreshToken: null, user: null } }
    })

    expect(screen.getByText('Login Page')).toBeInTheDocument()
    expect(screen.queryByText('Dashboard Page')).not.toBeInTheDocument()
  })

  it('renders the protected content when an access token is present', () => {
    renderWithProviders(<TestApp />, {
      route: '/dashboard',
      preloadedState: {
        auth: {
          accessToken: 'valid-token',
          refreshToken: 'refresh-token',
          user: { id: 'u1', firstName: 'Jane', lastName: 'Doe', email: 'jane@ats.local', role: 'Recruiter', isEmailVerified: true }
        }
      }
    })

    expect(screen.getByText('Dashboard Page')).toBeInTheDocument()
    expect(screen.queryByText('Login Page')).not.toBeInTheDocument()
  })
})
