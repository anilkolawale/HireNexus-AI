import '@testing-library/jest-dom/vitest'
import { afterEach } from 'vitest'
import { cleanup } from '@testing-library/react'

// Runs after each test file's tests to unmount rendered components and avoid leaks
// between tests (React Testing Library doesn't do this automatically outside of Jest).
afterEach(() => {
  cleanup()
})
