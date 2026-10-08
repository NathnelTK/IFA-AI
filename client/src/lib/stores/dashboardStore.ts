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

export const userProfile = writable({
  name: 'Nathnel',
  role: 'Learner',
  avatar: 'https://images.unsplash.com/photo-1534528741775-53994a69daeb?w=100&auto=format&fit=crop&q=80',
    greeting: 'Good morning, Nathnel',
  tagline: 'Small steps today, big goals tomorrow.'
});

// Re-export from coursesStore for convenience
export const activeCourse = activeCourseFromCourses;
export const overallProgress = overallProgressFromCourses;

export const skillsList = writable<SkillProgress[]>([
  { name: 'C#', percentage: 84, color: '#2A9D68', iconName: 'Terminal' },
  { name: 'Databases', percentage: 61, color: '#E07A5F', iconName: 'Database' },
  { name: 'APIs', percentage: 55, color: '#7C5CFC', iconName: 'Network' },
  { name: 'Authentication', percentage: 45, color: '#E11D48', iconName: 'KeyRound', isWeakArea: true },
  { name: 'Testing', percentage: 32, color: '#EF4444', iconName: 'CheckCircle2', isWeakArea: true }
]);

export const recommendations = writable<Recommendation[]>([
  {
    id: 'rec-1',
    title: 'Review JWT Authentication',
    subtitle: 'You struggled in your last quiz.',
    type: 'review',
    color: '#E07A5F',
    actionUrl: '/courses/csharp-backend'
  },
  {
    id: 'rec-2',
    title: 'Continue REST APIs',
    subtitle: 'Based on your current progress.',
    type: 'continue',
    color: '#2A9D68',
    actionUrl: '/courses/csharp-backend'
  },
  {
    id: 'rec-3',
    title: 'Practice SQL joins',
    subtitle: 'This is a weak area for you.',
    type: 'practice',
    color: '#7C5CFC',
    actionUrl: '/courses/sql-developers'
  }
]);

export const recentActivities = writable<RecentActivity[]>([
  {
    id: 'act-1',
    title: 'Completed Quiz',
    detail: 'C# Fundamentals',
    timeAgo: '2h ago',
    iconType: 'quiz'
  },
  {
    id: 'act-2',
    title: 'Started New Module',
    detail: 'REST APIs',
    timeAgo: '4h ago',
    iconType: 'module'
  },
  {
    id: 'act-3',
    title: 'Research Completed',
    detail: 'Clean Architecture',
    timeAgo: '6h ago',
    iconType: 'research'
  },
  {
    id: 'act-4',
    title: 'Voice Command',
    detail: '"Show my weak areas"',
    timeAgo: '8h ago',
    iconType: 'voice'
  }
]);

export const publicCourses = writable<CourseCard[]>([
  {
    id: 'pub-1',
    title: 'Python for Beginners',
    provider: 'By Google',
    badge: 'Popular',
    rating: 4.8,
    reviewCount: '12.5k',
    duration: '6 weeks',
    imageUrl: 'https://images.unsplash.com/photo-1526374965328-7f61d4dc18c5?w=400&auto=format&fit=crop&q=80'
  },
  {
    id: 'pub-2',
    title: 'Full Stack Web Development',
    provider: 'By Meta',
    badge: 'Popular',
    rating: 4.7,
    reviewCount: '9.2k',
    duration: '8 weeks',
    imageUrl: 'https://images.unsplash.com/photo-1498050108023-c5249f4df085?w=400&auto=format&fit=crop&q=80'
  },
  {
    id: 'pub-3',
    title: 'Data Science Fundamentals',
    provider: 'By IBM',
    badge: 'Bestseller',
    rating: 4.6,
    reviewCount: '8.7k',
    duration: '10 weeks',
    imageUrl: 'https://images.unsplash.com/photo-1551288049-bebda4e38f71?w=400&auto=format&fit=crop&q=80'
  },
  {
    id: 'pub-4',
    title: 'Cybersecurity Essentials',
    provider: 'By Microsoft',
    badge: 'Trending',
    rating: 4.5,
    reviewCount: '6.3k',
    duration: '6 weeks',
    imageUrl: 'https://images.unsplash.com/photo-1563986768609-322da13575f3?w=400&auto=format&fit=crop&q=80'
  }
]);

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
// API hydration — each source is independent and falls back to the demo values
// above when the API is unreachable or returns nothing.
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
    imageUrl: course.thumbnailUrl || undefined,
    shareCode: course.shareCode
  }));
}

/**
 * Hydrate the dashboard widgets (profile, skills, recommendations, activity,
 * public courses, peer comparison) from the API. Any source that errors or comes
 * back empty keeps its existing demo value, so the dashboard always renders.
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
          if (mapped.length > 0) skillsList.set(mapped);
        })
        .catch(() => undefined)
    );
  }

  tasks.push(
    recommendationsApi
      .list()
      .then((list) => {
        const mapped = mapRecommendations(list);
        if (mapped.length > 0) recommendations.set(mapped);
      })
      .catch(() => undefined)
  );

  tasks.push(
    activityApi
      .recent(6)
      .then((list) => {
        const mapped = mapActivities(list);
        if (mapped.length > 0) recentActivities.set(mapped);
      })
      .catch(() => undefined)
  );

  tasks.push(
    coursesApi
      .marketplace()
      .then((list) => {
        const mapped = mapPublicCourses(list);
        if (mapped.length > 0) publicCourses.set(mapped);
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
