import { writable, get } from 'svelte/store';
import type { SkillProgress, Recommendation, RecentActivity, CourseCard } from '../types';
import { activeCourse as activeCourseFromCourses, overallProgress as overallProgressFromCourses } from './coursesStore';
import { activityApi, coursesApi, learnerApi, recommendationsApi, skillsApi } from '$lib/api';
import type {
	LearningActivityDto,
	LearnerSkillsResultDto,
	MarketplaceCourseDto,
	RecommendationDto
} from '$lib/api';
import { getLearnerId } from './sessionStore';
import { resolveCover } from '$lib/utils/cover';

export const userProfile = writable({
  name: 'Learner',
  role: 'Learner',
  avatar: 'https://images.unsplash.com/photo-1534528741775-53994a69daeb?w=100&auto=format&fit=crop&q=80',
  greeting: 'Welcome to IFA',
  tagline: 'Small steps today, big goals tomorrow.'
});

// Re-export from coursesStore for convenience
export const activeCourse = activeCourseFromCourses;
export const overallProgress = overallProgressFromCourses;

export const skillsList = writable<SkillProgress[]>([]);

export const recommendations = writable<Recommendation[]>([]);

export const recentActivities = writable<RecentActivity[]>([]);

export const publicCourses = writable<CourseCard[]>([]);

export const peerComparison = writable({
  enabled: false,
  user: 'Nathnel',
  peerName: 'Ermiyas',
  userProgress: 72,
  peerProgress: 61,
  skillComparison: [
    { name: 'C#', userPercentage: 84, peerPercentage: 78 },
    { name: 'Databases', userPercentage: 61, peerPercentage: 72 },
    { name: 'APIs', userPercentage: 55, peerPercentage: 48 },
    { name: 'Authentication', userPercentage: 45, peerPercentage: 38 },
    { name: 'Testing', userPercentage: 32, peerPercentage: 42 }
  ]
});

// ---------------------------------------------------------------------------
// API hydration — each source is independent. The stores start empty and are
// filled from the API, so a new learner with no data sees empty states instead
// of demo content. A request that errors simply leaves its store untouched.
// ---------------------------------------------------------------------------

const SKILL_ICONS = ['Terminal', 'Database', 'Network', 'KeyRound', 'CheckCircle2'];
const RECOMMENDATION_COLORS = ['#2A9D68', '#E07A5F', '#7C5CFC', '#E11D48', '#EF4444'];

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

function mapSkills(result: LearnerSkillsResultDto): SkillProgress[] {
  return result.categories.flatMap((category) =>
    category.skills.map((skill, index) => ({
      name: skill.name,
      percentage: skill.percentage,
      color: skill.color,
      iconName: SKILL_ICONS[index % SKILL_ICONS.length],
      isWeakArea: skill.percentage < 50
    }))
  );
}

function mapRecommendations(list: RecommendationDto[]): Recommendation[] {
  return list.map((item, index) => {
    const isCourse = item.actionType === 'view_course' && Boolean(item.courseId);
    return {
      id: item.id,
      title: item.title,
      subtitle: item.reason,
      type: isCourse ? 'continue' : 'practice',
      color: RECOMMENDATION_COLORS[index % RECOMMENDATION_COLORS.length],
      actionUrl: isCourse ? `/courses/${item.courseId}` : '/courses'
    };
  });
}

function iconForActivity(activityType: string): RecentActivity['iconType'] {
  if (activityType.includes('quiz')) return 'quiz';
  if (activityType.includes('research')) return 'research';
  if (activityType.includes('voice')) return 'voice';
  return 'module';
}

function mapActivities(list: LearningActivityDto[]): RecentActivity[] {
  return list.map((item) => ({
    id: item.id,
    title: item.title,
    detail: item.description,
    timeAgo: relativeTime(item.createdAt),
    iconType: iconForActivity(item.activityType ?? '')
  }));
}

function normalizeBadge(badge: string | null): CourseCard['badge'] {
  const allowed: NonNullable<CourseCard['badge']>[] = ['Popular', 'Bestseller', 'Trending', 'New'];
  return allowed.includes(badge as NonNullable<CourseCard['badge']>)
    ? (badge as NonNullable<CourseCard['badge']>)
    : undefined;
}

function mapPublicCourses(list: MarketplaceCourseDto[]): CourseCard[] {
  return list.map((course) => ({
    id: course.id,
    title: course.title,
    provider: course.providerName ? `By ${course.providerName}` : 'By IFA AI',
    badge: normalizeBadge(course.badge),
    rating: course.rating,
    reviewCount: course.reviewCount,
    duration: course.estimatedDuration,
    imageUrl: resolveCover(course.thumbnailUrl, course.title),
    shareCode: course.shareCode
  }));
}

/**
 * Hydrate the dashboard widgets (profile, skills, recommendations, activity,
 * public courses, peer comparison) from the API. Each store reflects exactly what
 * the API returns (including empty), so a new learner sees empty states rather
 * than demo data; a request that errors leaves its store untouched.
 */
export async function loadDashboard(): Promise<void> {
  const learnerId = getLearnerId();
  const activeCourseId = get(activeCourseFromCourses)?.id;

  const tasks: Promise<void>[] = [];

  tasks.push(
    learnerApi
      .me()
      .then((me) => {
        const name = me.name || 'Learner';
        userProfile.update((profile) => ({
          ...profile,
          name,
          role: me.role || profile.role,
          avatar: me.avatarUrl || profile.avatar,
          greeting: `Good morning, ${name}`
        }));
      })
      .catch(() => undefined)
  );

  if (learnerId) {
    tasks.push(
      skillsApi
        .forLearner(learnerId)
        .then((result) => {
          const mapped = mapSkills(result);
          skillsList.set(mapped);
        })
        .catch(() => undefined)
    );
  }

  tasks.push(
    recommendationsApi
      .list()
      .then((list) => {
        const mapped = mapRecommendations(list);
        recommendations.set(mapped);
      })
      .catch(() => undefined)
  );

  tasks.push(
    activityApi
      .recent(6)
      .then((list) => {
        const mapped = mapActivities(list);
        recentActivities.set(mapped);
      })
      .catch(() => undefined)
  );

  tasks.push(
    coursesApi
      .marketplace()
      .then((list) => {
        const mapped = mapPublicCourses(list);
        publicCourses.set(mapped);
      })
      .catch(() => undefined)
  );

  if (activeCourseId) {
    tasks.push(
      coursesApi
        .peerComparison(activeCourseId)
        .then((pc) => {
          peerComparison.update((current) => ({
            ...current,
            userProgress: pc.userProgress ?? current.userProgress,
            peerProgress: pc.peerAverage ?? current.peerProgress
          }));
        })
        .catch(() => undefined)
    );
  }

  await Promise.all(tasks);
}
