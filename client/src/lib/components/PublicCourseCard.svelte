<script lang="ts">
  import { Star, Users, Clock, Bookmark, BookmarkCheck, ArrowRight } from 'lucide-svelte';

  export let course: any;
  export let onEnroll = () => {};
  export let onBookmark = () => {};

  let bookmarked = false;
</script>

<div class="bg-ifa-card rounded-2xl border border-ifa-border overflow-hidden hover:shadow-elevated transition group">
  <!-- Thumbnail -->
  <div class="aspect-video bg-gray-200 relative overflow-hidden">
    <img
      src={course.thumbnail}
      alt={course.title}
      class="w-full h-full object-cover group-hover:scale-105 transition-transform duration-300"
    />
    <div class="absolute top-3 right-3">
      <button
        type="button"
        on:click={() => { bookmarked = !bookmarked; onBookmark(); }}
        class="p-2 bg-white/90 backdrop-blur rounded-lg text-ifa-text-secondary hover:text-ifa-pine transition"
      >
        {#if bookmarked}
          <BookmarkCheck class="w-4 h-4 text-ifa-pine" />
        {:else}
          <Bookmark class="w-4 h-4" />
        {/if}
      </button>
    </div>
    <div class="absolute bottom-3 left-3">
      <span class="px-2 py-1 bg-black/70 text-white text-xs font-semibold rounded-full">
        {course.level}
      </span>
    </div>
  </div>

  <!-- Content -->
  <div class="p-4">
    <h3 class="text-sm font-bold text-ifa-text-primary line-clamp-2 mb-2">{course.title}</h3>
    <p class="text-xs text-ifa-text-secondary mb-3">{course.instructor}</p>

    <!-- Stats -->
    <div class="flex items-center gap-3 text-xs text-ifa-text-muted mb-3">
      <div class="flex items-center gap-1">
        <Star class="w-3 h-3 text-yellow-500 fill-yellow-500" />
        <span class="font-semibold text-ifa-text-primary">{course.rating}</span>
        <span>({course.reviews})</span>
      </div>
      <div class="flex items-center gap-1">
        <Users class="w-3 h-3" />
        <span>{course.enrolled.toLocaleString()}</span>
      </div>
    </div>

    <!-- Meta -->
    <div class="flex items-center gap-2 text-xs text-ifa-text-muted mb-4">
      <div class="flex items-center gap-1">
        <Clock class="w-3 h-3" />
        <span>{course.duration}</span>
      </div>
      <span>•</span>
      <span>{course.modules} modules</span>
    </div>

    <!-- Tags -->
    <div class="flex flex-wrap gap-1 mb-4">
      {#each course.tags.slice(0, 2) as tag}
        <span class="px-2 py-0.5 bg-ifa-card-muted text-ifa-text-secondary text-[10px] font-medium rounded-full">
          {tag}
        </span>
      {/each}
    </div>

    <!-- Action -->
    <button
      type="button"
      on:click={onEnroll}
      class="w-full py-2.5 bg-ifa-pine text-white rounded-xl text-xs font-semibold flex items-center justify-center gap-1.5 hover:bg-emerald-800 transition"
    >
      <span>Enroll Now</span>
      <ArrowRight class="w-3 h-3" />
    </button>
  </div>
</div>
