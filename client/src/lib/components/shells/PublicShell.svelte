<script lang="ts">
  import { page } from '$app/stores';
</script>

<div class="min-h-screen bg-ifa-bg text-ifa-text-primary">
  <header class="public-header">
    <a href="/" class="public-brand"><span><i></i><i></i><i></i></span>Witforge</a>
    <nav aria-label="Public navigation">
      <a href="/marketplace">Browse courses</a>
      <a href="/apply/educator">Teach on Witforge</a>
      <a href={`/login?redirect=${encodeURIComponent($page.url.pathname + $page.url.search)}`} class="sign-in">Sign in</a>
    </nav>
  </header>
  <main id="main-content"><slot /></main>
</div>

<style>
  .public-header{position:sticky;top:0;z-index:30;display:flex;align-items:center;justify-content:space-between;min-height:4.8rem;padding:0 4vw;border-bottom:1px solid var(--wf-border);background:color-mix(in srgb,var(--wf-surface-canvas) 90%,transparent);backdrop-filter:blur(18px)}.public-brand{display:flex;align-items:center;gap:.65rem;font-size:.85rem;font-weight:850}.public-brand>span{display:inline-flex;align-items:end;gap:2px;width:1.75rem;height:1.75rem;padding:5px;border:1px solid currentColor;border-radius:50%}.public-brand i{width:3px;border-radius:2px;background:#176b4a}.public-brand i:nth-child(1){height:35%}.public-brand i:nth-child(2){height:80%}.public-brand i:nth-child(3){height:55%}.public-header nav{display:flex;align-items:center;gap:1.4rem;font-size:.65rem;font-weight:750}.public-header nav a:hover{opacity:.6}.public-header .sign-in{padding:.65rem .9rem;border-radius:999px;color:#17221c;background:#c7f464;font-weight:850}@media(max-width:640px){.public-header{padding:0 1rem}.public-header nav a:nth-child(2){display:none}.public-header nav{gap:.8rem}}
</style>
