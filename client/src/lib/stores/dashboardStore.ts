import { writable } from 'svelte/store';
import type { SkillProgress, Recommendation, RecentActivity, CourseCard } from '../types';
import { activeCourse as activeCourseFromCourses, overallProgress as overallProgressFromCourses } from './coursesStore';

export const userProfile = writable({
  name: 'Nathnel',
  role: 'Learner',
  avatar: 'https://images.unsplash.com/photo-1534528741775-53994a69daeb?w=100&auto=format&fit=crop&q=80',
  greeting: 'Good morning, Nathnel ☀️',
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
