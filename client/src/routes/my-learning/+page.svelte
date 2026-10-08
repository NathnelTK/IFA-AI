<script lang="ts">
  import { ArrowRight, BookOpen, Pause, Play, CheckCircle, TrendingUp, Timer } from 'lucide-svelte';
  import ActiveCourseCard from '$lib/components/ActiveCourseCard.svelte';
  import CourseProgressCard from '$lib/components/CourseProgressCard.svelte';
  import CompletedCourseCard from '$lib/components/CompletedCourseCard.svelte';

  let activeTab = 'active'; // 'active' | 'completed' | 'paused'

  const activeCourses = [
    {
      id: 'csharp-backend',
      title: 'C# Backend Development',
      moduleInfo: 'Module 3 of 6',
      progressPercent: 78,
      nextLessonTitle: 'Working with REST APIs',
      nextLessonSummary: 'Learn how to build and consume REST APIs in ASP.NET Core.',
      nextLessonDuration: '12 min',
      courseThumbnail: 'C#',
      jitStatus: 'Module 3 Ready • Module 4 queued'
    },
    {
      id: 'python-fundamentals',
      title: 'Python Fundamentals',
      moduleInfo: 'Module 2 of 8',
      progressPercent: 32,
      nextLessonTitle: 'Data Structures',
      nextLessonSummary: 'Understanding lists, dictionaries, and sets in Python.',
      nextLessonDuration: '15 min',
      courseThumbnail: 'Python',
      jitStatus: 'Module 2 Ready • Module 3 blueprint'
    }
  ];

  const completedCourses = [
    {
      id: 'aspnet-fundamentals',
      title: 'ASP.NET Core Fundamentals',
      completedDate: '2 weeks ago',
      finalScore: 92,
      certificateUrl: '/certificates/aspnet-fundamentals'
    }
  ];

  const pausedCourses = [
    {
      id: 'sql-developers',
      title: 'SQL for Developers',
      moduleInfo: 'Module 1 of 4',
      progressPercent: 12,
      pausedDate: '1 week ago',
      courseThumbnail: 'SQL'
    }
  ];

  function handleContinueCourse(courseId: string) {
    console.log('Continuing course:', courseId);
  }

  function handleResumeCourse(courseId: string) {
    console.log('Resuming course:', courseId);
  }

  function handleViewCertificate(courseId: string) {
    console.log('Viewing certificate for:', courseId);
  }
</script>

<div class="p-6 md:p-8 space-y-6 max-w-[1600px] mx-auto">
  <!-- Header -->
  <div class="flex items-center justify-between">
    <div>
      <h1 class="text-2xl font-bold text-ifa-text-primary">My Learning</h1>
      <p class="text-sm text-ifa-text-secondary mt-1">Track your progress and continue your learning journey</p>
    </div>
    <div class="flex items-center gap-2">
      <div class="flex items-center gap-1 text-ifa-text-secondary text-sm">
        <Timer class="w-4 h-4" />
        <span>12h 30m this week</span>
      </div>
    </div>
  </div>

  <!-- Tab Navigation -->
  <div class="flex items-center gap-1 bg-ifa-card-muted rounded-xl p-1">
    <button
      type="button"
      on:click={() => activeTab = 'active'}
      class="flex-1 py-2 px-4 rounded-lg text-sm font-semibold transition {activeTab === 'active'
        ? 'bg-white text-ifa-pine shadow-soft'
        : 'text-ifa-text-secondary hover:text-ifa-pine'}"
    >
      Active ({activeCourses.length})
    </button>
    <button
      type="button"
      on:click={() => activeTab = 'completed'}
      class="flex-1 py-2 px-4 rounded-lg text-sm font-semibold transition {activeTab === 'completed'
        ? 'bg-white text-ifa-pine shadow-soft'
        : 'text-ifa-text-secondary hover:text-ifa-pine'}"
    >
      Completed ({completedCourses.length})
    </button>
    <button
      type="button"
      on:click={() => activeTab = 'paused'}
      class="flex-1 py-2 px-4 rounded-lg text-sm font-semibold transition {activeTab === 'paused'
        ? 'bg-white text-ifa-pine shadow-soft'
        : 'text-ifa-text-secondary hover:text-ifa-pine'}"
    >
      Paused ({pausedCourses.length})
    </button>
  </div>

  <!-- Active Courses Section -->
  {#if activeTab === 'active'}
    <div class="space-y-6">
      <!-- Continue Learning Card -->
      {#if activeCourses.length > 0}
        <ActiveCourseCard
          course={activeCourses[0]}
          onContinue={() => handleContinueCourse(activeCourses[0].id)}
        />
      {/if}

      <!-- Other Active Courses -->
      {#if activeCourses.length > 1}
        <div>
          <h2 class="text-lg font-bold text-ifa-text-primary mb-4">Your Courses</h2>
          <div class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
            {#each activeCourses.slice(1) as course}
              <CourseProgressCard
                course={course}
                onContinue={() => handleContinueCourse(course.id)}
              />
            {/each}
          </div>
        </div>
      {/if}
    </div>
  {/if}

  <!-- Completed Courses Section -->
  {#if activeTab === 'completed'}
    <div class="space-y-4">
      {#if completedCourses.length > 0}
        <div class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
          {#each completedCourses as course}
            <CompletedCourseCard
              course={course}
              onViewCertificate={() => handleViewCertificate(course.id)}
            />
          {/each}
        </div>
      {:else}
        <div class="text-center py-12">
          <CheckCircle class="w-12 h-12 text-ifa-text-muted mx-auto mb-4" />
          <p class="text-ifa-text-secondary">No completed courses yet. Keep learning!</p>
        </div>
      {/if}
    </div>
  {/if}

  <!-- Paused Courses Section -->
  {#if activeTab === 'paused'}
    <div class="space-y-4">
      {#if pausedCourses.length > 0}
        <div class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
          {#each pausedCourses as course}
            <CourseProgressCard
              course={course}
              onContinue={() => handleResumeCourse(course.id)}
              isPaused={true}
            />
          {/each}
        </div>
      {:else}
        <div class="text-center py-12">
          <Pause class="w-12 h-12 text-ifa-text-muted mx-auto mb-4" />
          <p class="text-ifa-text-secondary">No paused courses. You're on track!</p>
        </div>
      {/if}
    </div>
  {/if}

  <!-- Learning Stats -->
  <div class="bg-ifa-card rounded-2xl border border-ifa-border p-6">
    <h2 class="text-lg font-bold text-ifa-text-primary mb-4">Learning Statistics</h2>
    <div class="grid grid-cols-1 md:grid-cols-4 gap-4">
      <div class="bg-ifa-card-muted rounded-xl p-4">
        <div class="flex items-center gap-2 text-ifa-text-secondary text-sm mb-2">
          <BookOpen class="w-4 h-4" />
          <span>Total Courses</span>
        </div>
        <p class="text-2xl font-bold text-ifa-pine">{activeCourses.length + completedCourses.length + pausedCourses.length}</p>
      </div>
      <div class="bg-ifa-card-muted rounded-xl p-4">
        <div class="flex items-center gap-2 text-ifa-text-secondary text-sm mb-2">
          <TrendingUp class="w-4 h-4" />
          <span>Completion Rate</span>
        </div>
        <p class="text-2xl font-bold text-ifa-pine">
          {Math.round((completedCourses.length / (activeCourses.length + completedCourses.length + pausedCourses.length)) * 100)}%
        </p>
      </div>
      <div class="bg-ifa-card-muted rounded-xl p-4">
        <div class="flex items-center gap-2 text-ifa-text-secondary text-sm mb-2">
          <Timer class="w-4 h-4" />
          <span>Study Time</span>
        </div>
        <p class="text-2xl font-bold text-ifa-pine">12h 30m</p>
      </div>
      <div class="bg-ifa-card-muted rounded-xl p-4">
        <div class="flex items-center gap-2 text-ifa-text-secondary text-sm mb-2">
          <CheckCircle class="w-4 h-4" />
          <span>Modules Done</span>
        </div>
        <p class="text-2xl font-bold text-ifa-pine">8</p>
      </div>
    </div>
  </div>
</div>
