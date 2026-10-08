<script lang="ts">
  import { onMount } from 'svelte';
  import { Save, User, Check } from 'lucide-svelte';
  import LearningPreferences from '$lib/components/LearningPreferences.svelte';
  import ResourcePreferences from '$lib/components/ResourcePreferences.svelte';
  import NotificationPreferences from '$lib/components/NotificationPreferences.svelte';
  import ThemePreferences from '$lib/components/ThemePreferences.svelte';
  import { settingsApi, learnerApi, authApi } from '$lib/api';
  import { initSession } from '$lib/stores/sessionStore';
  import { hydratePreferences } from '$lib/stores/preferencesStore';

  let activeTab = 'learning'; // 'learning' | 'resources' | 'notifications' | 'theme' | 'account'
  let saving = false;
  let loading = true;
  let saveError = '';
  let saveSuccess = false;

  // These values are the ones the IFA advisor already set during course
  // creation; this page edits them rather than defining new ones.
  const userSettings = {
    learning: {
      preferredLanguage: 'English',
      learningStyle: 'practical',
      studyHours: 5,
      difficultyPreference: 'intermediate',
      videoPreference: 'enabled'
    },
    resources: {
      preferredTopics: [] as string[]
    },
    notifications: {
      courseReminders: true,
      assessmentResults: true,
      recommendations: true,
      emailDigest: 'weekly'
    },
    theme: {
      theme: 'system',
      accentColor: 'pine',
      fontSize: 'medium',
      compactMode: false,
      reducedMotion: false
    },
    account: {
      name: '',
      email: '',
      avatarUrl: ''
    }
  };

  // --- mapping helpers (backend stored value <-> select value) ---
  const LANG_TO_UI: Record<string, string> = { en: 'English', am: 'Amharic', fr: 'French', es: 'Spanish' };
  const LANG_TO_API: Record<string, string> = { English: 'en', Amharic: 'am', French: 'fr', Spanish: 'es' };
  const STYLE_TO_UI: Record<string, string> = {
    'hands-on': 'practical', practical: 'practical', theoretical: 'theoretical',
    visual: 'video', video: 'video', reading: 'reading', mixed: 'mixed'
  };

  function parseJsonArray(json: string | null | undefined): string[] {
    if (!json) return [];
    try {
      const parsed = JSON.parse(json);
      return Array.isArray(parsed) ? parsed.filter((x) => typeof x === 'string') : [];
    } catch {
      return [];
    }
  }

  onMount(async () => {
    try {
      await initSession();
      const [me, profile, settings] = await Promise.all([
        authApi.me().catch(() => null),
        learnerApi.profile().catch(() => null),
        settingsApi.get().catch(() => null)
      ]);

      if (me) {
        userSettings.account.name = me.name ?? '';
        userSettings.account.email = me.email ?? '';
        userSettings.account.avatarUrl = me.avatarUrl ?? '';
      }

      if (profile) {
        userSettings.learning.preferredLanguage = LANG_TO_UI[profile.preferredLanguage] ?? profile.preferredLanguage ?? 'English';
        userSettings.learning.learningStyle = STYLE_TO_UI[(profile.learningStyle ?? '').toLowerCase()] ?? 'practical';
        if (profile.weeklyStudyHours > 0) userSettings.learning.studyHours = profile.weeklyStudyHours;
        const level = (profile.currentLevel ?? '').toLowerCase();
        if (['beginner', 'intermediate', 'advanced', 'adaptive'].includes(level)) {
          userSettings.learning.difficultyPreference = level;
        }
      }

      if (settings) {
        userSettings.notifications.courseReminders = settings.dailyReminders;
        userSettings.notifications.assessmentResults = settings.assessmentResults;
        userSettings.notifications.recommendations = settings.courseRecommendations;
        userSettings.notifications.emailDigest = settings.weeklyDigest ? 'weekly' : 'never';
        userSettings.theme.theme = settings.theme ?? 'system';
        userSettings.theme.accentColor = settings.accentColor ?? 'pine';
        userSettings.theme.fontSize = settings.fontSize ?? 'medium';
        userSettings.theme.compactMode = settings.compactMode;
        userSettings.theme.reducedMotion = settings.reducedMotion;
        userSettings.resources.preferredTopics = parseJsonArray(settings.preferredTopicsJson);

        // Apply the learner's saved appearance preferences to the live app.
        hydratePreferences({
          theme: (settings.theme as any) ?? 'system',
          accentColor: (settings.accentColor as any) ?? 'pine',
          fontSize: (settings.fontSize as any) ?? 'medium',
          compactMode: settings.compactMode ?? false,
          reducedMotion: settings.reducedMotion ?? false
        });
      }
    } catch {
      /* keep defaults */
    } finally {
      loading = false;
    }
  });

  async function handleSaveSettings() {
    saving = true;
    saveError = '';
    saveSuccess = false;
    try {
      await Promise.all([
        settingsApi.update({
          dailyReminders: userSettings.notifications.courseReminders,
          weeklyDigest: userSettings.notifications.emailDigest !== 'never',
          assessmentResults: userSettings.notifications.assessmentResults,
          courseRecommendations: userSettings.notifications.recommendations,
          theme: userSettings.theme.theme,
          accentColor: userSettings.theme.accentColor,
          fontSize: userSettings.theme.fontSize,
          compactMode: userSettings.theme.compactMode,
          reducedMotion: userSettings.theme.reducedMotion,
          preferredTopicsJson: JSON.stringify(userSettings.resources.preferredTopics)
        }),
        learnerApi.updateProfile({
          weeklyStudyHours: userSettings.learning.studyHours,
          currentLevel: userSettings.learning.difficultyPreference,
          learningStyle: userSettings.learning.learningStyle,
          preferredLanguage: LANG_TO_API[userSettings.learning.preferredLanguage] ?? userSettings.learning.preferredLanguage
        }),
        authApi.updateMe({ name: userSettings.account.name, avatarUrl: userSettings.account.avatarUrl })
      ]);
      saveSuccess = true;
      setTimeout(() => (saveSuccess = false), 2500);
    } catch (cause) {
      saveError = cause instanceof Error ? cause.message : 'Could not save your settings.';
    } finally {
      saving = false;
    }
  }

  function handleTabChange(tab: string) {
    activeTab = tab;
  }

  const tabs = [
    { id: 'learning', label: 'Learning' },
    { id: 'resources', label: 'Topics' },
    { id: 'notifications', label: 'Notifications' },
    { id: 'theme', label: 'Theme' },
    { id: 'account', label: 'Account' }
  ];
</script>

<div class="p-6 md:p-8 space-y-6 max-w-[1200px] mx-auto">
  <!-- Header -->
  <div class="flex items-center justify-between">
    <div>
      <h1 class="text-2xl font-bold text-ifa-text-primary">Settings</h1>
      <p class="text-sm text-ifa-text-secondary mt-1">
        Edit the preferences your IFA advisor set up — changes apply to future course generation.
      </p>
    </div>
    <div class="flex items-center gap-3">
      {#if saveSuccess}
        <span class="flex items-center gap-1 text-sm text-emerald-600 font-semibold">
          <Check class="w-4 h-4" /> Saved
        </span>
      {/if}
      <button
        type="button"
        on:click={handleSaveSettings}
        disabled={saving || loading}
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
  </div>

  {#if saveError}
    <p role="alert" class="rounded-lg border border-red-200 bg-red-50 px-4 py-2 text-sm text-red-700">{saveError}</p>
  {/if}

  <!-- Tab Navigation -->
  <div class="flex items-center gap-1 bg-ifa-card-muted rounded-xl p-1">
    {#each tabs as tab}
      <button
        type="button"
        on:click={() => handleTabChange(tab.id)}
        class="flex-1 py-2 px-4 rounded-lg text-sm font-semibold transition {activeTab === tab.id
          ? 'bg-white text-ifa-pine shadow-soft'
          : 'text-ifa-text-secondary hover:text-ifa-pine'}"
      >
        {tab.label}
      </button>
    {/each}
  </div>

  <!-- Tab Content -->
  {#if activeTab === 'learning'}
    <LearningPreferences settings={userSettings.learning} />
  {:else if activeTab === 'resources'}
    <ResourcePreferences settings={userSettings.resources} />
  {:else if activeTab === 'notifications'}
    <NotificationPreferences settings={userSettings.notifications} />
  {:else if activeTab === 'theme'}
    <ThemePreferences settings={userSettings.theme} />
  {:else if activeTab === 'account'}
    <div class="bg-ifa-card rounded-2xl border border-ifa-border p-6">
      <h2 class="text-lg font-bold text-ifa-text-primary mb-6 flex items-center gap-2">
        <User class="w-5 h-5" />
        Account Settings
      </h2>
      <div class="space-y-4">
        <div class="flex items-center gap-4">
          <img
            src={userSettings.account.avatarUrl || 'https://ui-avatars.com/api/?name=' + encodeURIComponent(userSettings.account.name || 'Learner')}
            alt="Profile"
            class="w-16 h-16 rounded-full object-cover border border-ifa-border"
          />
          <div class="flex-1">
            <label for="account-avatar" class="block text-sm font-semibold text-ifa-text-primary mb-2">Profile Picture URL</label>
            <input
              id="account-avatar"
              type="url"
              bind:value={userSettings.account.avatarUrl}
              placeholder="https://..."
              class="w-full px-4 py-2.5 rounded-lg bg-ifa-card-muted border border-ifa-border text-sm text-ifa-text-primary focus:outline-none focus:ring-1 focus:ring-ifa-pine"
            />
          </div>
        </div>
        <div>
          <label for="account-name" class="block text-sm font-semibold text-ifa-text-primary mb-2">Display Name</label>
          <input
            id="account-name"
            type="text"
            bind:value={userSettings.account.name}
            class="w-full px-4 py-2.5 rounded-lg bg-ifa-card-muted border border-ifa-border text-sm text-ifa-text-primary focus:outline-none focus:ring-1 focus:ring-ifa-pine"
          />
        </div>
        <div>
          <label for="account-email" class="block text-sm font-semibold text-ifa-text-primary mb-2">Email</label>
          <input
            id="account-email"
            type="email"
            value={userSettings.account.email}
            readonly
            class="w-full px-4 py-2.5 rounded-lg bg-ifa-card-muted border border-ifa-border text-sm text-ifa-text-muted cursor-not-allowed"
          />
          <p class="text-xs text-ifa-text-muted mt-1">Your sign-in email can't be changed here.</p>
        </div>
      </div>
    </div>
  {/if}
</div>
