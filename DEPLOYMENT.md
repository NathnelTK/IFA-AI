# IFA-AI Production Deployment Guide

## Overview

IFA (Intelligent Future Academy) is an AI-powered adaptive learning companion built with .NET 10 and SvelteKit. This guide covers production deployment with PostgreSQL.

## Architecture

### Backend (.NET 10)
- **Clean Architecture** with Domain, Application, Infrastructure, and API layers
- **Entity Framework Core** with PostgreSQL
- **JWT Authentication** with secure token management
- **Multi-Model AI Pipeline**: Gemini, Groq, and Ollama LLM providers
- **RESTful API** with comprehensive endpoints

### Frontend (SvelteKit)
- **Modern SvelteKit** application with TypeScript
- **Tailwind CSS** for styling
- **Component-based architecture**
- **Responsive design** for all devices

### Database
- **PostgreSQL** primary database (production)
- **SQLite** fallback for local development
- **Entity Framework migrations** for schema management

## Prerequisites

### Required Software
- **.NET 10 SDK** or later
- **Node.js 18+** with npm
- **PostgreSQL 12+** database server
- **Docker** and Docker Compose (optional)

### API Keys
- **Groq API Key** and **ScholarXiv API Key** are used for the Ethiopian entrance-exam tutor, research, and module generation.
- **YouTube API Key** is optional; no video results are returned when it is not configured.
- **Gemini API Key** is optional for the other AI pipelines.

## Environment Configuration

### 1. Environment Variables

Copy `.env.example` to `.env` and configure:

```bash
# Database Configuration (PostgreSQL for Production)
DB_PROVIDER=postgres
DB_HOST=your-postgres-host
DB_PORT=5432
DB_NAME=ifa_production
DB_USER=your_db_user
DB_PASSWORD=your_secure_password

# JWT Security
JWT_SECRET=replace_with_a_unique_random_secret_of_at_least_32_characters

# AI Providers
GEMINI_API_KEY=your_gemini_api_key
GROQ_API_KEY=your_groq_api_key
SCHOLARXIV_API_KEY=your_scholarxiv_api_key
OLLAMA_BASE_URL=http://localhost:11434
AI_DEFAULT_PROVIDER=Groq

# External Services
YOUTUBE_API_KEY=your_youtube_api_key

# CORS Configuration
CORS_ALLOWED_ORIGINS=https://yourapp.com,https://www.yourapp.com

# Application Settings
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_URLS=https://+:443;http://+:80
```

The API seeds a public 12-week Ethiopian Grade 12 Natural Science preparation outline at startup. Lessons and quizzes are created on demand and require working Groq and ScholarXiv credentials. The outline is an independent study plan, not an official Ministry of Education syllabus or exam paper.

### 2. Database Setup

#### Option A: Using Docker Compose
```bash
# Start PostgreSQL container
docker-compose up postgres -d

# The database will be created automatically when the API starts
```

#### Option B: Manual PostgreSQL Setup
```sql
-- Connect to PostgreSQL as admin user
CREATE DATABASE ifa_production;
CREATE USER ifa_user WITH PASSWORD 'secure_password';
GRANT ALL PRIVILEGES ON DATABASE ifa_production TO ifa_user;
```

## Deployment Options

### Option 1: Docker Deployment (Recommended)

#### 1. Build and Run with Docker Compose
```bash
# Clone the repository
git clone https://github.com/your-org/ifa-ai.git
cd ifa-ai

# Configure environment variables
cp .env.example .env
# Edit .env with your production values

# Build and start all services
docker-compose up --build -d

# Check service status
docker-compose ps
```

#### 2. Access the Application
- **API**: http://localhost:5011
- **Web App**: http://localhost:5173
- **API Documentation**: http://localhost:5011/swagger

### Option 2: Manual Deployment

#### 1. Backend Deployment
```bash
# Navigate to API directory
cd src/IFA.API

# Restore dependencies
dotnet restore

# Build the application
dotnet build --configuration Release

# Run database migrations
dotnet ef database update --project ../IFA.Infrastructure

# Publish the application
dotnet publish --configuration Release --output ./publish

# Run the application
cd publish
dotnet IFA.API.dll
```

#### 2. Frontend Deployment
```bash
# Navigate to client directory
cd client

# Install dependencies
npm install

# Build for production
npm run build

# Serve the built application (using a static file server)
npm run preview
```

## Production Considerations

### Security
- **Never commit** `.env` files to version control
- Use **strong JWT secrets** (minimum 32 characters)
- **Enable HTTPS** in production environments
- **Rotate API keys** regularly
- **Validate CORS origins** for your domain

### Database
- **Use PostgreSQL** in production (SQLite is development-only)
- **Enable connection pooling** for better performance
- **Set up automated backups**
- **Monitor database performance**

### Monitoring
- **Health Check Endpoint**: `/api/health`
- **Application Insights** or equivalent logging
- **Database connection monitoring**
- **API response time tracking**

### Scaling
- **Use reverse proxy** (nginx, Apache) for static files
- **Configure load balancing** for multiple API instances
- **Database read replicas** for high traffic
- **CDN** for static assets

## API Endpoints

### Authentication
- `POST /api/auth/register` - User registration
- `POST /api/auth/login` - User login
- `POST /api/auth/demo-login` - Demo access
- `GET /api/auth/me` - Get current user profile

### Core Features
- `/api/courses` - Course management
- `/api/ai` - AI tutoring and assistance
- `/api/research` - Academic research integration
- `/api/recommendations` - Personalized recommendations
- `/api/health` - System health check

## Troubleshooting

### Common Issues

#### Database Connection Issues
```bash
# Check PostgreSQL status
systemctl status postgresql

# Test connection string
psql -h your-host -p 5432 -U your-user -d your-database
```

#### Missing API Keys
- Verify all required API keys are set in `.env`
- Check API key permissions and quotas
- Ensure keys are properly formatted

#### CORS Errors
- Add your domain to `CORS_ALLOWED_ORIGINS`
- Ensure protocol (http/https) matches
- Check for trailing slashes in URLs

### Logs and Debugging
```bash
# View Docker Compose logs
docker-compose logs api
docker-compose logs web

# Check application logs
tail -f /var/log/ifa/api.log

# Enable detailed logging
export ASPNETCORE_ENVIRONMENT=Development
```

## Maintenance

### Database Migrations
```bash
# Create new migration
dotnet ef migrations add MigrationName --project src/IFA.Infrastructure

# Apply migrations to production
dotnet ef database update --project src/IFA.Infrastructure
```

### Updates
```bash
# Pull latest code
git pull origin main

# Rebuild and restart services
docker-compose down
docker-compose up --build -d
```

## Support

For deployment issues or questions:
- Check the [GitHub Issues](https://github.com/your-org/ifa-ai/issues)
- Review the application logs
- Verify environment configuration

## Security Notice

This application handles user data and AI interactions. Ensure:
- All environment variables are properly secured
- Database connections use encrypted connections
- HTTPS is enabled for all production traffic
- Regular security updates are applied