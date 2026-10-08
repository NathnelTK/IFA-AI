<script lang="ts">
  import { onMount } from 'svelte';
  import { browser } from '$app/environment';
  import { preferences } from '$lib/stores/preferencesStore';

  /**
   * GlowCursor — an ambient "✦ small glowing cursor" with a shooting-star tail
   * that follows movement and brightens over clickable elements. Pure Canvas +
   * requestAnimationFrame, so there is no runtime dependency to install.
   *
   * Automatically disabled:
   *  - on touch devices (no fine pointer), and
   *  - when reduced motion is requested (user toggle or OS preference).
   *
   * Every trail particle fades; when motion is disabled the canvas is removed
   * entirely and the native cursor is left untouched.
   */
  let canvas: HTMLCanvasElement | null = null;

  let enabled = false; // set in onMount after device capability detection
  let osReduced = false; // live OS `prefers-reduced-motion`

  // Recompute whenever the toggle changes; also honors the OS preference.
  $: reduced = $preferences.reducedMotion || osReduced;
  $: active = enabled && !reduced;

  interface Particle {
    x: number;
    y: number;
    age: number;
    life: number;
    size: number;
    hue: number;
  }

  onMount(() => {
    if (!browser) return;

    const reducedMq = window.matchMedia('(prefers-reduced-motion: reduce)');
    osReduced = reducedMq.matches;
    const onReduced = () => (osReduced = reducedMq.matches);
    reducedMq.addEventListener('change', onReduced);

    const finePointer = window.matchMedia('(pointer: fine)').matches;
    const coarse = window.matchMedia('(pointer: coarse)').matches;
    const hasHover = window.matchMedia('(hover: hover)').matches;
    const touch = 'ontouchstart' in window || navigator.maxTouchPoints > 0;

    // Require a mouse/trackpad: fine pointer, hover capable, and not a touch device.
    enabled = finePointer && hasHover && !coarse && !touch;

    return () => reducedMq.removeEventListener('change', onReduced);
  });

  // The drawing loop lives in its own reactive block so it starts/stops with
  // `active` (which already accounts for the reduced-motion preference).
  $: if (browser && canvas && active) {
    startLoop(canvas);
  } else if (browser && canvas && !active) {
    stopLoop();
  }

  let frame = 0;
  let running = false;

  const pointer = { x: -100, y: -100, tx: -100, ty: -100 };
  let hoveringClickable = false;
  let glowBoost = 0; // eased 0..1 hover intensity
  const particles: Particle[] = [];
  const MAX_PARTICLES = 22;

  function isClickable(target: EventTarget | null): boolean {
    if (!(target instanceof Element)) return false;
    return Boolean(
      target.closest(
        'a, button, [role="button"], input, select, textarea, summary, label[for], .cursor-pointer'
      )
    );
  }

  function handleMove(e: PointerEvent) {
    if (!enabled) return;
    pointer.tx = e.clientX;
    pointer.ty = e.clientY;
    hoveringClickable = isClickable(e.target);
    if (pointer.x < 0) {
      pointer.x = pointer.tx;
      pointer.y = pointer.ty;
    }
  }

  function handleLeave() {
    pointer.tx = -100;
    pointer.ty = -100;
    hoveringClickable = false;
  }

  function resize() {
    if (!canvas) return;
    const dpr = Math.min(window.devicePixelRatio || 1, 2);
    canvas.width = window.innerWidth * dpr;
    canvas.height = window.innerHeight * dpr;
    canvas.style.width = `${window.innerWidth}px`;
    canvas.style.height = `${window.innerHeight}px`;
    const ctx = canvas.getContext('2d');
    if (ctx) ctx.setTransform(dpr, 0, 0, dpr, 0, 0);
  }

  function startLoop(el: HTMLCanvasElement) {
    if (running) return;
    running = true;
    resize();

    window.addEventListener('pointermove', handleMove, { passive: true });
    window.addEventListener('pointerdown', handleMove, { passive: true });
    document.addEventListener('pointerleave', handleLeave);
    window.addEventListener('resize', resize);

    const step = () => {
      if (!running || !el) return;
      draw(el);
      frame = requestAnimationFrame(step);
    };
    frame = requestAnimationFrame(step);
  }

  function stopLoop() {
    if (!running) return;
    running = false;
    cancelAnimationFrame(frame);
    window.removeEventListener('pointermove', handleMove);
    window.removeEventListener('pointerdown', handleMove);
    document.removeEventListener('pointerleave', handleLeave);
    window.removeEventListener('resize', resize);
    particles.length = 0;
  }

  function draw(el: HTMLCanvasElement) {
    const ctx = el.getContext('2d');
    if (!ctx) return;

    const w = window.innerWidth;
    const h = window.innerHeight;

    // Smooth ("🖱️ smooth movement") follow: the dot eases toward the pointer.
    const ease = 0.22;
    pointer.x += (pointer.tx - pointer.x) * ease;
    pointer.y += (pointer.ty - pointer.y) * ease;

    // Ease the hover glow toward its target.
    glowBoost += ((hoveringClickable ? 1 : 0) - glowBoost) * 0.12;

    ctx.clearRect(0, 0, w, h);

    const speed = Math.hypot(pointer.tx - pointer.x, pointer.ty - pointer.y);
    const moving = speed > 0.6 && pointer.tx > -50;

    // Emit a shooting-star tail particle while moving.
    if (moving && particles.length < MAX_PARTICLES) {
      particles.push({
        x: pointer.x + (Math.random() - 0.5) * 2,
        y: pointer.y + (Math.random() - 0.5) * 2,
        age: 0,
        life: 18 + Math.random() * 14,
        size: 0.8 + Math.random() * 1.8 + glowBoost * 1.4,
        hue: 150 + (Math.random() - 0.5) * 40 // emerald/pine range
      });
    }

    // Tail: a fading streak from oldest to newest.
    for (let i = particles.length - 1; i >= 0; i--) {
      const p = particles[i];
      p.age += 1;
      p.y += 0.15; // gentle gravity, like a falling star
      const t = 1 - p.age / p.life;
      if (t <= 0) {
        particles.splice(i, 1);
        continue;
      }
      const alpha = t * (0.35 + glowBoost * 0.4);
      ctx.beginPath();
      ctx.fillStyle = `hsla(${p.hue}, 70%, 62%, ${alpha})`;
      ctx.shadowColor = `hsla(${p.hue}, 80%, 60%, ${alpha})`;
      ctx.shadowBlur = 10 * t;
      ctx.arc(p.x, p.y, p.size * t, 0, Math.PI * 2);
      ctx.fill();
    }

    if (pointer.x < -50) {
      ctx.shadowBlur = 0;
      return;
    }

    // Main glow: a soft aura plus a bright core, stronger over clickables.
    const coreRadius = 3.2 + glowBoost * 1.6;
    const auraRadius = 14 + glowBoost * 12;

    const aura = ctx.createRadialGradient(pointer.x, pointer.y, 0, pointer.x, pointer.y, auraRadius);
    aura.addColorStop(0, `rgba(42, 157, 104, ${0.26 + glowBoost * 0.4})`);
    aura.addColorStop(0.5, `rgba(80, 220, 160, ${0.14 + glowBoost * 0.24})`);
    aura.addColorStop(1, 'rgba(42, 157, 104, 0)');
    ctx.beginPath();
    ctx.fillStyle = aura;
    ctx.arc(pointer.x, pointer.y, auraRadius, 0, Math.PI * 2);
    ctx.fill();

    ctx.beginPath();
    ctx.fillStyle = glowBoost > 0.5 ? '#eafff5' : '#bff5dd';
    ctx.shadowColor = 'rgba(90, 255, 190, 0.95)';
    ctx.shadowBlur = 16 + glowBoost * 14;
    ctx.arc(pointer.x, pointer.y, coreRadius, 0, Math.PI * 2);
    ctx.fill();
    ctx.shadowBlur = 0;
  }
</script>

{#if browser && active}
  <canvas
    bind:this={canvas}
    class="ifa-glow-cursor"
    aria-hidden="true"
  ></canvas>
{/if}

<style>
  .ifa-glow-cursor {
    position: fixed;
    inset: 0;
    width: 100vw;
    height: 100vh;
    pointer-events: none;
    z-index: 9998;
    mix-blend-mode: screen;
  }

  /* The glow is decorative — never intercept the pointer. */
  @media (prefers-reduced-motion: reduce) {
    .ifa-glow-cursor {
      display: none;
    }
  }
</style>
