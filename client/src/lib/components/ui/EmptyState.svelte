<script lang="ts">
  export let title: string;
  export let description: string;
</script>

<section class="rounded-2xl border border-dashed border-ifa-border bg-ifa-card-muted/40 px-6 py-10 text-center">
  <h2 class="text-lg font-bold text-ifa-text-primary">{title}</h2>
  <p class="mx-auto mt-2 max-w-xl text-sm leading-relaxed text-ifa-text-secondary">{description}</p>
  <div class="mt-5 flex justify-center"><slot /></div>
</section>
