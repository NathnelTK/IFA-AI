<script lang="ts">
  export let categories: any[] = [];
  export let activeFilter = 'all';
  export let onFilterChange = (filter: string) => {};
</script>

<div class="flex items-center gap-2 overflow-x-auto pb-2">
  {#each categories as category}
    <button
      type="button"
      on:click={() => onFilterChange(category.id)}
      class="px-4 py-2 rounded-full text-sm font-semibold whitespace-nowrap transition {activeFilter === category.id
        ? 'bg-ifa-pine text-white'
        : 'bg-ifa-card-muted text-ifa-text-secondary hover:bg-ifa-card-muted hover:text-ifa-pine'}"
    >
      {category.name}
    </button>
  {/each}
</div>
