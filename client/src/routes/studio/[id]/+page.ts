import { redirect } from '@sveltejs/kit';

export const load = ({ params }) => redirect(308, `/studio/courses/${params.id}/edit`);
