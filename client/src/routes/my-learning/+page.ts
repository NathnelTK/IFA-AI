import { redirect } from '@sveltejs/kit';

// "My Learning" was merged into "My Courses" (same logic, one source of truth).
// Keep the old path working by redirecting it to the courses library.
export const load = () => {
  throw redirect(307, '/courses');
};
