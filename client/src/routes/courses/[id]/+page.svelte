<script lang="ts">
  import { page } from '$app/stores';
  import { browser } from '$app/environment';
  import { goto } from '$app/navigation';
  import {
    ArrowLeft,
    BookOpen,
    CheckCircle2,
    Circle,
    ClipboardCheck,
    Clock,
    PlayCircle,
    Share2,
    Bookmark,
    BookmarkCheck,
    Loader2,
    Sparkles,
    ShieldCheck,
    Video,
    MessageSquare,
    FileCheck,
    Send,
    ExternalLink,
    Check
  } from 'lucide-svelte';
  import LessonView from '$lib/components/LessonView.svelte';
  import QuizView from '$lib/components/QuizView.svelte';
  import CourseChatbot from '$lib/components/CourseChatbot.svelte';
  import CourseShareDialog from '$lib/components/CourseShareDialog.svelte';
  import AssetUploader from '$lib/components/assets/AssetUploader.svelte';
  import CourseReviews from '$lib/components/CourseReviews.svelte';
  import {
    coursesApi,
    paymentsApi,
    apiErrorMessage,
    type CourseDiscussionDto,
    type LiveSessionDto,
    type AssignmentDto
  } from '$lib/api';
  import { session } from '$lib/stores/sessionStore';
  import {
    courseDetails,
    courseProgress,
    markLessonComplete,
    toggleBookmark,
    loadCourseDetail
  } from '$lib/stores/coursesStore';
  import type { Lesson, Module, Quiz } from '$lib/types';

  $: courseId = $page.params.id ?? '';
  // Reactively resolve the course from the store so progress updates live.
  $: course = $courseDetails[courseId] ?? null;
  $: progress = course ? courseProgress(course) : 0;
  $: isPreview = course?.isOwner === true || $page.url.searchParams.get('preview') === '1';
  $: hasContentAccess = course?.hasContentAccess === true;
  $: canParticipate = hasContentAccess && !isPreview && $session.authenticated && course?.enrollmentStatus === 'Active';
  $: courseAccent = /^#[0-9a-f]{6}$/i.test(course?.details?.accentColor || '') ? course!.details!.accentColor! : '#1B3D2F';
  $: courseThemeEnd = ({ slate: '#334155', emerald: '#065f46', indigo: '#3730a3', teal: '#115e59', amber: '#92400e' } as Record<string, string>)[course?.details?.themeMode || ''] || '#0D1F17';

  // View state: course outline, a specific lesson, or a module quiz.
  type View =
    | { kind: 'outline' }
    | { kind: 'lesson'; moduleId: string; lessonId: string }
    | { kind: 'quiz'; moduleId: string; quizId: string };
  let view: View = { kind: 'outline' };

  let outlineTab: 'curriculum' | 'discussions' | 'sessions' | 'assignments' = 'curriculum';

  let shareOpen = false;
  let requestedCourseKey = '';
  let courseLoading = true;
  let courseLoadError = '';
  let completingLessonId: string | null = null;
  let completionError = '';
  let enrolling = false;
  let enrollmentError = '';
  let enrollmentNotice = '';

  // Discussions state
  let discussions: CourseDiscussionDto[] = [];
  let loadingDiscussions = false;
  let discussionsLoaded = false;
  let discussionsError = '';
  let newDiscussionTitle = '';
  let newDiscussionContent = '';
  let postingDiscussion = false;
  let replyContents: Record<string, string> = {};
  let postingReplyId: string | null = null;

  // Live sessions state
  let liveSessions: LiveSessionDto[] = [];
  let loadingSessions = false;
  let sessionsLoaded = false;
  let sessionsError = '';

  // Assignments state
  let assignments: AssignmentDto[] = [];
  let loadingAssignments = false;
  let assignmentsLoaded = false;
  let assignmentsError = '';
  let submissionTexts: Record<string, string> = {};
  let submissionAssetIds: Record<string, string> = {};
  let submittingAssignmentId: string | null = null;
  let assignmentNotice = '';

  $: if (browser && $session.ready && courseId && requestedCourseKey !== `${courseId}:${$session.learnerId}:${$page.url.searchParams.get('preview')}`) {
    requestedCourseKey = `${courseId}:${$session.learnerId}:${$page.url.searchParams.get('preview')}`;
    view = { kind: 'outline' }; outlineTab = 'curriculum';
    discussions = []; liveSessions = []; assignments = [];
    discussionsLoaded = false; sessionsLoaded = false; assignmentsLoaded = false;
    discussionsError = ''; sessionsError = ''; assignmentsError = ''; completionError = ''; enrollmentError = ''; enrollmentNotice = '';
    submissionTexts = {}; submissionAssetIds = {}; replyContents = {}; newDiscussionTitle = ''; newDiscussionContent = '';
    void refreshCourse();
  }

  async function refreshCourse() {
    const key = requestedCourseKey;
    courseLoading = true;
    courseLoadError = '';
    try { await loadCourseDetail(courseId); }
    catch (cause) { if (key === requestedCourseKey) courseLoadError = apiErrorMessage(cause, 'Could not load this course.'); }
    finally { if (key === requestedCourseKey) courseLoading = false; }
  }

  async function enroll() {
    if (!course || enrolling || isPreview || course.isOwner) return;
    if (!$session.authenticated) { await goto(`/login?redirect=${encodeURIComponent(`/courses/${courseId}`)}`); return; }
    enrolling = true; enrollmentError = ''; enrollmentNotice = '';
    try {
      const result = await coursesApi.enroll(courseId);
      if (result.paymentRequired) {
        const payment = await paymentsApi.initialize(courseId, `${window.location.origin}/marketplace`);
        if (payment.enrollmentStatus === 'PendingApproval') {
          enrollmentNotice = payment.message || 'Payment is confirmed and educator approval is pending.';
          return;
        }
        if (!payment.checkoutUrl) throw new Error('The payment provider did not return a checkout URL.');
        window.location.assign(payment.checkoutUrl);
        return;
      }
      enrollmentNotice = result.status === 'PendingApproval' ? 'Enrollment requested. Your educator must approve access before you can start.' : 'Enrollment saved.';
      await refreshCourse();
    } catch (cause) { enrollmentError = apiErrorMessage(cause, 'Could not enroll.'); }
    finally { enrolling = false; }
  }

  async function shareCourse() {
    if (!course) return;
    if ($session.authenticated && (course.isOwner || hasContentAccess)) {
      shareOpen = true;
      return;
    }
    const url = `${window.location.origin}/courses/${course.id}`;
    try {
      if (navigator.share) await navigator.share({ title: course.title, url });
      else {
        await navigator.clipboard.writeText(url);
        enrollmentNotice = 'Public course link copied.';
      }
    } catch (cause) {
      if (cause instanceof DOMException && cause.name === 'AbortError') return;
      enrollmentError = 'Could not share this course link.';
    }
  }

  $: activeModule = (() => {
    if (view.kind === 'outline' || !course) return null;
    const moduleId = view.moduleId;
    return course.modules.find((m) => m.id === moduleId) ?? null;
  })();
  $: activeLesson = (() => {
    if (view.kind !== 'lesson' || !activeModule) return null;
    const lessonId = view.lessonId;
    return activeModule.lessons.find((l) => l.id === lessonId) ?? null;
  })();
  $: activeContextModuleId = view.kind === 'outline' ? null : view.moduleId;
  $: activeQuiz = (() => {
    if (view.kind !== 'quiz' || !activeModule) return null;
    const quizId = view.quizId;
    return activeModule.quizzes.find((q) => q.id === quizId) ?? null;
  })();

  $: flatLessons = course
    ? course.modules.flatMap((m) => m.lessons.map((l) => ({ moduleId: m.id, lesson: l })))
    : [];

  function openLesson(moduleId: string, lessonId: string) {
    if (!hasContentAccess) return;
    completionError = '';
    view = { kind: 'lesson', moduleId, lessonId };
    scrollTop();
  }
  function openQuiz(moduleId: string, quizId: string) {
    if (!hasContentAccess) return;
    view = { kind: 'quiz', moduleId, quizId };
    scrollTop();
  }
  function backToOutline() {
    view = { kind: 'outline' };
    scrollTop();
  }
  function scrollTop() {
    if (typeof window !== 'undefined') window.scrollTo({ top: 0, behavior: 'smooth' });
  }

  async function handleComplete(lessonId: string) {
    if (!course || !canParticipate || completingLessonId) return;
    const lesson = flatLessons.find((f) => f.lesson.id === lessonId)?.lesson;
    if (lesson?.completed) return;
    completingLessonId = lessonId; completionError = '';
    try { await markLessonComplete(course.id, lessonId); }
    catch (cause) { completionError = apiErrorMessage(cause, 'Could not save completion. Please retry.'); }
    finally { completingLessonId = null; }
  }

  function goNext(currentLessonId: string) {
    const idx = flatLessons.findIndex((f) => f.lesson.id === currentLessonId);
    const next = flatLessons[idx + 1];
    if (next) openLesson(next.moduleId, next.lesson.id);
    else backToOutline();
  }

  async function handleScored() {
    try { await loadCourseDetail(courseId); }
    catch { enrollmentError = 'Your quiz attempt was saved, but progress could not be refreshed. Reload the course to see it.'; }
  }

  function moduleProgress(module: Module): number {
    if (module.lessons.length === 0) return 0;
    return Math.round(
      (module.lessons.filter((l) => l.completed).length / module.lessons.length) * 100
    );
  }

  // Discussions loader & poster
  async function loadDiscussions() {
    if (!courseId || !canParticipate || loadingDiscussions) return;
    const id = courseId;
    discussionsLoaded = true; discussionsError = '';
    loadingDiscussions = true;
    try {
      const result = await coursesApi.discussions(id);
      if (id === courseId) discussions = result;
    } catch (cause) {
      if (id === courseId) discussionsError = apiErrorMessage(cause, 'Could not load discussions.');
    } finally {
      loadingDiscussions = false;
    }
  }

  async function handlePostDiscussion() {
    if (!newDiscussionContent.trim() || postingDiscussion || !courseId || !canParticipate) return;
    postingDiscussion = true;
    try {
      await coursesApi.postDiscussion(courseId, {
        title: newDiscussionTitle.trim() || 'Question',
        content: newDiscussionContent.trim()
      });
      newDiscussionTitle = '';
      newDiscussionContent = '';
      await loadDiscussions();
    } catch (cause) {
      discussionsError = apiErrorMessage(cause, 'Could not post your question.');
    } finally {
      postingDiscussion = false;
    }
  }

  async function handlePostReply(discussionId: string) {
    const text = (replyContents[discussionId] ?? '').trim();
    if (!text || postingReplyId || !courseId || !canParticipate) return;
    postingReplyId = discussionId;
    try {
      await coursesApi.postReply(courseId, discussionId, { content: text });
      replyContents[discussionId] = '';
      await loadDiscussions();
    } catch (cause) {
      discussionsError = apiErrorMessage(cause, 'Could not post your reply.');
    } finally {
      postingReplyId = null;
    }
  }

  // Live sessions loader
  async function loadLiveSessions() {
    if (!courseId || !canParticipate || loadingSessions) return;
    const id = courseId;
    sessionsLoaded = true; sessionsError = '';
    loadingSessions = true;
    try {
      const result = await coursesApi.liveSessions(id);
      if (id === courseId) liveSessions = result;
    } catch (cause) {
      if (id === courseId) sessionsError = apiErrorMessage(cause, 'Could not load live sessions.');
    } finally {
      loadingSessions = false;
    }
  }

  // Assignments loader & submitter
  async function loadAssignments() {
    if (!courseId || !canParticipate || loadingAssignments) return;
    const id = courseId;
    assignmentsLoaded = true; assignmentsError = '';
    loadingAssignments = true;
    try {
      const result = await coursesApi.assignments(id);
      if (id === courseId) {
        assignments = result;
        for (const assignment of result) {
          if (assignment.mySubmission?.status === 'ResubmissionRequested' && !submissionTexts[assignment.id]) {
            submissionTexts[assignment.id] = assignment.mySubmission.submissionText;
          }
        }
        submissionTexts = { ...submissionTexts };
      }
    } catch (cause) {
      if (id === courseId) assignmentsError = apiErrorMessage(cause, 'Could not load assignments.');
    } finally {
      loadingAssignments = false;
    }
  }

  async function handleSubmitAssignment(assignmentId: string) {
    const text = (submissionTexts[assignmentId] ?? '').trim();
    const assetId = submissionAssetIds[assignmentId];
    if ((!text && !assetId) || submittingAssignmentId || !courseId || !canParticipate) return;
    submittingAssignmentId = assignmentId;
    assignmentNotice = '';
    assignmentsError = '';
    try {
      await coursesApi.submitAssignment(courseId, assignmentId, {
        submissionText: text,
        assetId
      });
      assignmentNotice = 'Assignment submitted successfully!';
      await loadAssignments();
    } catch (cause) {
      assignmentsError = apiErrorMessage(cause, 'Failed to submit assignment. Please try again.');
    } finally {
      submittingAssignmentId = null;
    }
  }

  $: if (canParticipate && outlineTab === 'discussions' && !discussionsLoaded && !loadingDiscussions) {
    void loadDiscussions();
  }
  $: if (canParticipate && outlineTab === 'sessions' && !sessionsLoaded && !loadingSessions) {
    void loadLiveSessions();
  }
  $: if (canParticipate && outlineTab === 'assignments' && !assignmentsLoaded && !loadingAssignments) {
    void loadAssignments();
  }
</script>

<svelte:head><title>{course?.title || 'Course'} | Witforge</title></svelte:head>

{#if courseLoading || courseLoadError || !course}
  <div class="p-8 max-w-3xl mx-auto text-center py-20">
    <BookOpen class="w-12 h-12 text-ifa-text-muted mx-auto mb-4" />
    <h1 class="text-xl font-bold text-ifa-text-primary mb-2">
      {courseLoading ? 'Loading course…' : courseLoadError ? 'Could not load course' : 'Course not found'}
    </h1>
    <p role={courseLoadError ? 'alert' : undefined} class="text-ifa-text-secondary mb-6">
      {courseLoadError || (courseLoading ? 'Fetching course curriculum from Witforge API.' : "We couldn't find a course with that id.")}
    </p>
    <a
      href="/marketplace"
      class="inline-flex items-center gap-2 px-4 py-2 bg-ifa-pine text-white rounded-lg text-sm font-semibold hover:bg-ifa-pine-light transition"
    >
      <ArrowLeft class="w-4 h-4" />
      <span>Browse courses</span>
    </a>
    {#if courseLoadError}<button type="button" on:click={refreshCourse} class="ml-3 text-sm text-ifa-pine underline">Retry</button>{/if}
  </div>
{:else}
  <div class="p-4 sm:p-6 md:p-8 max-w-[1400px] mx-auto grid grid-cols-1 {canParticipate ? 'lg:grid-cols-[minmax(0,1fr)_22rem]' : ''} gap-6 items-start">
    <!-- Main course column -->
    <div class="min-w-0">

      <!-- Educator Preview Banner -->
      {#if isPreview}
        <div class="mb-4 bg-ifa-pine/10 border border-ifa-pine/20 rounded-2xl p-4 flex flex-wrap items-center justify-between gap-3 shadow-sm">
          <div class="flex items-center gap-2.5 text-xs font-semibold text-ifa-pine">
            <ShieldCheck class="w-4 h-4 shrink-0 text-ifa-pine" />
            <span>Saved Course Preview: Enrollment, submissions, and progress recording are disabled.</span>
          </div>
          <a
            href="/studio"
            class="px-3.5 py-1.5 rounded-xl bg-ifa-pine text-white text-xs font-bold hover:bg-ifa-pine-light transition shadow-sm"
          >
            Return to Studio
          </a>
        </div>
      {/if}

    {#if view.kind === 'lesson' && activeLesson && activeModule && hasContentAccess}
      <LessonView
        lesson={activeLesson}
        moduleTitle={activeModule.title}
        readOnly={!canParticipate}
        completing={completingLessonId === activeLesson.id}
        {completionError}
        hasNext={flatLessons.findIndex((f) => f.lesson.id === activeLesson.id) < flatLessons.length - 1}
        on:back={backToOutline}
        on:complete={(e) => handleComplete(e.detail.lessonId)}
        on:next={() => activeLesson && goNext(activeLesson.id)}
      />
    {:else if view.kind === 'quiz' && activeQuiz && activeModule && hasContentAccess}
      {#key activeQuiz.id}
      <QuizView
        quiz={activeQuiz}
        moduleTitle={activeModule.title}
        readOnly={!canParticipate}
        on:back={backToOutline}
        on:scored={handleScored}
      />
      {/key}
    {:else}
      <!-- Course outline -->
      <div class="space-y-6">
        <a
          href={$session.role === 'Educator' ? '/studio' : $session.authenticated ? '/courses' : '/marketplace'}
          class="inline-flex items-center gap-1.5 text-sm font-semibold text-ifa-text-muted hover:text-ifa-pine transition"
        >
          <ArrowLeft class="w-4 h-4" />
          <span>All courses</span>
        </a>

        <!-- Course header -->
        <div class="bg-ifa-card rounded-3xl border border-ifa-border border-t-4 p-6 md:p-8 shadow-card" style={`border-top-color: ${courseAccent}`}>
          <div class="flex flex-col md:flex-row gap-6">
            {#if course.details?.coverImageUrl || course.details?.thumbnailUrl}
              <img src={course.details.coverImageUrl || course.details.thumbnailUrl} alt="Course cover" class="w-full md:w-40 h-32 object-cover rounded-2xl shrink-0" />
            {:else}<div
              class="w-full md:w-40 h-32 rounded-2xl flex items-center justify-center text-white shrink-0 shadow-soft"
              style={`background: linear-gradient(135deg, ${courseAccent}, ${courseThemeEnd})`}
            >
              <span class="text-3xl font-black tracking-tight">{course.thumbnail}</span>
            </div>
            {/if}
            <div class="flex-1">
              <div class="flex items-start justify-between gap-4">
                <div>
                  <h1 class="text-2xl font-bold text-ifa-text-primary">{course.title}</h1>
                  <p class="text-sm text-ifa-text-secondary mt-1">{course.provider}</p>
                </div>
                <div class="flex items-center gap-2 shrink-0">
                  <button
                    type="button"
                    on:click={() => course && toggleBookmark(course.id)}
                    title="Bookmark"
                    aria-label="Bookmark course for this session"
                    disabled={!canParticipate}
                    class="w-9 h-9 rounded-full bg-ifa-card-muted border border-ifa-border flex items-center justify-center text-ifa-text-secondary hover:text-ifa-accent-purple transition"
                  >
                    {#if course.isBookmarked}
                      <BookmarkCheck class="w-4 h-4 text-ifa-accent-purple" />
                    {:else}
                      <Bookmark class="w-4 h-4" />
                    {/if}
                  </button>
                  <button
                    type="button"
                    on:click={shareCourse}
                    title="Share course"
                    aria-label="Share course"
                    disabled={!course.isPublished}
                    class="w-9 h-9 rounded-full bg-ifa-card-muted border border-ifa-border flex items-center justify-center text-ifa-text-secondary hover:text-ifa-pine transition"
                  >
                    <Share2 class="w-4 h-4" />
                  </button>
                </div>
              </div>
              <p class="text-sm text-ifa-text-secondary mt-3 leading-relaxed">{course.description}</p>

              <div class="flex flex-wrap items-center gap-4 mt-4 text-xs text-ifa-text-muted">
                <span class="flex items-center gap-1"><Clock class="w-3.5 h-3.5" />{course.duration}</span>
                <span class="flex items-center gap-1"><BookOpen class="w-3.5 h-3.5" />{course.modules.length} modules</span>
                <span class="flex items-center gap-1">
                  <ClipboardCheck class="w-3.5 h-3.5" />
                  {course.modules.flatMap((m) => m.lessons).length} lessons
                </span>
                {#if course.details?.level}<span>{course.details.level}</span>{/if}
                {#if course.details?.language}<span>{course.details.language}</span>{/if}
              </div>
              {#if course.details?.learningObjectives?.length}
                <div class="mt-4 text-sm text-ifa-text-secondary"><h2 class="font-semibold text-ifa-text-primary mb-2">What You Will Learn</h2><ul class="list-disc pl-5 space-y-1">{#each course.details.learningObjectives as objective}<li>{objective}</li>{/each}</ul></div>
              {/if}
              {#if course.details?.prerequisites}<p class="mt-3 text-xs text-ifa-text-secondary"><strong>Prerequisites:</strong> {course.details.prerequisites}</p>{/if}

              <!-- Progress bar -->
              {#if canParticipate}<div class="mt-4">
                <div class="flex items-center justify-between text-xs mb-1">
                  <span class="text-ifa-text-secondary font-medium">Progress</span>
                  <span class="font-bold text-ifa-pine">{progress}%</span>
                </div>
                <div class="w-full h-2 rounded-full bg-ifa-bg border border-ifa-border overflow-hidden">
                  <div class="h-full rounded-full bg-emerald-600 transition-all duration-500" style="width: {progress}%"></div>
                </div>
              </div>
              <p class="text-[11px] text-ifa-text-muted mt-2">{course.details?.requirePassingQuizzes ? 'Completion includes lessons and passing every required quiz.' : 'Completion is based on saved lesson progress.'}</p>
              {/if}
              {#if !hasContentAccess && !isPreview}
                <div class="mt-5 rounded-xl border border-ifa-border bg-ifa-card-muted p-4 space-y-3">
                  <p class="text-sm text-ifa-text-secondary">{course.enrollmentStatus === 'PendingApproval' ? 'Your enrollment is awaiting educator approval.' : course.enrollmentStatus === 'Active' ? 'This course is temporarily unavailable while the educator updates it.' : 'Preview the curriculum below. Enroll to access lessons, assessments, and course activities.'}</p>
                  {#if course.enrollmentStatus === 'PendingApproval' || course.enrollmentStatus === 'Active'}
                    <button type="button" on:click={refreshCourse} class="text-xs font-semibold text-ifa-pine underline">Refresh Access Status</button>
                  {:else}
                    <button type="button" on:click={enroll} disabled={enrolling || $session.role === 'Educator'} style={`background-color: ${courseAccent}`} class="px-4 py-2.5 rounded-xl text-white text-xs font-bold inline-flex items-center gap-2 disabled:opacity-50">{#if enrolling}<Loader2 class="w-4 h-4 animate-spin" />{/if}{$session.role === 'Educator' ? 'Learner Account Required' : !$session.authenticated ? 'Sign In to Enroll' : course.details?.isPaid ? `Continue to Checkout (${course.details.currency || 'ETB'} ${course.details.priceAmount})` : course.details?.enrollmentMode === 'ApprovalRequired' ? 'Request Enrollment' : 'Enroll Free'}</button>
                  {/if}
                </div>
              {/if}
              {#if enrollmentNotice}<p role="status" class="mt-3 text-sm text-ifa-text-secondary">{enrollmentNotice}</p>{/if}
              {#if enrollmentError}<p role="alert" class="mt-3 text-sm text-red-700 whitespace-pre-line">{enrollmentError}</p>{/if}
            </div>
          </div>
        </div>

        <!-- Section Navigation Tabs -->
        <div class="flex overflow-x-auto whitespace-nowrap border-b border-ifa-border bg-ifa-card rounded-2xl px-3 border shadow-sm">
          <button
            type="button"
            on:click={() => (outlineTab = 'curriculum')}
            class="py-3 px-4 text-xs font-bold border-b-2 transition flex items-center gap-1.5 {outlineTab === 'curriculum' ? 'border-ifa-pine text-ifa-pine' : 'border-transparent text-ifa-text-secondary hover:text-ifa-text-primary'}"
          >
            <BookOpen class="w-3.5 h-3.5" />
            <span>Curriculum</span>
          </button>
          {#if canParticipate}
          <button
            type="button"
            on:click={() => (outlineTab = 'discussions')}
            class="py-3 px-4 text-xs font-bold border-b-2 transition flex items-center gap-1.5 {outlineTab === 'discussions' ? 'border-ifa-pine text-ifa-pine' : 'border-transparent text-ifa-text-secondary hover:text-ifa-text-primary'}"
          >
            <MessageSquare class="w-3.5 h-3.5" />
            <span>Q&A Discussions</span>
          </button>
          <button
            type="button"
            on:click={() => (outlineTab = 'sessions')}
            class="py-3 px-4 text-xs font-bold border-b-2 transition flex items-center gap-1.5 {outlineTab === 'sessions' ? 'border-ifa-pine text-ifa-pine' : 'border-transparent text-ifa-text-secondary hover:text-ifa-text-primary'}"
          >
            <Video class="w-3.5 h-3.5" />
            <span>Live Sessions</span>
          </button>
          <button
            type="button"
            on:click={() => (outlineTab = 'assignments')}
            class="py-3 px-4 text-xs font-bold border-b-2 transition flex items-center gap-1.5 {outlineTab === 'assignments' ? 'border-ifa-pine text-ifa-pine' : 'border-transparent text-ifa-text-secondary hover:text-ifa-text-primary'}"
          >
            <FileCheck class="w-3.5 h-3.5" />
            <span>Assignments</span>
          </button>
          {/if}
        </div>

        <!-- TAB 1: CURRICULUM -->
        {#if outlineTab === 'curriculum'}
          <div class="space-y-4">
            {#each course.modules as module, mi}
              <div class="bg-ifa-card rounded-2xl border border-ifa-border shadow-card overflow-hidden">
                <div class="px-5 py-4 border-b border-ifa-border flex items-center justify-between gap-4">
                  <div class="flex items-center gap-3 min-w-0">
                    <div class="w-8 h-8 rounded-lg bg-ifa-pine/10 text-ifa-pine flex items-center justify-center text-xs font-bold shrink-0">
                      {mi + 1}
                    </div>
                    <div class="min-w-0">
                      <h3 class="text-sm font-bold text-ifa-text-primary truncate">{module.title}</h3>
                      <p class="text-[11px] text-ifa-text-muted truncate">{module.summary}</p>
                    </div>
                  </div>
                  {#if canParticipate}<span class="text-[11px] font-bold text-ifa-pine shrink-0">{moduleProgress(module)}% lessons</span>{/if}
                </div>

                <!-- Lessons -->
                <div class="divide-y divide-ifa-border-light">
                  {#each module.lessons as lesson}
                    <button
                      type="button"
                      on:click={() => openLesson(module.id, lesson.id)}
                      disabled={!hasContentAccess}
                      class="w-full flex items-center gap-3 px-5 py-3 text-left hover:bg-ifa-card-muted/60 transition group"
                    >
                      {#if lesson.completed && canParticipate}
                        <CheckCircle2 class="w-4 h-4 text-emerald-600 shrink-0" />
                      {:else}
                        <Circle class="w-4 h-4 text-ifa-text-muted shrink-0" />
                      {/if}
                      <div class="flex-1 min-w-0">
                        <p class="text-sm font-semibold text-ifa-text-primary group-hover:text-ifa-pine transition truncate">
                          {lesson.title}
                        </p>
                        <p class="text-[11px] text-ifa-text-muted truncate">{lesson.summary}</p>
                      </div>
                      <span class="text-[11px] text-ifa-text-muted flex items-center gap-1 shrink-0">
                        <Clock class="w-3 h-3" />{lesson.duration}
                      </span>
                      <PlayCircle class="w-4 h-4 text-ifa-text-muted group-hover:text-ifa-pine transition shrink-0" />
                    </button>
                  {/each}

                  {#if module.lessons.length === 0}
                    <div class="px-5 py-4">
                      <p class="text-xs text-ifa-text-secondary mb-3">
                        No saved lessons are available in this module yet. The educator can add content in Studio.
                      </p>
                    </div>
                  {/if}

                  <!-- Quiz rows -->
                  {#each module.quizzes as quiz}
                    <button
                      type="button"
                      on:click={() => openQuiz(module.id, quiz.id)}
                      disabled={!hasContentAccess}
                      class="w-full flex items-center gap-3 px-5 py-3 text-left hover:bg-ifa-card-muted/60 transition group bg-ifa-card-muted/30"
                    >
                      <ClipboardCheck class="w-4 h-4 {quiz.isExam ? 'text-ifa-accent-purple' : 'text-ifa-pine'} shrink-0" />
                      <div class="flex-1 min-w-0">
                        <p class="text-sm font-semibold text-ifa-text-primary group-hover:text-ifa-pine transition">
                          {quiz.title}
                        </p>
                        <p class="text-[11px] text-ifa-text-muted">
                          {quiz.isExam ? 'Module exam' : 'Knowledge check'} · {quiz.questionCount ?? quiz.questions.length} questions
                          {#if canParticipate && quiz.latestAttempt} · last score {quiz.latestAttempt.scorePercentage}% · {quiz.attemptCount} attempt(s){/if}
                        </p>
                      </div>
                      <span class="text-[11px] font-bold {quiz.isExam ? 'text-ifa-accent-purple' : 'text-ifa-pine'} shrink-0">
                        {!hasContentAccess ? 'Enroll to Access' : isPreview ? 'Preview' : quiz.isPassed ? 'Passed' : quiz.isExam ? 'Take Exam' : 'Take Quiz'}
                      </span>
                    </button>
                  {/each}
                </div>
              </div>
            {/each}
            {#if course.modules.length === 0}<p class="rounded-xl border border-ifa-border p-8 text-center text-sm text-ifa-text-secondary">No curriculum has been saved yet.</p>{/if}
          </div>

        <!-- TAB 2: DISCUSSIONS & Q&A -->
        {:else if outlineTab === 'discussions'}
          <div class="space-y-4">
            {#if discussionsError}<p role="alert" class="text-sm text-red-700">{discussionsError} <button type="button" on:click={loadDiscussions} class="underline">Retry Loading</button></p>{/if}
            <!-- Ask Question Composer -->
            <div class="bg-ifa-card rounded-2xl border border-ifa-border p-5 shadow-card space-y-3">
              <h3 class="text-xs font-bold text-ifa-text-primary">Ask a Question / Start a Discussion</h3>
              <input
                type="text"
                aria-label="Discussion title"
                bind:value={newDiscussionTitle}
                placeholder="Question topic (e.g. How to handle connection pooling?)"
                class="w-full px-3.5 py-2 rounded-xl bg-ifa-bg border border-ifa-border text-xs text-ifa-text-primary focus:ring-1 focus:ring-ifa-pine"
              />
              <textarea
                aria-label="Discussion content"
                bind:value={newDiscussionContent}
                rows="3"
                placeholder="Provide details or paste relevant code..."
                class="w-full px-3.5 py-2 rounded-xl bg-ifa-bg border border-ifa-border text-xs text-ifa-text-primary focus:ring-1 focus:ring-ifa-pine resize-y"
              ></textarea>
              <div class="flex justify-end">
                <button
                  type="button"
                  on:click={handlePostDiscussion}
                  disabled={postingDiscussion || !newDiscussionContent.trim()}
                  class="px-4 py-2 rounded-xl bg-ifa-pine text-white text-xs font-bold hover:bg-ifa-pine-light transition flex items-center gap-1.5 disabled:opacity-50 shadow-sm"
                >
                  {#if postingDiscussion}<Loader2 class="w-3.5 h-3.5 animate-spin" />{/if}
                  <Send class="w-3.5 h-3.5" />
                  <span>Post Question</span>
                </button>
              </div>
            </div>

            <!-- Discussions List -->
            {#if loadingDiscussions}
              <div class="flex justify-center py-8"><Loader2 class="w-5 h-5 animate-spin text-ifa-pine" /></div>
            {:else if discussions.length === 0}
              <div class="text-center py-12 bg-ifa-card rounded-2xl border border-ifa-border text-xs text-ifa-text-muted p-6">
                No discussions yet. Ask the first question above!
              </div>
            {:else}
              <div class="space-y-3">
                {#each discussions as d}
                  <div class="bg-ifa-card rounded-2xl border border-ifa-border p-5 shadow-card space-y-3">
                    <div class="flex items-start justify-between gap-3">
                      <div>
                        <h4 class="text-sm font-bold text-ifa-text-primary">{d.title}</h4>
                        <p class="text-xs text-ifa-text-secondary mt-1">{d.content}</p>
                        <p class="text-[10px] text-ifa-text-muted mt-2">
                          By <strong class="text-ifa-text-primary">{d.author?.name || 'Learner'}</strong> · {new Date(d.createdAt).toLocaleDateString()}
                          {#if d.isInstructorReply}
                            <span class="ml-2 px-2 py-0.5 rounded-full bg-ifa-pine/10 text-ifa-pine font-bold">Instructor</span>
                          {/if}
                        </p>
                      </div>
                    </div>

                    <!-- Replies -->
                    {#if d.replies && d.replies.length > 0}
                      <div class="pl-4 border-l-2 border-ifa-border space-y-2 pt-2">
                        {#each d.replies as r}
                          <div class="p-2.5 rounded-xl bg-ifa-card-muted/50 text-xs">
                            <p class="text-ifa-text-primary">{r.content}</p>
                            <p class="text-[10px] text-ifa-text-muted mt-1">
                              {r.author?.name || 'User'} · {new Date(r.createdAt).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })}
                              {#if r.isInstructorReply}
                                <span class="ml-1 px-1.5 py-0.2 rounded bg-emerald-100 text-emerald-800 font-bold">Instructor</span>
                              {/if}
                            </p>
                          </div>
                        {/each}
                      </div>
                    {/if}

                    <!-- Reply input -->
                    <div class="flex gap-2 pt-1">
                      <input
                        type="text"
                        aria-label={`Reply to ${d.title}`}
                        bind:value={replyContents[d.id]}
                        placeholder="Write a reply..."
                        class="flex-1 px-3 py-1.5 rounded-xl bg-ifa-bg border border-ifa-border text-xs text-ifa-text-primary focus:ring-1 focus:ring-ifa-pine"
                      />
                      <button
                        type="button"
                        on:click={() => handlePostReply(d.id)}
                        disabled={postingReplyId === d.id || !(replyContents[d.id] ?? '').trim()}
                        class="px-3 py-1.5 rounded-xl bg-ifa-pine/10 text-ifa-pine text-xs font-semibold hover:bg-ifa-pine/20 transition disabled:opacity-50"
                      >
                        Reply
                      </button>
                    </div>
                  </div>
                {/each}
              </div>
            {/if}
          </div>

        <!-- TAB 3: LIVE SESSIONS -->
        {:else if outlineTab === 'sessions'}
          <div class="space-y-4">
            {#if sessionsError}<p role="alert" class="text-sm text-red-700">{sessionsError} <button type="button" on:click={loadLiveSessions} class="underline">Retry</button></p>{/if}
            {#if loadingSessions}
              <div class="flex justify-center py-8"><Loader2 class="w-5 h-5 animate-spin text-ifa-pine" /></div>
            {:else if liveSessions.length === 0}
              <div class="text-center py-12 bg-ifa-card rounded-2xl border border-ifa-border text-xs text-ifa-text-muted p-6">
                No live classes scheduled for this course currently.
              </div>
            {:else}
              <div class="space-y-3">
                {#each liveSessions as s}
                  <div class="bg-ifa-card rounded-2xl border border-ifa-border p-5 shadow-card flex flex-col sm:flex-row items-start sm:items-center justify-between gap-4">
                    <div>
                      <h4 class="text-sm font-bold text-ifa-text-primary">{s.title}</h4>
                      <p class="text-xs text-ifa-text-secondary mt-1">{s.description || 'Interactive live session with your instructor and cohort.'}</p>
                      <div class="flex items-center gap-3 text-xs text-ifa-text-muted mt-2">
                        <span class="flex items-center gap-1"><Clock class="w-3.5 h-3.5" />{new Date(s.startTime).toLocaleString()}</span>
                        <span>·</span>
                        <span>{s.durationMinutes} mins</span>
                      </div>
                    </div>
                    {#if s.meetingUrl && (s.status === 'Scheduled' || s.status === 'Live')}
                      <a
                        href={s.meetingUrl}
                        target="_blank"
                        rel="noopener noreferrer"
                        class="px-4 py-2 rounded-xl bg-ifa-pine text-white text-xs font-bold hover:bg-ifa-pine-light transition flex items-center gap-1.5 shadow-sm shrink-0"
                      >
                        <Video class="w-4 h-4" />
                        <span>Join Live Session</span>
                        <ExternalLink class="w-3 h-3" />
                      </a>
                    {/if}
                  </div>
                {/each}
              </div>
            {/if}
          </div>

        <!-- TAB 4: ASSIGNMENTS -->
        {:else if outlineTab === 'assignments'}
          <div class="space-y-4">
            {#if assignmentsError}<p role="alert" class="text-sm text-red-700">{assignmentsError} <button type="button" on:click={loadAssignments} class="underline">Retry Loading</button></p>{/if}
            {#if assignmentNotice}
              <p class="rounded-xl border border-emerald-200 bg-emerald-50 p-3 text-xs text-emerald-800 flex items-center gap-2">
                <Check class="w-3.5 h-3.5" />
                {assignmentNotice}
              </p>
            {/if}

            {#if loadingAssignments}
              <div class="flex justify-center py-8"><Loader2 class="w-5 h-5 animate-spin text-ifa-pine" /></div>
            {:else if assignments.length === 0}
              <div class="text-center py-12 bg-ifa-card rounded-2xl border border-ifa-border text-xs text-ifa-text-muted p-6">
                No project assignments posted for this course.
              </div>
            {:else}
              <div class="space-y-4">
                {#each assignments as a}
                  <div class="bg-ifa-card rounded-2xl border border-ifa-border p-5 shadow-card space-y-3">
                    <div class="flex items-start justify-between gap-3">
                      <div>
                        <h4 class="text-sm font-bold text-ifa-text-primary">{a.title}</h4>
                        <p class="text-xs text-ifa-text-secondary mt-1 whitespace-pre-wrap">{a.instructionsMarkdown}</p>
                        <p class="text-[11px] text-ifa-text-muted mt-2">
                          Max Score: {a.maxScore} pts · {a.dueDaysAfterEnrollment > 0 ? `Due: ${a.dueDaysAfterEnrollment} days from enrollment` : 'No deadline'}
                        </p>
                      </div>
                    </div>

                    <!-- Submission Status or Form -->
                    {#if a.mySubmission}
                      <div class="p-3.5 bg-ifa-card-muted/60 rounded-xl border border-ifa-border space-y-1.5 text-xs">
                        <div class="flex items-center justify-between">
                          <span class="font-bold text-ifa-text-primary">Your Submission:</span>
                          <span class="px-2 py-0.5 rounded-full font-bold {a.mySubmission.status === 'Graded' ? 'bg-emerald-100 text-emerald-800' : 'bg-blue-100 text-blue-800'}">
                            {a.mySubmission.status} {a.mySubmission.score !== null ? `(${a.mySubmission.score}/${a.maxScore} pts)` : ''}
                          </span>
                        </div>
                        <p class="text-ifa-text-secondary">{a.mySubmission.submissionText}</p>
                        {#if a.mySubmission.fileUrl}
                          <a href={a.mySubmission.fileUrl} class="inline-flex items-center gap-1 text-xs font-semibold text-ifa-pine hover:underline">
                            <FileCheck class="h-3.5 w-3.5" />{a.mySubmission.fileName ?? 'Download submitted file'}
                          </a>
                        {/if}
                        {#if a.mySubmission.feedback}
                          <p class="text-xs text-ifa-pine font-semibold pt-1">
                            Instructor Feedback: {a.mySubmission.feedback}
                          </p>
                        {/if}
                      </div>
                    {/if}
                    {#if !a.mySubmission || a.mySubmission.status === 'ResubmissionRequested'}
                      <div class="space-y-2 pt-2 border-t border-ifa-border">
                        <label for="submission-{a.id}" class="block text-xs font-semibold text-ifa-text-primary">Submit Your Work / Solution</label>
                        <textarea
                          id="submission-{a.id}"
                          bind:value={submissionTexts[a.id]}
                          rows="3"
                          placeholder="Paste your solution, repository link, or explanation..."
                          class="w-full px-3 py-2 rounded-xl bg-ifa-bg border border-ifa-border text-xs text-ifa-text-primary resize-y"
                        ></textarea>
                        <AssetUploader
                          purpose="AssignmentSubmission"
                          {courseId}
                          accept={a.allowedFileTypes}
                          label="Attach a scanned assignment file"
                          on:complete={(event) => (submissionAssetIds = { ...submissionAssetIds, [a.id]: event.detail.assetId })}
                        />
                        <div class="flex justify-end">
                          <button
                            type="button"
                            on:click={() => handleSubmitAssignment(a.id)}
                            disabled={submittingAssignmentId === a.id || (!(submissionTexts[a.id] ?? '').trim() && !submissionAssetIds[a.id])}
                            class="px-4 py-2 rounded-xl bg-ifa-pine text-white text-xs font-bold hover:bg-ifa-pine-light transition flex items-center gap-1.5 disabled:opacity-50 shadow-sm"
                          >
                            {#if submittingAssignmentId === a.id}<Loader2 class="w-3.5 h-3.5 animate-spin" />{/if}
                            <span>{a.mySubmission ? 'Resubmit Assignment' : 'Submit Assignment'}</span>
                          </button>
                        </div>
                      </div>
                    {/if}
                  </div>
                {/each}
              </div>
            {/if}
          </div>
        {/if}

      </div>
    {/if}
    </div>

    <!-- Course chatbot sidebar -->
    {#if canParticipate}
    <aside class="lg:sticky lg:top-6">
      <CourseChatbot
        courseId={course.id}
        courseTitle={course.title}
        moduleId={activeContextModuleId}
        lessonId={activeLesson?.id ?? null}
        lessonTitle={activeLesson?.title ?? null}
      />
    </aside>
    {/if}
  </div>

  <CourseReviews courseId={course.id} canReview={canParticipate && !course.isOwner} />

  <CourseShareDialog isOpen={shareOpen} courseId={course.id} courseTitle={course.title} on:close={() => (shareOpen = false)} />
{/if}
