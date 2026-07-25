import axiosClient from '../axiosClient'

export const authApi = {
  logout: (refreshToken: string) => axiosClient.post('/auth/logout', { refreshToken }),

  forgotPassword: (email: string) => axiosClient.post('/auth/forgot-password', { email }),

  resetPassword: (token: string, newPassword: string) =>
    axiosClient.post('/auth/reset-password', { token, newPassword }),

  changePassword: (currentPassword: string, newPassword: string) =>
    axiosClient.post('/auth/change-password', { currentPassword, newPassword }),

  verifyEmail: (token: string) => axiosClient.post('/auth/verify-email', { token }),

  resendVerification: () => axiosClient.post('/auth/resend-verification')
}
