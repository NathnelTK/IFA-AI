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
  /** Optional share code so the card can enroll the learner directly. */
  shareCode?: string;
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
  /** Raw markdown body from the AI pipeline, rendered via the typography plugin. */
  contentMarkdown?: string;
  /** Legacy plain-text paragraphs (fallback for catalog/demo lessons). */
  content: string[];
  /** Optional embedded YouTube video surfaced from the research pipeline. */
  youTubeVideoId?: string | null;
  youTubeVideoTitle?: string | null;
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
  /** True for the comprehensive module exam; false for mid-module mini-quizzes. */
  isExam: boolean;
  /** Sort order within the module (mini-quizzes first, exam last). */
  orderIndex: number;
  /** Best score the learner has achieved, 0-100, or null if never attempted. */
  bestScore: number | null;
}

export interface Module {
  id: string;
  title: string;
  summary: string;
  lessons: Lesson[];
  quizzes: Quiz[];
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
  /**
   * Progress reported by the API enrollment. When present it is authoritative,
   * because the backend tracks completion per learner rather than on the lesson.
   */
  progressPercent?: number;
  /** API-provided "Module N of M" label, used when modules are not loaded yet. */
  moduleInfoLabel?: string;
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
