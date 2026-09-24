<script lang="ts">
  import { Youtube, Plus, X, Globe } from 'lucide-svelte';

  export let settings: any;

  let newChannel = '';

  function addChannel() {
    if (newChannel.trim()) {
      settings.preferredChannels = [...settings.preferredChannels, newChannel.trim()];
      newChannel = '';
    }
  }

  function removeChannel(index: number) {
    settings.preferredChannels = settings.preferredChannels.filter((_: any, i: number) => i !== index);
  }

  function addTopic() {
    // Simplified for demo
    console.log('Add topic');
  }
</script>

<div class="bg-ifa-card rounded-2xl border border-ifa-border p-6">
  <h2 class="text-lg font-bold text-ifa-text-primary mb-6 flex items-center gap-2">
    <Youtube class="w-5 h-5 text-red-500" />
    Content Preferences
  </h2>
  <div class="space-y-6">
    <div>
      <label class="block text-sm font-semibold text-ifa-text-primary mb-2">Preferred YouTube Channels</label>
      <div class="space-y-2 mb-3">
        {#each settings.preferredChannels as channel, index}
          <div class="flex items-center gap-2">
            <div class="flex-1 px-4 py-2 rounded-lg bg-ifa-card-muted border border-ifa-border text-sm text-ifa-text-primary">
              {channel}
            </div>
            <button
              type="button"
              on:click={() => removeChannel(index)}
              class="p-2 text-red-500 hover:bg-red-50 rounded-lg transition"
            >
              <X class="w-4 h-4" />
            </button>
          </div>
        {/each}
      </div>
      <div class="flex gap-2">
        <input
          type="text"
          bind:value={newChannel}
          placeholder="Add YouTube channel..."
          class="flex-1 px-4 py-2 rounded-lg bg-ifa-card-muted border border-ifa-border text-sm text-ifa-text-primary placeholder-ifa-text-muted focus:outline-none focus:ring-1 focus:ring-ifa-pine"
        />
        <button
          type="button"
          on:click={addChannel}
          class="px-4 py-2 bg-ifa-pine text-white rounded-lg text-sm font-semibold flex items-center gap-2 hover:bg-emerald-800 transition"
        >
          <Plus class="w-4 h-4" />
          <span>Add</span>
        </button>
      </div>
    </div>

    <div>
      <label class="block text-sm font-semibold text-ifa-text-primary mb-2">Preferred Topics</label>
      <div class="flex flex-wrap gap-2">
        {#each settings.preferredTopics as topic}
          <span class="px-3 py-1.5 rounded-full bg-ifa-pine/10 text-ifa-pine text-sm font-medium flex items-center gap-2">
            {topic}
            <button
              type="button"
              class="text-ifa-pine/60 hover:text-ifa-pine"
            >
              <X class="w-3 h-3" />
            </button>
          </span>
        {/each}
        <button
          type="button"
          on:click={addTopic}
          class="px-3 py-1.5 rounded-full border border-dashed border-ifa-border text-ifa-text-secondary text-sm font-medium hover:border-ifa-pine hover:text-ifa-pine transition"
        >
          + Add Topic
        </button>
      </div>
    </div>

    <div>
      <label class="block text-sm font-semibold text-ifa-text-primary mb-2">Exclude Channels</label>
      <p class="text-xs text-ifa-text-secondary mb-2">Channels you don't want recommendations from</p>
      <input
        type="text"
        placeholder="No excluded channels"
        class="w-full px-4 py-2.5 rounded-lg bg-ifa-card-muted border border-ifa-border text-sm text-ifa-text-primary placeholder-ifa-text-muted focus:outline-none focus:ring-1 focus:ring-ifa-pine"
      />
    </div>
  </div>
</div>
