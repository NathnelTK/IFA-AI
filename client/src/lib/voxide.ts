import { browser } from '$app/environment';
import { writable } from 'svelte/store';
import { goto } from '$app/navigation';
import { coursesApi } from '$lib/api';

/**
 * Voxide voice-assistant integration (capability-first browser SDK).
 *
 * The home-page advisor is voice-first and does NOT chat: Voxide maps speech to
 * the capabilities we register below — navigating sections, creating a course,
 * or opening the tutor — and runs the real handlers here.
 *
 * IMPORTANT: the browser SDK uses the *publishable* key (`vox_pub_...`), which
 * is safe in page source and protected by the domain whitelist in the Voxide
 * dashboard. The secret `vox_sk_...` key is NOT used here. Put your publishable
 * key in `VITE_VOXIDE_KEY` (client/.env) to enable voice.
 */

export type VoxideStatus =
  | 'idle' | 'armed' | 'connecting' | 'listening' | 'thinking' | 'speaking' | 'executing' | 'error'
  | 'disabled';

export interface VoxideMessage {
  role: string;
  content: string;
}

export const voxideStatus = writable<VoxideStatus>('idle');
export const voxideMessages = writable<VoxideMessage[]>([]);

/** Publishable key (`vox_pub_...`). Inlined from VITE_VOXIDE_KEY at build time. */
const VOXIDE_KEY = (import.meta.env as Record<string, string | undefined>).VITE_VOXIDE_KEY;

/** True only when a publishable key is configured. */
export const voxideEnabled = Boolean(VOXIDE_KEY);

const SECTION_ROUTES: Record<string, string> = {
  home: '/home',
  dashboard: '/home',
  courses: '/courses',
  'my courses': '/courses',
  'my course': '/courses',
  marketplace: '/marketplace',
  recommendations: '/recommendations',
  skills: '/my-skills',
  'my skills': '/my-skills',
  progress: '/progress',
  research: '/research',
  settings: '/settings',
  tutor: '/ai-tutor',
  'ai tutor': '/ai-tutor'
};

// eslint-disable-next-line @typescript-eslint/no-explicit-any
let client: any = null;
let initialized = false;

export async function initVoxide(): Promise<void> {
  if (!browser || initialized) return;
  const key = VOXIDE_KEY;
  if (!key) {
    voxideStatus.set('disabled');
    return;
  }
  initialized = true;

  try {
    const mod = await import('@voxide/react/core');
    const VoxideClient = (mod as { VoxideClient: new (opts: { publicKey: string }) => unknown }).VoxideClient;
    client = new VoxideClient({ publicKey: key });

    client.register({
      goToSection: {
        description:
          'Navigate the app to a named section: home, my courses, marketplace, recommendations, skills, progress, research, settings, or tutor.',
        params: { section: { type: 'string', required: true } },
        handler: async ({ section }: { section: string }) => {
          const route = SECTION_ROUTES[(section ?? '').toLowerCase().trim()];
          if (!route) return { status: 'error', message: `Unknown section: ${section}` };
          await goto(route);
          return { status: 'ok', route };
        }
      },
      createCourse: {
        description:
          'Create a brand-new personalized course for a topic the user wants to learn, then open it. Use when the user asks to learn, study, or prepare for something.',
        params: {
          topic: { type: 'string', required: true },
          hoursPerWeek: { type: 'number' }
        },
        handler: async ({ topic, hoursPerWeek }: { topic: string; hoursPerWeek?: number }) => {
          const course = await coursesApi.create({ goal: topic, hoursPerWeek: hoursPerWeek ?? 5 });
          await goto(`/courses/${course.id}`);
          return { status: 'ok', courseId: course.id, title: course.title };
        }
      },
      openTutor: {
        description: 'Open the AI tutor to explain or practice a specific topic.',
        params: { topic: { type: 'string' } },
        handler: async ({ topic }: { topic?: string }) => {
          await goto(topic ? `/ai-tutor?topic=${encodeURIComponent(topic)}` : '/ai-tutor');
          return { status: 'ok' };
        }
      }
    });

    client.bindState(() => ({
      route: browser ? window.location.pathname : '/',
      page: browser ? document.title : 'IFA'
    }));

    client.subscribe(() => {
      const snap = client.getSnapshot();
      voxideStatus.set((snap?.status as VoxideStatus) ?? 'idle');
      if (Array.isArray(snap?.messages)) voxideMessages.set(snap.messages as VoxideMessage[]);
    });

    await client.init();
  } catch (err) {
    console.error('Voxide init failed', err);
    voxideStatus.set('error');
  }
}

/** Start or stop a voice session (opens the mic + socket). */
export async function toggleVoxide(): Promise<void> {
  if (!client) {
    await initVoxide();
    if (!client) return;
  }
  const snap = client.getSnapshot?.();
  const status = snap?.status ?? 'idle';
  if (status === 'idle' || status === 'error') {
    await client.connect();
  } else {
    client.disconnect();
  }
}
