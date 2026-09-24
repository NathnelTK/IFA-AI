<script lang="ts">
  import { Bell, Mail, Zap } from 'lucide-svelte';

  export let settings: any;
</script>

<div class="bg-ifa-card rounded-2xl border border-ifa-border p-6">
  <h2 class="text-lg font-bold text-ifa-text-primary mb-6 flex items-center gap-2">
    <Bell class="w-5 h-5 text-ifa-pine" />
    Notification Preferences
  </h2>
  <div class="space-y-4">
    <div class="flex items-center justify-between">
      <div>
        <h3 class="text-sm font-semibold text-ifa-text-primary">Course Reminders</h3>
        <p class="text-xs text-ifa-text-secondary">Get reminded about your learning schedule</p>
      </div>
      <button
        type="button"
        on:click={() => settings.courseReminders = !settings.courseReminders}
        class="relative w-12 h-6 rounded-full transition-colors {settings.courseReminders ? 'bg-ifa-pine' : 'bg-gray-300'}"
      >
        <span
          class="absolute top-1 left-1 w-4 h-4 rounded-full bg-white transition-transform {settings.courseReminders ? 'translate-x-6' : 'translate-x-0'}"
        ></span>
      </button>
    </div>

    <div class="flex items-center justify-between">
      <div>
        <h3 class="text-sm font-semibold text-ifa-text-primary">New Module Alerts</h3>
        <p class="text-xs text-ifa-text-secondary">Notify when new modules are ready</p>
      </div>
      <button
        type="button"
        on:click={() => settings.newModuleAlerts = !settings.newModuleAlerts}
        class="relative w-12 h-6 rounded-full transition-colors {settings.newModuleAlerts ? 'bg-ifa-pine' : 'bg-gray-300'}"
      >
        <span
          class="absolute top-1 left-1 w-4 h-4 rounded-full bg-white transition-transform {settings.newModuleAlerts ? 'translate-x-6' : 'translate-x-0'}"
        ></span>
      </button>
    </div>

    <div class="flex items-center justify-between">
      <div>
        <h3 class="text-sm font-semibold text-ifa-text-primary">Assessment Results</h3>
        <p class="text-xs text-ifa-text-secondary">Get notified when you complete quizzes/tests</p>
      </div>
      <button
        type="button"
        on:click={() => settings.assessmentResults = !settings.assessmentResults}
        class="relative w-12 h-6 rounded-full transition-colors {settings.assessmentResults ? 'bg-ifa-pine' : 'bg-gray-300'}"
      >
        <span
          class="absolute top-1 left-1 w-4 h-4 rounded-full bg-white transition-transform {settings.assessmentResults ? 'translate-x-6' : 'translate-x-0'}"
        ></span>
      </button>
    </div>

    <div class="flex items-center justify-between">
      <div>
        <h3 class="text-sm font-semibold text-ifa-text-primary flex items-center gap-2">
          <Zap class="w-4 h-4" />
          IFA Recommendations
        </h3>
        <p class="text-xs text-ifa-text-secondary">Personalized learning suggestions</p>
      </div>
      <button
        type="button"
        on:click={() => settings.recommendations = !settings.recommendations}
        class="relative w-12 h-6 rounded-full transition-colors {settings.recommendations ? 'bg-ifa-pine' : 'bg-gray-300'}"
      >
        <span
          class="absolute top-1 left-1 w-4 h-4 rounded-full bg-white transition-transform {settings.recommendations ? 'translate-x-6' : 'translate-x-0'}"
        ></span>
      </button>
    </div>

    <div>
      <label class="block text-sm font-semibold text-ifa-text-primary mb-2 flex items-center gap-2">
        <Mail class="w-4 h-4" />
        Email Digest
      </label>
      <select
        bind:value={settings.emailDigest}
        class="w-full px-4 py-2.5 rounded-lg bg-ifa-card-muted border border-ifa-border text-sm text-ifa-text-primary focus:outline-none focus:ring-1 focus:ring-ifa-pine"
      >
        <option value="daily">Daily</option>
        <option value="weekly">Weekly</option>
        <option value="monthly">Monthly</option>
        <option value="never">Never</option>
      </select>
    </div>
  </div>
</div>
