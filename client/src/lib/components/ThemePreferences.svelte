<script lang="ts">
  import { Moon, Sun, Monitor, Palette, Check, Sparkles } from 'lucide-svelte';
  import { preferences, updatePreferences, type ThemeMode } from '$lib/stores/preferencesStore';

  export let settings: any;

  const themes: { id: ThemeMode; name: string; icon: any }[] = [
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

  // Keep the settings page's local model and the live store in lockstep; the
  // store applies changes to <html> immediately (no save required to preview).
  function setThemeMode(id: ThemeMode) {
    settings.theme = id;
    updatePreferences({ theme: id });
  }

  function setAccent(id: string) {
    settings.accentColor = id;
    updatePreferences({ accentColor: id as any });
  }

  function setFontSize(value: string) {
    settings.fontSize = value;
    updatePreferences({ fontSize: value as any });
  }

  function toggleCompact() {
    settings.compactMode = !settings.compactMode;
    updatePreferences({ compactMode: settings.compactMode });
  }

  function toggleReducedMotion() {
    settings.reducedMotion = !settings.reducedMotion;
    updatePreferences({ reducedMotion: settings.reducedMotion });
  }

  $: currentTheme = $preferences.theme;
  $: currentAccent = settings.accentColor;
</script>

<div class="bg-ifa-card rounded-2xl border border-ifa-border p-6">
  <h2 class="text-lg font-bold text-ifa-text-primary mb-6 flex items-center gap-2">
    <Palette class="w-5 h-5 text-ifa-pine" />
    Theme & Appearance
  </h2>
  <div class="space-y-6">
    <!-- Theme Selection -->
    <div>
      <span class="block text-sm font-semibold text-ifa-text-primary mb-3">Theme</span>
      <div class="grid grid-cols-3 gap-3">
        {#each themes as theme}
          <button
            type="button"
            on:click={() => setThemeMode(theme.id)}
            class="flex flex-col items-center gap-2 p-4 rounded-xl border-2 transition {currentTheme === theme.id
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
            {#if currentTheme === theme.id}
              <Check class="w-4 h-4 text-ifa-pine" />
            {/if}
          </button>
        {/each}
      </div>
    </div>

    <!-- Accent Color -->
    <div>
      <span class="block text-sm font-semibold text-ifa-text-primary mb-3">Accent Color</span>
      <div class="grid grid-cols-4 gap-3">
        {#each accentColors as color}
          <button
            type="button"
            on:click={() => setAccent(color.id)}
            class="relative p-4 rounded-xl border-2 transition flex flex-col items-center gap-2 {currentAccent === color.id
              ? 'border-ifa-pine'
              : 'border-ifa-border hover:border-ifa-pine/50'}"
          >
            <div class="w-8 h-8 rounded-full" style="background-color: {color.color}"></div>
            <span class="text-xs font-medium text-ifa-text-primary">{color.name}</span>
            {#if currentAccent === color.id}
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
      <label class="block text-sm font-semibold text-ifa-text-primary mb-2" for="pref-font-size">Font Size</label>
      <select
        id="pref-font-size"
        value={settings.fontSize}
        on:change={(e) => setFontSize((e.currentTarget as HTMLSelectElement).value)}
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
        <label class="text-sm font-semibold text-ifa-text-primary" for="pref-compact">Compact Mode</label>
        <p class="text-xs text-ifa-text-secondary">Reduce spacing for more content</p>
      </div>
      <button
        id="pref-compact"
        type="button"
        role="switch"
        aria-label="Compact mode"
        aria-checked={settings.compactMode}
        on:click={toggleCompact}
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
        <label class="text-sm font-semibold text-ifa-text-primary" for="pref-reduced-motion">Reduced Motion</label>
        <p class="text-xs text-ifa-text-secondary">Minimize animations and the cursor glow</p>
      </div>
      <button
        id="pref-reduced-motion"
        type="button"
        role="switch"
        aria-label="Reduced motion"
        aria-checked={settings.reducedMotion}
        on:click={toggleReducedMotion}
        class="relative w-12 h-6 rounded-full transition-colors {settings.reducedMotion ? 'bg-ifa-pine' : 'bg-gray-300'}"
      >
        <span
          class="absolute top-1 left-1 w-4 h-4 rounded-full bg-white transition-transform {settings.reducedMotion ? 'translate-x-6' : 'translate-x-0'}"
        ></span>
      </button>
    </div>

    <!-- Animated cursor info -->
    <div class="flex items-start gap-3 rounded-xl border border-ifa-border bg-ifa-card-muted p-4">
      <Sparkles class="w-4 h-4 text-ifa-pine mt-0.5 shrink-0" />
      <p class="text-xs text-ifa-text-secondary">
        The glowing cursor with a shooting-star tail follows your pointer, glows over clickable
        elements, and is automatically disabled on touch devices and when Reduced Motion is on.
      </p>
    </div>
  </div>
</div>
