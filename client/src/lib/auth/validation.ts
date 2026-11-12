export const AUTH_LIMITS = {
	name: 120,
	email: 254,
	passwordMin: 8,
	passwordMax: 128,
	mfaCode: 32
} as const;

export type RegistrationField = 'name' | 'email' | 'password' | 'confirm';
export type LoginField = 'email' | 'password';
export type ValidationErrors<Field extends string> = Partial<Record<Field, string>>;

export interface RegistrationValues {
	name: string;
	email: string;
	password: string;
	confirm: string;
}

export interface LoginValues {
	email: string;
	password: string;
}

const emailPattern = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;

export function validateRegistration(values: RegistrationValues): ValidationErrors<RegistrationField> {
	const errors: ValidationErrors<RegistrationField> = {};
	const name = values.name.trim();
	if (!name) errors.name = 'Enter your full name.';
	else if (name.length > AUTH_LIMITS.name) errors.name = `Use ${AUTH_LIMITS.name} characters or fewer.`;

	const emailError = validateEmail(values.email);
	if (emailError) errors.email = emailError;

	const passwordError = validatePassword(values.password);
	if (passwordError) errors.password = passwordError;
	if (!values.confirm) errors.confirm = 'Repeat your password.';
	else if (values.password !== values.confirm) errors.confirm = 'The passwords do not match.';
	return errors;
}

export function validateLogin(values: LoginValues): ValidationErrors<LoginField> {
	const errors: ValidationErrors<LoginField> = {};
	const emailError = validateEmail(values.email);
	if (emailError) errors.email = emailError;
	if (!values.password) errors.password = 'Enter your password.';
	else if (values.password.length > AUTH_LIMITS.passwordMax) {
		errors.password = `Use ${AUTH_LIMITS.passwordMax} characters or fewer.`;
	}
	return errors;
}

export function validateMfaCode(value: string): string | undefined {
	const code = value.trim();
	if (!code) return 'Enter your authenticator or recovery code.';
	if (code.length > AUTH_LIMITS.mfaCode) return `Use ${AUTH_LIMITS.mfaCode} characters or fewer.`;
	return undefined;
}

function validateEmail(value: string): string | undefined {
	const email = value.trim();
	if (!email) return 'Enter your email address.';
	if (email.length > AUTH_LIMITS.email) return `Use ${AUTH_LIMITS.email} characters or fewer.`;
	if (!emailPattern.test(email)) return 'Enter a complete email address, such as you@example.com.';
	return undefined;
}

function validatePassword(value: string): string | undefined {
	if (!value) return 'Create a password.';
	if (value.length < AUTH_LIMITS.passwordMin) return `Use at least ${AUTH_LIMITS.passwordMin} characters.`;
	if (value.length > AUTH_LIMITS.passwordMax) return `Use ${AUTH_LIMITS.passwordMax} characters or fewer.`;
	return undefined;
}
