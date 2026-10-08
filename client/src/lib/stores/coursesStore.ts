import { writable, derived, get } from 'svelte/store';
import type { Course, CourseStatus, Lesson, Module, Quiz } from '$lib/types';
import { demoCourses } from '$lib/data/catalog';
import { coursesApi, lessonsApi } from '$lib/api';
import type {
	BackendLessonDto,
	BackendModuleDto,
	BackendQuizDto,
	CourseDetailDto,
	CourseSummaryDto
} from '$lib/api';

/**
 * Courses store — the single reactive source of truth for enrolled courses.
 *
 * Seeded from the demo catalog so the app renders instantly and offline. When
 * the API is reachable, `loadCourses()` replaces the seed with the learner's real
 * enrollments (and demo data is kept on any failure). Lesson/quiz interactions
 * update the store optimistically and are mirrored to the backend when the ids
 * are real GUIDs.
 */

// Deep clone so mutations never leak back into the immutable catalog seed.
const seed: Course[] = JSON.parse(JSON.stringify(demoCourses));

export const courses = writable<Course[]>(seed);

/** True while an API load is in flight (drives optional loading affordances). */
export const coursesLoading = writable(false);
/** Where the current rows came from — 'demo' until a successful API load. */
export const coursesSource = writable<'demo' | 'api'>('demo');

const GUID_PATTERN = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i;

function isGuid(value: string): boolean {
	return GUID_PATTERN.test(value);
}

/** Total lessons across a course's modules. */
function totalLessons(course: Course): number {
  return course.modules.reduce((sum, m) => sum + m.lessons.length, 0);
}

/** Completed lessons across a course's modules. */
function completedLessons(course: Course): number {
  return course.modules.reduce(
    (sum, m) => sum + m.lessons.filter((l) => l.completed).length,
    0
  );
}

/** Course completion as a 0-100 integer. API enrollment progress wins when present. */
export function courseProgress(course: Course): number {
	if (typeof course.progressPercent === 'number') return course.progressPercent;
	const total = totalLessons(course);
	if (total === 0) return 0;
	return Math.round((completedLessons(course) / total) * 100);
}

/** "Module N of M" label based on the first module with an incomplete lesson. */
export function moduleInfo(course: Course): string {
	if (course.moduleInfoLabel) return course.moduleInfoLabel;
	const total = course.modules.length;
	const idx = course.modules.findIndex((m) => m.lessons.some((l) => !l.completed));
	const current = idx === -1 ? total : idx + 1;
	return `Module ${current} of ${total}`;
}

/** The next incomplete lesson in a course, or null if fully complete. */
export function nextLesson(course: Course) {
  for (const module of course.modules) {
    for (const lesson of module.lessons) {
      if (!lesson.completed) return { module, lesson };
    }
  }
  return null;
}

export const activeCourses = derived(courses, ($c) =>
  $c.filter((c) => c.status === 'inProgress')
);
export const completedCourses = derived(courses, ($c) =>
  $c.filter((c) => c.status === 'completed')
);
export const pausedCourses = derived(courses, ($c) =>
  $c.filter((c) => c.status === 'paused')
);
export const bookmarkedCourses = derived(courses, ($c) => $c.filter((c) => c.isBookmarked));

/** The primary course shown in the home "Continue Learning" card. */
export const activeCourse = derived(courses, ($c) => {
  const inProgress = $c.filter((c) => c.status === 'inProgress');
  return inProgress[0] ?? $c[0] ?? null;
});

/** Overall progress across all enrolled courses, weighted by lesson count. */
export const overallProgress = derived(courses, ($c) => {
  const total = $c.reduce((sum, c) => sum + totalLessons(c), 0);
  const done = $c.reduce((sum, c) => sum + completedLessons(c), 0);
  return total === 0 ? 0 : Math.round((done / total) * 100);
});

export function getCourse(id: string): Course | undefined {
  return get(courses).find((c) => c.id === id);
}

/** Recompute a course's status from its lesson progress (unless paused). */
function recomputeStatus(course: Course): CourseStatus {
  if (course.status === 'paused') return 'paused';
  const progress = courseProgress(course);
  if (progress >= 100) return 'completed';
  if (progress > 0) return 'inProgress';
  return course.status === 'notStarted' ? 'notStarted' : 'inProgress';
}

export function markLessonComplete(courseId: string, lessonId: string, completed = true) {
  courses.update(($c) =>
    $c.map((course) => {
      if (course.id !== courseId) return course;
      const updated: Course = {
        ...course,
        lastAccessed: 'Just now',
        modules: course.modules.map((m) => ({
          ...m,
          lessons: m.lessons.map((l) => (l.id === lessonId ? { ...l, completed } : l))
        }))
      };
      updated.status = recomputeStatus(updated);
      return updated;
    })
  );

  // Mirror to the backend for real (GUID) lessons; demo ids stay local-only.
  if (completed && isGuid(lessonId)) {
    void lessonsApi.complete(lessonId).catch(() => {
      /* keep the optimistic local state */
    });
  }
}

export function recordQuizScore(courseId: string, quizId: string, score: number) {
  courses.update(($c) =>
    $c.map((course) => {
      if (course.id !== courseId) return course;
      return {
        ...course,
        lastAccessed: 'Just now',
        modules: course.modules.map((m) => ({
          ...m,
          quizzes: m.quizzes.map((q) =>
            q.id === quizId
              ? { ...q, bestScore: Math.max(score, q.bestScore ?? 0) }
              : q
          )
        }))
      };
    })
  );
}

export function toggleBookmark(courseId: string) {
  courses.update(($c) =>
    $c.map((c) => (c.id === courseId ? { ...c, isBookmarked: !c.isBookmarked } : c))
  );
}

export function pauseCourse(courseId: string) {
  courses.update(($c) =>
    $c.map((c) => (c.id === courseId ? { ...c, status: 'paused' as CourseStatus } : c))
  );
}

export function resumeCourse(courseId: string) {
  courses.update(($c) =>
    $c.map((c) => {
      if (c.id !== courseId) return c;
      const resumed = { ...c, status: 'inProgress' as CourseStatus, lastAccessed: 'Just now' };
      resumed.status = recomputeStatus(resumed);
      return resumed;
    })
  );
}

/** Enroll in an external/marketplace course. No-op if already enrolled. */
export function enrollCourse(course: Course) {
  courses.update(($c) => {
    if ($c.some((c) => c.id === course.id)) return $c;
    return [...$c, { ...course, status: 'inProgress', enrolledDate: 'Just now', lastAccessed: 'Just now' }];
  });
}

export function isEnrolled(courseId: string): boolean {
  return get(courses).some((c) => c.id === courseId);
}

// ---------------------------------------------------------------------------
// API hydration
// ---------------------------------------------------------------------------

function relativeTime(iso?: string): string {
  if (!iso) return '';
  const then = new Date(iso).getTime();
  if (Number.isNaN(then)) return '';
  const minutes = Math.round((Date.now() - then) / 60000);
  if (minutes < 1) return 'Just now';
  if (minutes < 60) return `${minutes}m ago`;
  const hours = Math.round(minutes / 60);
  if (hours < 24) return `${hours}h ago`;
  const days = Math.round(hours / 24);
  if (days < 30) return `${days}d ago`;
  return new Date(iso).toLocaleDateString();
}

/** Short gradient-thumbnail label derived from the title (no image assets). */
function deriveThumbnail(title: string): string {
  const words = title.trim().split(/\s+/).filter(Boolean);
  if (words.length === 0) return 'IFA';
  if (words.length === 1) return words[0].slice(0, 4);
  return words
    .slice(0, 2)
    .map((w) => w[0])
    .join('')
    .toUpperCase();
}

function statusFromProgress(percent: number): CourseStatus {
  if (percent >= 100) return 'completed';
  if (percent > 0) return 'inProgress';
  return 'notStarted';
}

function mapSummary(dto: CourseSummaryDto): Course {
  const total = dto.totalModules || 1;
  const active = Math.min(Math.max(dto.activeModuleNumber || 1, 1), total);
  return {
    id: dto.id,
    title: dto.title,
    provider: dto.providerName || 'IFA AI',
    description: dto.description,
    thumbnail: deriveThumbnail(dto.title),
    category: dto.category,
    duration: dto.estimatedDuration,
    status: statusFromProgress(dto.progressPercentage),
    isBookmarked: false,
    enrolledDate: 'recently',
    lastAccessed: relativeTime(dto.lastAccessedAt),
    modules: [],
    progressPercent: dto.progressPercentage,
    moduleInfoLabel: `Module ${active} of ${total}`
  };
}

function mapLesson(dto: BackendLessonDto, existing?: Lesson): Lesson {
  const markdown = dto.contentMarkdown ?? '';
  return {
    id: dto.id,
    title: dto.title,
    summary: dto.summary,
    contentMarkdown: markdown,
    content: markdown
      .split(/\n{2,}/)
      .map((p) => p.trim())
      .filter(Boolean),
    youTubeVideoId: dto.youTubeVideoId ?? undefined,
    youTubeVideoTitle: dto.youTubeVideoTitle ?? undefined,
    duration: `${dto.readingTimeMinutes ?? 10} min`,
    completed: existing?.completed ?? false
  };
}

function mapQuiz(dto: BackendQuizDto, existing?: Quiz | null): Quiz {
  return {
    id: dto.id,
    title: dto.title,
    isExam: (dto.kind ?? 1) === 1,
    orderIndex: dto.orderIndex ?? 0,
    bestScore: existing?.bestScore ?? null,
    questions: (dto.questions ?? []).map((q) => ({
      id: q.id,
      prompt: q.prompt,
      options: q.options ?? [],
      correctIndex: q.correctOptionIndex ?? 0,
      explanation: q.explanation ?? ''
    }))
  };
}

function mapModule(dto: BackendModuleDto, existing?: Module): Module {
  const existingLessons = existing?.lessons ?? [];
  return {
    id: dto.id,
    title: dto.title,
    summary: dto.summary,
    lessons: (dto.lessons ?? [])
      .slice()
      .sort((a, b) => a.lessonNumber - b.lessonNumber)
      .map((l) => mapLesson(l, existingLessons.find((x) => x.id === l.id))),
    quizzes: (dto.quizzes ?? [])
      .slice()
      .sort((a, b) => (a.orderIndex ?? 0) - (b.orderIndex ?? 0))
      .map((q) => mapQuiz(q, existing?.quizzes?.find((x) => x.id === q.id)))
  };
}

/** Merge a full course detail response into an existing store row. */
function mergeDetail(base: Course, detail: CourseDetailDto): Course {
  const existingModules = base.modules ?? [];
  return {
    ...base,
    provider: detail.providerName || base.provider,
    description: detail.description || base.description,
    category: detail.category || base.category,
    duration: detail.estimatedDuration || base.duration,
    modules:
      detail.modules?.length > 0
        ? detail.modules
            .slice()
            .sort((a, b) => a.moduleNumber - b.moduleNumber)
            .map((m) => mapModule(m, existingModules.find((x) => x.id === m.id)))
        : existingModules
  };
}

/**
 * Load the learner's enrolled courses from the API and enrich each with its
 * module/lesson detail. Falls back silently to the demo catalog on any failure
 * (e.g. the API or database is not running).
 */
export async function loadCourses(): Promise<void> {
  coursesLoading.set(true);
  try {
    const summaries = await coursesApi.listMine();
    if (!summaries || summaries.length === 0) return; // keep demo seed

    const base = summaries.map(mapSummary);
    const enriched = await Promise.all(
      base.map(async (course) => {
        try {
          const detail = await coursesApi.getById(course.id);
          return mergeDetail(course, detail);
        } catch {
          return course;
        }
      })
    );

    courses.set(enriched);
    coursesSource.set('api');
  } catch {
    // API unreachable — keep the demo catalog so the UI stays usable.
  } finally {
    coursesLoading.set(false);
  }
}

/** Fetch a single course's full detail and merge it into the store. */
export async function loadCourseDetail(courseId: string): Promise<void> {
    const detail = await coursesApi.getById(courseId);
    courses.update(($c) => {
      const existing = $c.find((c) => c.id === courseId);
      if (existing) {
        return $c.map((c) => (c.id === courseId ? mergeDetail(c, detail) : c));
      }
      const mapped = mergeDetail(
        {
          id: detail.id,
          title: detail.title,
          provider: detail.providerName || 'IFA AI',
          description: detail.description,
          thumbnail: deriveThumbnail(detail.title),
          category: detail.category,
          duration: detail.estimatedDuration,
          status: 'notStarted',
          isBookmarked: false,
          enrolledDate: 'recently',
          lastAccessed: '',
          modules: []
        },
        detail
      );
      return [...$c, mapped];
    });
}
