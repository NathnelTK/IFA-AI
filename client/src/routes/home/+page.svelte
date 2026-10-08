<script lang="ts">
  import { goto } from '$app/navigation';
  import HeroSection from '$lib/components/HeroSection.svelte';
  import ContinueLearningCard from '$lib/components/ContinueLearningCard.svelte';
  import PublicCoursesGrid from '$lib/components/PublicCoursesGrid.svelte';
  import LearningOverviewDonut from '$lib/components/LearningOverviewDonut.svelte';
  import RecommendationsList from '$lib/components/RecommendationsList.svelte';
  import RecentActivityFeed from '$lib/components/RecentActivityFeed.svelte';
  import AiTutorDock from '$lib/components/AiTutorDock.svelte';
  import PeerComparisonView from '$lib/components/PeerComparisonView.svelte';
  import HeroGoalSuggestions from '$lib/components/HeroGoalSuggestions.svelte';
  import CourseShareDialog from '$lib/components/CourseShareDialog.svelte';
  import { activeCourse } from '$lib/stores/dashboardStore';

  let notificationToast: string | null = null;
  let showPeerComparison = false;
  let shareOpen = false;

  function showToast(msg: string) {
    notificationToast = msg;
    setTimeout(() => (notificationToast = null), 3500);
  }

  function handleGoalSubmit(goal: string) {
    showToast(`Personalized course pipeline created for: "${goal}"! Module 1 ready.`);
  }

  function handleContinueLearning() {
    if ($activeCourse) goto(`/courses/${$activeCourse.id}`);
    else showToast('Entering interactive lesson: "Working with REST APIs"');
  }

  function handleShareCourse() {
    shareOpen = true;
  }

  function handleCompareToggle() {
    showPeerComparison = !showPeerComparison;
    if (showPeerComparison) {
      showToast('Opening Peer Progress Comparison with Ermiyas!');
    }
  }

  function handlePublishCourse() {
    showToast('Your course "C# Backend Development" was published to Public Courses!');
  }

  function handleSelectRecommendation(id: string) {
    showToast(`Launching adaptive review module for recommendation [${id}]`);
  }
</script>

<div class="p-6 md:p-8 space-y-6 max-w-[1600px] mx-auto">
  <!-- Interactive Toast Notification -->
  {#if notificationToast}
    <div class="fixed top-5 right-5 z-50 bg-ifa-pine text-white px-4 py-3 rounded-2xl shadow-elevated border border-emerald-400/30 flex items-center gap-3 animate-in fade-in slide-in-from-top-3 duration-300">
      <span class="w-2 h-2 rounded-full bg-emerald-400 animate-ping"></span>
      <span class="text-xs font-semibold">{notificationToast}</span>
    </div>
  {/if}

  <!-- Main Responsive Grid: Center Content (2/3) + Right Analytics (1/3) -->
  <div class="grid grid-cols-1 xl:grid-cols-12 gap-6 items-start">
    <!-- Center / Primary Column (xl:col-span-8) -->
    <div class="xl:col-span-8 space-y-6">
      <!-- Hero Section with AI Assistant & Scoping FSM -->
      <HeroSection onGoalSubmit={handleGoalSubmit} />

      <!-- Goal Suggestions -->
      <HeroGoalSuggestions onSelectGoal={handleGoalSubmit} />

      <!-- Continue Your Learning Card -->
      <ContinueLearningCard
        onContinue={handleContinueLearning}
        onShare={handleShareCourse}
        onCompareToggle={handleCompareToggle}
      />

      <!-- Explore Public Courses Grid -->
      <PublicCoursesGrid onPublishCourse={handlePublishCourse} />
    </div>

    <!-- Right Sidebar Column (xl:col-span-4) -->
    <div class="xl:col-span-4 space-y-5">
      <!-- Donut Chart & Skill Breakdown -->
      <LearningOverviewDonut />

      <!-- Peer Progress Comparison View (Conditional) -->
      {#if showPeerComparison}
        <PeerComparisonView onClose={() => showPeerComparison = false} />
      {/if}

      <!-- IFA Recommends Adaptive Actions -->
      <RecommendationsList onSelectRecommendation={handleSelectRecommendation} />

      <!-- Recent Activity Timeline -->
      <RecentActivityFeed />

      <!-- Docked AI Tutor Glowing Orb Widget -->
      <AiTutorDock />
    </div>
  </div>

  <!-- Course Share Dialog -->
  {#if shareOpen}
    <CourseShareDialog
      isOpen={shareOpen}
      on:close={() => (shareOpen = false)}
      courseTitle={$activeCourse?.title || 'C# Backend Development'}
    />
  {/if}
</div>
