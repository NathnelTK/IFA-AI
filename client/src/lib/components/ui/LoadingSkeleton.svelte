<script lang="ts">
  export let rows = 3;
  export let label = 'Loading content';
</script>

<div role="status" aria-label={label} class="space-y-3">
  {#each Array(rows) as _}
    <div class="h-16 animate-pulse rounded-xl bg-ifa-card-muted"></div>
  {/each}
  <span class="sr-only">{label}</span>
</div>
