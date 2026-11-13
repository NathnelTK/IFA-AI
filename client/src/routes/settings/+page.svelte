<script lang="ts">
  import { onMount } from 'svelte';
  import { Save, User, Check } from 'lucide-svelte';
  import LearningPreferences from '$lib/components/LearningPreferences.svelte';
  import ResourcePreferences from '$lib/components/ResourcePreferences.svelte';
  import NotificationPreferences from '$lib/components/NotificationPreferences.svelte';
  import ThemePreferences from '$lib/components/ThemePreferences.svelte';
  import { apiErrorMessage, settingsApi, learnerApi, authApi } from '$lib/api';
  import { initSession, refreshSession } from '$lib/stores/sessionStore';
  import { hydratePreferences, type ThemeMode, type AccentColor, type FontSize } from '$lib/stores/preferencesStore';

  let activeTab = 'learning'; // 'learning' | 'resources' | 'notifications' | 'theme' | 'account'
  let saving = false;
  let loading = true;
  let saveError = '';
  let loadError = '';
  let saveSuccess = false;
  let loadedSlices = { account: false, profile: false, settings: false };
  $: canSave = loadedSlices.account || loadedSlices.profile || loadedSlices.settings;

  // Defaults render the form while loading; save remains disabled until every
  // backend slice succeeds so defaults cannot overwrite unavailable data.
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
      theme: 'light',
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

  onMount(loadSettings);

  async function loadSettings() {
    loading = true;
    loadError = '';
    try {
      await initSession();
      const [meResult, profileResult, settingsResult] = await Promise.allSettled([
        authApi.me(),
        learnerApi.profile(),
        settingsApi.get()
      ]);

      if (meResult.status === 'fulfilled') {
        const me = meResult.value;
        userSettings.account.name = me.name ?? '';
        userSettings.account.email = me.email ?? '';
        userSettings.account.avatarUrl = me.avatarUrl ?? '';
        loadedSlices.account = true;
      }

      if (profileResult.status === 'fulfilled') {
        const profile = profileResult.value;
        userSettings.learning.preferredLanguage = LANG_TO_UI[profile.preferredLanguage] ?? profile.preferredLanguage ?? 'English';
        userSettings.learning.learningStyle = STYLE_TO_UI[(profile.learningStyle ?? '').toLowerCase()] ?? 'practical';
        if (profile.weeklyStudyHours > 0) userSettings.learning.studyHours = profile.weeklyStudyHours;
        const level = (profile.currentLevel ?? '').toLowerCase();
        if (['beginner', 'intermediate', 'advanced', 'adaptive'].includes(level)) {
          userSettings.learning.difficultyPreference = level;
        }
        loadedSlices.profile = true;
      }

      if (settingsResult.status === 'fulfilled') {
        const settings = settingsResult.value;
        userSettings.notifications.courseReminders = settings.dailyReminders;
        userSettings.notifications.assessmentResults = settings.assessmentResults;
        userSettings.notifications.recommendations = settings.courseRecommendations;
        userSettings.notifications.emailDigest = settings.weeklyDigest ? 'weekly' : 'never';
        userSettings.resources.preferredTopics = parseJsonArray(settings.preferredTopicsJson);
        const appearance = {
          theme: (['light', 'dark', 'system'].includes(settings.theme) ? settings.theme : 'system') as ThemeMode,
          accentColor: (['pine', 'blue', 'purple', 'orange'].includes(settings.accentColor) ? settings.accentColor : 'pine') as AccentColor,
          fontSize: (['small', 'medium', 'large', 'extra-large'].includes(settings.fontSize) ? settings.fontSize : 'medium') as FontSize,
          compactMode: settings.compactMode,
          reducedMotion: settings.reducedMotion
        };
        hydratePreferences(appearance);
        Object.assign(userSettings.theme, appearance);
        loadedSlices.settings = true;
      }
      loadedSlices = { ...loadedSlices };
      const unavailable = Object.entries(loadedSlices).filter(([, loaded]) => !loaded).map(([slice]) => slice);
      if (unavailable.length > 0) {
        loadError = `The ${unavailable.join(', ')} settings could not be loaded. Available sections remain editable; unavailable data will not be overwritten.`;
      }

    } catch (cause) {
      loadError = apiErrorMessage(cause, 'Settings could not be loaded.');
    } finally {
      loading = false;
    }
  }

  async function handleSaveSettings() {
    if (!canSave) return;
    saving = true;
    saveError = '';
    saveSuccess = false;
    try {
      const updates: Promise<unknown>[] = [];
      if (loadedSlices.settings) updates.push(settingsApi.update({
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
        }));
      if (loadedSlices.profile) updates.push(learnerApi.updateProfile({
          weeklyStudyHours: userSettings.learning.studyHours,
          currentLevel: userSettings.learning.difficultyPreference,
          learningStyle: userSettings.learning.learningStyle,
          preferredLanguage: LANG_TO_API[userSettings.learning.preferredLanguage] ?? userSettings.learning.preferredLanguage
        }));
      if (loadedSlices.account) updates.push(authApi.updateMe({ name: userSettings.account.name, avatarUrl: userSettings.account.avatarUrl }));
      await Promise.all(updates);
      await refreshSession();
      saveSuccess = true;
      setTimeout(() => (saveSuccess = false), 2500);
    } catch (cause) {
      saveError = apiErrorMessage(cause, 'Could not save your settings.');
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

<div class="settings-shell space-y-6">
  <!-- Header -->
  <div class="settings-heading">
    <div>
      <p class="wf-kicker">Personal controls</p>
      <h1 class="wf-display">Make Witforge feel <em>like yours.</em></h1>
      <p class="text-sm text-ifa-text-secondary mt-1">
        Edit your learning preferences — they inform recommendations and future learning support.
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
        disabled={saving || loading || !canSave}
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
  {#if loadError}
    <div role="alert" class="flex flex-wrap items-center justify-between gap-3 rounded-lg border border-amber-300 bg-amber-50 px-4 py-3 text-sm text-amber-900"><span>{loadError}</span><button type="button" on:click={loadSettings} disabled={loading} class="font-bold underline">{loading ? 'Retrying…' : 'Retry unavailable sections'}</button></div>
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
        <div class="border-t border-ifa-border pt-4">
          <h3 class="text-sm font-bold text-ifa-text-primary">Sign-in security</h3>
          <p class="mt-1 text-xs text-ifa-text-secondary">Manage your password, authenticator app, recovery codes, and active sessions.</p>
          <a href="/settings/security" class="mt-3 inline-flex rounded-lg border border-ifa-border px-4 py-2 text-xs font-bold text-ifa-pine">Open security settings</a>
        </div>
      </div>
    </div>
  {/if}
</div>

<style>
  .settings-shell{width:min(100%,78rem);margin:0 auto;padding:clamp(1.5rem,3vw,3rem)}.settings-heading{display:flex;align-items:end;justify-content:space-between;gap:2rem;padding:1rem 0 2.8rem;border-bottom:1px solid var(--wf-border)}.settings-heading .wf-kicker{color:var(--wf-text-muted)}.settings-heading h1{max-width:54rem;margin:1rem 0 .7rem;font-size:clamp(3.5rem,6vw,6.7rem);font-weight:430;line-height:.84;letter-spacing:-.055em}.settings-heading h1 em{color:#176b4a;font-weight:430}.settings-heading>div:first-child>p:last-child{max-width:45rem;color:var(--wf-text-muted);font-size:.72rem;line-height:1.7}.settings-shell :global(.rounded-2xl),.settings-shell :global(.rounded-xl),.settings-shell :global(.rounded-lg){border-radius:0!important}.settings-shell :global(.bg-ifa-card){box-shadow:none!important}.settings-shell :global(.bg-ifa-pine){color:#17221c!important;background:#c7f464!important}@media(max-width:760px){.settings-shell{padding:1rem}.settings-heading{display:block}.settings-heading h1{font-size:4rem}.settings-heading>div+div{margin-top:1rem}}
</style>
