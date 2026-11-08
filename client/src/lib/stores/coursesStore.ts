import { writable, derived, get } from 'svelte/store';
import type { Course, CourseStatus, Lesson, Module, Quiz } from '$lib/types';
import { coursesApi, lessonsApi, getAuthToken } from '$lib/api';
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
 * Starts empty so a brand-new learner sees only the courses they actually create
 * or enrol in. `loadCourses()` fills it from the learner's real enrollments; an
 * empty API response leaves it empty. Completion is applied only after the
 * server confirms it. Public course previews never become local enrollments.
 */

export const courses = writable<Course[]>([]);
export const courseDetails = writable<Record<string, Course>>({});
export const coursesError = writable('');

/** True while an API load is in flight (drives optional loading affordances). */
export const coursesLoading = writable(false);
/** Retained for existing consumers; no demo courses are injected. */
export const coursesSource = writable<'demo' | 'api'>('demo');
let loadVersion = 0;

export function resetCourses() {
  loadVersion += 1;
  courses.set([]);
  courseDetails.set({});
  coursesError.set('');
  coursesLoading.set(false);
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
  const accessible = $c.filter((c) => !c.isOwner && c.enrollmentStatus === 'Active' && c.hasContentAccess !== false);
  const inProgress = accessible.filter((c) => c.status === 'inProgress');
  return inProgress[0] ?? accessible[0] ?? null;
});

/** Weight authoritative course progress by its lesson and required-quiz units. */
export const overallProgress = derived(courses, ($c) => {
  const active = $c.filter((course) => !course.isOwner && course.enrollmentStatus === 'Active');
  const units = (course: Course) => Math.max(1, totalLessons(course) + (course.details?.requirePassingQuizzes ? course.modules.reduce((sum, module) => sum + module.quizzes.length, 0) : 0));
  const total = active.reduce((sum, course) => sum + units(course), 0);
  const done = active.reduce((sum, course) => sum + units(course) * courseProgress(course) / 100, 0);
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

export async function markLessonComplete(courseId: string, lessonId: string): Promise<void> {
  const token = getAuthToken();
  const result = await lessonsApi.complete(lessonId);
  if (token !== getAuthToken()) return;
  const update = (course: Course): Course => {
    if (course.id !== courseId) return course;
    const updated = {
      ...course,
      progressPercent: result.progressPercentage,
      lastAccessed: 'Just now',
      modules: course.modules.map((module) => ({
        ...module,
        lessons: module.lessons.map((lesson) => lesson.id === lessonId ? { ...lesson, completed: result.isCompleted } : lesson)
      }))
    };
    return { ...updated, status: recomputeStatus(updated) };
  };
  courses.update((rows) => rows.map(update));
  courseDetails.update((rows) => rows[courseId] ? { ...rows, [courseId]: update(rows[courseId]) } : rows);
}

export function toggleBookmark(courseId: string) {
  courses.update(($c) =>
    $c.map((c) => (c.id === courseId ? { ...c, isBookmarked: !c.isBookmarked } : c))
  );
  courseDetails.update((rows) => rows[courseId] ? { ...rows, [courseId]: { ...rows[courseId], isBookmarked: !rows[courseId].isBookmarked } } : rows);
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
  if (words.length === 0) return 'W';
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
    provider: dto.providerName || 'Witforge',
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
    moduleInfoLabel: `Module ${active} of ${total}`,
    enrollmentStatus: dto.enrollmentStatus,
    isOwner: dto.isOwner,
    hasContentAccess: dto.hasContentAccess,
    isPublished: dto.isPublished
  };
}

function mapLesson(dto: BackendLessonDto): Lesson {
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
    completed: dto.isCompleted === true
  };
}

function mapQuiz(dto: BackendQuizDto): Quiz {
  return {
    id: dto.id,
    title: dto.title,
    isExam: (dto.kind ?? 1) === 1,
    orderIndex: dto.orderIndex ?? 0,
    bestScore: dto.latestAttempt?.scorePercentage ?? null,
    latestAttempt: dto.latestAttempt ?? null,
    attemptCount: dto.attemptCount ?? 0,
    passingScorePercentage: dto.passingScorePercentage,
    maxAttempts: dto.maxAttempts,
    isPassed: dto.isPassed ?? false,
    questionCount: dto.questionCount ?? dto.questions?.length ?? 0,
    questions: (dto.questions ?? []).map((q) => ({
      id: q.id,
      prompt: q.prompt,
      options: q.options ?? [],
      correctIndex: q.correctOptionIndex,
      explanation: q.explanation
    }))
  };
}

function mapModule(dto: BackendModuleDto): Module {
  return {
    id: dto.id,
    title: dto.title,
    summary: dto.summary,
    lessons: (dto.lessons ?? [])
      .slice()
      .sort((a, b) => a.lessonNumber - b.lessonNumber)
      .map(mapLesson),
    quizzes: (dto.quizzes ?? [])
      .slice()
      .sort((a, b) => (a.orderIndex ?? 0) - (b.orderIndex ?? 0))
      .map(mapQuiz)
  };
}

/** Merge a full course detail response into an existing store row. */
function mergeDetail(base: Course, detail: CourseDetailDto): Course {
  return {
    ...base,
    title: detail.title,
    provider: detail.providerName || base.provider,
    description: detail.description,
    category: detail.category,
    duration: detail.estimatedDuration,
    progressPercent: detail.progressPercentage ?? 0,
    status: statusFromProgress(detail.progressPercentage ?? 0),
    moduleInfoLabel: undefined,
    isOwner: detail.isOwner,
    hasContentAccess: detail.hasContentAccess,
    isPublished: detail.isPublished,
    enrollmentStatus: detail.enrollmentStatus,
    details: detail,
    modules: (detail.modules ?? []).slice().sort((a, b) => a.moduleNumber - b.moduleNumber).map(mapModule)
  };
}

/**
 * Load the learner's enrolled courses from the API and enrich each with its
 * module/lesson detail. Identity checks keep delayed responses from a previous
 * account out of the current learner's library.
 */
export async function loadCourses(): Promise<void> {
  const token = getAuthToken();
  const version = ++loadVersion;
  coursesLoading.set(true);
  coursesError.set('');
  try {
    const summaries = await coursesApi.listMine();
    if (version !== loadVersion || token !== getAuthToken()) return;
    if (!summaries || summaries.length === 0) {
      // No enrollments — a new learner's library stays empty.
      courses.set([]);
      coursesSource.set('api');
      return;
    }

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

    if (version !== loadVersion || token !== getAuthToken()) return;
    courses.set(enriched);
    coursesSource.set('api');
  } catch (cause) {
    if (version !== loadVersion || token !== getAuthToken()) return;
    coursesError.set(cause instanceof Error ? cause.message : 'Could not load your courses.');
  } finally {
    if (version === loadVersion) coursesLoading.set(false);
  }
}

/** Fetch a single course's full detail and merge it into the store. */
export async function loadCourseDetail(courseId: string): Promise<Course | null> {
    const token = getAuthToken();
    const detail = await coursesApi.getById(courseId);
    if (token !== getAuthToken()) return null;
    const existing = get(courses).find((c) => c.id === courseId);
    const mapped = mergeDetail(
      existing ??
        {
          id: detail.id,
          title: detail.title,
          provider: detail.providerName || 'Witforge',
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
    courseDetails.update((rows) => ({ ...rows, [courseId]: mapped }));
    if (!mapped.isOwner && mapped.enrollmentStatus === 'Active') {
      courses.update((rows) => rows.some((c) => c.id === courseId) ? rows.map((c) => c.id === courseId ? mapped : c) : [...rows, mapped]);
    }
    return mapped;
}
