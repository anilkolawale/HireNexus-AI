# 🚀 HireNexus AI
## Master System Architecture, Panel Operations & Audit Report


---

## 📌 1. Executive Summary

This **AI-Powered Applicant Tracking System (ATS)** is an enterprise-grade, full-stack recruitment platform designed to compete directly with traditional enterprise talent acquisition systems such as **Workday, Greenhouse, SmartRecruiters, Lever, iCIMS, and Taleo**.

The platform leverages **Google Gemini AI** for automated resume parsing, candidate match scoring, and skill-gap recommendations. It enforces **EEOC-compliant Blind Screening**, automates recruitment workflows with a custom rule engine, generates standard RFC 5545 calendar invitations (`.ics`), powers video/coding candidate assessments, and handles offer letter e-signatures and SOC2 audit compliance.

---

## 🏗️ 2. System Architecture & Tech Stack

```
                               ┌──────────────────────────────────────────┐
                               │             React 18 SPA                 │
                               │  (Vite + Tailwind CSS + Lucide Icons)    │
                               └────────────────────┬─────────────────────┘
                                                    │ REST API / Axios
                                                    ▼
                               ┌──────────────────────────────────────────┐
                               │           ASP.NET Core 8 Web API         │
                               │  (Clean Architecture + MediatR CQRS)     │
                               └────────┬───────────┬───────────┬─────────┘
                                        │           │           │
           ┌────────────────────────────┘           │           └──────────────────────────┐
           ▼                                        ▼                                      ▼
┌───────────────────────┐            ┌───────────────────────────┐           ┌──────────────────────────┐
│   EF Core SQL Server  │            │     Google Gemini AI      │           │     Hangfire Engine      │
│  (Relational Storage) │            │  (Resume Matching & Sc)  │           │   (Background Tasks)     │
└───────────────────────┘            └───────────────────────────┘           └──────────────────────────┘
```

* **Frontend**: React 18, TypeScript, Vite, Tailwind CSS, Framer Motion, TanStack Query (React Query), Axios.
* **Backend**: .NET 8 Web API, C#, Clean Architecture (Domain, Application, Infrastructure, API layers).
* **CQRS Pattern**: MediatR for strict request/handler separation and cross-cutting audit logging.
* **Database**: SQL Server / Entity Framework Core with migrations and automated database seeders.
* **AI Integration**: Google Gemini AI for resume extraction, skill-gap analysis, and offer email drafting.
* **Background Jobs**: Hangfire engine for background workflow processing and automation rule execution.

---

## 👥 3. The 5 User Panels & Operational Tasks

The system enforces strict Role-Based Access Control (RBAC) across **5 distinct user panels**, eliminating redundant or out-of-scope clutter from each panel:

### 👑 3.1. Super Admin Panel
* **Primary Role**: System-wide governance, security compliance, tenant management, and user auditing.
* **Access Level**: Unrestricted global access.
* **Key Tasks**:
  1. 🛡️ **SOC2 Compliance Audit Log** (`/admin/audit-log`): View real-time security logs, filter by entity type (`Job`, `Application`, `Offer`), and export audit trails.
  2. 👥 **User & Role Management** (`/admin/users`): Create, activate/deactivate users, and reassign system roles.
  3. ⚡ **Enterprise Webhooks Engine** (`/webhooks`): Strictly restricted to Super Admin. Configure outgoing webhooks (`offer.extended`, `application.status_changed`) to integrate with external ERPs (Workday, SAP).
  4. 🏢 **Company Settings** (`/admin/company-settings`): Manage company branding, departments, and office locations.

### 👔 3.2. HR Manager Panel
* **Primary Role**: Hiring strategy, job headcount budget approvals, offer letter creation, and automation workflow rules.
* **Access Level**: Company-level administrative access.
* **Key Tasks**:
  1. 📋 **Job Requisitions Approval** (`/requisitions`): Review & approve/reject new job headcount budget requests from department heads.
  2. ⚡ **Workflow Automations Engine** (`/settings/automations`): Set up automated hiring rules (e.g., *When candidate score > 85%, automatically send interview invite*).
  3. 🎁 **Offer Letter Generation** (`/my-offers`): Issue official offer letters with salary terms, joining dates, and signing bonuses.
  4. 📊 **Analytics & Executive Reports** (`/reports`): Track Time-to-Hire, Sourcing Channel ROI, and Offer Acceptance Rates.
  5. 📋 **Employee Onboarding Tracking** (`/onboarding`): Monitor pre-boarding tasks, background checks, and IT equipment provision.

### 🎯 3.3. Recruiter Panel
* **Primary Role**: Candidate sourcing, AI ranking, job board multi-posting, blind screening, and pipeline management.
* **Access Level**: Department and job-level management access.
* **Key Tasks**:
  1. ➕ **Create & Publish Jobs** (`/jobs/new`): Draft job descriptions with Gemini AI assistance and set required skills.
  2. 🌐 **Multi-Posting Job Boards** (`Job Boards tab`): Broadcast job postings to LinkedIn, Indeed, Glassdoor, and ZipRecruiter in 1 click.
  3. ⚖️ **EEOC Blind Screening** (`Ranked Candidates tab`): Toggle PII masking (names, photos, graduation years) to eliminate hiring bias.
  4. 🔀 **Kanban Recruitment Pipeline** (`/pipeline`): Drag and drop candidate cards through hiring stages (*Applied* ➔ *Shortlisted* ➔ *Offer*).
  5. 🎥 **Assessment Builder** (`/assessments/builder`): Assign webcam video interview prompts or HackerRank coding tests.
  6. 👥 **Talent Pool & CRM** (`/talent-pool`, `/talent-crm`): Search passive candidate database and send bulk outreach emails.
  7. 📥 **Bulk Candidate Import** (`/candidates/bulk-import`): Upload ZIP files containing candidate resumes for AI parsing.

### 📆 3.4. Interviewer Panel
* **Primary Role**: Candidate evaluation, technical interviews, schedule management, and 5-star scoring.
* **Access Level**: Assigned interview access.
* **Key Tasks**:
  1. 📅 **My Schedule** (`/my-schedule`): View upcoming candidate interviews, download `.ics` calendar files, or send email invites.
  2. ⭐ **5-Star Interview Feedback**: Rate candidates on Technical Skills, Communication, and Culture Fit after interview rounds.
  3. 📄 **Candidate Resume & Scorecard Review**: Inspect candidate resume, extracted skills, and past interview notes prior to interview calls.
  4. 🔍 **Read-Only Job Specs** (`/jobs`): View job titles and requirements for interview prep (No "Apply Now" button).

### 👤 3.5. Candidate Panel
* **Primary Role**: Job search, application tracking, assessment completion, and offer letter e-signing.
* **Access Level**: Candidate self-service portal (Cleaned of recruiter/admin dashboard clutter).
* **Key Tasks**:
  1. 🌐 **Career Portal** (`/careers`): Search published open roles and submit applications with resume upload.
  2. 🤖 **Instant AI Resume Matcher**: View instant Match Score % and missing skill recommendations upon application.
  3. 📄 **My Applications** (`/my-applications`): Track live status updates (*Screening*, *Interview*, *Offer*) across all applied jobs (Direct landing page).
  4. 🎥 **My Assessments** (`/my-assessments`): Record webcam video interview responses and take technical tests.
  5. ✍️ **My Offers & E-Sign** (`/my-offers`): Review official offer details and click **Accept Offer** to complete e-signature (Offer creation controls hidden).

---

## 🔒 4. Panel Scope & Access Control Matrix

| Feature / Page | Super Admin | HR Manager | Recruiter | Interviewer | Candidate |
| :--- | :---: | :---: | :---: | :---: | :---: |
| **SOC2 Security Audit Log** (`/admin/audit-log`) | ✅ | ❌ | ❌ | ❌ | ❌ |
| **User & Role Management** (`/admin/users`) | ✅ | ❌ | ❌ | ❌ | ❌ |
| **Developer Webhooks Engine** (`/webhooks`) | ✅ | ❌ | ❌ | ❌ | ❌ |
| **Job Requisitions Budget Approval** (`/requisitions`) | ✅ | ✅ | 📝 *Draft only* | ❌ | ❌ |
| **Workflow Automations Engine** (`/settings/automations`) | ✅ | ✅ | ❌ | ❌ | ❌ |
| **Company Settings & Branding** (`/admin/company-settings`) | ✅ | ✅ | ❌ | ❌ | ❌ |
| **Offer Letter Generation** (`/my-offers`) | ✅ | ✅ | ✅ | ❌ | ❌ |
| **Multi-Posting Job Boards** (`/jobs`) | ✅ | ✅ | ✅ | ❌ | ❌ |
| **Kanban Pipeline & Blind Screening** (`/pipeline`) | ✅ | ✅ | ✅ | ❌ | ❌ |
| **Talent Pool & CRM Sourcing** (`/talent-pool`) | ✅ | ✅ | ✅ | ❌ | ❌ |
| **Interview Scheduling & 5★ Feedback** (`/my-schedule`) | ✅ | ✅ | ✅ | ✅ | ❌ |
| **Read-Only Job Specs** (`/jobs`) | ✅ | ✅ | ✅ | ✅ | ❌ |
| **Career Portal & Job Applying** (`/careers`) | ❌ | ❌ | ❌ | ❌ | ✅ |
| **My Applications & AI Match Score** (`/my-applications`) | ❌ | ❌ | ❌ | ❌ | ✅ |
| **My Assessments Video Recorder** (`/my-assessments`) | ❌ | ❌ | ❌ | ❌ | ✅ |
| **My Offers E-Signing** (`/my-offers`) | ❌ | ❌ | ❌ | ❌ | ✅ |

---

## 🔑 5. Master Credentials Table

The system comes pre-seeded with ready-to-use accounts for testing every role:

### 🛡️ 5.1. System Default Accounts (Password: `Admin@12345`)

| Role | Username / Email | Password | Primary Functions |
| :--- | :--- | :--- | :--- |
| **Super Admin** | `admin@ats.local` | `Admin@12345` | Audit Log, Webhooks, User Management |
| **HR Manager** | `hr@ats.local` | `Admin@12345` | Requisitions, Company Settings, Offer Letters |
| **Recruiter** | `recruiter@ats.local` | `Admin@12345` | Post Jobs, Blind Screening, Job Board Sync |
| **Interviewer** | `interviewer@ats.local` | `Admin@12345` | View Schedule, Submit 5★ Feedback |
| **Candidate** | `candidate@ats.local` | `Admin@12345` | Candidate self-service portal |

### 🏢 5.2. Acme Corp Demo Accounts (Password: `Demo@12345`)

| Role / Name | Username / Email | Password | Pre-seeded Context |
| :--- | :--- | :--- | :--- |
| **HR Manager** *(Priya Sharma)* | `hr@acme-demo.local` | `Demo@12345` | Acme Corp HR Manager |
| **Recruiter** *(Daniel Cho)* | `recruiter@acme-demo.local` | `Demo@12345` | Acme Corp Recruiter |
| **Interviewer** *(Sofia Martinez)* | `interviewer@acme-demo.local` | `Demo@12345` | Lead Technical Interviewer |
| **Candidate** *(Jane Doe)* | `jane.doe@example-demo.local` | `Demo@12345` | Senior Backend Engineer applicant (92% AI Match) |
| **Candidate** *(Mark Chen)* | `mark.chen@example-demo.local` | `Demo@12345` | Frontend Developer applicant (88% AI Match) |
| **Candidate** *(Priya Nair)* | `priya.nair@example-demo.local` | `Demo@12345` | Product Manager applicant ($140,000 Offer Letter) |

---

## ⚡ 6. How to Run the Application Locally

### 1️⃣ Start Backend Server (.NET 8 Web API):
```powershell
cd backend
dotnet run --project src/ATS.API
```
* **API Base URL**: `http://localhost:5000/api`
* **Swagger Documentation**: `http://localhost:5000/swagger`

### 2️⃣ Start Frontend Server (React 18 SPA):
```powershell
cd frontend
npm run dev
```
* **Frontend App URL**: `http://localhost:5173`

---
*Report updated to match current codebase implementation.*
