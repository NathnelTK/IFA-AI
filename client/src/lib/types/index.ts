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

export interface PipelineModuleProposal {
  id: number;
  title: string;
  summary: string;
  estimatedHours: number;
  topics: string[];
}

// -----------------------------------------------------------------------------
// Three-model pipeline output (mirrors GeneratedModuleResult from the API —
// see src/IFA.Application/Common/Interfaces/ICourseGenerationService.cs).
// Field names match the ASP.NET camelCase JSON contract exactly.
// -----------------------------------------------------------------------------
export interface GeneratedQuestion {
  id: string;
  prompt: string;
  options: string[];
  correctOptionIndex: number;
  explanation: string;
  targetSkillName: string;
  bloomTaxonomyLevel?: string;
}

export interface GeneratedQuiz {
  id: string;
  title: string;
  passingScorePercentage: number;
  questions: GeneratedQuestion[];
}

export interface GeneratedLesson {
  id: string;
  lessonNumber: number;
  title: string;
  summary: string;
  contentMarkdown: string;
  readingTimeMinutes: number;
  youTubeVideoId?: string | null;
  youTubeVideoTitle?: string | null;
  scholarxivCitationDoi?: string | null;
  scholarxivPaperTitle?: string | null;
  keyTakeaways?: string[];
}

export interface GeneratedModule {
  id: string;
  moduleNumber: number;
  title: string;
  summary: string;
  estimatedHours: number;
  generationStatus?: string;
  isGenerated?: boolean;
}

export interface GeneratedModuleResult {
  module: GeneratedModule;
  lesson: GeneratedLesson;
  quiz: GeneratedQuiz;
}

export interface QuizResult {
  scorePercent: number;
  correctCount: number;
  totalQuestions: number;
  passed: boolean;
}
