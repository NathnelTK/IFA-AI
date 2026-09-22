import { writable, derived, get } from 'svelte/store';
import type {
  GeneratedModuleResult,
  PipelineModuleProposal,
  QuizResult
} from '../types';

// -----------------------------------------------------------------------------
// Learning session store — drives the Just-In-Time module experience (PR 4.2).
//
// Mirrors the three-model pipeline: the Course Architect (Model 2) proposes a
// blueprint of modules, and each module's concrete lesson/quiz is materialised
// on demand by the Course Builder (Model 3). Here that build step is produced
// locally with the same deterministic shape the backend falls back to, so the
// interactive flow works without a live API. Swap `buildModuleContent` for a
// POST to /api/coursepipeline/build-module to go live.
// -----------------------------------------------------------------------------

export type ModuleBuildStatus = 'blueprint' | 'generating' | 'ready';

export const courseTitle = writable('C# Backend Development');

export const courseBlueprint = writable<PipelineModuleProposal[]>([
  {
    id: 1,
    title: 'REST APIs with ASP.NET Core & C#',
    summary: 'Foundations of Minimal APIs, controllers, routing, and HTTP verbs.',
    estimatedHours: 4,
    topics: ['Minimal APIs', 'Endpoints & DTOs', 'Dependency Injection']
  },
  {
    id: 2,
    title: 'Database Architecture with EF Core',
    summary: 'Data modeling, migrations, PostgreSQL integration, and repository patterns.',
    estimatedHours: 5,
    topics: ['Entity Framework Core', 'LINQ queries', 'Database Migrations']
  },
  {
    id: 3,
    title: 'Authentication & JWT Security',
    summary: 'Securing endpoints, bearer tokens, role claims, and policy middleware.',
    estimatedHours: 3,
    topics: ['JWT Auth', 'Password Hashing', 'Role-based authorization']
  },
  {
    id: 4,
    title: 'Architecture, Clean Code & Testing',
    summary: 'Clean/Onion architecture principles, unit testing, and Docker deployment.',
    estimatedHours: 4,
    topics: ['Clean Architecture', 'xUnit Tests', 'Docker Containerization']
  }
]);

/** Materialised modules keyed by module number. */
export const generatedModules = writable<Record<number, GeneratedModuleResult>>({});

/** Per-module build status keyed by module number. */
export const moduleStatus = writable<Record<number, ModuleBuildStatus>>({});

/** Passed-quiz results keyed by module number. */
export const quizResults = writable<Record<number, QuizResult>>({});

/** The module the learner is currently viewing (1-based). */
export const activeModuleNumber = writable(1);

/** Overall course progress: share of blueprint modules with a passing quiz. */
export const courseProgress = derived(
  [courseBlueprint, quizResults],
  ([$blueprint, $results]) => {
    const total = $blueprint.length || 1;
    const passed = Object.values($results).filter((r) => r.passed).length;
    return Math.round((passed / total) * 100);
  }
);

function slugForModule(index: number): { videoId: string; videoTitle: string; doi: string; paper: string } {
  const catalog = [
    {
      videoId: 'AhAxLiGC7Pc',
      videoTitle: 'ASP.NET Core Minimal APIs — Full Course',
      doi: '10.1145/3468264.3468575',
      paper: 'Design Principles of Modern Web APIs'
    },
    {
      videoId: 'qkJ9keBmQWo',
      videoTitle: 'Entity Framework Core in .NET — Deep Dive',
      doi: '10.1109/TSE.2019.2942811',
      paper: 'Object-Relational Mapping: Patterns and Pitfalls'
    },
    {
      videoId: '_XbXkVdoG_0',
      videoTitle: 'JWT Authentication in ASP.NET Core',
      doi: '10.1145/3319535.3363192',
      paper: 'Token-Based Authentication for Stateless Services'
    },
    {
      videoId: 'RfE83Sbg5U8',
      videoTitle: 'Clean Architecture with .NET — Practical Guide',
      doi: '10.1109/MS.2012.51',
      paper: 'Empirical Studies in Clean Architecture'
    }
  ];
  return catalog[(index - 1) % catalog.length];
}

/**
 * Build one module's concrete content. Deterministic and instant-ish; stands in
 * for the Model 3 Course Builder call. Returns the same shape as the API's
 * GeneratedModuleResult so this can be swapped for a real fetch with no change
 * to the components that consume it.
 */
function buildModuleContent(proposal: PipelineModuleProposal): GeneratedModuleResult {
  const media = slugForModule(proposal.id);
  const objectives = proposal.topics.map((t) => `Understand and apply **${t}**.`);

  const contentMarkdown = `# ${proposal.title}

## Overview
${proposal.summary} This module grounds the concepts in production-ready practice so you can ship real features with confidence.

### Learning Objectives
${objectives.map((o) => `- ${o}`).join('\n')}

### Code Walkthrough
A minimal, idiomatic example you can run today:

\`\`\`csharp
var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.MapGet("/api/${proposal.topics[0]?.toLowerCase().replace(/[^a-z]/g, '') || 'resource'}", () =>
    Results.Ok(new { status = "success", module = ${proposal.id} }));

app.Run();
\`\`\`

### Best Practices
1. Keep business rules independent of frameworks and transport.
2. Validate input at the edge and return precise HTTP status codes.
3. Cover the happy path *and* the failure path with automated tests.`;

  return {
    module: {
      id: crypto.randomUUID(),
      moduleNumber: proposal.id,
      title: proposal.title,
      summary: proposal.summary,
      estimatedHours: proposal.estimatedHours,
      generationStatus: 'Ready',
      isGenerated: true
    },
    lesson: {
      id: crypto.randomUUID(),
      lessonNumber: 1,
      title: `${proposal.title} — Fundamentals`,
      summary: proposal.summary,
      contentMarkdown,
      readingTimeMinutes: Math.max(8, proposal.estimatedHours * 3),
      youTubeVideoId: media.videoId,
      youTubeVideoTitle: media.videoTitle,
      scholarxivCitationDoi: media.doi,
      scholarxivPaperTitle: media.paper,
      keyTakeaways: proposal.topics.map((t) => `You can now work confidently with ${t}.`)
    },
    quiz: {
      id: crypto.randomUUID(),
      title: `${proposal.title} Diagnostic`,
      passingScorePercentage: 70,
      questions: [
        {
          id: crypto.randomUUID(),
          prompt: `Which layer should contain the core rules for "${proposal.topics[0]}" with zero framework dependencies?`,
          options: ['Domain layer', 'Infrastructure layer', 'API layer', 'Web host'],
          correctOptionIndex: 0,
          explanation: 'The Domain layer stays persistence-ignorant and framework-free so business rules remain portable and testable.',
          targetSkillName: proposal.topics[0] ?? proposal.title
        },
        {
          id: crypto.randomUUID(),
          prompt: `When exposing "${proposal.topics[1] ?? proposal.title}", which HTTP status best signals a successful resource read?`,
          options: ['500 Internal Server Error', '200 OK', '403 Forbidden', '301 Moved Permanently'],
          correctOptionIndex: 1,
          explanation: '200 OK indicates the request succeeded and the response body carries the requested representation.',
          targetSkillName: proposal.topics[1] ?? proposal.title
        },
        {
          id: crypto.randomUUID(),
          prompt: 'What is the primary benefit of dependency injection in this module?',
          options: [
            'It makes classes depend on concrete implementations',
            'It removes the need for any interfaces',
            'It decouples components and makes them unit-testable',
            'It guarantees faster runtime performance'
          ],
          correctOptionIndex: 2,
          explanation: 'DI inverts control of dependencies, letting you substitute implementations (e.g. fakes in tests) without changing consumers.',
          targetSkillName: 'Dependency Injection'
        }
      ]
    }
  };
}

/**
 * Materialise a module on demand (JIT). Idempotent: returns the cached result
 * if it was already built. Simulates the builder latency for a realistic feel.
 */
export async function buildModule(moduleNumber: number): Promise<GeneratedModuleResult> {
  const existing = get(generatedModules)[moduleNumber];
  if (existing) return existing;

  const proposal = get(courseBlueprint).find((m) => m.id === moduleNumber);
  if (!proposal) throw new Error(`No blueprint module #${moduleNumber}`);

  moduleStatus.update((s) => ({ ...s, [moduleNumber]: 'generating' }));

  // Simulated Model 3 build latency. Replace this block with:
  //   const res = await fetch(`${API}/api/coursepipeline/build-module`, { ... })
  await new Promise((resolve) => setTimeout(resolve, 1400));
  const result = buildModuleContent(proposal);

  generatedModules.update((m) => ({ ...m, [moduleNumber]: result }));
  moduleStatus.update((s) => ({ ...s, [moduleNumber]: 'ready' }));
  return result;
}

/** Record a quiz result and, if passed, unlock the next module blueprint. */
export function recordQuizResult(moduleNumber: number, result: QuizResult): void {
  quizResults.update((r) => ({ ...r, [moduleNumber]: result }));
}

/** True when the given module's quiz has been passed. */
export function isModulePassed(moduleNumber: number): boolean {
  return Boolean(get(quizResults)[moduleNumber]?.passed);
}
