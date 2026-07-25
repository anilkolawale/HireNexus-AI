import type { ReactElement, ReactNode } from 'react'
import { render } from '@testing-library/react'
import { Provider } from 'react-redux'
import { configureStore } from '@reduxjs/toolkit'
import { MemoryRouter } from 'react-router-dom'
import authReducer from '../features/auth/authSlice'
import jobsReducer from '../features/jobs/jobsSlice'
import type { RootState } from '../app/store'

// Renders a component wrapped in a real (but test-scoped) Redux store and router, so
// components using useAppSelector/useAppDispatch/useNavigate work without mocking every hook.
// Pass `preloadedState` to seed auth/jobs state (e.g. a logged-in user) for a given test.
export function renderWithProviders(
  ui: ReactElement,
  {
    preloadedState,
    route = '/'
  }: { preloadedState?: Partial<RootState>; route?: string } = {}
) {
  const store = configureStore({
    reducer: { auth: authReducer, jobs: jobsReducer } as any,
    preloadedState: preloadedState as any
  })

  function Wrapper({ children }: { children: ReactNode }) {
    return (
      <Provider store={store}>
        <MemoryRouter initialEntries={[route]}>{children}</MemoryRouter>
      </Provider>
    )
  }

  return { store, ...render(ui, { wrapper: Wrapper }) }
}
