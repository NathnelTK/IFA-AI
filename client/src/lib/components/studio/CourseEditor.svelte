<script lang="ts">
  import { createEventDispatcher, onMount } from 'svelte';
  import { beforeNavigate } from '$app/navigation';
  import {
    Loader2,
    Save,
    Sparkles,
    ChevronDown,
    ChevronRight,
    BookOpen,
    Plus,
    Trash2,
    Eye,
    Palette,
    Video,
    FileCheck,
    Check,
    ExternalLink,
    ArrowUp,
    ArrowDown,
    Copy,
    Globe
  } from 'lucide-svelte';
  import { apiErrorMessage, coursesApi, educatorApi, type CourseDetailDto, type LiveSessionDto, type AssignmentDto, type QuizAuthoringDto, type CourseVisibility } from '$lib/api';
  import QuizEditor from './QuizEditor.svelte';

  export let courseId: string;
  export let refreshKey = '';
  export let generationActive = false;
  export let dirty = false;
  const dispatch = createEventDispatcher<{ saved: CourseDetailDto }>();

  let course: CourseDetailDto | null = null;
  let loading = true;
  let error = '';
  let notice = '';
  let activeTab: 'curriculum' | 'branding' | 'sessions' | 'assignments' = 'curriculum';
  let mounted = false;
  let loadedKey = '';
  let loadSequence = 0;
  let mutating = false;
  let publishPrice: number | undefined;

  // Curriculum state
  let savingModuleId: string | null = null;
  let aiBusyModuleId: string | null = null;
  let expandedModules: Record<string, boolean> = {};
  type ModuleDraft = {
    title: string;
    description: string;
    lessons: Array<{ id: string; title: string; content: string; order: number; readingTimeMinutes: number; youTubeVideoId: string }>;
  };
  let moduleDrafts: Record<string, ModuleDraft> = {};
  let quizDrafts: Record<string, QuizAuthoringDto[]> = {};
  let savedModules: Record<string, string> = {};
  let savedQuizzes: Record<string, string> = {};
  let savedSettings = '';
  let aiInstructions: Record<string, string> = {};

  // New module / lesson form state
  let showAddModule = false;
  let newModuleTitle = '';
  let newModuleHours = 3;
  let addingModule = false;

  let addingLessonForModuleId: string | null = null;
  let newLessonTitle = '';
  let addingLesson = false;

  // Branding state
  let accentColor = '#1B3D2F';
  let themeMode = 'pine';
  let enrollmentMode = 'Open';
  let passingScorePercentage = 70;
  let prerequisites = '';
  let instructorBio = '';
  let savingBranding = false;
  let title = '';
  let description = '';
  let overview = '';
  let category = '';
  let targetAudience = '';
  let estimatedDuration = '';
  let level = 'Beginner';
  let language = 'English';
  let objectivesText = '';
  let requirePassingQuizzes = true;
  let maxQuizAttempts = 3;
  let visibility: CourseVisibility = 'Public';
  let enrollmentLimit: number | undefined;
  let enrollmentOpensAt = '';
  let enrollmentClosesAt = '';
  let instructorTitle = '';
  let courseAssistantEnabled = true;
  let awardsCertificate = true;

  // Live Sessions state
  let liveSessions: LiveSessionDto[] = [];
  let loadingSessions = false;
  let sessionsLoaded = false;
  let sessionsError = '';
  let showAddSession = false;
  let sessionTitle = '';
  let sessionTime = '';
  let sessionDuration = 60;
  let sessionMeetingUrl = '';
  let editingSessionId: string | null = null;
  let addingSession = false;

  // Assignments state
  let assignments: AssignmentDto[] = [];
  let loadingAssignments = false;
  let assignmentsLoaded = false;
  let assignmentsError = '';
  let showAddAssignment = false;
  let assignmentTitle = '';
  let assignmentInstructions = '';
  let assignmentDueDays = 7;
  let assignmentMaxScore = 100;
  let addingAssignment = false;

  $: settingsDraft = { title, description, overview, category, targetAudience, estimatedDuration, level, language, objectivesText, requirePassingQuizzes, maxQuizAttempts, visibility, enrollmentLimit, enrollmentOpensAt, enrollmentClosesAt, instructorTitle, accentColor, themeMode, enrollmentMode, passingScorePercentage, prerequisites, instructorBio, courseAssistantEnabled, awardsCertificate };
  $: dirtyModules = Object.keys(moduleDrafts).filter((id) => JSON.stringify(moduleDrafts[id]) !== savedModules[id]);
  $: dirtyQuizzes = Object.keys(quizDrafts).filter((id) => JSON.stringify(quizDrafts[id]) !== savedQuizzes[id]);
  $: settingsDirty = !!course && JSON.stringify(settingsDraft) !== savedSettings;
  $: contentDirty = dirtyModules.length > 0 || dirtyQuizzes.length > 0 || settingsDirty;
  $: dirty = contentDirty || !!newModuleTitle || !!newLessonTitle || !!sessionTitle || !!sessionMeetingUrl || !!assignmentTitle || !!assignmentInstructions || (showAddSession && !!sessionTime);
  $: readOnly = generationActive || course?.isPublished === true;
  $: writing = mutating || addingModule || addingLesson || savingModuleId !== null || aiBusyModuleId !== null || savingBranding || addingSession || addingAssignment;

  onMount(() => { mounted = true; });
  $: if (mounted && courseId && loadedKey !== `${courseId}:${refreshKey}`) {
    loadedKey = `${courseId}:${refreshKey}`;
    if (dirty) notice = 'New saved content is available. Save your changes or use Reload to discard them.';
    else void loadCourse();
  }

  beforeNavigate((navigation) => {
    if (writing) { navigation.cancel(); return; }
    if (dirty && navigation.type !== 'leave' && !confirm('Leave this editor and discard unsaved changes?')) navigation.cancel();
  });

  function beforeUnload(event: BeforeUnloadEvent) {
    if (!dirty && !writing) return;
    event.preventDefault();
    event.returnValue = '';
  }

  function localDate(value?: string | null) {
    if (!value) return '';
    const date = new Date(value);
    return new Date(date.getTime() - date.getTimezoneOffset() * 60000).toISOString().slice(0, 16);
  }

  async function loadCourse(savedSection = '') {
    const requestedId = courseId;
    const sequence = ++loadSequence;
    loading = true;
    error = '';
    try {
      const detail = await coursesApi.getById(requestedId);
      if (courseId !== requestedId || sequence !== loadSequence) return;
      if (!detail.isOwner) throw new Error('Only the course owner can edit this course.');
      for (const mod of detail.modules) {
        if (!dirtyModules.includes(mod.id) || savedSection === `module:${mod.id}` || savedSection === 'discard') {
          moduleDrafts[mod.id] = {
            title: mod.title,
            description: mod.summary,
            lessons: [...mod.lessons].sort((a, b) => a.lessonNumber - b.lessonNumber).map((lesson, order) => ({
              id: lesson.id, title: lesson.title, content: lesson.contentMarkdown || '', order: order + 1,
              readingTimeMinutes: lesson.readingTimeMinutes, youTubeVideoId: lesson.youTubeVideoId || ''
            }))
          };
          savedModules[mod.id] = JSON.stringify(moduleDrafts[mod.id]);
        }
        if (!dirtyQuizzes.includes(mod.id) || savedSection === `quiz:${mod.id}` || savedSection === 'discard') {
          quizDrafts[mod.id] = mod.quizzes.map((quiz) => ({
            id: quiz.id, title: quiz.title, passingScorePercentage: quiz.passingScorePercentage, kind: quiz.kind === 0 ? 0 : 1,
            questions: quiz.questions.map((question) => ({
              id: question.id, prompt: question.prompt, options: [...question.options],
              correctOptionIndex: question.correctOptionIndex ?? -1, explanation: question.explanation || '', targetSkillName: question.targetSkillName || ''
            }))
          }));
          savedQuizzes[mod.id] = JSON.stringify(quizDrafts[mod.id]);
        }
      }
      const ids = new Set(detail.modules.map((mod) => mod.id));
      moduleDrafts = Object.fromEntries(Object.entries(moduleDrafts).filter(([id]) => ids.has(id)));
      quizDrafts = Object.fromEntries(Object.entries(quizDrafts).filter(([id]) => ids.has(id)));
      if (!settingsDirty || savedSection === 'settings' || savedSection === 'discard') {
        title = detail.title; description = detail.description; overview = detail.overview || ''; category = detail.category;
        targetAudience = detail.targetAudience; estimatedDuration = detail.estimatedDuration;
        level = detail.level || 'Beginner'; language = detail.language || 'English';
        objectivesText = (detail.learningObjectives || []).join('\n');
        requirePassingQuizzes = detail.requirePassingQuizzes ?? true;
        maxQuizAttempts = detail.maxQuizAttempts ?? 3;
        visibility = detail.visibility || 'Public'; enrollmentLimit = detail.enrollmentLimit ?? undefined;
        enrollmentOpensAt = localDate(detail.enrollmentOpensAt); enrollmentClosesAt = localDate(detail.enrollmentClosesAt);
        instructorTitle = detail.instructorTitle || '';
        accentColor = detail.accentColor || '#1B3D2F'; themeMode = detail.themeMode || 'pine';
        enrollmentMode = detail.enrollmentMode || 'Open'; passingScorePercentage = detail.passingScorePercentage ?? 70;
        prerequisites = detail.prerequisites || ''; instructorBio = detail.instructorBio || '';
        courseAssistantEnabled = detail.courseAssistantEnabled !== false; awardsCertificate = detail.awardsCertificate !== false;
        savedSettings = JSON.stringify({ title, description, overview, category, targetAudience, estimatedDuration, level, language, objectivesText, requirePassingQuizzes, maxQuizAttempts, visibility, enrollmentLimit, enrollmentOpensAt, enrollmentClosesAt, instructorTitle, accentColor, themeMode, enrollmentMode, passingScorePercentage, prerequisites, instructorBio, courseAssistantEnabled, awardsCertificate });
      }
      course = detail;
      dispatch('saved', detail);
    } catch (cause) {
      if (courseId === requestedId && sequence === loadSequence) error = apiErrorMessage(cause, 'Could not load the course.');
    } finally {
      if (sequence === loadSequence) loading = false;
    }
  }

  async function reload() {
    if (writing || (dirty && !confirm('Discard all unsaved changes and reload saved content?'))) return;
    newModuleTitle = ''; newLessonTitle = ''; sessionTitle = ''; sessionMeetingUrl = ''; editingSessionId = null; showAddSession = false; assignmentTitle = ''; assignmentInstructions = '';
    await loadCourse('discard');
  }

  function canChangeStructure() {
    if (readOnly || writing) return false;
    if (contentDirty) {
      error = 'Save your module, assessment, and settings changes before changing the saved structure or asking AI to replace content.';
      return false;
    }
    return true;
  }

  async function mutate(action: () => Promise<unknown>, message: string, savedSection = '') {
    if (mutating) return;
    mutating = true; error = ''; notice = '';
    try {
      await action();
      await loadCourse(savedSection);
      notice = message;
    } catch (cause) {
      error = apiErrorMessage(cause);
    } finally {
      mutating = false;
    }
  }

  function toggleModule(moduleId: string) {
    expandedModules = { ...expandedModules, [moduleId]: !expandedModules[moduleId] };
  }

  async function handleAddModule() {
    if (!newModuleTitle.trim() || addingModule || !canChangeStructure()) return;
    addingModule = true;
    error = '';
    try {
      await educatorApi.addModule(courseId, {
        title: newModuleTitle.trim(),
        estimatedHours: newModuleHours
      });
      newModuleTitle = '';
      showAddModule = false;
      notice = 'New module added successfully.';
      await loadCourse();
    } catch (cause) {
      error = apiErrorMessage(cause, 'Could not add module.');
    } finally {
      addingModule = false;
    }
  }

  async function handleDeleteModule(moduleId: string, title: string) {
    if (!canChangeStructure()) return;
    if (!confirm(`Are you sure you want to delete "${title}" and all its lessons?`)) return;
    await mutate(() => educatorApi.deleteModule(courseId, moduleId), `Module "${title}" deleted.`);
  }

  async function handleAddLesson(moduleId: string) {
    if (!newLessonTitle.trim() || addingLesson || !canChangeStructure()) return;
    addingLesson = true;
    error = '';
    try {
      await educatorApi.addLesson(courseId, moduleId, {
        title: newLessonTitle.trim(),
        contentMarkdown: '',
        readingTimeMinutes: 10
      });
      newLessonTitle = '';
      addingLessonForModuleId = null;
      notice = 'Lesson added.';
      await loadCourse();
    } catch (cause) {
      error = cause instanceof Error ? cause.message : 'Could not add lesson.';
    } finally {
      addingLesson = false;
    }
  }

  async function handleDeleteLesson(moduleId: string, lessonId: string, title: string) {
    if (!canChangeStructure()) return;
    if (!confirm(`Delete lesson "${title}"?`)) return;
    await mutate(() => educatorApi.deleteLesson(courseId, moduleId, lessonId), `Lesson "${title}" deleted.`);
  }

  async function saveModule(moduleId: string) {
    if (!course || savingModuleId || readOnly || mutating) return;
    savingModuleId = moduleId;
    await mutate(() => educatorApi.updateModule(courseId, moduleId, moduleDrafts[moduleId]), 'Module changes saved.', `module:${moduleId}`);
    savingModuleId = null;
  }

  async function saveQuizzes(moduleId: string) {
    if (readOnly || mutating) return;
    await mutate(() => educatorApi.updateQuizzes(courseId, moduleId, quizDrafts[moduleId]), 'Assessments saved.', `quiz:${moduleId}`);
  }

  function moveLesson(moduleId: string, index: number, direction: number) {
    const lessons = [...moduleDrafts[moduleId].lessons];
    const target = index + direction;
    if (target < 0 || target >= lessons.length || readOnly) return;
    [lessons[index], lessons[target]] = [lessons[target], lessons[index]];
    moduleDrafts[moduleId].lessons = lessons.map((lesson, order) => ({ ...lesson, order: order + 1 }));
    moduleDrafts = { ...moduleDrafts };
  }

  async function moveModule(index: number, direction: number) {
    if (!course || !canChangeStructure()) return;
    const ids = course.modules.map((module) => module.id);
    const target = index + direction;
    if (target < 0 || target >= ids.length) return;
    [ids[index], ids[target]] = [ids[target], ids[index]];
    await mutate(() => educatorApi.reorderModules(courseId, ids), 'Module order saved.');
  }

  async function duplicate(moduleId: string, lessonId?: string) {
    if (!canChangeStructure()) return;
    await mutate(() => lessonId ? educatorApi.duplicateLesson(courseId, moduleId, lessonId) : educatorApi.duplicateModule(courseId, moduleId), 'Saved content duplicated.');
  }

  async function aiUpdateModule(moduleId: string) {
    const instruction = (aiInstructions[moduleId] ?? '').trim();
    if (!course || !instruction || aiBusyModuleId || !canChangeStructure()) return;
    aiBusyModuleId = moduleId;
    notice = '';
    error = '';
    try {
      await educatorApi.aiUpdateModule(courseId, moduleId, instruction);
      aiInstructions = { ...aiInstructions, [moduleId]: '' };
      notice = 'The AI updated the module — reload applied.';
      await loadCourse();
    } catch (cause) {
      error = apiErrorMessage(cause, 'The AI update failed.');
    } finally {
      aiBusyModuleId = null;
    }
  }

  async function handleSaveBranding() {
    if (readOnly || mutating) return;
    savingBranding = true;
    await mutate(() => educatorApi.updateBranding(courseId, {
      title, description, overview, category, targetAudience, estimatedDuration, level, language,
      learningObjectives: objectivesText.split('\n').map((item) => item.trim()).filter(Boolean),
      requirePassingQuizzes, maxQuizAttempts, visibility, enrollmentLimit: enrollmentLimit ?? null,
      enrollmentOpensAt: enrollmentOpensAt ? new Date(enrollmentOpensAt).toISOString() : null,
      enrollmentClosesAt: enrollmentClosesAt ? new Date(enrollmentClosesAt).toISOString() : null,
      instructorTitle, accentColor, themeMode, enrollmentMode, passingScorePercentage, prerequisites, instructorBio,
      courseAssistantEnabled, awardsCertificate
    }), 'Course details and settings saved.', 'settings');
    savingBranding = false;
  }

  async function publish(isPaid = false) {
    if (!course || readOnly || writing) return;
    if (dirty) { error = 'Save all changes before publishing. Only saved content can be published.'; return; }
    if (isPaid && (!publishPrice || publishPrice <= 0)) { error = 'Enter a valid price for paid checkout.'; return; }
    await mutate(() => educatorApi.publishCourse(courseId, isPaid ? { isPaid: true, price: publishPrice, currency: 'ETB' } : { isPaid: false }), 'Course published with the saved visibility and enrollment settings.');
  }

  async function unpublish() {
    if (mutating || !confirm('Unpublish this course before editing? Learner access will pause until you publish it again.')) return;
    await mutate(() => educatorApi.unpublishCourse(courseId), 'Course is now a draft. You can edit and publish it again.');
  }

  async function loadLiveSessions() {
    if (loadingSessions) return;
    sessionsLoaded = true;
    sessionsError = '';
    loadingSessions = true;
    try {
      liveSessions = await educatorApi.liveSessions(courseId);
    } catch (cause) {
      sessionsError = apiErrorMessage(cause, 'Could not load live sessions.');
    } finally {
      loadingSessions = false;
    }
  }

  function resetSessionForm() {
    sessionTitle = ''; sessionTime = ''; sessionMeetingUrl = ''; sessionDuration = 60;
    editingSessionId = null; showAddSession = false;
  }

  function editSession(session: LiveSessionDto) {
    sessionTitle = session.title;
    sessionTime = localDate(session.startTime);
    sessionDuration = session.durationMinutes;
    sessionMeetingUrl = session.meetingUrl;
    editingSessionId = session.id;
    showAddSession = true;
  }

  async function handleAddSession() {
    if (!sessionTitle.trim() || !sessionTime || !sessionMeetingUrl.trim() || addingSession) return;
    addingSession = true;
    error = '';
    try {
      const startTime = new Date(sessionTime).toISOString();
      const body = {
        title: sessionTitle.trim(),
        description: '',
        startTime,
        durationMinutes: sessionDuration,
        timeZone: 'UTC',
        meetingUrl: sessionMeetingUrl.trim()
      };
      if (editingSessionId) await educatorApi.updateLiveSession(courseId, editingSessionId, body);
      else await educatorApi.createLiveSession(courseId, body);
      notice = editingSessionId ? 'Live session updated.' : 'Live session scheduled.';
      resetSessionForm();
      await loadLiveSessions();
    } catch (cause) {
      error = cause instanceof Error ? cause.message : 'Could not schedule session.';
    } finally {
      addingSession = false;
    }
  }

  async function handleDeleteSession(sessionId: string) {
    if (!confirm('Cancel this scheduled session?')) return;
    try {
      await educatorApi.deleteLiveSession(courseId, sessionId);
      await loadLiveSessions();
      notice = 'Session cancelled.';
    } catch (cause) {
      error = cause instanceof Error ? cause.message : 'Could not cancel session.';
    }
  }

  async function loadAssignments() {
    if (loadingAssignments) return;
    assignmentsLoaded = true;
    assignmentsError = '';
    loadingAssignments = true;
    try {
      assignments = await educatorApi.assignments(courseId);
    } catch (cause) {
      assignmentsError = apiErrorMessage(cause, 'Could not load assignments.');
    } finally {
      loadingAssignments = false;
    }
  }

  async function handleAddAssignment() {
    if (!assignmentTitle.trim() || !assignmentInstructions.trim() || addingAssignment || readOnly) return;
    addingAssignment = true;
    error = '';
    try {
      await educatorApi.createAssignment(courseId, {
        title: assignmentTitle.trim(),
        instructionsMarkdown: assignmentInstructions.trim(),
        dueDaysAfterEnrollment: assignmentDueDays,
        maxScore: assignmentMaxScore
      });
      assignmentTitle = '';
      assignmentInstructions = '';
      showAddAssignment = false;
      notice = 'Assignment created.';
      await loadAssignments();
    } catch (cause) {
      error = cause instanceof Error ? cause.message : 'Could not create assignment.';
    } finally {
      addingAssignment = false;
    }
  }

  $: if (mounted && course && activeTab === 'sessions' && !sessionsLoaded && !loadingSessions) {
    void loadLiveSessions();
  }
  $: if (mounted && course && activeTab === 'assignments' && !assignmentsLoaded && !loadingAssignments) {
    void loadAssignments();
  }
</script>

<svelte:window on:beforeunload={beforeUnload} />

<section class="bg-ifa-card rounded-2xl border border-ifa-border shadow-card overflow-hidden">
  <!-- Header & Tab Navigation -->
  <div class="p-5 border-b border-ifa-border bg-ifa-card/80 backdrop-blur flex flex-wrap items-center justify-between gap-3">
    <div class="flex items-center gap-3">
      <div class="w-9 h-9 rounded-xl bg-ifa-pine/10 text-ifa-pine flex items-center justify-center">
        <BookOpen class="w-5 h-5" />
      </div>
      <div>
        <h2 class="text-sm font-bold text-ifa-text-primary">Course Studio & Curriculum</h2>
        <p class="text-xs text-ifa-text-secondary">Manage curriculum, live sessions, assignments, and branding.</p>
      </div>
    </div>

    <!-- Quick Preview CTA -->
    <div class="flex flex-wrap items-center gap-2">
      <a
        href="/courses/{courseId}?preview=1"
        target="_blank"
        rel="noopener noreferrer"
        class="px-3.5 py-1.5 rounded-xl border border-ifa-border bg-ifa-card hover:bg-ifa-card-muted text-xs font-semibold text-ifa-text-primary flex items-center gap-1.5 transition shadow-sm"
      >
        <Eye class="w-3.5 h-3.5 text-ifa-pine" />
        <span>Preview Saved Course</span>
        <ExternalLink class="w-3 h-3 text-ifa-text-muted" />
      </a>
      <a href={`/studio/courses/${courseId}/design`} class="px-3.5 py-1.5 rounded-xl border border-ifa-border bg-ifa-card hover:bg-ifa-card-muted text-xs font-semibold text-ifa-text-primary flex items-center gap-1.5 transition shadow-sm"><Palette class="w-3.5 h-3.5 text-ifa-pine" /><span>Canvas Studio</span></a>
      <button
        type="button"
        on:click={reload}
        disabled={loading || writing}
        class="text-xs font-semibold text-ifa-pine hover:underline px-2"
      >
        Reload
      </button>
      {#if course?.isPublished}
        <button type="button" on:click={unpublish} disabled={writing} class="px-3.5 py-2 rounded-xl border border-ifa-border text-xs font-bold text-ifa-text-primary disabled:opacity-50">Unpublish to Edit</button>
      {:else if course}
        <button type="button" on:click={() => publish(false)} disabled={loading || writing || generationActive || dirty} class="px-3.5 py-2 rounded-xl bg-ifa-pine text-white text-xs font-bold inline-flex items-center gap-1.5 disabled:opacity-50"><Globe class="w-3.5 h-3.5" />Publish Free</button>
      {/if}
    </div>
  </div>

  <!-- Tabs -->
  <div class="flex overflow-x-auto whitespace-nowrap border-b border-ifa-border px-5 bg-ifa-bg/50">
    <button
      type="button"
      on:click={() => (activeTab = 'curriculum')}
      class="py-3 px-4 text-xs font-bold border-b-2 transition flex items-center gap-1.5 {activeTab === 'curriculum' ? 'border-ifa-pine text-ifa-pine' : 'border-transparent text-ifa-text-secondary hover:text-ifa-text-primary'}"
    >
      <BookOpen class="w-3.5 h-3.5" />
      <span>Curriculum ({course?.modules?.length ?? 0})</span>
    </button>
    <button
      type="button"
      on:click={() => (activeTab = 'branding')}
      class="py-3 px-4 text-xs font-bold border-b-2 transition flex items-center gap-1.5 {activeTab === 'branding' ? 'border-ifa-pine text-ifa-pine' : 'border-transparent text-ifa-text-secondary hover:text-ifa-text-primary'}"
    >
      <Palette class="w-3.5 h-3.5" />
      <span>Details & Settings</span>
    </button>
    <button
      type="button"
      on:click={() => (activeTab = 'sessions')}
      class="py-3 px-4 text-xs font-bold border-b-2 transition flex items-center gap-1.5 {activeTab === 'sessions' ? 'border-ifa-pine text-ifa-pine' : 'border-transparent text-ifa-text-secondary hover:text-ifa-text-primary'}"
    >
      <Video class="w-3.5 h-3.5" />
      <span>Live Sessions</span>
    </button>
    <button
      type="button"
      on:click={() => (activeTab = 'assignments')}
      class="py-3 px-4 text-xs font-bold border-b-2 transition flex items-center gap-1.5 {activeTab === 'assignments' ? 'border-ifa-pine text-ifa-pine' : 'border-transparent text-ifa-text-secondary hover:text-ifa-text-primary'}"
    >
      <FileCheck class="w-3.5 h-3.5" />
      <span>Assignments</span>
    </button>
  </div>

  <!-- Notices & Feedback -->
  <div class="p-5 space-y-4">
    {#if error}
      <p role="alert" class="rounded-xl border border-red-200 bg-red-50 p-3 text-xs text-red-700 whitespace-pre-line">{error}</p>
    {/if}
    {#if notice}
      <p role="status" class="rounded-xl border border-emerald-200 bg-emerald-50 p-3 text-xs text-emerald-800 flex items-center gap-2">
        <Check class="w-3.5 h-3.5" />
        {notice}
      </p>
    {/if}

    {#if loading}
      <div class="flex items-center justify-center py-12">
        <Loader2 class="w-6 h-6 text-ifa-pine animate-spin" />
      </div>
    {:else if course}
      <div class="rounded-xl border border-ifa-border bg-ifa-card-muted p-3 text-xs text-ifa-text-secondary" aria-live="polite">
        {#if generationActive}
          Content is generating. Editing and publishing are paused until all jobs finish.
        {:else if course.isPublished}
          Published ({course.visibility}). Unpublish before editing course content or settings; the live schedule remains manageable.
        {:else if dirty}
          Unsaved changes. Save each changed module, assessment, and settings section before publishing. Preview shows saved content only.
        {:else}
          Saved draft. Review lessons and assessments, preview the saved course, then publish.
        {/if}
      </div>
      <fieldset disabled={writing || (readOnly && activeTab !== 'sessions')} class="min-w-0">

      <!-- TAB 1: CURRICULUM -->
      {#if activeTab === 'curriculum'}
        <div class="space-y-4">
          <div class="flex items-center justify-between">
            <h3 class="text-xs font-bold uppercase tracking-wider text-ifa-text-muted">Modules & Lessons</h3>
            <button
              type="button"
              on:click={() => (showAddModule = !showAddModule)}
              class="px-3 py-1.5 rounded-xl bg-ifa-pine text-white text-xs font-bold hover:bg-ifa-pine-light transition flex items-center gap-1.5 shadow-sm"
            >
              <Plus class="w-3.5 h-3.5" />
              <span>Add Module</span>
            </button>
          </div>

          <!-- Add Module Inline Card -->
          {#if showAddModule}
            <div class="bg-ifa-card-muted rounded-xl border border-ifa-border p-4 space-y-3">
              <h4 class="text-xs font-bold text-ifa-text-primary">New Module</h4>
              <div class="grid grid-cols-1 sm:grid-cols-4 gap-3">
                <input
                  type="text"
                  bind:value={newModuleTitle}
                  placeholder="Module title (e.g. Advanced Routing & Middleware)"
                  class="sm:col-span-3 px-3 py-2 rounded-xl bg-ifa-card border border-ifa-border text-xs text-ifa-text-primary focus:ring-1 focus:ring-ifa-pine"
                />
                <input
                  type="number"
                  min="1"
                  max="40"
                  bind:value={newModuleHours}
                  placeholder="Est. Hours"
                  class="px-3 py-2 rounded-xl bg-ifa-card border border-ifa-border text-xs text-ifa-text-primary focus:ring-1 focus:ring-ifa-pine"
                />
              </div>
              <div class="flex justify-end gap-2 pt-1">
                <button
                  type="button"
                  on:click={() => (showAddModule = false)}
                  class="px-3 py-1.5 rounded-xl text-xs font-semibold text-ifa-text-secondary hover:bg-black/5"
                >
                  Cancel
                </button>
                <button
                  type="button"
                  on:click={handleAddModule}
                  disabled={addingModule || !newModuleTitle.trim()}
                  class="px-3.5 py-1.5 rounded-xl bg-ifa-pine text-white text-xs font-bold hover:bg-ifa-pine-light transition flex items-center gap-1.5 disabled:opacity-50"
                >
                  {#if addingModule}<Loader2 class="w-3.5 h-3.5 animate-spin" />{/if}
                  <span>Save Module</span>
                </button>
              </div>
            </div>
          {/if}

          <!-- Modules List -->
          <div class="space-y-3">
            {#each course.modules as mod, mi (mod.id)}
              <div class="rounded-xl border border-ifa-border bg-ifa-card overflow-hidden transition shadow-sm">
                <!-- Module Header -->
                <div class="p-3.5 bg-ifa-card-muted/50 flex flex-wrap items-center justify-between gap-3">
                  <button
                    type="button"
                    on:click={() => toggleModule(mod.id)}
                    aria-expanded={!!expandedModules[mod.id]}
                    class="flex items-center gap-2.5 text-left flex-1 min-w-0"
                  >
                    {#if expandedModules[mod.id]}
                      <ChevronDown class="w-4 h-4 text-ifa-text-muted shrink-0" />
                    {:else}
                      <ChevronRight class="w-4 h-4 text-ifa-text-muted shrink-0" />
                    {/if}
                    <span class="text-xs font-bold text-ifa-text-primary truncate">
                      Module {mod.moduleNumber}: {moduleDrafts[mod.id]?.title || mod.title}{dirtyModules.includes(mod.id) || dirtyQuizzes.includes(mod.id) ? ' (Unsaved)' : ''}
                    </span>
                    <span class="text-[10px] text-ifa-text-muted shrink-0">
                      ({mod.lessons.length} lessons · {mod.estimatedHours}h)
                    </span>
                  </button>

                  <div class="flex items-center gap-1.5">
                    <button type="button" aria-label="Move module up" disabled={mi === 0} on:click={() => moveModule(mi, -1)} class="p-1.5 rounded-lg text-ifa-text-secondary hover:bg-ifa-card disabled:opacity-30"><ArrowUp class="w-3.5 h-3.5" /></button>
                    <button type="button" aria-label="Move module down" disabled={mi === course.modules.length - 1} on:click={() => moveModule(mi, 1)} class="p-1.5 rounded-lg text-ifa-text-secondary hover:bg-ifa-card disabled:opacity-30"><ArrowDown class="w-3.5 h-3.5" /></button>
                    <button type="button" aria-label="Duplicate saved module" title="Duplicate saved module" on:click={() => duplicate(mod.id)} class="p-1.5 rounded-lg text-ifa-text-secondary hover:bg-ifa-card"><Copy class="w-3.5 h-3.5" /></button>
                    <button
                      type="button"
                      on:click={() => (addingLessonForModuleId = addingLessonForModuleId === mod.id ? null : mod.id)}
                      class="px-2.5 py-1 rounded-lg border border-ifa-border bg-ifa-card text-[11px] font-semibold text-ifa-pine hover:bg-ifa-pine/5 flex items-center gap-1 transition"
                    >
                      <Plus class="w-3 h-3" />
                      <span>Add lesson</span>
                    </button>
                    <button
                      type="button"
                      title="Delete module"
                      aria-label="Delete module"
                      on:click={() => handleDeleteModule(mod.id, mod.title)}
                      class="p-1.5 rounded-lg text-ifa-text-muted hover:text-red-600 hover:bg-red-50 transition"
                    >
                      <Trash2 class="w-3.5 h-3.5" />
                    </button>
                  </div>
                </div>

                <!-- Add Lesson Inline -->
                {#if addingLessonForModuleId === mod.id}
                  <div class="p-4 bg-ifa-pine/5 border-t border-ifa-border space-y-2">
                    <h5 class="text-xs font-bold text-ifa-text-primary">Add Lesson to Module {mod.moduleNumber}</h5>
                    <div class="flex gap-2">
                      <input
                        type="text"
                        bind:value={newLessonTitle}
                        placeholder="Lesson title (e.g. Asynchronous Middleware Pipelines)"
                        class="flex-1 px-3 py-2 rounded-xl bg-ifa-card border border-ifa-border text-xs text-ifa-text-primary focus:ring-1 focus:ring-ifa-pine"
                      />
                      <button
                        type="button"
                        on:click={() => handleAddLesson(mod.id)}
                        disabled={addingLesson || !newLessonTitle.trim()}
                        class="px-3.5 py-2 rounded-xl bg-ifa-pine text-white text-xs font-bold hover:bg-ifa-pine-light transition flex items-center gap-1.5 disabled:opacity-50"
                      >
                        {#if addingLesson}<Loader2 class="w-3.5 h-3.5 animate-spin" />{/if}
                        <span>Add</span>
                      </button>
                      <button
                        type="button"
                        on:click={() => (addingLessonForModuleId = null)}
                        class="px-2.5 py-2 text-xs text-ifa-text-secondary"
                      >
                        Cancel
                      </button>
                    </div>
                  </div>
                {/if}

                <!-- Expanded Module Body -->
                {#if expandedModules[mod.id]}
                  <div class="p-4 space-y-4 border-t border-ifa-border">
                    <div>
                      <label class="block text-[11px] font-semibold text-ifa-text-secondary mb-1" for="m-title-{mod.id}">
                        Module Title
                      </label>
                      <input
                        id="m-title-{mod.id}"
                        type="text"
                        bind:value={moduleDrafts[mod.id].title}
                        class="w-full px-3 py-2 rounded-xl bg-ifa-card border border-ifa-border text-xs text-ifa-text-primary focus:ring-1 focus:ring-ifa-pine"
                      />
                    </div>
                    <div>
                      <label for="m-summary-{mod.id}" class="block text-[11px] font-semibold text-ifa-text-secondary mb-1">Module Summary</label>
                      <textarea id="m-summary-{mod.id}" bind:value={moduleDrafts[mod.id].description} rows="2" class="w-full px-3 py-2 rounded-xl bg-ifa-card border border-ifa-border text-xs text-ifa-text-primary"></textarea>
                    </div>

                    <!-- Lessons in Module -->
                    <div class="space-y-3 pt-2">
                      <h5 class="text-[11px] font-bold text-ifa-text-muted uppercase tracking-wider">Lessons</h5>
                      {#each moduleDrafts[mod.id].lessons as lesson, li (lesson.id)}
                        <div class="rounded-xl border border-ifa-border p-3.5 bg-ifa-card-muted/30 space-y-2.5">
                          <div class="flex items-center justify-between gap-2">
                            <input
                              type="text"
                              bind:value={lesson.title}
                              aria-label="Lesson title"
                              placeholder="Lesson Title"
                              class="font-semibold text-xs text-ifa-text-primary bg-transparent border-b border-dashed border-ifa-border hover:border-ifa-pine focus:border-ifa-pine focus:outline-none flex-1 py-1"
                            />
                            <button type="button" aria-label="Move lesson up" disabled={li === 0} on:click={() => moveLesson(mod.id, li, -1)} class="p-1 text-ifa-text-secondary disabled:opacity-30"><ArrowUp class="w-3.5 h-3.5" /></button>
                            <button type="button" aria-label="Move lesson down" disabled={li === moduleDrafts[mod.id].lessons.length - 1} on:click={() => moveLesson(mod.id, li, 1)} class="p-1 text-ifa-text-secondary disabled:opacity-30"><ArrowDown class="w-3.5 h-3.5" /></button>
                            <button type="button" aria-label="Duplicate saved lesson" on:click={() => duplicate(mod.id, lesson.id)} class="p-1 text-ifa-text-secondary"><Copy class="w-3.5 h-3.5" /></button>
                            <button
                              type="button"
                              on:click={() => handleDeleteLesson(mod.id, lesson.id, lesson.title)}
                              class="text-ifa-text-muted hover:text-red-600 p-1 rounded transition"
                              title="Delete lesson"
                              aria-label="Delete lesson"
                            >
                              <Trash2 class="w-3.5 h-3.5" />
                            </button>
                          </div>
                          <div>
                            <label for="lesson-content-{lesson.id}" class="block text-[11px] font-semibold text-ifa-text-secondary mb-1">Lesson Content (Markdown)</label>
                            <textarea
                              id="lesson-content-{lesson.id}"
                              bind:value={lesson.content}
                              rows="6"
                              placeholder="Lesson markdown content..."
                              class="w-full px-3 py-2 rounded-xl bg-ifa-card border border-ifa-border text-xs font-mono text-ifa-text-primary focus:ring-1 focus:ring-ifa-pine resize-y"
                            ></textarea>
                          </div>
                          <div class="grid grid-cols-1 sm:grid-cols-2 gap-3">
                            <label class="text-[11px] text-ifa-text-secondary">Reading Time (Minutes)
                              <input type="number" min="1" max="600" bind:value={lesson.readingTimeMinutes} class="mt-1 w-full px-3 py-2 rounded-xl bg-ifa-card border border-ifa-border text-xs text-ifa-text-primary" />
                            </label>
                            <label class="text-[11px] text-ifa-text-secondary">YouTube Video ID (Optional)
                              <input type="text" bind:value={lesson.youTubeVideoId} placeholder="11-character video ID" class="mt-1 w-full px-3 py-2 rounded-xl bg-ifa-card border border-ifa-border text-xs text-ifa-text-primary" />
                            </label>
                          </div>
                        </div>
                      {:else}
                        <p class="text-xs text-ifa-text-muted">No lessons yet. Add a lesson, write its content, and save the module.</p>
                      {/each}
                    </div>

                    <!-- AI Update Instruction -->
                    <div class="flex items-center gap-2 pt-2">
                      <input
                        type="text"
                        bind:value={aiInstructions[mod.id]}
                        aria-label="AI module refinement instruction"
                        placeholder="Ask AI to refine this module (e.g. 'Add real-world code examples to lesson 1')"
                        class="flex-1 px-3 py-2 rounded-xl bg-ifa-card border border-ifa-border text-xs text-ifa-text-primary placeholder-ifa-text-muted focus:ring-1 focus:ring-ifa-pine"
                      />
                      <button
                        type="button"
                        on:click={() => aiUpdateModule(mod.id)}
                        disabled={aiBusyModuleId !== null || !(aiInstructions[mod.id] ?? '').trim()}
                        class="px-3 py-2 rounded-xl bg-ifa-pine/10 text-ifa-pine text-xs font-semibold hover:bg-ifa-pine/20 transition flex items-center gap-1.5 disabled:opacity-50 shrink-0"
                      >
                        {#if aiBusyModuleId === mod.id}
                          <Loader2 class="w-3.5 h-3.5 animate-spin" />
                        {:else}
                          <Sparkles class="w-3.5 h-3.5" />
                        {/if}
                        <span>Refine with AI</span>
                      </button>
                    </div>

                    <!-- Save Module Button -->
                    <div class="flex justify-end pt-2">
                      <button
                        type="button"
                        on:click={() => saveModule(mod.id)}
                        disabled={savingModuleId !== null || !dirtyModules.includes(mod.id)}
                        class="px-4 py-2 rounded-xl bg-ifa-pine text-white text-xs font-bold flex items-center gap-1.5 hover:bg-ifa-pine-light transition disabled:opacity-60 shadow-sm"
                      >
                        {#if savingModuleId === mod.id}
                          <Loader2 class="w-3.5 h-3.5 animate-spin" />
                          <span>Saving…</span>
                        {:else}
                          <Save class="w-3.5 h-3.5" />
                          <span>Save Module Changes</span>
                        {/if}
                      </button>
                    </div>
                    <QuizEditor bind:value={quizDrafts[mod.id]} prefix={mod.id} defaultPassingScore={passingScorePercentage} disabled={readOnly || mutating} />
                    <div class="flex justify-end">
                      <button type="button" on:click={() => saveQuizzes(mod.id)} disabled={mutating || !dirtyQuizzes.includes(mod.id)} class="px-4 py-2 rounded-xl bg-ifa-pine text-white text-xs font-bold inline-flex items-center gap-1.5 disabled:opacity-50"><Save class="w-3.5 h-3.5" />Save Assessments</button>
                    </div>
                  </div>
                {/if}
              </div>
            {/each}
            {#if course.modules.length === 0}
              <p class="rounded-xl border border-dashed border-ifa-border p-8 text-center text-xs text-ifa-text-secondary">Your draft has no modules yet. Add the first module to start building the curriculum.</p>
            {/if}
          </div>
        </div>

      <!-- TAB 2: BRANDING & ACCESS -->
      {:else if activeTab === 'branding'}
        <div class="space-y-4 max-w-xl">
          <div class="space-y-1">
            <h3 class="text-xs font-bold uppercase tracking-wider text-ifa-text-muted">Branding & Course Configuration</h3>
            <p class="text-xs text-ifa-text-secondary">Edit the saved course details, appearance, and learner enrollment rules.</p>
          </div>

          <div class="grid grid-cols-1 sm:grid-cols-2 gap-4">
            <label class="sm:col-span-2 text-xs font-semibold text-ifa-text-primary">Course Title
              <input type="text" bind:value={title} required class="mt-1 w-full px-3 py-2 rounded-xl bg-ifa-card border border-ifa-border text-xs" />
            </label>
            <label class="sm:col-span-2 text-xs font-semibold text-ifa-text-primary">Description
              <textarea bind:value={description} rows="3" class="mt-1 w-full px-3 py-2 rounded-xl bg-ifa-card border border-ifa-border text-xs"></textarea>
            </label>
            <label class="sm:col-span-2 text-xs font-semibold text-ifa-text-primary">Detailed Course Overview
              <textarea bind:value={overview} rows="6" maxlength="12000" placeholder="Explain the course approach, scope, learner fit, and expected experience." class="mt-1 w-full px-3 py-2 rounded-xl bg-ifa-card border border-ifa-border text-xs"></textarea>
            </label>
            <label class="text-xs font-semibold text-ifa-text-primary">Category
              <input type="text" bind:value={category} class="mt-1 w-full px-3 py-2 rounded-xl bg-ifa-card border border-ifa-border text-xs" />
            </label>
            <label class="text-xs font-semibold text-ifa-text-primary">Target Audience
              <input type="text" bind:value={targetAudience} class="mt-1 w-full px-3 py-2 rounded-xl bg-ifa-card border border-ifa-border text-xs" />
            </label>
            <label class="text-xs font-semibold text-ifa-text-primary">Level
              <input type="text" bind:value={level} class="mt-1 w-full px-3 py-2 rounded-xl bg-ifa-card border border-ifa-border text-xs" />
            </label>
            <label class="text-xs font-semibold text-ifa-text-primary">Language
              <input type="text" bind:value={language} class="mt-1 w-full px-3 py-2 rounded-xl bg-ifa-card border border-ifa-border text-xs" />
            </label>
            <label class="sm:col-span-2 text-xs font-semibold text-ifa-text-primary">Estimated Duration
              <input type="text" bind:value={estimatedDuration} placeholder="e.g. 6 weeks" class="mt-1 w-full px-3 py-2 rounded-xl bg-ifa-card border border-ifa-border text-xs" />
            </label>
            <label class="sm:col-span-2 text-xs font-semibold text-ifa-text-primary">Learning Objectives (One Per Line)
              <textarea bind:value={objectivesText} rows="4" class="mt-1 w-full px-3 py-2 rounded-xl bg-ifa-card border border-ifa-border text-xs"></textarea>
            </label>
          </div>

          <div class="grid grid-cols-1 sm:grid-cols-2 gap-4">
            <div>
              <label for="course-accent" class="block text-xs font-semibold text-ifa-text-primary mb-1">Accent Color</label>
              <div class="flex items-center gap-2">
                <input
                  type="color"
                  id="course-accent"
                  bind:value={accentColor}
                  class="w-10 h-10 rounded-xl cursor-pointer border border-ifa-border p-1 bg-ifa-card"
                />
                <input
                  type="text"
                  aria-label="Accent color hex value"
                  bind:value={accentColor}
                  class="flex-1 px-3 py-2 rounded-xl bg-ifa-card border border-ifa-border text-xs text-ifa-text-primary"
                />
              </div>
            </div>

            <div>
              <label for="course-theme" class="block text-xs font-semibold text-ifa-text-primary mb-1">Theme Palette</label>
              <select
                id="course-theme"
                bind:value={themeMode}
                class="w-full px-3 py-2 rounded-xl bg-ifa-card border border-ifa-border text-xs text-ifa-text-primary"
              >
                <option value="pine">Pine (Default Green)</option>
                <option value="emerald">Emerald</option>
                <option value="slate">Slate</option>
                <option value="indigo">Indigo</option>
                <option value="amber">Amber</option>
              </select>
            </div>
          </div>

          <div class="grid grid-cols-1 sm:grid-cols-2 gap-4">
            <div>
              <label for="course-enrollment" class="block text-xs font-semibold text-ifa-text-primary mb-1">Enrollment Mode</label>
              <select
                id="course-enrollment"
                bind:value={enrollmentMode}
                class="w-full px-3 py-2 rounded-xl bg-ifa-card border border-ifa-border text-xs text-ifa-text-primary"
              >
                <option value="Open">Open Enrollment (Instant Access)</option>
                <option value="ApprovalRequired">Educator Approval Required</option>
                <option value="InviteOnly">Invite Only (Share Code)</option>
              </select>
            </div>

            <div>
              <label for="course-score" class="block text-xs font-semibold text-ifa-text-primary mb-1">Default Quiz Passing Score (%)</label>
              <input
                id="course-score"
                type="number"
                min="1"
                max="100"
                bind:value={passingScorePercentage}
                class="w-full px-3 py-2 rounded-xl bg-ifa-card border border-ifa-border text-xs text-ifa-text-primary"
              />
            </div>
          </div>

          <div class="grid grid-cols-1 sm:grid-cols-2 gap-4">
            <label class="text-xs font-semibold text-ifa-text-primary">Visibility After Publishing
              <select bind:value={visibility} class="mt-1 w-full px-3 py-2 rounded-xl bg-ifa-card border border-ifa-border text-xs"><option value="Public">Public (Marketplace)</option><option value="Unlisted">Unlisted (Direct Link)</option><option value="Private">Private (Invite Only)</option></select>
            </label>
            <label class="text-xs font-semibold text-ifa-text-primary">Maximum Attempts Per Quiz
              <input type="number" min="1" max="100" bind:value={maxQuizAttempts} class="mt-1 w-full px-3 py-2 rounded-xl bg-ifa-card border border-ifa-border text-xs" />
            </label>
            <label class="sm:col-span-2 flex items-center gap-2 text-xs text-ifa-text-primary"><input type="checkbox" bind:checked={requirePassingQuizzes} />Require passing quizzes to complete this course</label>
            <label class="sm:col-span-2 flex items-start gap-2 rounded-xl border border-ifa-border bg-ifa-card-muted p-3 text-xs text-ifa-text-primary"><input class="mt-0.5" type="checkbox" bind:checked={courseAssistantEnabled} /><span><strong class="block">Enable Course Assistant</strong><span class="text-ifa-text-muted">Learners can use the course-aware AI assistant while studying. Disable it to hide and block the assistant for this course.</span></span></label>
            <label class="sm:col-span-2 flex items-start gap-2 rounded-xl border border-ifa-border bg-ifa-card-muted p-3 text-xs text-ifa-text-primary"><input class="mt-0.5" type="checkbox" bind:checked={awardsCertificate} /><span><strong class="block">Offer a completion certificate</strong><span class="text-ifa-text-muted">Show learners that successful completion can earn a certificate.</span></span></label>
            <label class="sm:col-span-2 text-xs font-semibold text-ifa-text-primary">Enrollment Limit (Blank for Unlimited)
              <input type="number" min="1" bind:value={enrollmentLimit} class="mt-1 w-full px-3 py-2 rounded-xl bg-ifa-card border border-ifa-border text-xs" />
            </label>
            <label class="text-xs font-semibold text-ifa-text-primary">Enrollment Opens (Local Time)
              <input type="datetime-local" bind:value={enrollmentOpensAt} class="mt-1 w-full px-3 py-2 rounded-xl bg-ifa-card border border-ifa-border text-xs" />
            </label>
            <label class="text-xs font-semibold text-ifa-text-primary">Enrollment Closes (Local Time)
              <input type="datetime-local" bind:value={enrollmentClosesAt} class="mt-1 w-full px-3 py-2 rounded-xl bg-ifa-card border border-ifa-border text-xs" />
            </label>
          </div>
          <p class="text-[11px] text-ifa-text-muted">Dates are saved in UTC. Each assessment has its own passing score; the default is used for new assessments.</p>

          <div>
            <label for="course-prerequisites" class="block text-xs font-semibold text-ifa-text-primary mb-1">Course Prerequisites</label>
            <input
              id="course-prerequisites"
              type="text"
              bind:value={prerequisites}
              placeholder="e.g. Basic understanding of JavaScript and HTML"
              class="w-full px-3 py-2 rounded-xl bg-ifa-card border border-ifa-border text-xs text-ifa-text-primary"
            />
          </div>

          <div>
            <label for="course-bio" class="block text-xs font-semibold text-ifa-text-primary mb-1">Instructor Bio</label>
            <textarea
              id="course-bio"
              bind:value={instructorBio}
              rows="3"
              placeholder="Tell learners about your experience and background..."
              class="w-full px-3 py-2 rounded-xl bg-ifa-card border border-ifa-border text-xs text-ifa-text-primary resize-y"
            ></textarea>
          </div>
          <label class="block text-xs font-semibold text-ifa-text-primary">Instructor Title
            <input type="text" bind:value={instructorTitle} class="mt-1 w-full px-3 py-2 rounded-xl bg-ifa-card border border-ifa-border text-xs" />
          </label>

          <div class="pt-2">
            <button
              type="button"
              on:click={handleSaveBranding}
              disabled={savingBranding || !settingsDirty}
              class="px-5 py-2.5 rounded-xl bg-ifa-pine text-white text-xs font-bold hover:bg-ifa-pine-light transition flex items-center gap-1.5 disabled:opacity-60 shadow-sm"
            >
              {#if savingBranding}<Loader2 class="w-3.5 h-3.5 animate-spin" />{/if}
              <span>Save Details & Settings</span>
            </button>
          </div>
          <details class="border-t border-ifa-border pt-4">
            <summary class="text-xs font-semibold text-ifa-text-secondary cursor-pointer">Paid Publishing (Existing Chapa Integration)</summary>
            <p class="text-xs text-ifa-text-muted mt-2">Optional. Free publishing above does not require a payment provider.</p>
            <div class="flex flex-wrap items-end gap-2 mt-3">
              <label class="text-xs text-ifa-text-primary">Price (ETB)<input type="number" min="1" bind:value={publishPrice} class="mt-1 block w-36 px-3 py-2 rounded-xl bg-ifa-card border border-ifa-border" /></label>
              <button type="button" on:click={() => publish(true)} disabled={dirty || !publishPrice || mutating} class="px-3 py-2 rounded-xl border border-ifa-border text-xs font-semibold text-ifa-text-primary disabled:opacity-50">Publish Paid</button>
            </div>
          </details>
        </div>

      <!-- TAB 3: LIVE SESSIONS -->
      {:else if activeTab === 'sessions'}
        <div class="space-y-4">
          <div class="flex items-center justify-between">
            <div>
              <h3 class="text-xs font-bold uppercase tracking-wider text-ifa-text-muted">Live Classes & Cohort Sessions</h3>
              <p class="text-xs text-ifa-text-secondary">Schedule live video sessions (Zoom, Google Meet, Microsoft Teams) for your cohort.</p>
            </div>
            <button
              type="button"
              on:click={() => { if (showAddSession) resetSessionForm(); else { editingSessionId = null; showAddSession = true; } }}
              class="px-3 py-1.5 rounded-xl bg-ifa-pine text-white text-xs font-bold hover:bg-ifa-pine-light transition flex items-center gap-1.5 shadow-sm"
            >
              <Plus class="w-3.5 h-3.5" />
              <span>Schedule Session</span>
            </button>
          </div>

          {#if showAddSession}
            <div class="bg-ifa-card-muted rounded-xl border border-ifa-border p-4 space-y-3">
              <h4 class="text-xs font-bold text-ifa-text-primary">{editingSessionId ? 'Edit Class' : 'Schedule New Class'}</h4>
              <div class="grid grid-cols-1 sm:grid-cols-2 gap-3">
                <input
                  type="text"
                  aria-label="Live session title"
                  bind:value={sessionTitle}
                  placeholder="Session title (e.g. Week 1 Live Q&A & Code Review)"
                  class="px-3 py-2 rounded-xl bg-ifa-card border border-ifa-border text-xs text-ifa-text-primary focus:ring-1 focus:ring-ifa-pine"
                />
                <input
                  type="datetime-local"
                  aria-label="Live session start time"
                  bind:value={sessionTime}
                  class="px-3 py-2 rounded-xl bg-ifa-card border border-ifa-border text-xs text-ifa-text-primary focus:ring-1 focus:ring-ifa-pine"
                />
                <input
                  type="url"
                  aria-label="Meeting URL"
                  bind:value={sessionMeetingUrl}
                  placeholder="Meeting URL (Zoom / Google Meet link)"
                  class="px-3 py-2 rounded-xl bg-ifa-card border border-ifa-border text-xs text-ifa-text-primary focus:ring-1 focus:ring-ifa-pine"
                />
                <input
                  type="number"
                  aria-label="Session duration in minutes"
                  min="15"
                  max="180"
                  bind:value={sessionDuration}
                  placeholder="Duration (Minutes)"
                  class="px-3 py-2 rounded-xl bg-ifa-card border border-ifa-border text-xs text-ifa-text-primary focus:ring-1 focus:ring-ifa-pine"
                />
              </div>
              <div class="flex justify-end gap-2 pt-1">
                <button
                  type="button"
                  on:click={resetSessionForm}
                  class="px-3 py-1.5 text-xs text-ifa-text-secondary"
                >
                  Cancel
                </button>
                <button
                  type="button"
                  on:click={handleAddSession}
                  disabled={addingSession || !sessionTitle.trim() || !sessionTime || !sessionMeetingUrl.trim()}
                  class="px-4 py-1.5 rounded-xl bg-ifa-pine text-white text-xs font-bold hover:bg-ifa-pine-light transition flex items-center gap-1.5 disabled:opacity-50"
                >
                  {#if addingSession}<Loader2 class="w-3.5 h-3.5 animate-spin" />{/if}
                  <span>{editingSessionId ? 'Update Session' : 'Save Session'}</span>
                </button>
              </div>
            </div>
          {/if}

          {#if sessionsError}
            <p role="alert" class="text-xs text-red-700">{sessionsError} <button type="button" on:click={loadLiveSessions} class="underline">Retry</button></p>
          {/if}
          {#if loadingSessions}
            <div class="flex justify-center py-8"><Loader2 class="w-5 h-5 animate-spin text-ifa-pine" /></div>
          {:else if liveSessions.length === 0}
            <div class="text-center py-8 bg-ifa-card rounded-xl border border-ifa-border p-6 text-ifa-text-muted text-xs">
              No live sessions scheduled yet. Click "+ Schedule Session" above to add your first live class.
            </div>
          {:else}
            <div class="divide-y divide-ifa-border border border-ifa-border rounded-xl overflow-hidden bg-ifa-card">
              {#each liveSessions as s}
                <div class="p-3.5 flex items-center justify-between gap-3">
                  <div>
                    <h5 class="text-xs font-bold text-ifa-text-primary">{s.title}</h5>
                    <p class="text-[11px] text-ifa-text-secondary mt-0.5">
                      {new Date(s.startTime).toLocaleString()} · {s.durationMinutes} mins · <span class="text-emerald-700 font-semibold">{s.status}</span>
                    </p>
                    {#if s.meetingUrl}
                      <a
                        href={s.meetingUrl}
                        target="_blank"
                        rel="noopener noreferrer"
                        class="text-[11px] text-ifa-pine hover:underline inline-flex items-center gap-1 mt-1 font-semibold"
                      >
                        <span>Join Meeting</span>
                        <ExternalLink class="w-3 h-3" />
                      </a>
                    {/if}
                  </div>
                  {#if s.status === 'Scheduled'}
                    <div class="flex items-center gap-1">
                      <button type="button" on:click={() => editSession(s)} class="px-2 py-1 rounded-lg text-[11px] font-semibold text-ifa-pine hover:bg-ifa-pine/5">Edit</button>
                      <button
                        type="button"
                        on:click={() => handleDeleteSession(s.id)}
                        aria-label="Cancel session"
                        class="p-1.5 rounded-lg text-ifa-text-muted hover:text-red-600 hover:bg-red-50 transition"
                      >
                        <Trash2 class="w-4 h-4" />
                      </button>
                    </div>
                  {/if}
                </div>
              {/each}
            </div>
          {/if}
        </div>

      <!-- TAB 4: ASSIGNMENTS -->
      {:else if activeTab === 'assignments'}
        <div class="space-y-4">
          <div class="flex items-center justify-between">
            <div>
              <h3 class="text-xs font-bold uppercase tracking-wider text-ifa-text-muted">Course Assignments</h3>
              <p class="text-xs text-ifa-text-secondary">Project deliverables, rubrics, and text or repository-link submissions.</p>
            </div>
            <button
              type="button"
              on:click={() => (showAddAssignment = !showAddAssignment)}
              class="px-3 py-1.5 rounded-xl bg-ifa-pine text-white text-xs font-bold hover:bg-ifa-pine-light transition flex items-center gap-1.5 shadow-sm"
            >
              <Plus class="w-3.5 h-3.5" />
              <span>Create Assignment</span>
            </button>
          </div>

          {#if showAddAssignment}
            <div class="bg-ifa-card-muted rounded-xl border border-ifa-border p-4 space-y-3">
              <h4 class="text-xs font-bold text-ifa-text-primary">Create Assignment</h4>
              <div class="grid grid-cols-1 sm:grid-cols-3 gap-3">
                <input
                  type="text"
                  aria-label="Assignment title"
                  bind:value={assignmentTitle}
                  placeholder="Assignment title (e.g. Capstone Microservices API)"
                  class="sm:col-span-2 px-3 py-2 rounded-xl bg-ifa-card border border-ifa-border text-xs text-ifa-text-primary focus:ring-1 focus:ring-ifa-pine"
                />
                <input
                  type="number"
                  aria-label="Assignment maximum score"
                  min="10"
                  max="1000"
                  bind:value={assignmentMaxScore}
                  placeholder="Max Score"
                  class="px-3 py-2 rounded-xl bg-ifa-card border border-ifa-border text-xs text-ifa-text-primary focus:ring-1 focus:ring-ifa-pine"
                />
              </div>
              <div>
                <textarea
                  aria-label="Assignment instructions"
                  bind:value={assignmentInstructions}
                  rows="4"
                  placeholder="Markdown instructions and grading criteria..."
                  class="w-full px-3 py-2 rounded-xl bg-ifa-card border border-ifa-border text-xs font-mono text-ifa-text-primary resize-y"
                ></textarea>
              </div>
              <div class="flex justify-end gap-2 pt-1">
                <button
                  type="button"
                  on:click={() => (showAddAssignment = false)}
                  class="px-3 py-1.5 text-xs text-ifa-text-secondary"
                >
                  Cancel
                </button>
                <button
                  type="button"
                  on:click={handleAddAssignment}
                  disabled={addingAssignment || !assignmentTitle.trim() || !assignmentInstructions.trim()}
                  class="px-4 py-1.5 rounded-xl bg-ifa-pine text-white text-xs font-bold hover:bg-ifa-pine-light transition flex items-center gap-1.5 disabled:opacity-50"
                >
                  {#if addingAssignment}<Loader2 class="w-3.5 h-3.5 animate-spin" />{/if}
                  <span>Save Assignment</span>
                </button>
              </div>
            </div>
          {/if}

          {#if assignmentsError}
            <p role="alert" class="text-xs text-red-700">{assignmentsError} <button type="button" on:click={loadAssignments} class="underline">Retry</button></p>
          {/if}
          {#if loadingAssignments}
            <div class="flex justify-center py-8"><Loader2 class="w-5 h-5 animate-spin text-ifa-pine" /></div>
          {:else if assignments.length === 0}
            <div class="text-center py-8 bg-ifa-card rounded-xl border border-ifa-border p-6 text-ifa-text-muted text-xs">
              No assignments created yet. Click "+ Create Assignment" to add a project rubric.
            </div>
          {:else}
            <div class="divide-y divide-ifa-border border border-ifa-border rounded-xl overflow-hidden bg-ifa-card">
              {#each assignments as a}
                <div class="p-3.5 flex items-center justify-between gap-3">
                  <div>
                    <h5 class="text-xs font-bold text-ifa-text-primary">{a.title}</h5>
                    <p class="text-[11px] text-ifa-text-secondary mt-0.5">
                      Max score: {a.maxScore} pts · Due: {a.dueDaysAfterEnrollment} days after enroll · Submissions: {a.submissionsCount ?? 0}
                    </p>
                  </div>
                </div>
              {/each}
            </div>
          {/if}
        </div>
      {/if}
      </fieldset>
    {/if}
  </div>
</section>
