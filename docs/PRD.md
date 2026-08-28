# Product Requirements Document (PRD): MotionArabia

## 1. Product Vision & Problem Statement
**Vision:** To democratize high-converting, motion-graphics video ad creation for GCC e-commerce merchants through a fully automated, Arabic-first generation platform.
**Problem:** Existing automated video ad generators fail at Arabic RTL (Right-to-Left) typography, ligatures, and rendering. Merchants on local platforms like Salla and Zid spend excessive time and budget on manual video creation. MotionArabia solves this by seamlessly extracting product data and generating ready-to-run MP4 video ads with flawless RTL text rendering.

## 2. Target Audience & Personas
- **E-commerce Merchants (Salla/Zid/Shopify KSA):** Small to medium business owners in Saudi Arabia and the broader GCC who need daily content for Snapchat, TikTok, and Instagram ads but lack video editing skills.
- **Digital Marketers / Media Buyers:** Professionals running performance marketing campaigns who require rapid creative testing and high-volume video ad variations.

## 3. Core Feature Specifications
### 3.1 Auth & Tenant Subscription Tiers
- **Authentication:** JWT-based secure authentication.
- **Tiers:**
  - **Free Trial:** 3 watermarked video exports, 7-day validity.
  - **Starter:** 30 HD videos/month, standard templates.
  - **Pro:** Unlimited 4K videos, custom branding, API access, priority rendering.

### 3.2 Product URL Scraper
- **Supported Platforms:** Salla, Zid, Shopify KSA.
- **Functionality:** Headless extraction of product title, price, discount price, main images, and description via HTML/JSON parsing. Caches output to reduce redundant scraping.

### 3.3 Template Engine
- **Typography:** Native support for Arabic web fonts like Cairo and Tajawal. Proper handling of RTL text shaping.
- **Animation Payload:** GSAP (GreenSock) for high-performance, web-based DOM motion graphics, controlled via dynamic JSON payloads.

### 3.4 Video Rendering Queue
- **Architecture:** Asynchronous event-driven architecture using .NET Background Services.
- **Renderer:** Playwright CLI to record the GSAP DOM animations in a headless Chromium browser instance.
- **Stitching:** FFmpeg to combine video frames, apply transitions, and overlay audio tracks into a final optimized MP4 file.

### 3.5 Angular 18 User Dashboard
- **Features:** 
  - URL input bar for instant ad generation.
  - Video preview player.
  - Asset library for download links (MP4).
  - Subscription status and billing management dashboard.
  - Dark/Light mode toggle.

### 3.6 GCC Payments Integration
- **Gateways:** Moyasar and Tap Payments.
- **Supported Methods:** Mada, Visa, Mastercard, Apple Pay, stc pay.
- **Webhooks:** Automated subscription lifecycle management via secure webhook endpoints.

## 4. Functional & Non-Functional Requirements
### Functional
- System must generate an ad from a valid Salla/Zid URL without manual user intervention.
- System must properly format Saudi Riyal (SAR) pricing structures.

### Non-Functional
- **Performance:** Sub-20s rendering latency from URL submission to downloadable MP4 preview.
- **Accessibility:** Angular UI must strictly adhere to WCAG AA UI contrast standards.
- **Security:** Strict enforcement of OWASP API security guidelines, particularly mitigating BOLA (Broken Object Level Authorization) across tenant workspaces.

## 5. Clean Architecture Layer Mapping
The backend will strictly follow Clean Architecture principles using .NET 10.
- **Domain Layer:** Enterprise entities (Tenant, VideoAd, Subscription) and core business rules. No external dependencies.
- **Application Layer:** MediatR for CQRS (Commands/Queries), FluentValidation, and abstraction interfaces (IScraperService, IRenderingQueue).
- **Infrastructure Layer:** EF Core mapping to SQL Server, Moyasar/Tap HTTP clients, Playwright/FFmpeg CLI wrappers.
- **Presentation Layer:** .NET 10 Web API endpoints, Swagger/OpenAPI documentation, Authentication/Authorization middleware.
