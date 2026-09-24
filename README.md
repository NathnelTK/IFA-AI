# IFA — AI-Powered Adaptive Learning Companion

> **STARK Hackathon 2026** — Team XOR (Nathnel Teklemariam, Ermiyas Eshetu, Negede Tekleyes)

IFA is an intelligent learning companion that uses AI to provide personalized course recommendations, adaptive learning paths, and social learning features. The platform helps learners discover courses, track progress, connect with peers, and achieve their learning goals.

## Features

### Phase 1: Foundation
- **SvelteKit 2 + Svelte 5** frontend with Tailwind CSS
- **ASP.NET Core** Clean Architecture backend
- **PostgreSQL** database with Entity Framework Core
- **Docker** containerization for local development

### Phase 2: AI Infrastructure
- **Three-model pipeline**: Conversational Advisor, Course Architect, Course Builder
- **Scholarxiv** academic research integration
- **YouTube** resource integration
- **Voxide** voice interaction support

### Phase 3: Course Generation Pipeline
- Just-in-time module generation
- Adaptive Learning Engine
- Assessment-driven content shaping

### Phase 4: Full Learning Experience
- **My Learning Dashboard** with active, completed, and paused courses
- **Courses Library** with search, filter, and sort
- **My Skills & Skill Profile** with radar charts and skill breakdowns
- **Research Workspace** with Scholarxiv, web, and YouTube sources
- **Progress Dashboard** with analytics and activity charts
- **Settings & Preferences** for learning, resources, notifications, and theme
- **Global Search Command Palette** for keyboard-driven navigation
- **Notification Center** for managing alerts
- **Learner Profile Menu** for account navigation
- **Hero Goal Suggestions** for AI-recommended learning goals

### Phase 5: Social Learning & Marketplace
- **Course Sharing** with shareable links and enrollment codes
- **Course Marketplace** for public course discovery
- **Activity Stream** for peer activity feed
- **Peer Progress Comparison** for skill benchmarking
- **Recommendation Engine** for AI-powered course suggestions

### Phase 6: Hackathon Polish
- Performance optimization
- Accessibility improvements
- Deployment configuration
- Documentation and pitch materials

## Technology Stack

### Frontend
- **SvelteKit 2** (Svelte 5)
- **TypeScript**
- **Tailwind CSS**
- **Lucide Svelte** icons

### Backend
- **ASP.NET Core** (.NET 10)
- **Clean/Onion Architecture**
- **Entity Framework Core**
- **PostgreSQL**
- **Gemini** and **Groq** AI providers

### DevOps
- **Docker** & Docker Compose
- **EthioDeploy** deployment tooling
- **STARK** changelog automation

## Getting Started

### Prerequisites
- .NET 10 SDK
- Node.js 18+
- PostgreSQL 14+
- Docker (optional, for containerized development)

### Local Development

1. **Clone the repository**
   ```bash
   git clone <repository-url>
   cd IFA-AI
   ```

2. **Backend setup**
   ```bash
   # Restore dependencies
   dotnet restore

   # Configure environment
   cp src/IFA.API/appsettings.json src/IFA.API/appsettings.Development.json
   # Update connection string and API keys

   # Run migrations
   dotnet ef database update

   # Run API
   cd src/IFA.API
   dotnet run
   ```

3. **Frontend setup**
   ```bash
   cd client
   npm install
   npm run dev
   ```

4. **Docker (optional)**
   ```bash
   docker-compose up
   ```

### Environment Variables

#### Backend (src/IFA.API/appsettings.json)
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=ifa;Username=postgres;Password=your_password"
  },
  "AI": {
    "GeminiApiKey": "your_gemini_api_key",
    "GroqApiKey": "your_groq_api_key"
  },
  "Scholarxiv": {
    "ApiKey": "your_scholarxiv_api_key"
  }
}
```

#### Frontend (client/.env)
```env
VITE_API_URL=http://localhost:5000
```

## Project Structure

```
IFA-AI/
├── client/                 # SvelteKit frontend
│   ├── src/
│   │   ├── lib/           # Shared components and utilities
│   │   ├── routes/        # Page routes
│   │   └── stores/        # Svelte stores
│   ├── static/            # Static assets
│   └── package.json
├── src/
│   ├── IFA.Domain/        # Domain entities
│   ├── IFA.Application/   # Application logic
│   ├── IFA.Infrastructure/ # Data access and services
│   └── IFA.API/           # Web API
├── docs/                  # Documentation
├── docker/                # Docker configurations
└── package.json           # Root scripts
```

## Scripts

### Root
```bash
npm run changelog:add     # Add changelog entry
npm run changelog:verify  # Verify changelog format
```

### Backend
```bash
dotnet build              # Build solution
dotnet test               # Run tests
dotnet ef migrations add  # Add migration
dotnet ef database update # Apply migrations
```

### Frontend
```bash
npm run dev               # Start dev server
npm run build             # Build for production
npm run check             # Type-check with svelte-check
npm run lint              # Run ESLint
```

## Changelog

All project milestones are tracked in the STARK-formatted changelog:

- [docs/stark-changelog.md](docs/stark-changelog.md)

## Implementation Plan

Detailed implementation plan and PR tracking:

- [IFA_AI_Learning_Companion_Implementation_Plan_v3.md](IFA_AI_Learning_Companion_Implementation_Plan_v3.md)

## Team

- **Nathnel Teklemariam** — Team Lead
- **Ermiyas Eshetu** — Backend Development
- **Negede Tekleyes** — Frontend Development

## License

[License information]

## Acknowledgments

- STARK Hackathon 2026
- Gemini AI
- Groq AI
- Scholarxiv
