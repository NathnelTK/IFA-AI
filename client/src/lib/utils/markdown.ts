import { marked } from 'marked';

/**
 * Renders AI-generated lesson/tutor markdown into HTML.
 *
 * The 3-model pipeline returns rich markdown (headings, bold, lists, code,
 * blockquotes, links). Previously the UI split it on blank lines and printed it
 * as literal paragraphs, so learners saw raw `##` and `**` instead of formatted
 * content. We render it with `marked` and style the output with the Tailwind
 * typography (`prose`) plugin.
 *
 * NOTE: content originates from our own AI pipeline (not other users), so the
 * XSS surface is low. If untrusted authored content is ever rendered here,
 * add a sanitizer (e.g. DOMPurify) before the `{@html}` sink.
 */
marked.setOptions({
  gfm: true,
  breaks: true
});

export function renderMarkdown(input: string | null | undefined): string {
  if (!input) return '';
  const trimmed = input.trim();
  if (!trimmed) return '';
  return marked.parse(trimmed) as string;
}
