import node from '@sveltejs/adapter-node';
import vercel from '@sveltejs/adapter-vercel';
import { vitePreprocess } from '@sveltejs/vite-plugin-svelte';

// Vercel sets VERCEL=1 in its build environment, so the same source tree builds
// for both targets: adapter-vercel on Vercel, adapter-node everywhere else
// (local `npm run preview` and the docker/Dockerfile.frontend image that runs
// `node build`).
const isVercel = !!process.env.VERCEL;

/** @type {import('@sveltejs/kit').Config} */
const config = {
	preprocess: vitePreprocess(),
	kit: {
		adapter: isVercel ? vercel() : node()
	}
};

export default config;
