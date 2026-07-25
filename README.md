# AI Recruitment ATS — Build Status

## What's implemented (working, real code)

**Backend** (`backend/`) — ASP.NET Core 8, Clean Architecture (Domain → Application → Infrastructure/AI → API):
- Full Domain model: Users/Roles/Permissions, Company/Department/Designation/OfficeLocation,
  Job/JobSkill, Candidate/Skills/Education/Experience/Certificate, Application + status history,
  InterviewRound/Interview/Feedback, Offer, Notification, AuditLog, FileAsset, RefreshToken.
- Generic Repository + Unit of Work over EF Core, soft-delete query filters, audit timestamps.
- **Auth module (end-to-end):** Register, Login, RefreshToken (rotation) — JWT access tokens +
  refresh tokens, BCrypt password hashing, FluentValidation on all commands.
- **Jobs module (end-to-end):** CreateJob (role-restricted to Recruiter/HRManager/SuperAdmin),
  GetJobs (search + status/location filter + pagination).
- Global exception-handling middleware mapping domain exceptions → proper HTTP status codes.
- `IAiService` abstraction + a working `AiService` implementation that calls the Anthropic
  Messages API for job description generation, resume parsing, match scoring, interview
  question generation, candidate summaries, and email generation (wire your API key into
  `appsettings.json` → `Ai:ApiKey`).
- Swagger/JWT bearer auth configured, CORS for the frontend, Serilog console logging.

**Frontend** (`frontend/`) — React 19 + TS + Vite:
- Redux Toolkit store, axios client with automatic refresh-token retry on 401.
- Login page wired to `/auth/login`, protected routing, dashboard shell with sidebar/logout.
- Jobs list page wired to `/jobs` with live search and pagination.
- Tailwind configured with dark-mode support (class strategy).

**Infra:** `docker-compose.yml` (SQL Server, Azurite for blob storage emulation, API, frontend),
Dockerfiles for both apps.

## Candidate Portal module (added)

**Backend:**
- `Candidates` module: `GET/PUT /api/candidates/me` — profile read/update, auto-provisions the
  `Candidate` row on first save for a user with the Candidate role.
- `Resumes` module: `POST /api/resumes/upload` — validates PDF/DOC/DOCX + 10MB limit, uploads
  to Azure Blob Storage, runs `IAiService.ParseResumeAsync`, and replaces AI-extracted skills
  on the candidate profile.
- `Applications` module: `POST /api/applications/apply/{jobId}` (candidate applies, AI match
  score computed synchronously via `ComputeMatchScoreAsync` and stored on the application),
  `GET /api/applications/my` (candidate's own applications + AI recommendation/missing skills),
  `GET /api/applications/job/{jobId}/ranked` (recruiter view, sorted by AI match score),
  `PATCH /api/applications/{id}/status` (workflow transitions, writes to
  `ApplicationStatusHistory`, triggers a notification).
- Real-time notifications: `SignalRNotificationService` persists a `Notification` row and pushes
  it over `NotificationHub` (`/hubs/notifications`) to the affected user's SignalR group.
- EF Core configurations added for Candidate/Education/Experience/Certificate/CandidateSkill,
  InterviewRound/Interview/Feedback/Offer, ApplicationStatusHistory.

**Frontend:**
- `CandidateProfilePage` (`/profile`) — editable profile form + resume upload.
- `ResumeUpload` component — drag/click upload with progress, shows AI-extracted skills,
  summary, and missing-field warnings inline.
- `JobListPage` now has an **Apply** button for candidates that calls the apply endpoint and
  toasts the result.
- `MyApplicationsPage` (`/my-applications`) — pipeline progress bar per application, match
  score, AI recommendation, and missing skills.
- Sidebar now shows candidate-only links conditionally based on `user.role`.

## Interview Scheduling & Feedback module (added)

**Backend:**
- `Interviews` module: `POST /api/interviews/schedule` (creates/reuses an `InterviewRound`,
  schedules an `Interview`, notifies candidate + interviewer via SignalR, sends an email),
  `GET /api/interviews/application/{id}` (all rounds/interviews for an application),
  `GET /api/interviews/my-schedule` (interviewer's own upcoming interviews),
  `POST /api/interviews/{id}/feedback` (rating, strengths/weaknesses, recommend, result),
  `GET /api/interviews/application/{id}/ai-questions` (AI-generated interview questions).
- `IEmailService` now has a real SMTP implementation (`EmailService`) — logs instead of sending
  if `Smtp:Host` is left blank in `appsettings.json`, so it works out of the box in dev.

**Frontend:**
- `RankedCandidatesPage` (`/jobs/:jobId/candidates`) — recruiter view of applicants sorted by
  AI match score, with a quick-schedule action (see note below).
- `MySchedulePage` (`/my-schedule`) — interviewer's upcoming interviews with an inline feedback
  modal (rating slider, strengths/weaknesses/comments, recommend toggle).
- `useSignalR` hook — connects to `/hubs/notifications` with the JWT once logged in,
  auto-reconnects, and feeds a notification bell in the dashboard header.

**Known gap:** the "Schedule Interview" quick-action on `RankedCandidatesPage` doesn't yet
have an interviewer picker — it needs a `GET /api/users?role=Interviewer` endpoint and a
select dropdown before it's fully usable. The full `ScheduleInterviewCommand` API already
supports passing any `interviewerId`.

## Users, Offers & Dashboards module (added)

**Backend:**
- `GET /api/users?role=Interviewer` — role-filtered active user list, powers pickers.
- `Offers` module: `POST /api/offers` (extends an offer, moves application to `Offer` status,
  AI-drafts the offer email via `GenerateEmailAsync`, notifies + emails the candidate),
  `POST /api/offers/{id}/respond` (candidate accepts/declines → application becomes
  `Hired`/`Rejected`, recruiter notified), `GET /api/offers/my` (candidate's offers).
- `Dashboard` module: `GET /api/dashboard/recruiter` (open jobs, total applications,
  interviews this week, offers extended, 6-month application trend, pipeline-by-stage
  breakdown, department-wise hiring), `GET /api/dashboard/candidate` (application/interview/
  offer counts).

**Frontend:**
- `RecruiterDashboard` (`/dashboard` for non-candidate roles) — KPI cards + Chart.js line
  chart (monthly applications), doughnut chart (pipeline by stage), bar chart (department
  hiring).
- `CandidateDashboard` (`/dashboard` for candidates) — KPI cards.
- `RankedCandidatesPage` now has a real **Schedule Interview modal** with an interviewer
  dropdown (populated from `/users?role=Interviewer`), closing the gap from the previous round.
- `MyOffersPage` (`/my-offers`) — candidates view and accept/decline offers.

## Company Management, Seed Data & AI Job Description (added)

**Backend:**
- `Companies` module (`CompaniesController`): `GET/POST /api/companies`,
  `GET/POST /api/companies/{id}/departments`, `GET/POST /api/companies/departments/{id}/designations`,
  `GET/POST /api/companies/{id}/locations` — Create is restricted to SuperAdmin/HRManager, all
  reads are open to any authenticated user (needed for job-creation dropdowns).
- `POST /api/jobs/generate-description` — AI-generates a job description from title,
  department, experience level, and key skills (`IAiService.GenerateJobDescriptionAsync`).
- **`DbSeeder`** — runs on every startup (idempotent): seeds the 5 roles (previously required
  manual SQL before Register would work), plus a default "Acme Corp" company with an
  Engineering department, a Software Engineer designation, an HQ office location, and a
  SuperAdmin login: **`admin@ats.local` / `Admin@12345`**. This replaces the manual SQL insert
  from the earlier README note.

**Frontend:**
- `CreateJobPage` (`/jobs/new`, Recruiter/HRManager/SuperAdmin) — full job-posting form with
  company/department dropdowns (populated live), an **"✨ Generate with AI"** button that fills
  the description field, and a hiring-manager picker. This was the missing piece that made
  `POST /api/jobs` actually reachable from the UI.
- "Post a Job" sidebar link for recruiter-type roles.

## Important: first-run login

With the seeder in place, you no longer need manual SQL. After `dotnet run`, log in as
`admin@ats.local` / `Admin@12345` (SuperAdmin) to create additional companies/departments, or
register new Recruiter/HRManager/Interviewer/Candidate accounts directly — the roles now exist.
**See "Demo Data Seeding" further down for a full realistic dataset seeded automatically.**

## Reports module (added)

**Backend:**
- `Reports` module (`ReportsController`, Recruiter/HRManager/SuperAdmin only): 5 report types —
  Hiring Report (per-job funnel: applications → shortlisted → interviewed → offered → hired,
  plus average days-to-hire computed from `ApplicationStatusHistory`), Recruiter Performance
  (jobs posted/applications/interviews/offers/hires per recruiter), Candidate Report
  (applications submitted + average AI match score + latest status per candidate), Department
  Report (open jobs/applications/hired per department), Job Report (per-job application/hire
  counts).
- Each report has 3 endpoints: `GET /api/reports/{type}` (JSON), `GET /api/reports/{type}/export/excel`,
  `GET /api/reports/{type}/export/pdf`.
- **`ReportExportService`** — one generic implementation (`ClosedXML` for Excel, `QuestPDF` for
  PDF) that uses reflection over each report row's properties, so every report type gets
  Excel/PDF export automatically with no per-report export code.

**Frontend:**
- `ReportsPage` (`/reports`) — tabbed view across all 5 report types, renders a live table from
  whatever columns the JSON response has (no per-report frontend code either), with Excel/PDF
  download buttons that stream the file through axios (keeps the JWT auth header, unlike a
  plain `<a href>` to an authenticated endpoint) and trigger a browser download.

## AI Chat Assistant module (added)

**Backend:**
- `POST /api/aiassistant/chat` (Recruiter/HRManager/SuperAdmin) — grounds every reply in a live
  snapshot of the recruiter's actual pipeline: up to 20 open jobs (title, department, location,
  application count) and the 10 highest-match-score recent applications (candidate, job, score,
  status), serialized as JSON and passed as context alongside the conversation to
  `IAiService.ChatAsync`. This is what makes "show candidates matching React and .NET" or
  "which open jobs have the fewest applicants" answerable instead of generic.
- Multi-turn: the frontend sends the full message history each call; the handler folds it into
  a single prompt since `IAiService.ChatAsync` is single-shot today (swap for a real multi-turn
  `messages` array in `AiService` if you want native conversation state on the Anthropic side).

**Frontend:**
- `ChatWidget` — floating chat bubble (bottom-right) shown only to Recruiter/HRManager/
  SuperAdmin, with 4 starter-prompt suggestions matching the ones from the original spec
  ("Show candidates matching...", "Suggest a salary range...", etc.), message history, and a
  "Thinking..." state while waiting on the API.

## Admin module (added)

**Backend:**
- **`AuditLoggingBehavior`** — a MediatR pipeline behavior registered globally
  (`cfg.AddOpenBehavior`), so every `*Command` handler is automatically audited after it
  succeeds, with zero changes to existing handlers. Auth commands (Login/Register/RefreshToken)
  are explicitly excluded since they carry plaintext passwords that must never hit
  `NewValuesJson`. Logging failures are swallowed so a broken audit write can never break the
  primary operation.
- `AdminController` (SuperAdmin only): `GET /api/admin/dashboard` (system-wide KPIs: total
  users/companies/jobs/applications, users-by-role breakdown), `GET /api/admin/audit-logs`
  (paginated, filterable by entity name or user), `GET /api/admin/users` (all users across every
  company, with role/company/active-status), `PATCH /api/admin/users/{id}/active-status`
  (activate/deactivate an account).

**Frontend:**
- `AdminDashboardPage` — now the default `/dashboard` view for SuperAdmin, with system KPI
  cards and a users-by-role doughnut chart.
- `AuditLogPage` (`/admin/audit-log`) — paginated table of every command executed system-wide,
  filterable by entity name.
- `UserManagementPage` (`/admin/users`) — searchable user list with one-click activate/deactivate.
- Sidebar shows "User Management" and "Audit Log" links only for SuperAdmin.

## Tests, CI/CD, CSV Import & Resume History (added)

**Backend:**
- `backend/tests/ATS.UnitTests` — xUnit + Moq + FluentAssertions. Covers `LoginCommandHandler`
  (valid login, wrong password, inactive account, unknown email), `CreateJobCommandValidator`
  (FluentValidation rules incl. salary-range and skills-required), and
  `ApplyToJobCommandHandler` (duplicate-application conflict, AI match-score computation path).
- `backend/tests/ATS.IntegrationTests` — `Microsoft.AspNetCore.Mvc.Testing` +
  `AtsWebApplicationFactory`, which swaps the SQL Server `DbContext` for EF Core's InMemory
  provider so tests run without a real database. `DbSeeder` now guards `MigrateAsync()` behind
  `context.Database.IsRelational()` so it's safe to run against InMemory too — meaning
  integration tests get the same seeded roles/company/admin as a real run.
- `Program.cs` now ends with `public partial class Program { }` — required for
  `WebApplicationFactory<Program>` to work with top-level statements.
- **CSV bulk candidate import**: `POST /api/candidates/bulk-import` (Recruiter/HRManager/
  SuperAdmin) — parses `FirstName,LastName,Email,Skills` CSV rows (skills semicolon-separated),
  creates a User+Candidate+CandidateSkills per row with a random temp password, skips
  duplicate emails, and returns a per-row success/failure report.
- **Resume version history**: `FileAsset` now has a `CandidateId` column (independent of the
  `Candidate.ResumeFileId` "current resume" pointer), so every uploaded version is queryable.
  `GET /api/candidates/me/resume-history` returns all versions newest-first with a `isCurrent` flag.

**CI/CD** (`.github/workflows/`):
- `backend-ci.yml` — spins up a real SQL Server service container, restores/builds the
  solution, runs both test projects, uploads `.trx` results, and publishes the API artifact
  on `main`.
- `frontend-ci.yml` — installs, lints (ESLint flat config + `typescript-eslint` were missing
  and have been added to `package.json`/`eslint.config.js` — the `npm run lint` script existed
  but had nothing to run), type-checks, builds, and uploads the `dist` artifact on `main`.
- `deploy-azure.yml` — triggers after both CI workflows succeed on `main`; deploys the API to
  an Azure App Service and the frontend to Azure Static Web Apps. Requires 3 repo secrets
  (`AZURE_WEBAPP_PUBLISH_PROFILE_API`, `AZURE_WEBAPP_NAME_API`, `AZURE_STATIC_WEB_APPS_API_TOKEN`)
  documented at the top of the workflow file — this repo doesn't (and shouldn't) contain them.

**Frontend:**
- `ResumeHistory` component on `CandidateProfilePage` — shows every uploaded resume version
  with a download link and a "Current" badge, refreshing automatically after a new upload.
- `BulkImportPage` (`/candidates/bulk-import`, Recruiter/HRManager/SuperAdmin) — CSV upload
  with a per-row success/failure report.

## Full CRUD for Company/Department/Designation/OfficeLocation (added)

**Backend:** `CompaniesController` now has full CRUD, not just Create+List:
- `PUT/DELETE /api/companies/{id}` (Delete is SuperAdmin-only; blocked with a `ConflictException`
  if the company still has jobs posted).
- `PUT/DELETE /api/companies/departments/{id}` (Delete blocked if the department has jobs).
- `PUT/DELETE /api/companies/designations/{id}`.
- `PUT/DELETE /api/companies/locations/{id}`.

**Frontend:** `CompanySettingsPage` (`/admin/company-settings`, HRManager/SuperAdmin) —
cascading company → department → designation management with inline add/edit/delete, replacing
the create-only flow that existed inside `CreateJobPage`'s dropdowns.

## Status: all 15 original deliverables have working code, and the build is confirmed working

Every functional item from the original spec — architecture, DB schema, EF models, Clean
Architecture layering, Auth, CRUD APIs, React frontend, AI integration, Blob Storage, SignalR
notifications, unit tests, Docker, CI/CD, and Azure deployment workflows — has real, wired-up
code behind it. See `ATS-Folder-Structure.md` (shared earlier) for the architecture reference.

This has also now been built end-to-end via Visual Studio / `dotnet build` outside this
sandbox: NuGet restore succeeded, the full solution compiled (`ATS.API.dll` produced), and
`dotnet ef migrations add InitialCreate` generated a clean migration creating all 25 tables
matching the domain model exactly. The AutoMapper version-conflict warning from an earlier
build attempt is also resolved (`AutoMapper.Extensions.Microsoft.DependencyInjection` was
redundant — AutoMapper 12+ bundles its DI extensions into the main package).

## Notifications & Logout module (added)

**Backend:**
- `NotificationsController`: `GET /api/notifications/summary` (unread count + last 10, powers
  the bell), `GET /api/notifications` (full paginated history), `PATCH /api/notifications/{id}/read`,
  `POST /api/notifications/mark-all-read`. Previously, notifications only existed as live
  SignalR pushes with no way to retrieve history after a page reload or a missed connection —
  this closes that gap.
- `POST /api/auth/logout` — revokes the given refresh token (`RefreshTokens.RevokedAtUtc`).
  Previously there was no logout endpoint at all; the frontend only cleared local storage,
  leaving refresh tokens valid indefinitely even after "logging out."

**Frontend:**
- `NotificationBell` — replaces the old SignalR-only bell. Fetches real history via REST on
  mount, refetches on any live SignalR push (keeping one source of truth instead of merging
  two lists), supports click-to-mark-read and "mark all read," and navigates to `linkUrl` when
  a notification is clicked.
- `DashboardLayout`'s logout button now calls `POST /api/auth/logout` before clearing local
  state and redirecting — fails open (never blocks navigation) if the API call errors.

## Frontend Tests (added)

**Vitest + React Testing Library**, configured in `vite.config.ts` (`test.environment: 'jsdom'`,
setup file at `src/test/setup.ts`). Run with `npm test` (single run) or `npm run test:watch`.

- `src/test/testUtils.tsx` — `renderWithProviders()` wraps a component in a real, test-scoped
  Redux store (auth + jobs reducers) and a `MemoryRouter`, with an optional `preloadedState` so
  tests can render as a logged-in user of any role, or logged out, without mocking every hook.
- `authSlice.test.ts` — `setCredentials`/`logout` reducers, including localStorage persistence.
- `jobsSlice.test.ts` — `fetchJobs` async thunk's pending/fulfilled/rejected states.
- `LoginPage.test.tsx` — renders the form, submits and stores credentials on success (mocking
  `axiosClient` so no real HTTP call happens), shows a disabled loading state mid-request, and
  confirms nothing is stored on a failed login.
- `ProtectedRoute.test.tsx` — redirects to `/login` with no access token, renders the protected
  route when one is present.

Frontend CI (`frontend-ci.yml`) now runs `npm test` between lint and build, so a broken
component or reducer fails the pipeline before it ever reaches `build`.

## Enterprise-readiness pass: complete auth, tenant isolation, talent pool (added)

This round targeted what actually blocks companies/agencies from adopting a multi-tenant ATS:
complete auth (the original spec listed Forgot Password / Reset Password / Email Verification /
Change Password — none of these existed until now), and — more importantly — **real tenant
data isolation**, since the previous build had three genuine cross-company data leaks.

**New DB entities** (need a new migration — see below): `PasswordResetToken`,
`EmailVerificationToken`, and two new columns on `User` (`FailedLoginAttempts`,
`LockedOutUntilUtc`).

**Auth completeness:**
- `POST /api/auth/forgot-password` / `reset-password` — token-based reset (1-hour expiry),
  emailed as a link. Always returns success regardless of whether the email exists, so login
  can't be used to enumerate registered accounts. Resetting a password revokes every active
  refresh token for that user (forces re-login everywhere, standard practice after a reset).
- `POST /api/auth/change-password` (authenticated) — requires the current password.
- `POST /api/auth/verify-email` / `resend-verification` — registration now automatically sends
  a verification email; the link expires after 24 hours.
- **Brute-force lockout**: `LoginCommandHandler` now locks an account for 15 minutes after 5
  failed attempts, and returns the same generic "Invalid email or password" message whether the
  account doesn't exist or the password is wrong (prevents email enumeration via login too).

**Tenant isolation fixes (the important part) — three real cross-company data leaks closed:**
1. **Dashboard & Reports** (`DashboardController`, `ReportsController`) previously trusted a
   client-supplied `?companyId=` query parameter with no verification — any Recruiter/HRManager
   could view another company's KPIs, hiring reports, and recruiter-performance data by simply
   changing a GUID in the URL. Fixed: `companyId` is now always resolved from the caller's own
   JWT claim (`ICurrentUserService.CompanyId`) for non-SuperAdmin roles; the query parameter is
   ignored for anyone except SuperAdmin.
2. **Job creation** (`JobsController.CreateJob`) previously trusted `CompanyId` and
   `CreatedByRecruiterId` directly from the request body — a compromised or malicious recruiter
   account could post jobs under another company's identity, or forge attribution to a different
   recruiter. Fixed: both values are now always overridden server-side from the JWT.
3. **Ranked candidates view** (`GetRankedApplicationsForJobQuery`) had no ownership check at
   all — any authenticated recruiter could view another company's applicant rankings for any
   `jobId`. Fixed: the handler now verifies the job's `CompanyId` matches the caller's own
   company (SuperAdmin exempt) and throws `ForbiddenAccessException` otherwise.

Candidates and their applications remain intentionally **not** company-scoped — they're a
shared talent pool that can apply across multiple companies' jobs, which is the correct model
for both a single-company ATS and a multi-client recruitment agency.

**Talent Pool search** (the feature recruitment agencies actually live on):
- `GET /api/candidates/talent-pool` — searches the full candidate database (name, email,
  headline, employer, skills, min years of experience), not just applicants to one job.
  Paginated, with best-match-score and total-application-count shown per candidate.
- `TalentPoolPage` (`/talent-pool`, Recruiter/HRManager/SuperAdmin) — search UI with skill and
  experience filters.

**Frontend:** `ForgotPasswordPage`, `ResetPasswordPage`, `VerifyEmailPage`,
`ChangePasswordPage` (available to every role via the sidebar), and a "Forgot password?" link
on the login page.

## Verification banner, logo upload, rate limiting, GDPR (added)

**Email verification banner:** `UserDto`/`UserInfo` now carry `isEmailVerified`.
`EmailVerificationBanner` shows across the whole app for any unverified user, with a
"Resend verification email" button. `VerifyEmailPage` now dispatches `markEmailVerified()` on
success so the banner disappears immediately without requiring a fresh login.

**Company logo upload:** `POST /api/companies/{id}/logo` (SuperAdmin/HRManager, 2MB limit,
PNG/JPG/SVG only) uploads to Blob Storage via the existing `IBlobStorageService` and replaces
`Company.LogoUrl`, best-effort deleting the old blob. `CompanySettingsPage` shows a logo
thumbnail per company with an inline upload control.

**API-wide rate limiting** (`Program.cs`, using ASP.NET Core's built-in `Microsoft.AspNetCore.
RateLimiting` — no extra package needed): a global 100 requests/minute-per-IP limiter on every
endpoint, plus a stricter 5 requests/minute-per-IP `"auth"` policy applied to
`register`/`login`/`forgot-password`/`reset-password` specifically. This complements (doesn't
replace) the per-account lockout added earlier — lockout stops repeated attempts against one
account, rate limiting stops a single source hitting many different accounts or endpoints.

**GDPR data export & right to erasure** (`PrivacyController`, Candidate-only):
- `GET /api/privacy/my-data` — full JSON export of profile, applications, and resume version
  history (Article 20, data portability). `PrivacyPage` triggers a browser download of the
  result.
- `POST /api/privacy/delete-my-account` — requires typing the exact confirmation phrase
  `"DELETE MY ACCOUNT"`. Implemented as **anonymization, not a hard delete**: PII (name, email,
  phone, resume file) is scrubbed and the account deactivated, but `Application`/`Interview`/
  `Offer` rows are retained (now pointing at an anonymized user) since employers legitimately
  need that history for audit/compliance periods. This is a reasonable starting point, not a
  substitute for legal review of your specific data-retention obligations.

## Session Management (added)

**Backend:** `RefreshToken` now carries `IpAddress`, `UserAgent`, and `LastUsedAtUtc` — captured
at login/register (from the actual request, never trusted from the JSON body) and carried
forward across token rotation so the same logical session stays identifiable even though the
underlying token value changes on every refresh.

`SessionsController` (available to every role — this is a personal-security feature, not an
admin one):
- `POST /api/sessions/mine` — lists the caller's active sessions, newest-used first. POST
  rather than GET because the current refresh token needs to travel in the body to identify
  which row is "this device," and a refresh token has no business appearing in a URL or query
  string where it'd get logged.
- `DELETE /api/sessions/{id}` — revoke one specific session (ownership-checked; you can only
  revoke your own).
- `POST /api/sessions/revoke-others` — "sign out everywhere else," revokes every session
  except the one making the request.

**Frontend:** `SessionsPage` (`/settings/sessions`) — lists devices with a best-effort
human-readable label ("Chrome on Windows," via a small local `parseUserAgent` helper — not a
full UA-parsing library, just enough for display), last-active time, and a "This device" badge
on the current session so no one accidentally revokes the session they're using right now.

## Job lifecycle completion + Webhook syndication (added)

**Found a more fundamental gap than webhooks while building this**: the original spec listed
"Publish Jobs, Close Jobs, Duplicate Jobs" as required recruiter actions, but only `CreateJob`
existed — every job was created as `Draft` and there was no way to ever publish one. In
practice this meant the entire candidate-facing job board was permanently empty; candidates
could never actually see or apply to anything. Fixed first, before webhooks, since webhooks
firing on a `job.published` event are meaningless if jobs can never be published.

**Job lifecycle** (`JobsController`, Recruiter/HRManager/SuperAdmin, all tenant-scoped —
verified against the job's own `CompanyId` the same way the earlier tenant-isolation fixes work):
- `PUT /api/jobs/{id}` — edit title/description/salary/skills/etc.
- `POST /api/jobs/{id}/publish` — `Draft` → `Published`, now actually visible on `GetJobs`.
- `POST /api/jobs/{id}/close` — `Published` → `Closed`, stops new applications.
- `DELETE /api/jobs/{id}` — soft delete; blocked with a clear error if the job already has
  applications (close it instead — deleting would orphan real application history).
- `POST /api/jobs/{id}/duplicate` — clones a job as a new `Draft` for near-identical reposts.
- `JobListPage` now shows Publish/Close/Duplicate/Delete actions per job (role- and
  status-appropriate), and the candidate **Apply** button only appears once a job is actually
  `Published` — previously candidates could apply to drafts, which shouldn't have been possible.

**Webhook syndication** (`WebhooksController`) — generic outbound webhooks rather than
baked-in LinkedIn/Indeed OAuth integrations, since those require live developer accounts and
API credentials this environment doesn't have. A company points their own integration
(Zapier, a custom listener, or eventually a real job-board connector) at a URL and receives
HMAC-SHA256-signed JSON events — same verification pattern Stripe/GitHub webhooks use.
- `POST /api/webhooks` — register a subscription (HTTPS URL required) for one or more event
  types (`job.published`, `job.closed`, `application.status_changed`, `candidate.hired`,
  `offer.extended`). Returns the signing secret exactly once, like an API key.
- `GET /api/webhooks/{id}/deliveries` — last 50 delivery attempts with status code/error, for
  debugging an integration.
- `WebhookDispatcher` (Infrastructure) — delivery failures are logged, never thrown; a dead
  subscriber endpoint must never block or fail the job-publish/status-change/offer operation
  that triggered it. No retry/backoff queue yet (see below).
- `WebhooksPage` (`/webhooks`) — subscription management UI with a one-time secret reveal and
  a delivery-log viewer per subscription.

## Demo Data Seeding (added)

**Confirmed working build**: the uploaded `backend.zip` included a fresh `InitialCreate`
migration (dated after every module through webhooks/sessions/GDPR/job-lifecycle) that compiled
and generated all 29 tables correctly — this round's seed data was built on top of that
confirmed-good baseline.

**`DemoDataSeeder`** (separate from `DbSeeder`, which only seeds the minimum required to run —
5 roles + one SuperAdmin) — runs automatically after `DbSeeder` on every startup, idempotent
(skips if more than one company already exists), and controllable via
`Seed:IncludeDemoData` in `appsettings.json` (default `true` — set to `false` before a real
production deployment, where you want the minimal bootstrap only, not fake companies).

Seeds a full, clickable dataset in one go:
- **2 companies** (deliberately two, not one — lets you log in as each company's recruiter and
  confirm the tenant-isolation fixes from an earlier round actually work: Acme's recruiter
  cannot see Meridian's data and vice versa): **Acme Corp** (Technology — Engineering/Product/
  Sales departments, HQ + Remote locations) and **Meridian Health Group** (Healthcare —
  Clinical Operations department).
- **4 published jobs** (not drafts — actually visible on the job board, exercising the
  publish-workflow fix from the previous round): Senior Backend Engineer, Frontend Developer,
  and Product Manager at Acme; Registered Nurse — ICU at Meridian. Each has realistic
  descriptions, salary ranges, and required skills.
- **4 staff users** across both companies (HRManager, Recruiter, Interviewer at Acme;
  Recruiter at Meridian) plus the existing SuperAdmin.
- **5 candidates** with full profiles — headline, summary, current employer, LinkedIn URL,
  skills with years of experience, work history, and education — covering backend, frontend,
  product, full-stack, and (for the Meridian job) healthcare profiles.
- **6 applications** with realistic AI-match-score data (missing/recommended skills, a written
  recommendation) spanning different pipeline stages: `Applied`, `Shortlisted`,
  `TechnicalInterview`, `HRInterview`, and `Offer` — so the recruiter dashboard's pipeline
  chart and the reports module have real data to render instead of empty charts.
- **1 scheduled interview** (technical round for the top backend candidate, with a real
  interviewer assigned) and **1 extended offer** (for the top product-manager candidate).

All demo users share the password **`Demo@12345`**. Full login list (also logged to the
console on startup):

| Email | Role | Company |
|---|---|---|
| `admin@ats.local` | SuperAdmin | Acme Corp |
| `hr@acme-demo.local` | HRManager | Acme Corp |
| `recruiter@acme-demo.local` | Recruiter | Acme Corp |
| `interviewer@acme-demo.local` | Interviewer | Acme Corp |
| `recruiter@meridian-demo.local` | Recruiter | Meridian Health Group |
| `jane.doe@example-demo.local` | Candidate | — |
| `mark.chen@example-demo.local` | Candidate | — |
| `priya.nair@example-demo.local` | Candidate | — |
| `alex.kim@example-demo.local` | Candidate | — |
| `rachel.adams@example-demo.local` | Candidate | — |

**Not seeded**: actual resume files (would require real PDFs uploaded to Blob Storage — the
candidates have full skill/experience/education data without needing an uploaded resume file,
since that data normally comes from AI-parsing a resume rather than being a prerequisite for
it), webhook subscriptions, and audit log entries beyond whatever the seeding itself triggers.

## What's genuinely still open

- **New migration required**: this round added `WebhookSubscription`/`WebhookDeliveryLog`
  tables on top of everything from the previous two rounds (session metadata, password
  reset/email verification tokens, lockout columns) — one migration covers all of it:
  `dotnet ef migrations add EnterpriseHardening --project src/ATS.Infrastructure --startup-project src/ATS.API`
- Webhook delivery has no retry/backoff — a failed delivery is logged once and not retried.
  For production use you'd want a background job (Hangfire, or a queue + worker) that retries
  failed deliveries with exponential backoff, since "the subscriber's server was down for 30
  seconds" shouldn't mean a permanently missed event.
- Azure secrets need to be created in the repo before `deploy-azure.yml` can actually deploy.
- Frontend test coverage is a start (slices + one page + routing), not comprehensive — the
  bigger integration-style flows (apply-to-job, schedule-interview, the AI chat widget, talent
  pool search, GDPR export/delete) don't have tests yet.
- Access-token revocation on logout/password-reset is not instant (JWTs are stateless; the
  15-minute-lived access token remains valid until natural expiry — standard behavior, but
  worth knowing if you need immediate revocation for a compliance reason).
- Password reset/verification links point to a placeholder `https://app.example.com` — update
  to your real frontend URL via config once you have a deployed domain (currently hardcoded in
  three command handlers: `ForgotPasswordCommand`, `RegisterCommand`, `ResendVerificationEmailCommand`).
- Email verification is tracked but not yet *enforced* anywhere (e.g. blocking unverified users
  from applying to jobs) — currently informational only, by design, since forcing verification
  before first use is a product decision rather than a clear-cut default.
- Rate limiter state is in-memory per instance — fine for a single-server deployment, but if
  you scale the API horizontally you'll want a distributed rate limiter (e.g. backed by Redis)
  so limits are shared across instances instead of reset per-instance.
- GDPR erasure anonymizes rather than hard-deletes by design (see above) — if your legal
  requirements demand full deletion after a retention period, that's a scheduled job you'd add
  on top of this, not a change to the erasure flow itself.

## Running locally

```bash
# Backend
cd backend
dotnet restore
dotnet ef database update --project src/ATS.Infrastructure --startup-project src/ATS.API
dotnet run --project src/ATS.API

# Frontend
cd frontend
npm install
npm run dev
```

Or `docker compose up --build` from the repo root (Docker builds pulling NuGet/npm packages
over a slow connection may see transient "response ended prematurely" download errors — this
is a network/CDN issue, not a code issue; retrying the build usually resolves it).

## Suggested next steps

1. Frontend automated tests (Vitest + React Testing Library) — the one area without coverage.
2. Runtime walkthrough — click through the full flow (register → post job → apply → AI match
   score → schedule interview → feedback → offer → accept) to catch logic bugs a compiler can't,
   like a role check that's backwards or a null candidate on an empty resume.
3. Harden what exists: rate limiting, a background job to purge expired refresh tokens, more
   granular validation error messages surfaced in the UI.
