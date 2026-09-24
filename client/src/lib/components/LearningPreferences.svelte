<script lang="ts">
  import { Clock, Brain, Target, Video } from 'lucide-svelte';

  export let settings: any;
</script>

<div class="bg-ifa-card rounded-2xl border border-ifa-border p-6">
  <h2 class="text-lg font-bold text-ifa-text-primary mb-6 flex items-center gap-2">
    <Brain class="w-5 h-5 text-ifa-pine" />
    Learning Preferences
  </h2>
  <div class="space-y-6">
    <div>
      <label class="block text-sm font-semibold text-ifa-text-primary mb-2">Preferred Language</label>
      <select
        bind:value={settings.preferredLanguage}
        class="w-full px-4 py-2.5 rounded-lg bg-ifa-card-muted border border-ifa-border text-sm text-ifa-text-primary focus:outline-none focus:ring-1 focus:ring-ifa-pine"
      >
        <option value="English">English</option>
        <option value="Amharic">Amharic</option>
        <option value="French">French</option>
        <option value="Spanish">Spanish</option>
      </select>
    </div>

    <div>
      <label class="block text-sm font-semibold text-ifa-text-primary mb-2">Learning Style</label>
      <select
        bind:value={settings.learningStyle}
        class="w-full px-4 py-2.5 rounded-lg bg-ifa-card-muted border border-ifa-border text-sm text-ifa-text-primary focus:outline-none focus:ring-1 focus:ring-ifa-pine"
      >
        <option value="practical">Practical Projects</option>
        <option value="theoretical">Theoretical Deep Dives</option>
        <option value="video">Video-First</option>
        <option value="reading">Reading-First</option>
        <option value="mixed">Mixed Approach</option>
      </select>
    </div>

    <div>
      <label class="block text-sm font-semibold text-ifa-text-primary mb-2 flex items-center gap-2">
        <Clock class="w-4 h-4" />
        Weekly Study Hours
      </label>
      <input
        type="number"
        bind:value={settings.studyHours}
        min="1"
        max="40"
        class="w-full px-4 py-2.5 rounded-lg bg-ifa-card-muted border border-ifa-border text-sm text-ifa-text-primary focus:outline-none focus:ring-1 focus:ring-ifa-pine"
      />
      <p class="text-xs text-ifa-text-muted mt-1">Recommended: 5-10 hours per week for optimal progress</p>
    </div>

    <div>
      <label class="block text-sm font-semibold text-ifa-text-primary mb-2 flex items-center gap-2">
        <Target class="w-4 h-4" />
        Difficulty Preference
      </label>
      <select
        bind:value={settings.difficultyPreference}
        class="w-full px-4 py-2.5 rounded-lg bg-ifa-card-muted border border-ifa-border text-sm text-ifa-text-primary focus:outline-none focus:ring-1 focus:ring-ifa-pine"
      >
        <option value="beginner">Beginner</option>
        <option value="intermediate">Intermediate</option>
        <option value="advanced">Advanced</option>
        <option value="adaptive">Adaptive (IFA Decides)</option>
      </select>
    </div>

    <div class="flex items-center justify-between">
      <div>
        <label class="text-sm font-semibold text-ifa-text-primary flex items-center gap-2">
          <Video class="w-4 h-4" />
          Video Content
        </label>
        <p class="text-xs text-ifa-text-muted">Include video lessons in your courses</p>
      </div>
      <button
        type="button"
        on:click={() => settings.videoPreference = settings.videoPreference === 'enabled' ? 'disabled' : 'enabled'}
        class="relative w-12 h-6 rounded-full transition-colors {settings.videoPreference === 'enabled' ? 'bg-ifa-pine' : 'bg-gray-300'}"
      >
        <span
          class="absolute top-1 left-1 w-4 h-4 rounded-full bg-white transition-transform {settings.videoPreference === 'enabled' ? 'translate-x-6' : 'translate-x-0'}"
        ></span>
      </button>
    </div>
  </div>
</div>
