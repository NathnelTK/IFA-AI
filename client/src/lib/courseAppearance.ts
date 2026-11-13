import type { CourseAppearanceDto } from '$lib/api';

export const DEFAULT_COURSE_APPEARANCE: CourseAppearanceDto = {
	canvasLayout: 'Classic',
	primaryColor: '#176B4A',
	secondaryColor: '#C7F464',
	backgroundColor: '#F7F5EF',
	surfaceColor: '#FFFFFF',
	textColor: '#252525',
	headingFont: 'Manrope',
	bodyFont: 'Inter',
	contentWidth: 'Standard',
	spacing: 'Comfortable',
	cardStyle: 'Rounded',
	buttonStyle: 'Solid',
	borderStyle: 'Subtle'
};

export const COURSE_PALETTES = [
	{ name: 'Witforge Forest', primaryColor: '#176B4A', secondaryColor: '#C7F464', backgroundColor: '#F7F5EF', surfaceColor: '#FFFFFF', textColor: '#252525' },
	{ name: 'Oxford Blue', primaryColor: '#1D3557', secondaryColor: '#A8DADC', backgroundColor: '#F1FAEE', surfaceColor: '#FFFFFF', textColor: '#17212B' },
	{ name: 'Warm Editorial', primaryColor: '#8A3B2E', secondaryColor: '#E9C46A', backgroundColor: '#FAF4E8', surfaceColor: '#FFFDF8', textColor: '#2B2118' },
	{ name: 'Studio Violet', primaryColor: '#5B4B9A', secondaryColor: '#D8C7FF', backgroundColor: '#F6F3FC', surfaceColor: '#FFFFFF', textColor: '#241F31' }
] as const;

export const COURSE_FONT_COMBINATIONS = [
	{ name: 'Modern Sans', headingFont: 'Manrope', bodyFont: 'Inter' },
	{ name: 'Editorial', headingFont: 'Merriweather', bodyFont: 'SourceSerif' },
	{ name: 'Academic', headingFont: 'SourceSerif', bodyFont: 'IbmPlexSans' },
	{ name: 'Clean Technical', headingFont: 'IbmPlexSans', bodyFont: 'Inter' }
] satisfies Array<Pick<CourseAppearanceDto, 'headingFont' | 'bodyFont'> & { name: string }>;

const fonts: Record<CourseAppearanceDto['headingFont'], string> = {
	Manrope: 'Manrope, Inter, system-ui, sans-serif',
	Inter: 'Inter, system-ui, sans-serif',
	SourceSerif: '"Source Serif 4", Georgia, serif',
	Merriweather: 'Merriweather, Georgia, serif',
	IbmPlexSans: '"IBM Plex Sans", Inter, system-ui, sans-serif'
};

export function normalizeCourseAppearance(value: CourseAppearanceDto | null | undefined): CourseAppearanceDto {
	return value ? { ...DEFAULT_COURSE_APPEARANCE, ...value } : { ...DEFAULT_COURSE_APPEARANCE };
}

export function courseAppearanceStyle(appearance: CourseAppearanceDto): string {
	const width = appearance.contentWidth === 'Narrow' ? '760px' : appearance.contentWidth === 'Wide' ? '1200px' : '960px';
	const space = appearance.spacing === 'Compact' ? '0.75rem' : appearance.spacing === 'Airy' ? '2rem' : '1.25rem';
	const radius = appearance.cardStyle === 'Flat' ? '0px' : appearance.cardStyle === 'Elevated' ? '18px' : '12px';
	const shadow = appearance.cardStyle === 'Elevated' ? '0 18px 45px rgba(20, 30, 25, .14)' : 'none';
	const borderWidth = appearance.borderStyle === 'None' ? '0px' : appearance.borderStyle === 'Strong' ? '2px' : '1px';
	return [
		`--course-primary:${appearance.primaryColor}`,
		`--course-secondary:${appearance.secondaryColor}`,
		`--course-background:${appearance.backgroundColor}`,
		`--course-surface:${appearance.surfaceColor}`,
		`--course-text:${appearance.textColor}`,
		`--course-heading-font:${fonts[appearance.headingFont]}`,
		`--course-body-font:${fonts[appearance.bodyFont]}`,
		`--course-content-width:${width}`,
		`--course-space:${space}`,
		`--course-radius:${radius}`,
		`--course-shadow:${shadow}`,
		`--course-border-width:${borderWidth}`
	].join(';');
}
