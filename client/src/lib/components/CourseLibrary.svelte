<script lang="ts">
  import { ArrowRight, Bookmark, BookmarkCheck, Clock, Play, CheckCircle } from 'lucide-svelte';

  export let courses: any[] = [];
  export let viewMode = 'grid'; // 'grid' | 'list'
  export let onToggleBookmark = (id: string) => {};
  export let onContinue = (id: string) => {};
</script>

<!-- Grid View -->
{#if viewMode === 'grid'}
  <div class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4 gap-4">
    {#each courses as course}
      <div class="bg-ifa-card rounded-2xl border border-ifa-border p-5 shadow-card hover:shadow-elevated transition group">
        <!-- Header -->
        <div class="flex items-start justify-between mb-3">
          <div class="flex-1">
            <h3 class="text-sm font-bold text-ifa-text-primary line-clamp-2">{course.title}</h3>
            <p class="text-xs text-ifa-text-secondary mt-1">{course.provider}</p>
          </div>
          <button
            type="button"
            on:click={() => onToggleBookmark(course.id)}
            class="text-ifa-text-muted hover:text-ifa-accent-purple transition"
          >
            {#if course.isBookmarked}
              <BookmarkCheck class="w-4 h-4 text-ifa-accent-purple" />
            {:else}
              <Bookmark class="w-4 h-4" />
            {/if}
          </button>
        </div>

        <!-- Status Badge -->
        <div class="flex items-center gap-2 mb-3">
          {#if course.status === 'completed'}
            <div class="flex items-center gap-1.5 px-2 py-1 rounded-full text-[10px] font-semibold text-emerald-600 bg-emerald-100">
              <CheckCircle class="w-3 h-3" />
              <span>Completed</span>
            </div>
          {:else if course.status === 'inProgress'}
            <div class="flex items-center gap-1.5 px-2 py-1 rounded-full text-[10px] font-semibold text-ifa-pine bg-emerald-100">
              <Play class="w-3 h-3" />
              <span>In Progress</span>
            </div>
          {:else}
            <div class="flex items-center gap-1.5 px-2 py-1 rounded-full text-[10px] font-semibold text-ifa-text-muted bg-ifa-card-muted">
              <Clock class="w-3 h-3" />
              <span>Not Started</span>
            </div>
          {/if}
        </div>

        <!-- Progress -->
        {#if course.status === 'inProgress' || course.status === 'completed'}
          <div class="mb-3">
            <div class="flex items-center justify-between text-xs mb-1">
              <span class="text-ifa-text-secondary">{course.moduleInfo}</span>
              <span class="font-semibold text-ifa-pine">{course.progressPercent}%</span>
            </div>
            <div class="w-full h-1.5 bg-ifa-card-muted rounded-full">
              <div
                class="h-full bg-ifa-pine rounded-full transition-all duration-1000"
                style="width: {course.progressPercent}%"
              ></div>
            </div>
          </div>
        {/if}

        <!-- Meta Info -->
        <div class="flex items-center gap-3 text-[10px] text-ifa-text-muted mb-4">
          <div class="flex items-center gap-1">
            <Clock class="w-3 h-3" />
            <span>{course.duration}</span>
          </div>
          <div class="flex items-center gap-1">
            <span>Enrolled {course.enrolledDate}</span>
          </div>
        </div>

        <!-- Action -->
        <button
          type="button"
          on:click={() => onContinue(course.id)}
          class="w-full py-2.5 bg-ifa-pine text-white rounded-xl text-xs font-semibold flex items-center justify-center gap-1.5 hover:bg-emerald-800 transition group-hover:shadow-soft"
        >
          {#if course.status === 'completed'}
            <span>Review Course</span>
          {:else}
            <span>Continue</span>
          {/if}
          <ArrowRight class="w-3 h-3" />
        </button>
      </div>
    {/each}
  </div>
{:else}
  <!-- List View -->
  <div class="space-y-3">
    {#each courses as course}
      <div class="bg-ifa-card rounded-xl border border-ifa-border p-4 shadow-card hover:shadow-elevated transition flex items-center gap-4">
        <!-- Thumbnail -->
        <div class="w-16 h-16 rounded-lg bg-ifa-card-muted flex items-center justify-center text-ifa-text-muted shrink-0">
          <span class="text-xs font-bold">{course.thumbnail}</span>
        </div>

        <!-- Content -->
        <div class="flex-1 min-w-0">
          <div class="flex items-start justify-between mb-1">
            <div>
              <h3 class="text-sm font-bold text-ifa-text-primary">{course.title}</h3>
              <p class="text-xs text-ifa-text-secondary">{course.provider}</p>
            </div>
            <button
              type="button"
              on:click={() => onToggleBookmark(course.id)}
              class="text-ifa-text-muted hover:text-ifa-accent-purple transition shrink-0"
            >
              {#if course.isBookmarked}
                <BookmarkCheck class="w-4 h-4 text-ifa-accent-purple" />
              {:else}
                <Bookmark class="w-4 h-4" />
              {/if}
            </button>
          </div>

          <div class="flex items-center gap-4 text-xs">
            <div class="flex items-center gap-1.5 text-ifa-text-secondary">
              {#if course.status === 'completed'}
                <CheckCircle class="w-3 h-3" />
              {:else if course.status === 'inProgress'}
                <Play class="w-3 h-3" />
              {:else}
                <Clock class="w-3 h-3" />
              {/if}
              <span>{course.moduleInfo}</span>
            </div>
            <div class="flex items-center gap-1 text-ifa-text-muted">
              <Clock class="w-3 h-3" />
              <span>{course.duration}</span>
            </div>
          </div>

          {#if course.status === 'inProgress' || course.status === 'completed'}
            <div class="mt-2">
              <div class="flex items-center justify-between text-xs mb-1">
                <span class="text-ifa-text-secondary">Progress</span>
                <span class="font-semibold text-ifa-pine">{course.progressPercent}%</span>
              </div>
              <div class="w-full h-1.5 bg-ifa-card-muted rounded-full">
                <div
                  class="h-full bg-ifa-pine rounded-full transition-all duration-1000"
                  style="width: {course.progressPercent}%"
                ></div>
              </div>
            </div>
          {/if}
        </div>

        <!-- Action -->
        <button
          type="button"
          on:click={() => onContinue(course.id)}
          class="px-4 py-2 bg-ifa-pine text-white rounded-lg text-xs font-semibold flex items-center gap-1.5 hover:bg-emerald-800 transition shrink-0"
        >
          {#if course.status === 'completed'}
            <span>Review</span>
          {:else}
            <span>Continue</span>
          {/if}
          <ArrowRight class="w-3 h-3" />
        </button>
      </div>
    {/each}
  </div>
{/if}
