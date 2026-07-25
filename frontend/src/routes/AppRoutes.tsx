import { lazy, Suspense } from 'react'
import { Routes, Route, Navigate } from 'react-router-dom'
import LoginPage from '../features/auth/LoginPage'
import ForgotPasswordPage from '../features/auth/ForgotPasswordPage'
import ResetPasswordPage from '../features/auth/ResetPasswordPage'
import VerifyEmailPage from '../features/auth/VerifyEmailPage'
import ChangePasswordPage from '../features/settings/ChangePasswordPage'
import PrivacyPage from '../features/settings/PrivacyPage'
import SessionsPage from '../features/settings/SessionsPage'
import JobListPage from '../features/jobs/JobListPage'
import CreateJobPage from '../features/jobs/CreateJobPage'
import CandidateProfilePage from '../features/candidate/CandidateProfilePage'
import MyApplicationsPage from '../features/applications/MyApplicationsPage'
import RankedCandidatesPage from '../features/recruiter/RankedCandidatesPage'
import TalentPoolPage from '../features/recruiter/TalentPoolPage'
import MySchedulePage from '../features/interviews/MySchedulePage'
import RecruiterDashboard from '../features/dashboard/RecruiterDashboard'
import CandidateDashboard from '../features/dashboard/CandidateDashboard'
import MyOffersPage from '../features/offers/MyOffersPage'
import ReportsPage from '../features/reports/ReportsPage'
import AdminDashboardPage from '../features/admin/AdminDashboardPage'
import AuditLogPage from '../features/admin/AuditLogPage'
import UserManagementPage from '../features/admin/UserManagementPage'
import BulkImportPage from '../features/admin/BulkImportPage'
import CompanySettingsPage from '../features/admin/CompanySettingsPage'
import WebhooksPage from '../features/webhooks/WebhooksPage'
import NotFoundPage from '../features/errors/NotFoundPage'
// Phase 5 — Enterprise features
import CareerPortalPage from '../features/careers/CareerPortalPage'
import RequisitionsPage from '../features/requisitions/RequisitionsPage'
import TalentCRMPage from '../features/crm/TalentCRMPage'
import OnboardingPage from '../features/onboarding/OnboardingPage'
// Phase 6 — Industry Dominance features
import AutomationRulesPage from '../features/settings/AutomationRulesPage'
import CandidateAssessmentPage from '../features/interviews/CandidateAssessmentPage'
import AssessmentBuilderPage from '../features/interviews/AssessmentBuilderPage'
import DashboardLayout from '../components/layout/DashboardLayout'
import ProtectedRoute from './ProtectedRoute'
import { ErrorBoundary } from '../components/ErrorBoundary'
import { useAppSelector } from '../app/hooks'

// Lazy-loaded pages
const PipelinePage = lazy(() => import('../features/applications/PipelinePage'))

function PageLoader() {
  return (
    <div className="flex items-center justify-center h-64">
      <div className="w-8 h-8 rounded-full border-2 border-indigo-500/30 border-t-indigo-500 animate-spin" />
    </div>
  )
}

function DashboardHome() {
  const role = useAppSelector((s) => s.auth.user?.role)
  if (role === 'Candidate') return <Navigate to="/my-applications" replace />
  if (role === 'SuperAdmin') return <AdminDashboardPage />
  return <RecruiterDashboard />
}


export default function AppRoutes() {
  return (
    <ErrorBoundary>
      <Routes>
        {/* Public routes — no auth */}
        <Route path="/login" element={<LoginPage />} />
        <Route path="/forgot-password" element={<ForgotPasswordPage />} />
        <Route path="/reset-password" element={<ResetPasswordPage />} />
        <Route path="/verify-email" element={<VerifyEmailPage />} />
        <Route path="/careers" element={<CareerPortalPage />} />

        {/* Protected routes */}
        <Route element={<ProtectedRoute />}>
          <Route element={<DashboardLayout />}>
            <Route path="/dashboard" element={<DashboardHome />} />
            <Route path="/jobs" element={<JobListPage />} />
            <Route path="/jobs/new" element={<CreateJobPage />} />
            <Route path="/pipeline" element={
              <Suspense fallback={<PageLoader />}>
                <PipelinePage />
              </Suspense>
            } />
            <Route path="/profile" element={<CandidateProfilePage />} />
            <Route path="/my-applications" element={<MyApplicationsPage />} />
            <Route path="/my-offers" element={<MyOffersPage />} />
            <Route path="/jobs/:jobId/candidates" element={<RankedCandidatesPage />} />
            <Route path="/talent-pool" element={<TalentPoolPage />} />
            <Route path="/my-schedule" element={<MySchedulePage />} />
            <Route path="/reports" element={<ReportsPage />} />
            <Route path="/admin/users" element={<UserManagementPage />} />
            <Route path="/admin/audit-log" element={<AuditLogPage />} />
            <Route path="/candidates/bulk-import" element={<BulkImportPage />} />
            <Route path="/admin/company-settings" element={<CompanySettingsPage />} />
            <Route path="/webhooks" element={<WebhooksPage />} />
            <Route path="/settings/change-password" element={<ChangePasswordPage />} />
            <Route path="/settings/privacy" element={<PrivacyPage />} />
            <Route path="/settings/sessions" element={<SessionsPage />} />
            {/* Phase 5 enterprise routes */}
            <Route path="/requisitions" element={<RequisitionsPage />} />
            <Route path="/talent-crm" element={<TalentCRMPage />} />
            <Route path="/onboarding" element={<OnboardingPage />} />
            {/* Phase 6: Industry Dominance routes */}
            <Route path="/settings/automations" element={<AutomationRulesPage />} />
            <Route path="/my-assessments" element={<CandidateAssessmentPage />} />
            <Route path="/assessments/builder" element={<AssessmentBuilderPage />} />
          </Route>
        </Route>
        <Route path="/" element={<Navigate to="/dashboard" replace />} />
        <Route path="*" element={<NotFoundPage />} />
      </Routes>
    </ErrorBoundary>
  )
}
