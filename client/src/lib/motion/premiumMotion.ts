import { browser } from '$app/environment';

type Cleanup = () => void;

function reduced(): boolean {
	return !browser || window.matchMedia('(prefers-reduced-motion: reduce)').matches
		|| document.documentElement.dataset.reducedMotion === 'true';
}

export function reveal(
	node: HTMLElement,
	options: { y?: number; delay?: number; start?: string } = {}
): { destroy: Cleanup } {
	let cleanup: Cleanup = () => {};
	if (reduced()) return { destroy: cleanup };
	void (async () => {
		const [{ gsap }, { ScrollTrigger }] = await Promise.all([
			import('gsap'),
			import('gsap/ScrollTrigger')
		]);
		gsap.registerPlugin(ScrollTrigger);
		const context = gsap.context(() => {
			gsap.fromTo(node,
				{ autoAlpha: 0, y: options.y ?? 34 },
				{
					autoAlpha: 1,
					y: 0,
					duration: 0.85,
					delay: options.delay ?? 0,
					ease: 'power3.out',
					scrollTrigger: { trigger: node, start: options.start ?? 'top 88%', once: true }
				});
		});
		cleanup = () => context.revert();
	})();
	return { destroy: () => cleanup() };
}

export function parallax(
	node: HTMLElement,
	options: { y?: number; start?: string; end?: string } = {}
): { destroy: Cleanup } {
	let cleanup: Cleanup = () => {};
	if (reduced()) return { destroy: cleanup };
	void (async () => {
		const [{ gsap }, { ScrollTrigger }] = await Promise.all([
			import('gsap'),
			import('gsap/ScrollTrigger')
		]);
		gsap.registerPlugin(ScrollTrigger);
		const context = gsap.context(() => {
			gsap.to(node, {
				y: options.y ?? -70,
				ease: 'none',
				scrollTrigger: {
					trigger: node.parentElement ?? node,
					start: options.start ?? 'top bottom',
					end: options.end ?? 'bottom top',
					scrub: 1
				}
			});
		});
		cleanup = () => context.revert();
	})();
	return { destroy: () => cleanup() };
}

export async function withGsap<T>(
	setup: (gsap: typeof import('gsap').gsap, ScrollTrigger: typeof import('gsap/ScrollTrigger').ScrollTrigger) => T
): Promise<T | undefined> {
	if (reduced()) return undefined;
	const [{ gsap }, { ScrollTrigger }] = await Promise.all([
		import('gsap'),
		import('gsap/ScrollTrigger')
	]);
	gsap.registerPlugin(ScrollTrigger);
	return setup(gsap, ScrollTrigger);
}
