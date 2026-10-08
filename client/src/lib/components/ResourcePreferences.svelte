<script lang="ts">
  import { Tag, Plus, X } from 'lucide-svelte';

  export let settings: { preferredTopics: string[] };

  let newTopic = '';

  function addTopic() {
    const value = newTopic.trim();
    if (value && !settings.preferredTopics.includes(value)) {
      settings.preferredTopics = [...settings.preferredTopics, value];
    }
    newTopic = '';
  }

  function removeTopic(index: number) {
    settings.preferredTopics = settings.preferredTopics.filter((_, i) => i !== index);
  }

  function onKeydown(event: KeyboardEvent) {
    if (event.key === 'Enter') {
      event.preventDefault();
      addTopic();
    }
  }
</script>

<div class="bg-ifa-card rounded-2xl border border-ifa-border p-6">
  <h2 class="text-lg font-bold text-ifa-text-primary mb-2 flex items-center gap-2">
    <Tag class="w-5 h-5 text-ifa-pine" />
    Preferred Topics
  </h2>
  <p class="text-sm text-ifa-text-secondary mb-6">
    Topics you want IFA to emphasize when generating and recommending courses. External sources
    (websites, YouTube, PDFs) are now managed in the Research section.
  </p>

  <div class="flex flex-wrap gap-2 mb-3">
    {#each settings.preferredTopics as topic, index}
      <span class="px-3 py-1.5 rounded-full bg-ifa-pine/10 text-ifa-pine text-sm font-medium flex items-center gap-2">
        {topic}
        <button
          type="button"
          on:click={() => removeTopic(index)}
          class="text-ifa-pine/60 hover:text-ifa-pine"
          aria-label={`Remove ${topic}`}
        >
          <X class="w-3 h-3" />
        </button>
      </span>
    {/each}
    {#if settings.preferredTopics.length === 0}
      <p class="text-sm text-ifa-text-muted">No topics yet — add a few below.</p>
    {/if}
  </div>

  <div class="flex gap-2">
    <input
      type="text"
      bind:value={newTopic}
      on:keydown={onKeydown}
      placeholder="Add a topic (e.g. API Design)…"
      class="flex-1 px-4 py-2 rounded-lg bg-ifa-card-muted border border-ifa-border text-sm text-ifa-text-primary placeholder-ifa-text-muted focus:outline-none focus:ring-1 focus:ring-ifa-pine"
    />
    <button
      type="button"
      on:click={addTopic}
      class="px-4 py-2 bg-ifa-pine text-white rounded-lg text-sm font-semibold flex items-center gap-2 hover:bg-emerald-800 transition"
    >
      <Plus class="w-4 h-4" />
      <span>Add</span>
    </button>
  </div>
</div>
