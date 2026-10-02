export interface SkillProgress {
  name: string;
  percentage: number;
  color: string;
  iconName: string;
  isWeakArea?: boolean;
}

export interface Recommendation {
  id: string;
  title: string;
  subtitle: string;
  type: 'review' | 'continue' | 'practice';
  color: string;
  actionUrl: string;
}

export interface RecentActivity {
  id: string;
  title: string;
  detail: string;
  timeAgo: string;
  iconType: 'quiz' | 'module' | 'research' | 'voice';
}

export interface CourseCard {
  id: string;
  title: string;
  provider: string;
  providerLogo?: string;
  badge?: 'Popular' | 'Bestseller' | 'Trending' | 'New';
  rating: number;
  reviewCount: string;
  duration: string;
  imageUrl?: string;
  enrolled?: boolean;
}

export interface CurrentLearningModule {
  courseTitle: string;
  moduleInfo: string;
  progressPercent: number;
  nextLessonTitle: string;
  nextLessonSummary: string;
  nextLessonDuration: string;
  courseThumbnail: string;
}

export type CourseStatus = 'inProgress' | 'completed' | 'paused' | 'notStarted';

export interface Lesson {
  id: string;
  title: string;
  summary: string;
  /** Reader body, rendered as simple paragraphs/sections in LessonView. */
  content: string[];
  duration: string;
  completed: boolean;
}

export interface QuizQuestion {
  id: string;
  prompt: string;
  options: string[];
  /** Index into `options`. */
  correctIndex: number;
  explanation: string;
}

export interface Quiz {
  id: string;
  title: string;
  questions: QuizQuestion[];
  /** Best score the learner has achieved, 0-100, or null if never attempted. */
  bestScore: number | null;
}

export interface Module {
  id: string;
  title: string;
  summary: string;
  lessons: Lesson[];
  quiz?: Quiz;
}

export interface Course {
  id: string;
  title: string;
  provider: string;
  description: string;
  /** Short label rendered in the gradient thumbnail (e.g. "C#"). */
  thumbnail: string;
  category: string;
  duration: string;
  status: CourseStatus;
  isBookmarked: boolean;
  enrolledDate: string;
  lastAccessed: string;
  modules: Module[];
}

export interface PipelineModuleProposal {
  id: number;
  title: string;
  summary: string;
  estimatedHours: number;
  topics: string[];
}

export interface SkillComparison {
  name: string;
  userPercentage: number;
  peerPercentage: number;
}

export interface PeerComparison {
  enabled: boolean;
  user: string;
  peerName: string;
  userProgress: number;
  peerProgress: number;
  skillComparison: SkillComparison[];
}
