# IFA — AI-Powered Adaptive Learning Companion

> **STARK Hackathon 2026** — Team XOR (Nathnel Teklemariam, Ermiyas Eshetu, Negede Tekleyes)

IFA turns a conversation into a personalized course. You describe what you're trying to achieve, a
conversational advisor asks the questions that matter, a course architect drafts a blueprint you
approve, and a builder writes the lessons — grounded in **ScholarXiv** academic research, YouTube and
web sources, generated **one module at a time**. The whole app can be driven hands-free with
**Voxide** voice navigation.

## Live Demo

| | |
|---|---|
| **Web app** | https://ifa-ai-red.vercel.app |
| **API** | https://ifa-api-8kgf.onrender.com/api/health |
| **Demo learner** | `nathnel@ifa.local` / `ifa12345` |

Sign in with the demo account (or use the one-click demo login on the sign-in page) — the catalog,
skills, progress and research data are seeded automatically, so every surface has real content.

> The API runs on Render's free tier and sleeps after ~15 minutes idle. The first request afterwards
> takes about a minute to wake; the health endpoint above is the quickest way to warm it up.

## How It Works

The pipeline is deliberately split across separate models. Each stage hands a clean, inspectable
artifact to the next one, and the learner sits in the middle of it.

```
  Conversation            Blueprint              Research + Build
 ┌──────────────┐      ┌──────────────┐      ┌──────────────────────┐
 │   Model 1    │      │   Model 2    │      │       Model 3        │
 │  Learning    │ ───► │   Course     │ ───► │   JIT Course Builder │
 │  Advisor     │      │  Architect   │      │                      │
 └──────────────┘      └──────────────┘      └──────────────────────┘
   intake chat            approve gate           Module 1 written now
   → learner profile      → modules, topics      → later modules on demand
                          → hours, editable      → research reused
```

1. **Model 1 — Learning Advisor.** A real conversation, not a search box. It asks about hours per
   week and creator preferences, and only advances to course setup once the learner profile is
   complete. One-word or off-topic input stays in conversation rather than generating a course.
2. **Model 2 — Course Architect.** Produces a reviewable blueprint: modules, the topics inside them
   and hour estimates. Nothing is generated until you approve it.
3. **Research.** On approval, ScholarXiv academic search runs alongside web and YouTube lookups, and
   the results are saved as a **reusable research package** on the course — so later modules don't pay
   for the search again.
4. **Model 3 — JIT Course Builder.** Writes **Module 1 immediately**: lessons plus exam-style quizzes.
   You are auto-enrolled and the course appears in **My Courses**. Every subsequent module is
   generated on demand from the course page.
5. **Adaptive engine & AI Tutor.** Assessment results feed back as flagged weak skills, which shape
   recommendations and what later modules emphasise. The AI Tutor is a Socratic learning assistant.

Any links you supply (docs, repos, slides) are stored per-course and enforced into lesson content
under a `## Course Sources` section — the model is told to cite them, and IFA guarantees they appear.

## Features

### Learning experience
- **Home dashboard** with hero goal suggestions and AI-recommended goals
- **My Courses** (`/courses`) — enrolled, created and completed courses in one library
- **Courses Library** with search, filter and sort
- **My Skills & Skill Profile** with radar charts and per-skill breakdowns
- **Progress Dashboard** with analytics and activity charts
- **Research Workspace** with ScholarXiv, web and YouTube sources
- **Settings & Preferences** for learning, resources, notifications and theme
- **Global Search Command Palette** (⌘K / Ctrl+K) for keyboard-driven navigation
- **Notification Center** and learner profile menu

### AI-powered
- **Three-model pipeline** — conversational advisor, course architect, JIT course builder
- **ScholarXiv research** integration for academic, peer-reviewed sources
- **YouTube** resource matching, including a preferred channel
- **Voxide voice navigation** — spoken requests map onto the same capabilities the UI calls
- **AI Tutor** — Socratic learning assistant, course-aware
- **Adaptive engine** — assessment-driven content shaping
- **Recommendation engine** — personalized course suggestions, including remediation courses

### Social learning
- **Course sharing** with shareable links and enrollment codes
- **Course marketplace** for public course discovery and publishing
- **Activity stream** for peer activity
- **Peer progress comparison** for skill benchmarking

### Landing page
A standalone marketing page (`client/src/routes/+page.svelte`) with an animated hero, a live pipeline
demo, and dedicated sections for ScholarXiv research and Voxide voice. Fully theme-aware (light/dark)
and reduced-motion aware.

## Technology Stack

### Frontend
- **SvelteKit 2** (Svelte 5) with **TypeScript**
- **Tailwind CSS** with the custom IFA design system
- **Lucide Svelte** icons
- **Voxide** browser SDK for voice

### Backend
- **ASP.NET Core** (.NET 10) — Clean/Onion Architecture
- **Entity Framework Core** with **PostgreSQL**
- **Gemini** — `gemini-3.5-flash` (conversation, architecture, tutor)
- **Groq** — `openai/gpt-oss-120b` (fast inference, automatic failover)
- **Ollama** — optional, zero-cost local provider

### DevOps
- **Docker** & Docker Compose for local development
- **Render** for the API (Docker web service, `render.yaml` blueprint)
- **Vercel** for the frontend (`@sveltejs/adapter-vercel`)

## Getting Started

### Prerequisites
- .NET 10 SDK
- Node.js 18+
- PostgreSQL 14+ (or a hosted Postgres such as Supabase)
- Docker (optional, for containerized development)

### Local Development

1. **Clone and configure**
   ```bash
   git clone https://github.com/NathnelTK/IFA-AI.git
   cd IFA-AI
   cp .env.example .env                      # then fill in real values
   ```
   The API and Docker Compose both read the root `.env`. `JWT_SECRET` must be at least 32
   characters or the API refuses to start.

2. **Backend**
   ```bash
   dotnet restore
   cd src/IFA.API
   dotnet run                                # http://localhost:5011
   ```
   The schema and demo data are created automatically on first start (see
   [Database & Schema](#database--schema)).

3. **Frontend**
   ```bash
   cd client
   npm install
   cp .env.example .env                      # VITE_API_URL, VITE_VOXIDE_KEY
   npm run dev                               # http://localhost:5173
   ```

4. **Free / local AI (Ollama)** — optional, no API key required

   The LLM gateway supports Ollama as a zero-cost provider. If no provider is
   reachable it falls back to a deterministic template provider, so course
   creation still works offline.

   ```powershell
   # from the repository root
   powershell -ExecutionPolicy Bypass -File scripts/setup-ollama.ps1
   ```

   Then set `AI_DEFAULT_PROVIDER=Ollama` in the root `.env` and restart the API.
   (Prefer a hosted key? Set `GEMINI_API_KEY` or `GROQ_API_KEY` in `.env` instead.)

5. **Docker (optional)**
   ```bash
   docker-compose up
   ```

## Environment Variables

### Backend (root `.env`)

```env
# Database
DB_PROVIDER=postgres
DB_HOST=localhost
DB_PORT=5432
DB_NAME=ifa
DB_USER=ifa
DB_PASSWORD=change_me
# Alternative to the DB_* block (takes priority):
# DATABASE_URL=postgresql://user:password@host:port/database

# Auth — minimum 32 characters
JWT_SECRET=replace_with_a_unique_random_secret_of_at_least_32_characters

# LLM providers
AI_DEFAULT_PROVIDER=Groq
GEMINI_API_KEY=your_gemini_api_key
GROQ_API_KEY=your_groq_api_key
OLLAMA_BASE_URL=http://localhost:11434

# External services
SCHOLARXIV_API_KEY=your_scholarxiv_api_key
YOUTUBE_API_KEY=your_youtube_api_key

# CORS — comma-separated frontend origins. Required; the API uses
# AllowCredentials(), so a wildcard origin is not accepted.
CORS_ALLOWED_ORIGINS=http://localhost:5173,https://yourapp.vercel.app

ASPNETCORE_ENVIRONMENT=Development
ASPNETCORE_URLS=http://+:8080
```

Connection resolution order: `ConnectionStrings:DefaultConnection` → `DATABASE_URL` → the `DB_*`
values. Provider tuning lives in `src/IFA.API/appsettings.json` (models, per-pipeline provider,
`Scholarxiv:UseMockData`).

### Frontend (`client/.env`)

```env
VITE_API_URL=http://localhost:5011
# Voxide PUBLISHABLE key only (vox_pub_...). The secret key (vox_sk_...) is
# never used in the browser. Whitelist your origin in the Voxide dashboard.
VITE_VOXIDE_KEY=vox_pub_xxxxxxxxxxxx
```

Without a Voxide key the app still runs; the mic reports "Voice needs a key".

## Deployment

### Backend → Render

The API deploys as a Docker web service driven by [`render.yaml`](render.yaml):

- Build: `docker/Dockerfile.backend`, context `.`
- Health check: `/api/health`
- Region: `frankfurt` (co-located with the Supabase project in `eu-central-1`)

1. Render Dashboard → **New → Blueprint** → select this repository.
2. Supply the values marked `sync: false` (database credentials, `JWT_SECRET`, AI keys, and
   `CORS_ALLOWED_ORIGINS` set to your Vercel domain).
3. Deploy, then confirm `https://<service>.onrender.com/api/health` returns 200.

Two operational notes:

- **Ports.** Render assigns `$PORT` (default **10000**), not 8080. `Dockerfile.backend` binds to
  `${ASPNETCORE_URLS:-http://+:${PORT:-8080}}`, so it works on Render and on local Docker. The image
  also installs `icu-libs` — the Alpine .NET runtime calls `FailFast` at startup without ICU, because
  globalization is not set to invariant.
- **Env var changes do not redeploy.** After editing variables (via dashboard or API), trigger a
  manual deploy from the Render dashboard or the API.

### Frontend → Vercel

1. **New Project** → import this repository.
2. Set **Root Directory** to `client`.
3. Add environment variables `VITE_API_URL` (the Render URL) and `VITE_VOXIDE_KEY`.
4. Deploy.

`client/svelte.config.js` selects the adapter automatically: `@sveltejs/adapter-vercel` when Vercel
sets `VERCEL=1`, and `@sveltejs/adapter-node` everywhere else — so the same source builds for Vercel,
for `npm run preview`, and for `docker/Dockerfile.frontend` (which runs `node build`).

Finally, add the Vercel domain(s) to `CORS_ALLOWED_ORIGINS` on the API. Each Vercel branch preview
gets its own hostname, so add any preview domains you intend to use.

## Database & Schema

The API does **not** use EF migrations at runtime. On startup it checks `information_schema` for base
tables in the `public` schema and, if the schema is empty, creates it from
`Database.GenerateCreateScript()`, then seeds the entrance-exam catalog and demo data.

Consequence: **`EnsureCreated` cannot alter existing tables.** After a model change that adds or
changes columns, reset the schema and restart the API:

```sql
DROP SCHEMA public CASCADE;
CREATE SCHEMA public;
```

Until that is done, reads touching the new columns will fail.

## Project Structure

```
IFA-AI/
├── client/                       # SvelteKit frontend
│   ├── src/
│   │   ├── lib/
│   │   │   ├── components/      # Shell, header, hero, chat, cards
│   │   │   ├── stores/          # Courses, UI/theme, preferences
│   │   │   ├── api/             # Typed API client
│   │   │   └── voxide.ts        # Voice integration
│   │   └── routes/              # Pages (home, courses, skills, progress, ...)
│   ├── static/
│   └── svelte.config.js         # adapter-node / adapter-vercel switch
├── src/
│   ├── IFA.Domain/               # Entities
│   ├── IFA.Application/          # Use cases and interfaces
│   ├── IFA.Infrastructure/       # EF Core, AI providers, research, generation
│   └── IFA.API/                  # Web API, controllers, startup
├── docker/                       # Dockerfile.backend / Dockerfile.frontend
├── docs/                         # Pitch deck, changelog
├── scripts/                      # Ollama setup, changelog, SQL helpers
├── render.yaml                   # Render Blueprint (API)
└── docker-compose.yml
```

## Scripts

### Root
```bash
npm run changelog:add     # Add changelog entry
npm run changelog:verify  # Verify changelog format
```

### AI / Maintenance (scripts/)
```powershell
scripts/setup-ollama.ps1           # Install/verify a free local model (Ollama)
```
```sql
-- preview + remove duplicate courses (keeps the oldest per title)
scripts/cleanup-duplicate-courses.sql
```

### Backend
```bash
dotnet build              # Build solution
dotnet run --project src/IFA.API
```

### Frontend
```bash
npm run dev               # Start dev server
npm run build             # Production build (adapter-node locally)
npm run preview           # Serve the production build
npm run check             # Type-check with svelte-check
```

## Documentation

- [Demo script](docs/DEMO_SCRIPT.md) — a single end-to-end use case for presenting IFA
- [Pitch deck](docs/PITCH_DECK.md) — problem, solution, architecture, roadmap
- [STARK changelog](docs/stark-changelog.md) — milestone history
- [Implementation plan](IFA_AI_Learning_Companion_Implementation_Plan_v3.md) — design and PR tracking

## Team

- **Nathnel Teklemariam** — Team Lead, architecture and system design
- **Ermiyas Eshetu** — Backend, ASP.NET Core API, database, AI integration
- **Negede Tekleyes** — Frontend, SvelteKit UI, design system, accessibility

## Acknowledgments

- STARK Hackathon 2026
- Gemini AI · Groq AI · Ollama
- ScholarXiv
- Voxide
