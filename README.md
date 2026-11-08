# Witforge - AI-Assisted Learning and Course Creation

Witforge (formerly IFA) is a persisted learning platform for educators and learners. Educators can
create a course manually, start from one of seven reusable templates, save a reviewed AI proposal,
or build from supported public source links. Every path leads to the same editable Studio draft,
preview, validation, and publish workflow.

Learners can discover public courses, request or complete enrollment, study authorized lesson
content, submit server-graded quizzes and text assignments, attend scheduled external-link classes,
and see progress backed by PostgreSQL data. AI, research, payment, image, YouTube, and voice tools
remain optional and fail clearly when their credentials or services are unavailable.

## Live Demo

| | |
|---|---|
| **Web app** | https://ifa-ai-red.vercel.app |
| **API** | https://ifa-api-8kgf.onrender.com/api/health |
| **Demo learner** | `nathnel@ifa.local` / `ifa12345` |

Demo accounts and catalog data are seeded in Development. A production deployment exposes demo
login only when `DEMO_DATA_ENABLED=true` is explicitly configured.

> The API runs on Render's free tier and sleeps after ~15 minutes idle. The first request afterwards
> takes about a minute to wake; the health endpoint above is the quickest way to warm it up.

## How It Works

Manual authoring is a first-class path and does not require an AI provider. When AI is configured,
its proposal remains an inspectable draft that the educator must review and edit.

```text
Manual | Template | Reviewed AI | Supported Sources
                       |
                       v
             Persisted Studio Draft
                       |
                Preview + Validate
                       |
                       v
        Published Delivery + Saved Progress
```

1. **Choose a creation path.** Blank manual draft, reusable template, reviewed topic proposal,
   conversation brief, or supported public PDF/text/web sources.
2. **Edit one shared course.** Configure course information, access rules, modules, lessons, MCQs,
   assignments, branding, and external-link live sessions in Studio.
3. **Preview and validate.** Preview reads the actual saved course. Publishing rejects missing
   objectives, incomplete lessons, invalid assessments, unfinished generation, and invalid access settings.
4. **Enroll and learn.** Public, unlisted, private, open, approval, invite, date, capacity, and paid
   pending states are enforced by the API rather than only hidden in the UI.
5. **Persist evidence.** Lesson completion, quiz attempts, assignments, feedback, skills,
   recommendations, and educator metrics derive from saved learner actions.

Source extraction is bounded and source metadata stays attached to the educator project. Witforge
does not claim that model-generated text is a verified citation merely because a source URL exists.

## Features

### Learning experience
- **Home dashboard** with progress and assessment-backed learning suggestions
- **My Courses** (`/courses`) — enrolled, created and completed courses in one library
- **Courses Library** with search, filter and sort
- **My Skills & Skill Profile** with radar charts and per-skill breakdowns
- **Progress Dashboard** with analytics and activity charts
- **Research Workspace** with academic (OpenAlex, Semantic Scholar, arXiv), web and YouTube sources
- **Settings & Preferences** for learning, resources, notifications and theme
- **Global Search Command Palette** (⌘K / Ctrl+K) for keyboard-driven navigation
- **Notification Center** backed by persisted API notifications and a learner profile menu

### AI-powered
- **Optional model pipeline** — conversational brief, course architect, and content builder
- **Shared Studio creation** — `/chat` routes educators to Studio and learners to the course-aware tutor
- **Configured research providers** — OpenAlex, Semantic Scholar, arXiv, Tavily and Wikimedia Commons
- **Educator Studio** — turn linked materials into structured courses: source panel, AI research
  suggestions, conflict/duplicate resolution, blueprint review, background module-by-module
  generation with per-module jobs and notifications
- **YouTube** resource matching, including a preferred channel
- **Voxide voice navigation** — spoken requests map onto the same capabilities the UI calls
- **AI Tutor** — Socratic learning assistant, course-aware
- **Adaptive engine** — assessment-driven content shaping
- **Recommendation engine** — actions linked to persisted weak-skill evidence and matching published metadata

### Social learning
- **Course sharing** with expiring links and backend-enforced visibility/enrollment rules
- **Course marketplace** for discovering educator-published courses
- **Paid-course handoff** via configured **Chapa** checkout and webhook verification; payment is never simulated
- **Activity stream** for peer activity
- **Peer progress comparison** for skill benchmarking

### Landing page
A standalone marketing page (`client/src/routes/+page.svelte`) with an animated hero, a labeled workflow
preview, and dedicated sections for Educator Studio, academic research and optional Voxide voice. Fully
theme-aware (light/dark) and reduced-motion aware.

## Technology Stack

### Frontend
- **SvelteKit 2** (Svelte 5) with **TypeScript**
- **Tailwind CSS** with the existing design tokens (internal `ifa-*` names retained for compatibility)
- **Lucide Svelte** icons
- **Voxide** browser SDK for voice

### Backend
- **ASP.NET Core** (.NET 10) — Clean/Onion Architecture
- **Entity Framework Core** with **PostgreSQL**
- **Gemini** — `gemini-3.5-flash` (conversation, architecture, tutor)
- **Groq** — configured hosted inference; cross-hosted-provider failover is opt-in
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
    Development schema and demo data are created automatically on first start (see
    [Database & Schema](#database--schema)). Production demo data is opt-in.

3. **Frontend**
   ```bash
   cd client
   npm install
   cp .env.example .env                      # VITE_API_URL, VITE_VOXIDE_KEY
   npm run dev                               # http://localhost:5173
   ```

4. **Free / local AI (Ollama)** — optional, no API key required

   The LLM gateway supports Ollama as a zero-cost provider. If no AI provider is
   reachable, AI routes return `503 ai_unavailable`; manual and template course
   creation continue to work without synthetic fallback content.

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
AI_ALLOW_CROSS_PROVIDER_FAILOVER=false
GEMINI_API_KEY=your_gemini_api_key
GROQ_API_KEY=your_groq_api_key
OLLAMA_BASE_URL=http://localhost:11434

# External services
SEMANTIC_SCHOLAR_API_KEY=your_semantic_scholar_api_key   # optional, raises rate limits
TAVILY_API_KEY=your_tavily_api_key                       # optional, web/image search
YOUTUBE_API_KEY=your_youtube_api_key

# Payments — Chapa (Telebirr supported). Sandbox by default.
CHAPA_SECRET_KEY=your_chapa_secret_key
CHAPA_BASE_URL=https://api.chapa.co/v1
CHAPA_WEBHOOK_SECRET=your_chapa_webhook_secret

# Apply EF migrations at startup (recommended for existing and production databases).
DB_USE_MIGRATIONS=true

# Development seeds demo data automatically. Production requires explicit opt-in.
DEMO_DATA_ENABLED=false

# CORS — comma-separated frontend origins. Required; the API uses
# AllowCredentials(), so a wildcard origin is not accepted.
CORS_ALLOWED_ORIGINS=http://localhost:5173,https://yourapp.vercel.app

ASPNETCORE_ENVIRONMENT=Development
ASPNETCORE_URLS=http://+:8080
```

Connection resolution order: `ConnectionStrings:DefaultConnection` → `DATABASE_URL` → the `DB_*`
values. Provider tuning lives in `src/IFA.API/appsettings.json` (models, per-pipeline provider).

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

Set `DB_USE_MIGRATIONS=true` for existing and production databases. Migration
`20261029110932_AddWitforgeCourseWorkflow` adds publication/access settings, live sessions,
discussions, assignments, and submissions while preserving legacy course visibility and user data.

For local empty databases the create-if-empty path remains available. It also reconciles the known
pre-migration Witforge startup schema before stamping migration history. Do not reset or drop a
schema to adopt these changes; take a normal database backup and apply the migrations.

Development seeds demo accounts and catalog data. Production does not seed or expose demo login
unless `DEMO_DATA_ENABLED=true` is intentionally set.

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

- [Upgrade audit and delivery](docs/WITFORGE_UPGRADE.md) — implemented slice, verification, and remaining dependencies
- [Demo script](docs/DEMO_SCRIPT.md) — a single end-to-end use case for presenting Witforge
- [Pitch deck](docs/PITCH_DECK.md) — problem, solution, architecture, roadmap
- [Changelog](docs/stark-changelog.md) — milestone history
- [Implementation plan](IFA_AI_Learning_Companion_Implementation_Plan_v3.md) — design and PR tracking

## Acknowledgments

- Gemini AI · Groq AI · Ollama
- OpenAlex · Semantic Scholar · arXiv · Tavily · Wikimedia Commons
- Chapa (Telebirr)
- Voxide
