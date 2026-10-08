<script lang="ts">
  import { Filter, ArrowUpDown } from 'lucide-svelte';

  export let activeFilter = 'all';
  export let sortBy = 'recent';
  export let onFilterChange = (filter: string) => {};
  export let onSortChange = (sort: string) => {};

  const filters = [
    { id: 'all', label: 'All Courses' },
    { id: 'inProgress', label: 'In Progress' },
    { id: 'completed', label: 'Completed' },
    { id: 'bookmarked', label: 'Bookmarked' }
  ];

  const sortOptions = [
    { id: 'recent', label: 'Most Recent' },
    { id: 'progress', label: 'Progress' },
    { id: 'name', label: 'Name' },
    { id: 'duration', label: 'Duration' }
  ];
</script>

<div class="flex items-center gap-3">
  <!-- Filter Dropdown -->
  <div class="relative">
    <select
      bind:value={activeFilter}
      on:change={() => onFilterChange(activeFilter)}
      class="appearance-none bg-ifa-card-muted border border-ifa-border rounded-lg pl-10 pr-8 py-2.5 text-sm text-ifa-text-primary focus:outline-none focus:ring-1 focus:ring-ifa-pine cursor-pointer"
    >
      {#each filters as filter}
        <option value={filter.id}>{filter.label}</option>
      {/each}
    </select>
    <Filter class="absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-ifa-text-muted pointer-events-none" />
  </div>

  <!-- Sort Dropdown -->
  <div class="relative">
    <select
      bind:value={sortBy}
      on:change={() => onSortChange(sortBy)}
      class="appearance-none bg-ifa-card-muted border border-ifa-border rounded-lg pl-10 pr-8 py-2.5 text-sm text-ifa-text-primary focus:outline-none focus:ring-1 focus:ring-ifa-pine cursor-pointer"
    >
      {#each sortOptions as option}
        <option value={option.id}>{option.label}</option>
      {/each}
    </select>
    <ArrowUpDown class="absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-ifa-text-muted pointer-events-none" />
  </div>
</div>
