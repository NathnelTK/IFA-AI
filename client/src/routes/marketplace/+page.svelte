<script lang="ts">
  import { onMount } from 'svelte';
  import { Search, Filter, Grid, List, BookOpen, Star, Users, Clock, TrendingUp } from 'lucide-svelte';
  import PublicCourseCard from '$lib/components/PublicCourseCard.svelte';
  import MarketplaceFilters from '$lib/components/MarketplaceFilters.svelte';
  import { coursesApi, type MarketplaceCourseDto } from '$lib/api';

  interface MarketplaceCard {
    id: string;
    title: string;
    instructor: string;
    rating: number;
    reviews: number;
    enrolled: number;
    modules: number;
    duration: string;
    level: string;
    category: string;
    tags: string[];
    thumbnail: string;
    isPublished: boolean;
    shareCode?: string;
  }

  let searchQuery = '';
  let viewMode = 'grid';
  let activeFilter = 'all';

  const demoMarketplace: MarketplaceCard[] = [
    {
      id: 'pub-1',
      title: 'React for Beginners',
      instructor: 'FreeCodeCamp',
      rating: 4.8,
      reviews: 1240,
      enrolled: 15600,
      modules: 6,
      duration: '6 weeks',
      level: 'Beginner',
      category: 'Frontend',
      tags: ['React', 'JavaScript', 'Frontend'],
      thumbnail: 'https://via.placeholder.com/400x225/3B82F6/FFFFFF?text=React',
      isPublished: true
    },
    {
      id: 'pub-2',
      title: 'Advanced Python Data Science',
      instructor: 'DataCamp',
      rating: 4.9,
      reviews: 890,
      enrolled: 8200,
      modules: 8,
      duration: '10 weeks',
      level: 'Advanced',
      category: 'Data Science',
      tags: ['Python', 'Data Science', 'ML'],
      thumbnail: 'https://via.placeholder.com/400x225/E07A5F/FFFFFF?text=Python',
      isPublished: true
    },
    {
      id: 'pub-3',
      title: 'Machine Learning Fundamentals',
      instructor: 'Andrew Ng',
      rating: 4.9,
      reviews: 2500,
      enrolled: 45000,
      modules: 12,
      duration: '12 weeks',
      level: 'Intermediate',
      category: 'AI/ML',
      tags: ['ML', 'AI', 'Python'],
      thumbnail: 'https://via.placeholder.com/400x225/7C5CFC/FFFFFF?text=ML',
      isPublished: true
    },
    {
      id: 'pub-4',
      title: 'Cloud Architecture with AWS',
      instructor: 'AWS Solutions',
      rating: 4.7,
      reviews: 650,
      enrolled: 12000,
      modules: 10,
      duration: '8 weeks',
      level: 'Intermediate',
      category: 'Cloud',
      tags: ['AWS', 'Cloud', 'DevOps'],
      thumbnail: 'https://via.placeholder.com/400x225/F59E0B/FFFFFF?text=AWS',
      isPublished: true
    },
    {
      id: 'pub-5',
      title: 'Docker & Kubernetes Mastery',
      instructor: 'DevOps Academy',
      rating: 4.8,
      reviews: 780,
      enrolled: 9500,
      modules: 9,
      duration: '9 weeks',
      level: 'Advanced',
      category: 'DevOps',
      tags: ['Docker', 'Kubernetes', 'DevOps'],
      thumbnail: 'https://via.placeholder.com/400x225/10B981/FFFFFF?text=K8s',
      isPublished: true
    },
    {
      id: 'pub-6',
      title: 'Web Security & Ethical Hacking',
      instructor: 'CyberSec Pro',
      rating: 4.6,
      reviews: 420,
      enrolled: 6800,
      modules: 7,
      duration: '7 weeks',
      level: 'Intermediate',
      category: 'Security',
      tags: ['Security', 'Hacking', 'Network'],
      thumbnail: 'https://via.placeholder.com/400x225/EF4444/FFFFFF?text=Security',
      isPublished: true
    }
  ];

  const categories = [
    { id: 'all', name: 'All Courses' },
    { id: 'frontend', name: 'Frontend' },
    { id: 'backend', name: 'Backend' },
    { id: 'data-science', name: 'Data Science' },
    { id: 'ai-ml', name: 'AI/ML' },
    { id: 'cloud', name: 'Cloud' },
    { id: 'devops', name: 'DevOps' },
    { id: 'security', name: 'Security' }
  ];

  function handleEnroll(course: MarketplaceCard) {
    if (!course.shareCode) {
      console.info(`No share code for "${course.title}"; open it from your courses to enroll.`);
      return;
    }
    void coursesApi.join(course.shareCode).catch((error) => {
      console.error('Enrollment failed:', error);
    });
  }

  function handleBookmark(courseId: string) {
    console.log('Bookmarking course:', courseId);
  }

  let publicCourses: MarketplaceCard[] = demoMarketplace;

  function parseCount(value: string | undefined): number {
    if (!value) return 0;
    const match = value.trim().toLowerCase().match(/^([\d.]+)\s*([km]?)/);
    if (!match) return 0;
    const amount = parseFloat(match[1]);
    if (Number.isNaN(amount)) return 0;
    if (match[2] === 'k') return Math.round(amount * 1000);
    if (match[2] === 'm') return Math.round(amount * 1_000_000);
    return Math.round(amount);
  }

  function placeholderThumbnail(title: string): string {
    const label =
      title
        .split(/\s+/)
        .slice(0, 2)
        .map((word) => word[0])
        .join('')
        .toUpperCase() || 'IFA';
    return `https://via.placeholder.com/400x225/1B3D2F/FFFFFF?text=${encodeURIComponent(label)}`;
  }

  function toCard(course: MarketplaceCourseDto): MarketplaceCard {
    return {
      id: course.id,
      title: course.title,
      instructor: course.providerName || 'IFA AI',
      rating: course.rating,
      reviews: parseCount(course.reviewCount),
      enrolled: 0,
      modules: course.moduleCount,
      duration: course.estimatedDuration,
      level: course.targetAudience || 'All levels',
      category: course.category,
      tags: [course.category],
      thumbnail: course.thumbnailUrl || placeholderThumbnail(course.title),
      isPublished: true,
      shareCode: course.shareCode
    };
  }

  onMount(async () => {
    try {
      const list = await coursesApi.marketplace();
      if (list && list.length > 0) publicCourses = list.map(toCard);
    } catch {
      /* keep the demo marketplace when the API is unavailable */
    }
  });

  $: filteredCourses = publicCourses.filter(course => {
    const matchesSearch = course.title.toLowerCase().includes(searchQuery.toLowerCase()) ||
                        course.instructor.toLowerCase().includes(searchQuery.toLowerCase()) ||
                        course.tags.some(tag => tag.toLowerCase().includes(searchQuery.toLowerCase()));
    const matchesFilter = activeFilter === 'all' || course.category.toLowerCase() === activeFilter.replace('-', '');
    return matchesSearch && matchesFilter;
  });
</script>

<div class="p-6 md:p-8 space-y-6 max-w-[1600px] mx-auto">
  <!-- Header -->
  <div class="flex items-center justify-between mb-6">
    <div>
      <h1 class="text-2xl font-bold text-ifa-text-primary">Course Marketplace</h1>
      <p class="text-sm text-ifa-text-secondary mt-1">Discover courses published by the community</p>
    </div>
    <div class="flex items-center gap-2">
      <div class="relative">
        <Search class="absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-ifa-text-muted" />
        <input
          type="text"
          bind:value={searchQuery}
          placeholder="Search courses..."
          class="pl-10 pr-4 py-2.5 rounded-xl bg-ifa-card-muted border border-ifa-border text-sm text-ifa-text-primary placeholder-ifa-text-muted focus:outline-none focus:ring-1 focus:ring-ifa-pine w-64"
        />
      </div>
      <button
        type="button"
        on:click={() => viewMode = viewMode === 'grid' ? 'list' : 'grid'}
        class="p-2.5 rounded-lg bg-ifa-card-muted border border-ifa-border text-ifa-text-secondary hover:text-ifa-pine transition"
      >
        {#if viewMode === 'grid'}
          <List class="w-5 h-5" />
        {:else}
          <Grid class="w-5 h-5" />
        {/if}
      </button>
    </div>
  </div>

  <!-- Categories -->
  <MarketplaceFilters
    categories={categories}
    activeFilter={activeFilter}
    onFilterChange={(filter) => activeFilter = filter}
  />

  <!-- Stats -->
  <div class="flex items-center gap-6 text-sm">
    <div class="flex items-center gap-2 text-ifa-text-secondary">
      <BookOpen class="w-4 h-4" />
      <span>{publicCourses.length} courses available</span>
    </div>
    <div class="flex items-center gap-2 text-ifa-text-secondary">
      <Users class="w-4 h-4" />
      <span>{publicCourses.reduce((sum, c) => sum + c.enrolled, 0).toLocaleString()} total enrollments</span>
    </div>
    <div class="flex items-center gap-2 text-emerald-600">
      <TrendingUp class="w-4 h-4" />
      <span>Community powered</span>
    </div>
  </div>

  <!-- Course Grid -->
  {#if filteredCourses.length === 0}
    <div class="text-center py-12">
      <BookOpen class="w-12 h-12 text-ifa-text-muted mx-auto mb-4" />
      <p class="text-ifa-text-secondary">No courses found matching your criteria</p>
    </div>
  {:else}
    <div class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4 gap-4">
      {#each filteredCourses as course}
        <PublicCourseCard
          course={course}
          onEnroll={() => handleEnroll(course)}
          onBookmark={() => handleBookmark(course.id)}
        />
      {/each}
    </div>
  {/if}
</div>
