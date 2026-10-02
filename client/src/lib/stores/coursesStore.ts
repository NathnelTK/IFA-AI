import { writable, derived, get } from 'svelte/store';
import type { Course, CourseStatus } from '$lib/types';
import { demoCourses } from '$lib/data/catalog';

/**
 * Courses store — the single reactive source of truth for enrolled courses.
 *
 * Seeded from the demo catalog. Because progress is derived from the lessons'
 * `completed` flags, marking a lesson complete (or passing a quiz) here updates
 * the home donut, My Learning, the Courses library, and the course detail page
 * all at once. Swap the seed for API data when the backend is ready.
 */

// Deep clone so mutations never leak back into the immutable catalog seed.
const seed: Course[] = JSON.parse(JSON.stringify(demoCourses));

export const courses = writable<Course[]>(seed);

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

/** Course completion as a 0-100 integer. */
export function courseProgress(course: Course): number {
  const total = totalLessons(course);
  if (total === 0) return 0;
  return Math.round((completedLessons(course) / total) * 100);
}

/** "Module N of M" label based on the first module with an incomplete lesson. */
export function moduleInfo(course: Course): string {
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
}

export function recordQuizScore(courseId: string, quizId: string, score: number) {
  courses.update(($c) =>
    $c.map((course) => {
      if (course.id !== courseId) return course;
      return {
        ...course,
        lastAccessed: 'Just now',
        modules: course.modules.map((m) =>
          m.quiz && m.quiz.id === quizId
            ? { ...m, quiz: { ...m.quiz, bestScore: Math.max(score, m.quiz.bestScore ?? 0) } }
            : m
        )
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
