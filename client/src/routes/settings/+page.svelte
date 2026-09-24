<script lang="ts">
  import { Save, Bell, User, Globe, Volume2, Moon, Sun, Palette } from 'lucide-svelte';
  import LearningPreferences from '$lib/components/LearningPreferences.svelte';
  import ResourcePreferences from '$lib/components/ResourcePreferences.svelte';
  import NotificationPreferences from '$lib/components/NotificationPreferences.svelte';

  let activeTab = 'learning'; // 'learning' | 'resources' | 'notifications' | 'account'
  let saving = false;

  const userSettings = {
    learning: {
      preferredLanguage: 'English',
      learningStyle: 'practical',
      studyHours: 5,
      difficultyPreference: 'intermediate',
      videoPreference: 'enabled'
    },
    resources: {
      preferredChannels: ['Nick Chapsas', 'FreeCodeCamp', 'sentdex'],
      preferredTopics: ['Backend Development', 'API Design', 'Database Optimization'],
      excludeChannels: []
    },
    notifications: {
      courseReminders: true,
      newModuleAlerts: true,
      assessmentResults: true,
      recommendations: true,
      emailDigest: 'weekly'
    },
    account: {
      name: 'Nathnel',
      email: 'nathnel@example.com',
      timezone: 'UTC+3'
    }
  };

  function handleSaveSettings() {
    saving = true;
    setTimeout(() => {
      saving = false;
      console.log('Settings saved:', userSettings);
    }, 1000);
  }

  function handleTabChange(tab: string) {
    activeTab = tab;
  }
</script>

<div class="p-6 md:p-8 space-y-6 max-w-[1200px] mx-auto">
  <!-- Header -->
  <div class="flex items-center justify-between">
    <div>
      <h1 class="text-2xl font-bold text-ifa-text-primary">Settings</h1>
      <p class="text-sm text-ifa-text-secondary mt-1">Customize your learning experience and preferences</p>
    </div>
    <button
      type="button"
      on:click={handleSaveSettings}
      disabled={saving}
      class="px-4 py-2 bg-ifa-pine text-white rounded-lg text-sm font-semibold flex items-center gap-2 hover:bg-emerald-800 transition disabled:opacity-50 disabled:cursor-not-allowed"
    >
      {#if saving}
        <span>Saving...</span>
      {:else}
        <Save class="w-4 h-4" />
        <span>Save Changes</span>
      {/if}
    </button>
  </div>

  <!-- Tab Navigation -->
  <div class="flex items-center gap-1 bg-ifa-card-muted rounded-xl p-1">
    <button
      type="button"
      on:click={() => handleTabChange('learning')}
      class="flex-1 py-2 px-4 rounded-lg text-sm font-semibold transition {activeTab === 'learning'
        ? 'bg-white text-ifa-pine shadow-soft'
        : 'text-ifa-text-secondary hover:text-ifa-pine'}"
    >
      Learning
    </button>
    <button
      type="button"
      on:click={() => handleTabChange('resources')}
      class="flex-1 py-2 px-4 rounded-lg text-sm font-semibold transition {activeTab === 'resources'
        ? 'bg-white text-ifa-pine shadow-soft'
        : 'text-ifa-text-secondary hover:text-ifa-pine'}"
    >
      Resources
    </button>
    <button
      type="button"
      on:click={() => handleTabChange('notifications')}
      class="flex-1 py-2 px-4 rounded-lg text-sm font-semibold transition {activeTab === 'notifications'
        ? 'bg-white text-ifa-pine shadow-soft'
        : 'text-ifa-text-secondary hover:text-ifa-pine'}"
    >
      Notifications
    </button>
    <button
      type="button"
      on:click={() => handleTabChange('account')}
      class="flex-1 py-2 px-4 rounded-lg text-sm font-semibold transition {activeTab === 'account'
        ? 'bg-white text-ifa-pine shadow-soft'
        : 'text-ifa-text-secondary hover:text-ifa-pine'}"
    >
      Account
    </button>
  </div>

  <!-- Tab Content -->
  {#if activeTab === 'learning'}
    <LearningPreferences settings={userSettings.learning} />
  {:else if activeTab === 'resources'}
    <ResourcePreferences settings={userSettings.resources} />
  {:else if activeTab === 'notifications'}
    <NotificationPreferences settings={userSettings.notifications} />
  {:else if activeTab === 'account'}
    <div class="bg-ifa-card rounded-2xl border border-ifa-border p-6">
      <h2 class="text-lg font-bold text-ifa-text-primary mb-6 flex items-center gap-2">
        <User class="w-5 h-5" />
        Account Settings
      </h2>
      <div class="space-y-4">
        <div>
          <label class="block text-sm font-semibold text-ifa-text-primary mb-2">Display Name</label>
          <input
            type="text"
            bind:value={userSettings.account.name}
            class="w-full px-4 py-2.5 rounded-lg bg-ifa-card-muted border border-ifa-border text-sm text-ifa-text-primary focus:outline-none focus:ring-1 focus:ring-ifa-pine"
          />
        </div>
        <div>
          <label class="block text-sm font-semibold text-ifa-text-primary mb-2">Email</label>
          <input
            type="email"
            bind:value={userSettings.account.email}
            class="w-full px-4 py-2.5 rounded-lg bg-ifa-card-muted border border-ifa-border text-sm text-ifa-text-primary focus:outline-none focus:ring-1 focus:ring-ifa-pine"
          />
        </div>
        <div>
          <label class="block text-sm font-semibold text-ifa-text-primary mb-2">Timezone</label>
          <select
            bind:value={userSettings.account.timezone}
            class="w-full px-4 py-2.5 rounded-lg bg-ifa-card-muted border border-ifa-border text-sm text-ifa-text-primary focus:outline-none focus:ring-1 focus:ring-ifa-pine"
          >
            <option value="UTC+3">UTC+3 (East Africa Time)</option>
            <option value="UTC+0">UTC+0 (GMT)</option>
            <option value="UTC-5">UTC-5 (Eastern Time)</option>
            <option value="UTC-8">UTC-8 (Pacific Time)</option>
          </select>
        </div>
      </div>
    </div>
  {/if}
</div>
