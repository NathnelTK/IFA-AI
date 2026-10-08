/**
 * Central icon registry.
 *
 * Consolidates the string->component icon maps that were previously duplicated
 * across LearningOverviewDonut, SkillBreakdown, SkillProgressChart, etc., and
 * gives each section an intent-specific icon (instead of a generic Sparkles/Star).
 *
 * Usage:
 *   import { getSkillIcon, recommendationIcons } from '$lib/icons';
 *   <svelte:component this={getSkillIcon(skill.iconName)} />
 */
import type { ComponentType, SvelteComponent } from 'svelte';
import {
  Terminal,
  Database,
  Network,
  KeyRound,
  CheckCircle2,
  Code2,
  Palette,
  RefreshCw,
  PlayCircle,
  Target,
  Lightbulb,
  Award,
  BookOpen,
  FlaskConical,
  History,
  Mic
} from 'lucide-svelte';

/** A lucide-svelte icon component, usable with <svelte:component this={...} />. */
export type IconComponent = ComponentType<SvelteComponent>;

/** Skill name / iconName -> icon. Mirrors the colors defined in the courses data. */
export const skillIcons: Record<string, IconComponent> = {
  Terminal,
  Database,
  Network,
  KeyRound,
  CheckCircle2,
  Code2,
  Palette
};

export function getSkillIcon(name: string): IconComponent {
  return skillIcons[name] ?? Terminal;
}

/** Recommendation type -> icon. review = revisit, continue = resume, practice = drill a weak area. */
export const recommendationIcons: Record<string, IconComponent> = {
  review: RefreshCw,
  continue: PlayCircle,
  practice: Target
};

export function getRecommendationIcon(type: string): IconComponent {
  return recommendationIcons[type] ?? Lightbulb;
}

/** Recent-activity type -> icon. */
export const activityIcons: Record<string, IconComponent> = {
  quiz: CheckCircle2,
  module: BookOpen,
  research: FlaskConical,
  voice: Mic,
  achievement: Award
};

export function getActivityIcon(type: string): IconComponent {
  return activityIcons[type] ?? History;
}

/** Section-header badge icons, keyed by intent rather than decoration. */
export const sectionIcons = {
  recommendations: Lightbulb,
  activity: History,
  continueLearning: PlayCircle,
  research: FlaskConical,
  goals: Target,
  achievements: Award
} satisfies Record<string, IconComponent>;
