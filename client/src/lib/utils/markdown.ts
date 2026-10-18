import DOMPurify from 'dompurify';
import { marked } from 'marked';

/**
 * Renders AI-generated lesson/tutor/educator markdown into HTML.
 *
 * The 3-model pipeline returns rich markdown (headings, bold, lists, code,
 * blockquotes, links). Previously the UI split it on blank lines and printed it
 * as literal paragraphs, so learners saw raw `##` and `**` instead of formatted
 * content. We render it with `marked` and style the output with the Tailwind
 * typography (`prose`) plugin.
 *
 * The HTML is sanitized with DOMPurify before it reaches any `{@html}` sink —
 * this is now used for AI chat, lesson content and educator-authored course
 * material, so scripts/styles/event handlers are stripped defensively even for
 * content that is supposed to be trusted.
 */
marked.setOptions({
  gfm: true,
  breaks: true
});

export function renderMarkdown(input: string | null | undefined): string {
  if (!input) return '';
  const trimmed = input.trim();
  if (!trimmed) return '';
  const html = marked.parse(trimmed) as string;
  return DOMPurify.sanitize(html, { USE_PROFILES: { html: true } });
}
