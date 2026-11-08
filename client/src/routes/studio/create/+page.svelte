<script lang="ts">
  import { onMount } from 'svelte';
  import { beforeNavigate, goto } from '$app/navigation';
  import { page } from '$app/stores';
  import {
    ArrowLeft,
    Sparkles,
    BookOpen,
    Clock,
    FileText,
    Link2,
    Layers,
    Send,
    LogIn,
    Loader2,
    CheckCircle2,
    Palette,
    Terminal,
    GraduationCap,
    ClipboardCheck,
    Video,
    Users,
    Zap,
    ExternalLink
  } from 'lucide-svelte';
  import { aiApi, apiErrorMessage, educatorApi, type CourseTemplateDto, type PipelineProposalModuleDto, type CourseVisibility } from '$lib/api';
  import { initSession, session } from '$lib/stores/sessionStore';
  import { renderMarkdown } from '$lib/utils/markdown';
  import type { IntakeHistoryMessage, IntakeProfileDto } from '$lib/api/types';

  type CreationMode = 'manual' | 'templates' | 'ai_prompt' | 'materials' | 'conversation';

  let currentMode: CreationMode = 'manual';
  let sessionReady = false;
  let loading = false;
  let error = '';
  let notice = '';

  // 1. Manual & Template Form State
  let manualTitle = '';
  let manualDescription = '';
  let manualCategory = 'Software Engineering';
  let manualAudience = 'All Levels';
  let manualDuration = '6 weeks';
  let manualAccentColor = '#1B3D2F';
  let manualThemeMode = 'pine';
  let manualEnrollmentMode = 'Open';
  let selectedTemplateId = '';
  let creatingManual = false;
  let manualLevel = 'Beginner';
  let manualLanguage = 'English';
  let manualObjectives = '';
  let manualVisibility: CourseVisibility = 'Public';
  let leavingAfterSave = false;

  // Templates
  let templates: CourseTemplateDto[] = [];
  let loadingTemplates = false;
  let templatesError = '';
  let previewTemplate: CourseTemplateDto | null = null;

  // 2. AI Prompt Generation State
  let aiTopic = '';
  let aiAudience = 'Beginner to Intermediate';
  let aiDurationHours = '6';
  let aiPreferredCreator = 'freeCodeCamp';
  let aiGenerating = false;
  let aiProposalModules: PipelineProposalModuleDto[] = [];
  let aiProposalReady = false;
  let aiSaving = false;
  let aiProposalTitle = '';
  let aiProposalDescription = '';

  // 3. Materials Ingestion State
  let materialsText = '';
  let materialsGoal = '';
  let materialsProcessing = false;
  let materialsProjectId = '';
  let addedMaterialLinks: string[] = [];

  // 4. Conversational Chat State
  interface ChatMessage { role: 'user' | 'assistant'; content: string }
  let messages: ChatMessage[] = [];
  let chatInput = '';
  let chatBusy = false;
  let conversationProfile: IntakeProfileDto | null = null;

  $: hasDraft = !!manualTitle.trim() || !!aiTopic.trim() || !!materialsGoal.trim() || messages.some((message) => message.role === 'user');
  beforeNavigate((navigation) => {
    if (!leavingAfterSave && hasDraft && navigation.type !== 'leave' && !confirm('Leave course creation and discard unsaved inputs?')) navigation.cancel();
  });
  function beforeUnload(event: BeforeUnloadEvent) {
    if (!hasDraft || leavingAfterSave) return;
    event.preventDefault(); event.returnValue = '';
  }

  onMount(async () => {
    await initSession().catch(() => {});
    sessionReady = true;
    const mode = $page.url.searchParams.get('mode');
    if (mode === 'ai_prompt' || mode === 'templates' || mode === 'materials' || mode === 'conversation') {
      currentMode = mode;
      if (mode === 'conversation') startConversation();
    }

    if ($session.authenticated && $session.role !== 'Educator') {
      await goto('/home', { replaceState: true });
      return;
    }

    void loadTemplates();
  });

  async function loadTemplates() {
    loadingTemplates = true;
    templatesError = '';
    try {
      templates = await educatorApi.templates();
      if (templates.length > 0) {
        previewTemplate = templates[0];
      }
    } catch (cause) {
      templatesError = apiErrorMessage(cause, 'Could not load templates. Manual creation is still available.');
    } finally {
      loadingTemplates = false;
    }
  }

  function getTemplateIcon(icon: string) {
    switch (icon) {
      case 'Terminal': return Terminal;
      case 'GraduationCap': return GraduationCap;
      case 'ClipboardCheck': return ClipboardCheck;
      case 'Video': return Video;
      case 'Layers': return Layers;
      case 'Users': return Users;
      case 'Zap': return Zap;
      default: return BookOpen;
    }
  }

  // Handle Manual Course Creation (Directly or from Template)
  async function handleCreateManual() {
    if (!manualTitle.trim() || creatingManual) return;
    creatingManual = true;
    error = '';
    try {
      const res = await educatorApi.createManualCourse({
        title: manualTitle.trim(),
        description: manualDescription.trim(),
        category: manualCategory,
        targetAudience: manualAudience,
        estimatedDuration: manualDuration,
        templateId: selectedTemplateId || undefined,
        accentColor: manualAccentColor,
        themeMode: manualThemeMode,
        enrollmentMode: manualEnrollmentMode,
        level: manualLevel,
        language: manualLanguage,
        learningObjectives: manualObjectives.split('\n').map((item) => item.trim()).filter(Boolean),
        visibility: manualVisibility,
        requirePassingQuizzes: true
      });
      leavingAfterSave = true;
      await goto(`/studio/${res.projectId}`);
    } catch (cause) {
      error = apiErrorMessage(cause, 'Could not create course.');
    } finally {
      creatingManual = false;
    }
  }

  function applyTemplateToManual(template: CourseTemplateDto) {
    if (manualTitle && !confirm('Replace the current setup details with this template?')) return;
    selectedTemplateId = template.id;
    manualTitle = template.title;
    manualDescription = template.description;
    manualCategory = template.category;
    manualAudience = template.targetAudience;
    manualDuration = template.estimatedDuration;
    manualAccentColor = template.accentColor || '#1B3D2F';
    manualThemeMode = template.themeMode || 'pine';
    manualLevel = template.level || 'Beginner';
    manualObjectives = (template.learningObjectives || []).join('\n');
    currentMode = 'manual';
    notice = `Applied "${template.title}" template. Review details and click "Create Course".`;
  }

  // Handle AI Prompt Course Proposal
  async function handleGenerateAiProposal() {
    if (!aiTopic.trim() || aiGenerating) return;
    if (aiProposalReady && !confirm('Replace the current proposal, including your review edits?')) return;
    aiGenerating = true;
    error = '';
    aiProposalReady = false;
    try {
      const proposal = await aiApi.proposeCourse({
        goal: `${aiTopic.trim()}\nTarget audience and level: ${aiAudience}`,
        hoursPerWeek: Number(aiDurationHours) || 5,
        preferredCreator: aiPreferredCreator
      });
      aiProposalModules = proposal.modules ?? [];
      if (aiProposalModules.length === 0) throw new Error('The AI returned no modules. Retry or create a manual draft.');
      aiProposalTitle = proposal.courseTitle || aiTopic.trim();
      aiProposalDescription = proposal.targetGoal || aiTopic.trim();
      aiProposalReady = true;
    } catch (cause) {
      error = apiErrorMessage(cause, 'Could not generate a proposal. You can still create a manual or template course.');
    } finally {
      aiGenerating = false;
    }
  }

  async function handleSaveAiCourse() {
    if (!aiProposalReady || !aiProposalTitle.trim() || aiProposalModules.length === 0 || aiSaving) return;
    aiSaving = true;
    error = '';
    try {
      const saved = await educatorApi.createManualCourse({
        title: aiProposalTitle.trim(),
        description: aiProposalDescription.trim(),
        targetAudience: aiAudience,
        level: aiAudience,
        language: manualLanguage,
        learningObjectives: conversationProfile?.targetOutcome ? [conversationProfile.targetOutcome] : [],
        estimatedDuration: `${aiProposalModules.reduce((sum, module) => sum + Number(module.estimatedHours || 0), 0)} hours`,
        visibility: 'Public',
        requirePassingQuizzes: true,
        modules: aiProposalModules.map((module) => ({
          title: module.title.trim(), summary: module.summary.trim(), estimatedHours: Number(module.estimatedHours),
          lessons: (module.keyTopics || []).filter((topic) => topic.trim()).map((topic) => ({
            title: topic.trim(), summary: '', contentMarkdown: '', readingTimeMinutes: 10
          }))
        }))
      });
      leavingAfterSave = true;
      await goto(`/studio/${saved.projectId}`);
    } catch (cause) {
      error = apiErrorMessage(cause, 'Could not save the reviewed draft.');
    } finally {
      aiSaving = false;
    }
  }

  // Handle Materials Ingestion
  async function handleCreateFromMaterials() {
    if (!materialsGoal.trim() || materialsProcessing) return;
    materialsProcessing = true;
    error = '';
    try {
      const links = [...new Set(materialsText.split(/\r?\n/).map((link) => link.trim()).filter(Boolean))];
      if (links.length === 0) throw new Error('Add at least one source URL. This entry point accepts links, not file uploads.');
      if (links.some((link) => { try { return !['http:', 'https:'].includes(new URL(link).protocol); } catch { return true; } })) throw new Error('Each source must be a valid HTTP or HTTPS URL, one per line.');
      if (!materialsProjectId) {
        const project = await educatorApi.createProject({ title: materialsGoal.trim(), description: 'Course built from referenced link materials.' });
        materialsProjectId = project.id;
      }
      const failures: string[] = [];
      for (const link of links) {
        if (addedMaterialLinks.includes(link)) continue;
        try {
          await educatorApi.addSource(materialsProjectId, { url: link, title: link });
          addedMaterialLinks = [...addedMaterialLinks, link];
        } catch (cause) { failures.push(`${link}: ${apiErrorMessage(cause)}`); }
      }
      if (failures.length) throw new Error(`The project was saved, but ${failures.length} link(s) could not be added. Retry these links or open the saved project.\n${failures.join('\n')}`);
      leavingAfterSave = true;
      await goto(`/studio/${materialsProjectId}`);
    } catch (cause) {
      error = apiErrorMessage(cause, 'Could not process link materials.');
    } finally {
      materialsProcessing = false;
    }
  }

  // Handle Conversational Creation
  function startConversation() {
    if (messages.length === 0) {
      messages = [{
        role: 'assistant',
        content: 'Describe the topic, target learners, and outcomes you want to teach. We will review the brief before proposing a curriculum and saving an editable draft.'
      }];
    }
  }

  async function sendChatMessage() {
    const text = chatInput.trim();
    if (!text || chatBusy) return;
    chatInput = '';
    chatBusy = true;
    error = '';
    const history: IntakeHistoryMessage[] = messages.map((m) => ({ role: m.role, content: m.content }));
    messages = [...messages, { role: 'user', content: text }];
    try {
      const res = await aiApi.intakeMessage(text, history);
      const reply = res.reply || res.followUpQuestions?.[0] || 'Understood. Could you specify the target skill level?';
      messages = [...messages, { role: 'assistant', content: reply }];

      if (res.isProfileReady && res.profile) {
        conversationProfile = res.profile;
      }
    } catch (cause) {
      error = cause instanceof Error ? cause.message : 'Could not communicate with AI.';
    } finally {
      chatBusy = false;
    }
  }

  function reviewConversation(withAi: boolean) {
    if (!conversationProfile) return;
    const profile = conversationProfile;
    manualLanguage = profile.preferredLanguage || 'English';
    if (withAi) {
      aiTopic = `${profile.learningGoal}\nLearning outcomes: ${profile.targetOutcome}`;
      aiAudience = profile.currentLevel || 'Beginner';
      aiDurationHours = String(profile.weeklyStudyHours || 6);
      currentMode = 'ai_prompt';
      aiProposalReady = false;
      void handleGenerateAiProposal();
    } else {
      if (manualTitle && !confirm('Replace manual setup with the conversation brief?')) return;
      manualTitle = profile.learningGoal;
      manualDescription = profile.targetOutcome;
      manualAudience = profile.currentLevel;
      manualLevel = profile.currentLevel;
      manualObjectives = profile.targetOutcome;
      selectedTemplateId = '';
      currentMode = 'manual';
      notice = 'Review this conversation brief, then create a saved draft. Lesson content is authored in the shared editor.';
    }
  }
</script>

<svelte:window on:beforeunload={beforeUnload} />

<svelte:head>
  <title>Create Course — Witforge Educator Studio</title>
</svelte:head>

<div class="min-h-screen bg-ifa-bg text-ifa-text-primary antialiased flex flex-col">
  <!-- Top Navigation Header -->
  <header class="h-16 shrink-0 px-6 flex items-center justify-between border-b border-ifa-border bg-ifa-card/80 backdrop-blur sticky top-0 z-30">
    <div class="flex items-center gap-4">
      <button
        type="button"
        on:click={() => goto('/studio')}
        class="flex items-center gap-2 text-xs font-bold text-ifa-text-secondary hover:text-ifa-text-primary transition px-2.5 py-1.5 rounded-lg hover:bg-black/5"
      >
        <ArrowLeft class="w-4 h-4" />
        <span>Studio Home</span>
      </button>
      <div class="h-4 w-[1px] bg-ifa-border"></div>
      <h1 class="text-sm font-bold text-ifa-text-primary flex items-center gap-2">
        <Sparkles class="w-4 h-4 text-ifa-pine" />
        <span>New Course Creator</span>
      </h1>
    </div>

    <div class="text-xs text-ifa-text-muted hidden sm:block">
      Select your preferred creation method below
    </div>
  </header>

  {#if !sessionReady}
    <div class="flex-1 flex items-center justify-center">
      <Loader2 class="w-6 h-6 text-ifa-pine animate-spin" />
    </div>
  {:else if !$session.authenticated}
    <!-- Sign-in Prompt -->
    <div class="flex-1 flex items-center justify-center p-6">
      <div class="w-full max-w-md bg-ifa-card rounded-2xl border border-ifa-border shadow-elevated p-8 text-center space-y-4">
        <div class="w-12 h-12 mx-auto rounded-full bg-ifa-pine/10 flex items-center justify-center text-ifa-pine">
          <LogIn class="w-5 h-5" />
        </div>
        <h2 class="text-lg font-bold text-ifa-text-primary">Educator Account Required</h2>
        <p class="text-xs text-ifa-text-secondary leading-relaxed">
          Course authoring and publishing are reserved for educator accounts. Please sign in or register as an educator.
        </p>
        <div class="pt-2">
          <a
            href="/login?redirect=/studio/create"
            class="w-full py-2.5 rounded-xl bg-ifa-pine hover:bg-ifa-pine-light text-white text-xs font-bold transition flex items-center justify-center gap-2 shadow-sm"
          >
            <LogIn class="w-4 h-4" />
            <span>Sign in to Educator Studio</span>
          </a>
        </div>
      </div>
    </div>
  {:else}
    <!-- Main Studio Creation Hub -->
    <main class="flex-1 max-w-6xl w-full mx-auto p-6 md:p-8 space-y-6">

      <!-- Creation Mode Switcher (The 4 Entry Points + Templates) -->
      <div class="grid grid-cols-2 sm:grid-cols-5 gap-2 bg-ifa-card p-1.5 rounded-2xl border border-ifa-border shadow-sm">
        <button
          type="button"
          on:click={() => (currentMode = 'manual')}
          class="flex flex-col sm:flex-row items-center justify-center gap-2 py-3 px-2 rounded-xl text-xs font-bold transition {currentMode === 'manual' ? 'bg-ifa-pine text-white shadow-sm' : 'text-ifa-text-secondary hover:text-ifa-text-primary hover:bg-ifa-card-muted'}"
        >
          <BookOpen class="w-4 h-4" />
          <span>Manual Setup</span>
        </button>

        <button
          type="button"
          on:click={() => (currentMode = 'templates')}
          class="flex flex-col sm:flex-row items-center justify-center gap-2 py-3 px-2 rounded-xl text-xs font-bold transition {currentMode === 'templates' ? 'bg-ifa-pine text-white shadow-sm' : 'text-ifa-text-secondary hover:text-ifa-text-primary hover:bg-ifa-card-muted'}"
        >
          <Layers class="w-4 h-4" />
          <span>Template Library</span>
        </button>

        <button
          type="button"
          on:click={() => (currentMode = 'ai_prompt')}
          class="flex flex-col sm:flex-row items-center justify-center gap-2 py-3 px-2 rounded-xl text-xs font-bold transition {currentMode === 'ai_prompt' ? 'bg-ifa-pine text-white shadow-sm' : 'text-ifa-text-secondary hover:text-ifa-text-primary hover:bg-ifa-card-muted'}"
        >
          <Sparkles class="w-4 h-4" />
          <span>Generate with AI</span>
        </button>

        <button
          type="button"
          on:click={() => (currentMode = 'materials')}
          class="flex flex-col sm:flex-row items-center justify-center gap-2 py-3 px-2 rounded-xl text-xs font-bold transition {currentMode === 'materials' ? 'bg-ifa-pine text-white shadow-sm' : 'text-ifa-text-secondary hover:text-ifa-text-primary hover:bg-ifa-card-muted'}"
        >
          <Link2 class="w-4 h-4" />
          <span>From Materials</span>
        </button>

        <button
          type="button"
          on:click={() => { currentMode = 'conversation'; startConversation(); }}
          class="flex flex-col sm:flex-row items-center justify-center gap-2 py-3 px-2 rounded-xl text-xs font-bold transition {currentMode === 'conversation' ? 'bg-ifa-pine text-white shadow-sm' : 'text-ifa-text-secondary hover:text-ifa-text-primary hover:bg-ifa-card-muted'}"
        >
          <Send class="w-4 h-4" />
          <span>AI Conversation</span>
        </button>
      </div>

      <!-- Feedback / Alerts -->
      {#if error}
        <div role="alert" class="rounded-xl border border-red-200 bg-red-50 p-4 text-xs text-red-700 whitespace-pre-line">{error}</div>
      {/if}
      {#if notice}
        <div role="status" class="rounded-xl border border-emerald-200 bg-emerald-50 p-4 text-xs text-emerald-800 flex items-center gap-2">
          <CheckCircle2 class="w-4 h-4 shrink-0" />
          <span>{notice}</span>
        </div>
      {/if}

      <!-- MODE 1: MANUAL SETUP (WITHOUT AI) -->
      {#if currentMode === 'manual'}
        <div class="bg-ifa-card rounded-2xl border border-ifa-border shadow-card p-6 md:p-8 space-y-6">
          <div class="border-b border-ifa-border pb-4">
            <h2 class="text-base font-bold text-ifa-text-primary flex items-center gap-2">
              <BookOpen class="w-5 h-5 text-ifa-pine" />
              <span>Create Course Manually</span>
            </h2>
            <p class="text-xs text-ifa-text-secondary mt-1">
              Start with a clean blank canvas or choose a starter template. You can add, edit, and reorder all curriculum modules and lessons in the course editor.
            </p>
          </div>

          <form on:submit|preventDefault={handleCreateManual} class="space-y-5">
            <div class="grid grid-cols-1 md:grid-cols-2 gap-5">
              <div class="space-y-1.5 md:col-span-2">
                <label for="manual-title" class="block text-xs font-bold text-ifa-text-primary">Course Title *</label>
                <input
                  id="manual-title"
                  type="text"
                  bind:value={manualTitle}
                  required
                  placeholder="e.g. Production Microservices with C# & .NET 10"
                  class="w-full px-4 py-2.5 rounded-xl bg-ifa-bg border border-ifa-border text-xs text-ifa-text-primary focus:ring-2 focus:ring-ifa-pine/20 focus:border-ifa-pine transition"
                />
              </div>

              <div class="space-y-1.5 md:col-span-2">
                <label for="manual-desc" class="block text-xs font-bold text-ifa-text-primary">Course Description</label>
                <textarea
                  id="manual-desc"
                  bind:value={manualDescription}
                  rows="3"
                  placeholder="What will learners achieve upon completing this course?"
                  class="w-full px-4 py-2.5 rounded-xl bg-ifa-bg border border-ifa-border text-xs text-ifa-text-primary focus:ring-2 focus:ring-ifa-pine/20 focus:border-ifa-pine transition resize-y"
                ></textarea>
              </div>

              <div class="space-y-1.5">
                <label for="manual-category" class="block text-xs font-bold text-ifa-text-primary">Category</label>
                <select
                  id="manual-category"
                  bind:value={manualCategory}
                  class="w-full px-4 py-2.5 rounded-xl bg-ifa-bg border border-ifa-border text-xs text-ifa-text-primary"
                >
                  <option value="Software Engineering">Software Engineering</option>
                  <option value="Computer Science">Computer Science</option>
                  <option value="Data Science & AI">Data Science & AI</option>
                  <option value="Cloud & DevOps">Cloud & DevOps</option>
                  <option value="Cybersecurity">Cybersecurity</option>
                  <option value="Exam Preparation">Exam Preparation</option>
                  <option value="Business & Design">Business & Design</option>
                </select>
              </div>

              <div class="space-y-1.5">
                <label for="manual-duration" class="block text-xs font-bold text-ifa-text-primary">Estimated Duration</label>
                <input
                  id="manual-duration"
                  type="text"
                  bind:value={manualDuration}
                  placeholder="e.g. 6 weeks or 20 hours"
                  class="w-full px-4 py-2.5 rounded-xl bg-ifa-bg border border-ifa-border text-xs text-ifa-text-primary"
                />
              </div>

              <div class="space-y-1.5">
                <label for="manual-audience" class="block text-xs font-bold text-ifa-text-primary">Target Audience / Level</label>
                <input
                  id="manual-audience"
                  type="text"
                  bind:value={manualAudience}
                  placeholder="e.g. Intermediate Developers"
                  class="w-full px-4 py-2.5 rounded-xl bg-ifa-bg border border-ifa-border text-xs text-ifa-text-primary"
                />
              </div>

              <div class="space-y-1.5">
                <label for="manual-enroll" class="block text-xs font-bold text-ifa-text-primary">Enrollment Access</label>
                <select
                  id="manual-enroll"
                  bind:value={manualEnrollmentMode}
                  class="w-full px-4 py-2.5 rounded-xl bg-ifa-bg border border-ifa-border text-xs text-ifa-text-primary"
                >
                  <option value="Open">Open Enrollment (Immediate Access)</option>
                  <option value="ApprovalRequired">Educator Approval Required</option>
                  <option value="InviteOnly">Invite-Only (Share Code)</option>
                </select>
              </div>
              <label class="text-xs font-bold text-ifa-text-primary">Level
                <input type="text" bind:value={manualLevel} class="mt-1.5 w-full px-4 py-2.5 rounded-xl bg-ifa-bg border border-ifa-border text-xs" />
              </label>
              <label class="text-xs font-bold text-ifa-text-primary">Language
                <input type="text" bind:value={manualLanguage} class="mt-1.5 w-full px-4 py-2.5 rounded-xl bg-ifa-bg border border-ifa-border text-xs" />
              </label>
              <label class="md:col-span-2 text-xs font-bold text-ifa-text-primary">Learning Objectives (One Per Line)
                <textarea bind:value={manualObjectives} rows="3" class="mt-1.5 w-full px-4 py-2.5 rounded-xl bg-ifa-bg border border-ifa-border text-xs"></textarea>
              </label>
              <label class="md:col-span-2 text-xs font-bold text-ifa-text-primary">Visibility When Published
                <select bind:value={manualVisibility} class="mt-1.5 w-full px-4 py-2.5 rounded-xl bg-ifa-bg border border-ifa-border text-xs"><option value="Public">Public (Marketplace)</option><option value="Unlisted">Unlisted (Direct Link)</option><option value="Private">Private (Invite Only)</option></select>
              </label>
            </div>
            <p class="text-xs text-ifa-text-secondary">Creation saves an unpublished draft. Review and publish it separately from the editor.</p>

            <!-- Optional Starter Template Badge -->
            {#if selectedTemplateId}
              <div class="p-3 bg-ifa-pine/5 border border-ifa-pine/20 rounded-xl flex items-center justify-between text-xs">
                <span class="text-ifa-text-secondary">
                  Initialized with starter template: <strong class="text-ifa-text-primary font-bold">{selectedTemplateId}</strong>
                </span>
                <button
                  type="button"
                  on:click={() => (selectedTemplateId = '')}
                  class="text-xs text-red-600 font-semibold hover:underline"
                >
                  Clear Template
                </button>
              </div>
            {/if}

            <div class="flex items-center justify-end gap-3 pt-4 border-t border-ifa-border">
              <button
                type="button"
                on:click={() => (currentMode = 'templates')}
                class="px-4 py-2.5 rounded-xl border border-ifa-border bg-ifa-card hover:bg-ifa-card-muted text-xs font-semibold text-ifa-text-primary transition"
              >
                Browse Templates
              </button>
              <button
                type="submit"
                disabled={creatingManual || !manualTitle.trim()}
                class="px-6 py-2.5 rounded-xl bg-ifa-pine text-white text-xs font-bold hover:bg-ifa-pine-light transition flex items-center gap-2 disabled:opacity-50 shadow-sm"
              >
                {#if creatingManual}<Loader2 class="w-4 h-4 animate-spin" />{/if}
                <span>Create & Open Course Builder</span>
              </button>
            </div>
          </form>
        </div>

      <!-- MODE 2: TEMPLATE LIBRARY -->
      {:else if currentMode === 'templates'}
        <div class="space-y-6">
          <div class="bg-ifa-card rounded-2xl border border-ifa-border p-6 shadow-sm">
            <h2 class="text-base font-bold text-ifa-text-primary flex items-center gap-2">
              <Layers class="w-5 h-5 text-ifa-pine" />
              <span>Course Template Library</span>
            </h2>
            <p class="text-xs text-ifa-text-secondary mt-1">
              Start with editable example curricula, lessons, and assessment scaffolds. Review and adapt all template content before publishing.
            </p>
          </div>

          {#if loadingTemplates}
            <div class="flex justify-center py-16"><Loader2 class="w-6 h-6 animate-spin text-ifa-pine" /></div>
          {:else if templatesError}
            <p role="alert" class="text-sm text-red-700">{templatesError} <button type="button" on:click={loadTemplates} class="underline">Retry</button></p>
          {:else if templates.length === 0}
            <p class="text-sm text-ifa-text-secondary">No templates are available. Manual setup does not require a template.</p>
          {:else}
            <div class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-5">
              {#each templates as t}
                <div class="bg-ifa-card rounded-2xl border border-ifa-border p-5 shadow-card hover:border-ifa-pine/40 transition flex flex-col justify-between space-y-4">
                  <div class="space-y-3">
                    <div class="flex items-start justify-between gap-2">
                      <div class="w-10 h-10 rounded-xl bg-ifa-pine/10 text-ifa-pine flex items-center justify-center shrink-0">
                        <svelte:component this={getTemplateIcon(t.icon)} class="w-5 h-5" />
                      </div>
                      <span class="text-[10px] font-bold px-2 py-0.5 rounded-full bg-ifa-card-muted text-ifa-text-secondary border border-ifa-border">
                        {t.level}
                      </span>
                    </div>

                    <div>
                      <h3 class="text-sm font-bold text-ifa-text-primary">{t.title}</h3>
                      <p class="text-xs text-ifa-text-secondary mt-1 line-clamp-2 leading-relaxed">
                        {t.description}
                      </p>
                    </div>

                    <div class="flex items-center gap-3 text-[11px] text-ifa-text-muted">
                      <span>{(t.modules || []).length} modules</span>
                      <span>·</span>
                      <span>{t.estimatedDuration}</span>
                    </div>

                    <ul class="text-[11px] text-ifa-text-secondary space-y-1">
                      {#each (t.keyFeatures || []).slice(0, 3) as f}
                        <li class="flex items-center gap-1.5">
                          <CheckCircle2 class="w-3 h-3 text-ifa-pine shrink-0" />
                          <span class="truncate">{f}</span>
                        </li>
                      {/each}
                    </ul>
                    {#if t.assessmentSuggestions?.[0]}
                      <p class="rounded-lg bg-ifa-card-muted px-2.5 py-2 text-[11px] text-ifa-text-secondary">
                        Assessment: {t.assessmentSuggestions[0]}
                      </p>
                    {/if}
                  </div>

                  <div class="pt-3 border-t border-ifa-border flex items-center justify-between gap-2">
                    <button
                      type="button"
                      on:click={() => applyTemplateToManual(t)}
                      class="w-full py-2 rounded-xl bg-ifa-pine text-white text-xs font-bold hover:bg-ifa-pine-light transition flex items-center justify-center gap-1.5 shadow-sm"
                    >
                      <span>Use Template</span>
                    </button>
                  </div>
                </div>
              {/each}
            </div>
          {/if}
        </div>

      <!-- MODE 3: GENERATE USING AI -->
      {:else if currentMode === 'ai_prompt'}
        <div class="bg-ifa-card rounded-2xl border border-ifa-border shadow-card p-6 md:p-8 space-y-6">
          <div class="border-b border-ifa-border pb-4">
            <h2 class="text-base font-bold text-ifa-text-primary flex items-center gap-2">
              <Sparkles class="w-5 h-5 text-ifa-pine" />
              <span>AI Course Architect</span>
            </h2>
            <p class="text-xs text-ifa-text-secondary mt-1">
              Review and edit the proposed curriculum before saving. The proposal contains module summaries and lesson headings, not finished lesson content.
            </p>
          </div>

          <div class="space-y-4">
            <div>
              <label for="ai-topic" class="block text-xs font-bold text-ifa-text-primary mb-1">What do you want to teach? *</label>
              <input
                id="ai-topic"
                type="text"
                bind:value={aiTopic}
                placeholder="e.g. Distributed Caching and Redis in High-Load Systems"
                class="w-full px-4 py-2.5 rounded-xl bg-ifa-bg border border-ifa-border text-xs text-ifa-text-primary focus:ring-2 focus:ring-ifa-pine/20 focus:border-ifa-pine"
              />
            </div>

            <div class="grid grid-cols-1 sm:grid-cols-3 gap-4">
              <div>
                <label for="ai-level" class="block text-xs font-bold text-ifa-text-primary mb-1">Skill Level</label>
                <select
                  id="ai-level"
                  bind:value={aiAudience}
                  class="w-full px-3 py-2 rounded-xl bg-ifa-bg border border-ifa-border text-xs text-ifa-text-primary"
                >
                  <option value="Beginner">Beginner</option>
                  <option value="Beginner to Intermediate">Beginner to Intermediate</option>
                  <option value="Intermediate to Advanced">Intermediate to Advanced</option>
                  <option value="Exit Exam / Certification">Exit Exam / Certification</option>
                </select>
              </div>

              <div>
                <label for="ai-hours" class="block text-xs font-bold text-ifa-text-primary mb-1">Weekly Hours</label>
                <input
                  id="ai-hours"
                  type="number"
                  min="2"
                  max="40"
                  bind:value={aiDurationHours}
                  class="w-full px-3 py-2 rounded-xl bg-ifa-bg border border-ifa-border text-xs text-ifa-text-primary"
                />
              </div>

              <div>
                <label for="ai-creator" class="block text-xs font-bold text-ifa-text-primary mb-1">Preferred Creator / Channel</label>
                <input
                  id="ai-creator"
                  type="text"
                  bind:value={aiPreferredCreator}
                  placeholder="e.g. freeCodeCamp"
                  class="w-full px-3 py-2 rounded-xl bg-ifa-bg border border-ifa-border text-xs text-ifa-text-primary"
                />
              </div>
            </div>

            <div class="pt-2">
              <button
                type="button"
                on:click={handleGenerateAiProposal}
                disabled={aiGenerating || !aiTopic.trim()}
                class="px-5 py-2.5 rounded-xl bg-ifa-pine text-white text-xs font-bold hover:bg-ifa-pine-light transition flex items-center gap-2 disabled:opacity-50 shadow-sm"
              >
                {#if aiGenerating}<Loader2 class="w-4 h-4 animate-spin" />{/if}
                <span>Generate Blueprint Proposal</span>
              </button>
            </div>
          </div>

          <!-- Generated Proposal Preview & Approval -->
          {#if aiProposalReady}
            <div class="p-5 bg-ifa-bg rounded-xl border border-ifa-border space-y-4 pt-4 mt-4">
              <div class="flex items-center justify-between">
                <h3 class="text-xs font-bold uppercase tracking-wider text-ifa-text-muted">Proposed Blueprint</h3>
                <span class="text-xs text-ifa-pine font-semibold">{aiProposalModules.length} Modules Designed</span>
              </div>

              <div class="space-y-3">
                <label class="block text-xs font-semibold text-ifa-text-primary">Draft Course Title<input type="text" bind:value={aiProposalTitle} class="mt-1 w-full px-3 py-2 rounded-xl bg-ifa-card border border-ifa-border" /></label>
                <label class="block text-xs font-semibold text-ifa-text-primary">Draft Description<textarea bind:value={aiProposalDescription} rows="3" class="mt-1 w-full px-3 py-2 rounded-xl bg-ifa-card border border-ifa-border"></textarea></label>
                {#each aiProposalModules as mod, mi}
                  <div class="p-3 bg-ifa-card rounded-xl border border-ifa-border space-y-1.5">
                    <div class="flex items-center justify-between">
                      <h4 class="text-xs font-bold text-ifa-text-primary">Module {mi + 1}</h4>
                      <button type="button" on:click={() => aiProposalModules = aiProposalModules.filter((_, index) => index !== mi)} class="text-xs text-red-700">Remove Module</button>
                    </div>
                    <label class="block text-xs text-ifa-text-secondary">Title<input type="text" bind:value={mod.title} class="mt-1 w-full px-3 py-2 rounded-lg bg-ifa-card border border-ifa-border text-ifa-text-primary" /></label>
                    <label class="block text-xs text-ifa-text-secondary">Summary<textarea bind:value={mod.summary} rows="2" class="mt-1 w-full px-3 py-2 rounded-lg bg-ifa-card border border-ifa-border text-ifa-text-primary"></textarea></label>
                    <label class="block text-xs text-ifa-text-secondary">Estimated Hours<input type="number" min="1" step="1" max="500" bind:value={mod.estimatedHours} class="mt-1 w-32 px-3 py-2 rounded-lg bg-ifa-card border border-ifa-border text-ifa-text-primary" /></label>
                    {#if mod.keyTopics && mod.keyTopics.length > 0}
                      <div class="space-y-2 pt-2">
                        <p class="text-xs text-ifa-text-secondary">Lesson Headings (Content Will Be Empty)</p>
                        {#each mod.keyTopics as t, ti}
                          <input type="text" bind:value={mod.keyTopics[ti]} aria-label={`Module ${mi + 1}, lesson heading ${ti + 1}`} class="w-full px-3 py-2 rounded-lg bg-ifa-card border border-ifa-border text-xs text-ifa-text-primary" />
                        {/each}
                      </div>
                    {/if}
                  </div>
                {/each}
              </div>
              <p class="text-xs text-ifa-text-secondary">Approval saves exactly these reviewed modules in the shared editor. Add real lesson content and assessments there before publishing.</p>

              <div class="flex justify-end pt-2">
                <button
                  type="button"
                  on:click={handleSaveAiCourse}
                  disabled={aiSaving || !aiProposalTitle.trim() || aiProposalModules.length === 0 || aiProposalModules.some((module) => !module.title.trim())}
                  class="px-6 py-2.5 rounded-xl bg-ifa-pine text-white text-xs font-bold hover:bg-ifa-pine-light transition flex items-center gap-2 shadow-sm"
                >
                  {#if aiSaving}<Loader2 class="w-4 h-4 animate-spin" />{/if}
                  <span>Approve & Save Editable Draft</span>
                </button>
              </div>
            </div>
          {/if}
        </div>

      <!-- MODE 4: FROM LEARNING MATERIALS -->
      {:else if currentMode === 'materials'}
        <div class="bg-ifa-card rounded-2xl border border-ifa-border shadow-card p-6 md:p-8 space-y-6">
          <div class="border-b border-ifa-border pb-4">
            <h2 class="text-base font-bold text-ifa-text-primary flex items-center gap-2">
              <Link2 class="w-5 h-5 text-ifa-pine" />
              <span>Create from Link Materials</span>
            </h2>
            <p class="text-xs text-ifa-text-secondary mt-1">
              Link public documentation, articles, or supported documents. This is a links-only workflow, not a binary file uploader. Review extraction results in Studio before generating a course.
            </p>
          </div>

          <div class="space-y-4">
            <div>
              <label for="materials-goal" class="block text-xs font-bold text-ifa-text-primary mb-1">Course Title / Topic *</label>
              <input
                id="materials-goal"
                type="text"
                bind:value={materialsGoal}
                placeholder="e.g. Modern Web APIs and System Integration"
                class="w-full px-4 py-2.5 rounded-xl bg-ifa-bg border border-ifa-border text-xs text-ifa-text-primary focus:ring-2 focus:ring-ifa-pine/20 focus:border-ifa-pine"
              />
            </div>

            <div>
              <label for="materials-links" class="block text-xs font-bold text-ifa-text-primary mb-1">Source URLs (one per line)</label>
              <textarea
                id="materials-links"
                bind:value={materialsText}
                rows="5"
                placeholder="https://docs.microsoft.com/en-us/aspnet/core/&#10;https://github.com/example/api-reference"
                class="w-full px-4 py-2.5 rounded-xl bg-ifa-bg border border-ifa-border text-xs font-mono text-ifa-text-primary focus:ring-2 focus:ring-ifa-pine/20 focus:border-ifa-pine resize-y"
              ></textarea>
            </div>

            <div class="flex justify-end pt-2">
              <button
                type="button"
                on:click={handleCreateFromMaterials}
                disabled={materialsProcessing || !materialsGoal.trim() || !materialsText.trim()}
                class="px-6 py-2.5 rounded-xl bg-ifa-pine text-white text-xs font-bold hover:bg-ifa-pine-light transition flex items-center gap-2 disabled:opacity-50 shadow-sm"
              >
                {#if materialsProcessing}<Loader2 class="w-4 h-4 animate-spin" />{/if}
                <span>Process Materials in Studio</span>
              </button>
            </div>
            {#if materialsProjectId}
              <button type="button" on:click={() => { leavingAfterSave = true; void goto(`/studio/${materialsProjectId}`); }} class="text-xs font-semibold text-ifa-pine underline">Open Saved Source Project</button>
            {/if}
          </div>
        </div>

      <!-- MODE 5: AI RESEARCH CONVERSATION -->
      {:else if currentMode === 'conversation'}
        <div class="bg-ifa-card rounded-2xl border border-ifa-border shadow-card overflow-hidden flex flex-col h-[600px]">
          <div class="p-4 border-b border-ifa-border bg-ifa-card/80 flex items-center justify-between">
            <h2 class="text-xs font-bold text-ifa-text-primary flex items-center gap-2">
              <Send class="w-4 h-4 text-ifa-pine" />
              <span>Socratic Course Conversation</span>
            </h2>
            <span class="text-[11px] text-ifa-text-muted">Chat to define outcomes & curriculum</span>
          </div>

          <!-- Chat Messages List -->
          <div class="flex-1 p-4 overflow-y-auto space-y-3 bg-ifa-bg/50">
            {#each messages as msg}
              <div class="flex {msg.role === 'user' ? 'justify-end' : 'justify-start'}">
                <div class="max-w-lg rounded-2xl px-4 py-2.5 text-xs {msg.role === 'user' ? 'bg-ifa-pine text-white rounded-br-none' : 'bg-ifa-card border border-ifa-border text-ifa-text-primary rounded-bl-none shadow-sm'}">
                  {@html renderMarkdown(msg.content)}
                </div>
              </div>
            {/each}
            {#if chatBusy}
              <div class="flex justify-start">
                <div class="bg-ifa-card border border-ifa-border rounded-2xl px-4 py-2 text-xs text-ifa-text-muted flex items-center gap-2">
                  <Loader2 class="w-3.5 h-3.5 animate-spin text-ifa-pine" />
                  <span>Witforge is thinking...</span>
                </div>
              </div>
            {/if}
          </div>

          <!-- Input Composer -->
          {#if conversationProfile}
            <div class="p-4 border-t border-ifa-border space-y-3">
              <p class="text-xs text-ifa-text-primary"><strong>Ready for review:</strong> {conversationProfile.learningGoal}</p>
              <p class="text-xs text-ifa-text-secondary">{conversationProfile.targetOutcome}</p>
              <div class="flex flex-wrap gap-2">
                <button type="button" on:click={() => reviewConversation(true)} disabled={chatBusy} class="px-4 py-2 rounded-xl bg-ifa-pine text-white text-xs font-bold disabled:opacity-50">Propose Curriculum for Review</button>
                <button type="button" on:click={() => reviewConversation(false)} class="px-4 py-2 rounded-xl border border-ifa-border text-ifa-text-primary text-xs font-semibold">Review Brief & Create Manually</button>
              </div>
            </div>
          {/if}
          <div class="p-3 border-t border-ifa-border bg-ifa-card flex gap-2">
            <input
              type="text"
              aria-label="Course conversation message"
              bind:value={chatInput}
              on:keydown={(e) => e.key === 'Enter' && void sendChatMessage()}
              placeholder="Tell Witforge what you'd like to teach..."
              class="flex-1 px-4 py-2 rounded-xl bg-ifa-bg border border-ifa-border text-xs text-ifa-text-primary focus:ring-1 focus:ring-ifa-pine"
            />
            <button
              type="button"
              on:click={() => void sendChatMessage()}
              disabled={chatBusy || !chatInput.trim()}
              class="px-4 py-2 rounded-xl bg-ifa-pine text-white text-xs font-bold hover:bg-ifa-pine-light transition disabled:opacity-50"
            >
              Send
            </button>
          </div>
        </div>
      {/if}

    </main>
  {/if}
</div>
