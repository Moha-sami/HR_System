# Product

<!-- impeccable:product-schema 1 -->

## Platform

web

## Users

HR staff, admins, and any employee, each working within their role. HR and admins manage the workforce (employees, jobs, roles, sites, shifts and schedules, requests, attendance, points, rewards). Employees use self-service surfaces (requests, recognitions, news, their own data).

## Product Purpose

Internal HR operations system for running the full employee lifecycle in one place: employee records, job and role management, site management, shift-template scheduling, time and attendance, internal requests, and engagement (points, rewards, recognitions, news). Success means HR and managers complete routine workforce tasks quickly and employees can self-serve without HR involvement.

## Positioning

Internal operations tool, not a product for sale. There is no market claim to defend; the bar is operational reliability and speed for daily HR work.

## Operating Context

Bilingual English/Arabic with full RTL mirroring. Used on desktop in office settings. Role-based access (Admin, HR, Manager, employee-level roles) gates every surface.

## Capabilities and Constraints

Confirmed capabilities: employee management, job management, role-based access, site management, shift-template scheduling (create/edit, timeline coverage view, employee assignment via strip), request management, time and attendance, points, rewards, recognitions, news.

Constraints: Angular 21 SPA frontend (`src/Buy2.Frontend/Front`) on a .NET API (`api/v1`). Tailwind v4 with theme tokens in `src/styles.css`. All UI text via EN/AR i18n keys. Backend remains the authority on validation (e.g. shift overlap rules); the frontend mirrors it.

## Brand Commitments

Buy2 name and logo. Brand blue #234199 with the primary/error/success/warning/neutral/table token scales defined in `src/styles.css`. The incumbent visual system is binding: future work refines it, never replaces it unasked.

## Evidence on Hand

Live codebase at this repo root: `src/Buy2.Frontend/Front` (Angular app, routes under `src/app/features/`), `src/Buy2.Api`, `src/Buy2.Application`, `src/Buy2.Domain`. Brand tokens: `src/Buy2.Frontend/Front/src/styles.css`. i18n: `public/assets/i18n/en.json`, `ar.json`. No invented customers, testimonials, or benchmarks exist and none may be fabricated.

## Product Principles

1. Role-appropriate surfaces: every user sees what their role permits, nothing more.
2. Bilingual by construction: no feature ships English-only.
3. Backend is truth: the UI mirrors server rules and never invents its own.
4. Operational speed over expression: this is a tool for daily work, not a showcase.
5. Incumbent identity is binding: refine the existing system; do not rebrand it.
