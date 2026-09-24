<script lang="ts">
  import { Moon, Sun, Monitor, Palette, Check } from 'lucide-svelte';

  export let settings: any;

  const themes = [
    { id: 'light', name: 'Light', icon: Sun },
    { id: 'dark', name: 'Dark', icon: Moon },
    { id: 'system', name: 'System', icon: Monitor }
  ];

  const accentColors = [
    { id: 'pine', name: 'Pine Green', color: '#2A9D68' },
    { id: 'blue', name: 'Ocean Blue', color: '#3B82F6' },
    { id: 'purple', name: 'Royal Purple', color: '#7C5CFC' },
    { id: 'orange', name: 'Sunset Orange', color: '#F59E0B' }
  ];

  function handleThemeChange(themeId: string) {
    settings.theme = themeId;
  }

  function handleAccentColorChange(colorId: string) {
    settings.accentColor = colorId;
  }
</script>

<div class="bg-ifa-card rounded-2xl border border-ifa-border p-6">
  <h2 class="text-lg font-bold text-ifa-text-primary mb-6 flex items-center gap-2">
    <Palette class="w-5 h-5 text-ifa-pine" />
    Theme & Appearance
  </h2>
  <div class="space-y-6">
    <!-- Theme Selection -->
    <div>
      <label class="block text-sm font-semibold text-ifa-text-primary mb-3">Theme</label>
      <div class="grid grid-cols-3 gap-3">
        {#each themes as theme}
          <button
            type="button"
            on:click={() => handleThemeChange(theme.id)}
            class="flex flex-col items-center gap-2 p-4 rounded-xl border-2 transition {settings.theme === theme.id
              ? 'border-ifa-pine bg-ifa-pine/10'
              : 'border-ifa-border hover:border-ifa-pine/50'}"
          >
            {#if theme.icon === Sun}
              <Sun class="w-6 h-6 text-ifa-text-primary" />
            {:else if theme.icon === Moon}
              <Moon class="w-6 h-6 text-ifa-text-primary" />
            {:else}
              <Monitor class="w-6 h-6 text-ifa-text-primary" />
            {/if}
            <span class="text-sm font-medium text-ifa-text-primary">{theme.name}</span>
            {#if settings.theme === theme.id}
              <Check class="w-4 h-4 text-ifa-pine" />
            {/if}
          </button>
        {/each}
      </div>
    </div>

    <!-- Accent Color -->
    <div>
      <label class="block text-sm font-semibold text-ifa-text-primary mb-3">Accent Color</label>
      <div class="grid grid-cols-4 gap-3">
        {#each accentColors as color}
          <button
            type="button"
            on:click={() => handleAccentColorChange(color.id)}
            class="relative p-4 rounded-xl border-2 transition flex flex-col items-center gap-2 {settings.accentColor === color.id
              ? 'border-ifa-pine'
              : 'border-ifa-border hover:border-ifa-pine/50'}"
          >
            <div
              class="w-8 h-8 rounded-full"
              style="background-color: {color.color}"
            ></div>
            <span class="text-xs font-medium text-ifa-text-primary">{color.name}</span>
            {#if settings.accentColor === color.id}
              <div class="absolute top-2 right-2 w-5 h-5 rounded-full bg-ifa-pine flex items-center justify-center">
                <Check class="w-3 h-3 text-white" />
              </div>
            {/if}
          </button>
        {/each}
      </div>
    </div>

    <!-- Font Size -->
    <div>
      <label class="block text-sm font-semibold text-ifa-text-primary mb-2">Font Size</label>
      <select
        bind:value={settings.fontSize}
        class="w-full px-4 py-2.5 rounded-lg bg-ifa-card-muted border border-ifa-border text-sm text-ifa-text-primary focus:outline-none focus:ring-1 focus:ring-ifa-pine"
      >
        <option value="small">Small</option>
        <option value="medium">Medium</option>
        <option value="large">Large</option>
        <option value="extra-large">Extra Large</option>
      </select>
    </div>

    <!-- Compact Mode -->
    <div class="flex items-center justify-between">
      <div>
        <label class="text-sm font-semibold text-ifa-text-primary">Compact Mode</label>
        <p class="text-xs text-ifa-text-secondary">Reduce spacing for more content</p>
      </div>
      <button
        type="button"
        on:click={() => settings.compactMode = !settings.compactMode}
        class="relative w-12 h-6 rounded-full transition-colors {settings.compactMode ? 'bg-ifa-pine' : 'bg-gray-300'}"
      >
        <span
          class="absolute top-1 left-1 w-4 h-4 rounded-full bg-white transition-transform {settings.compactMode ? 'translate-x-6' : 'translate-x-0'}"
        ></span>
      </button>
    </div>

    <!-- Reduced Motion -->
    <div class="flex items-center justify-between">
      <div>
        <label class="text-sm font-semibold text-ifa-text-primary">Reduced Motion</label>
        <p class="text-xs text-ifa-text-secondary">Minimize animations</p>
      </div>
      <button
        type="button"
        on:click={() => settings.reducedMotion = !settings.reducedMotion}
        class="relative w-12 h-6 rounded-full transition-colors {settings.reducedMotion ? 'bg-ifa-pine' : 'bg-gray-300'}"
      >
        <span
          class="absolute top-1 left-1 w-4 h-4 rounded-full bg-white transition-transform {settings.reducedMotion ? 'translate-x-6' : 'translate-x-0'}"
        ></span>
      </button>
    </div>
  </div>
</div>
