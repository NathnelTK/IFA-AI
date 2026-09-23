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
