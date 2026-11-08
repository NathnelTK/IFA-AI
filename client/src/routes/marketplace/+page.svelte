<script lang="ts">
  import { onMount } from 'svelte';
  import { page } from '$app/stores';
  import { goto } from '$app/navigation';
  import { Search, Filter, Grid, List, BookOpen, Star, Users, Clock, TrendingUp, BadgeCheck, Loader2, AlertCircle } from 'lucide-svelte';
  import PublicCourseCard from '$lib/components/PublicCourseCard.svelte';
  import MarketplaceFilters from '$lib/components/MarketplaceFilters.svelte';
  import { apiErrorMessage, coursesApi, paymentsApi, type MarketplaceCourseDto } from '$lib/api';
  import { resolveCover } from '$lib/utils/cover';
  import { session } from '$lib/stores/sessionStore';

  interface MarketplaceCard {
    id: string;
    title: string;
    instructor: string;
    rating: number;
    reviews: number;
    enrolled?: number;
    modules: number;
    duration: string;
    level: string;
    category: string;
    tags: string[];
    thumbnail: string;
    isPublished: boolean;
    shareCode?: string;
    isPaid?: boolean;
    priceAmount?: number | string | null;
    currency?: string | null;
    isOwner?: boolean;
    enrollmentStatus?: string | null;
    enrollmentMode?: string | null;
  }

  let searchQuery = '';
  let viewMode = 'grid';
  let activeFilter = 'all';
  let publicCourses: MarketplaceCard[] = [];
  let loading = true;
  let error = '';
  let notice = '';
  let enrollingId: string | null = null;
  // Chapa checkout returns to /marketplace?tx_ref=… — verify on arrival.
  let paymentState: 'verifying' | 'success' | 'pending' | 'failed' | null = null;
  let paymentMessage = '';

  $: categories = [{ id: 'all', name: 'All Courses' }, ...Array.from(new Set(publicCourses.map((course) => course.category).filter(Boolean))).map((category) => ({ id: category, name: category }))];

  async function handleEnroll(course: MarketplaceCard) {
    if (enrollingId) return;
    // Anonymous visitors go to sign-in first and come straight back here.
    if (!$session.authenticated) {
      await goto(`/login?redirect=${encodeURIComponent(`/courses/${course.id}`)}`);
      return;
    }
    if (course.isOwner || $session.role === 'Educator') {
      await goto(`/courses/${course.id}${course.isOwner ? '?preview=1' : ''}`);
      return;
    }

    enrollingId = course.id;
    error = ''; notice = '';
    try {
      const result = await coursesApi.enroll(course.id);
      if (result.paymentRequired) {
        // Server insists on payment even if the flag was missing client-side.
        const payment = await paymentsApi.initialize(course.id, `${window.location.origin}/marketplace`);
        if (payment.enrollmentStatus === 'PendingApproval') {
          notice = payment.message || 'Payment is confirmed and educator approval is pending.';
          return;
        }
        if (!payment.checkoutUrl) throw new Error('The payment provider did not return a checkout URL.');
        window.location.href = payment.checkoutUrl;
        return;
      }
      if (result.status === 'PendingApproval') {
        publicCourses = publicCourses.map((item) => item.id === course.id ? { ...item, enrollmentStatus: result.status } : item);
        notice = `Enrollment requested for "${course.title}". Access begins after educator approval.`;
        return;
      }
      await goto(`/courses/${course.id}`);
    } catch (cause) {
      error = apiErrorMessage(cause, 'Enrollment failed.');
    } finally {
      enrollingId = null;
    }
  }

  async function verifyPayment(txRef: string) {
    paymentState = 'verifying';
    try {
      const result = await paymentsApi.verify(txRef);
      const status = result.status.toLowerCase();
      if (result.enrollmentStatus === 'Active') {
        paymentState = 'success';
        paymentMessage = 'Payment confirmed — you are enrolled. Find the course in My Courses.';
      } else if (status === 'completed' && result.enrollmentStatus === 'PendingApproval') {
        paymentState = 'pending';
        paymentMessage = 'Payment confirmed. Access begins after educator approval.';
      } else if (status === 'pending') {
        paymentState = 'pending';
        paymentMessage = 'Payment is still processing. Your enrollment activates as soon as it settles.';
      } else {
        paymentState = 'failed';
        paymentMessage = 'We could not confirm the payment. Contact support if you were charged.';
      }
    } catch {
      paymentState = 'failed';
      paymentMessage = 'We could not verify the payment right now. Please try again later.';
    }
  }

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

  function toCard(course: MarketplaceCourseDto): MarketplaceCard {
    return {
      id: course.id,
      title: course.title,
      instructor: course.providerName || 'Witforge',
      rating: course.rating,
      reviews: parseCount(course.reviewCount),
      enrolled: course.enrollmentCount,
      modules: course.moduleCount,
      duration: course.estimatedDuration,
      level: course.level || course.targetAudience || 'All levels',
      category: course.category,
      tags: [course.category],
      thumbnail: resolveCover(course.coverImageUrl || course.thumbnailUrl, course.title),
      isPublished: true,
      shareCode: course.shareCode,
      isPaid: course.isPaid,
      priceAmount: course.priceAmount,
      currency: course.currency,
      isOwner: course.isOwner,
      enrollmentStatus: course.enrollmentStatus,
      enrollmentMode: course.enrollmentMode
    };
  }

  onMount(async () => {
    // Returned from the Chapa checkout with a transaction reference?
    const txRef = $page.url.searchParams.get('tx_ref') || $page.url.searchParams.get('trx_ref');
    if (txRef) await verifyPayment(txRef);
    await loadCatalog();
  });

  async function loadCatalog() {
    loading = true; error = '';
    try {
      const list = await coursesApi.marketplace();
      publicCourses = list.map(toCard);
    } catch (cause) {
      error = cause instanceof Error ? cause.message : 'Could not load the public course catalog.';
    } finally {
      loading = false;
    }
  }

  $: filteredCourses = publicCourses.filter(course => {
    const matchesSearch = course.title.toLowerCase().includes(searchQuery.toLowerCase()) ||
                        course.instructor.toLowerCase().includes(searchQuery.toLowerCase()) ||
                        course.tags.some(tag => tag.toLowerCase().includes(searchQuery.toLowerCase()));
    const matchesFilter = activeFilter === 'all' || course.category === activeFilter;
    return matchesSearch && matchesFilter;
  });
</script>

<div class="p-6 md:p-8 space-y-6 max-w-[1600px] mx-auto">
  <!-- Header -->
  <div class="flex flex-wrap items-center justify-between gap-4 mb-6">
    <div>
      <h1 class="text-2xl font-bold text-ifa-text-primary">Course Marketplace</h1>
      <p class="text-sm text-ifa-text-secondary mt-1">Discover courses published by the community</p>
    </div>
    <div class="flex items-center gap-2">
      <div class="relative">
        <Search class="absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-ifa-text-muted" />
        <input
          type="text"
          aria-label="Search marketplace courses"
          bind:value={searchQuery}
          placeholder="Search courses..."
          class="pl-10 pr-4 py-2.5 rounded-xl bg-ifa-card-muted border border-ifa-border text-sm text-ifa-text-primary placeholder-ifa-text-muted focus:outline-none focus:ring-1 focus:ring-ifa-pine w-64"
        />
      </div>
      <button
        type="button"
        aria-label={viewMode === 'grid' ? 'Show list layout' : 'Show grid layout'}
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
  <div class="flex flex-wrap items-center gap-6 text-sm">
    <div class="flex items-center gap-2 text-ifa-text-secondary">
      <BookOpen class="w-4 h-4" />
      <span>{publicCourses.length} courses available</span>
    </div>
    {#if publicCourses.some((course) => typeof course.enrolled === 'number')}<div class="flex items-center gap-2 text-ifa-text-secondary">
      <Users class="w-4 h-4" />
      <span>{publicCourses.reduce((sum, c) => sum + (c.enrolled ?? 0), 0).toLocaleString()} total enrollments</span>
    </div>{/if}
    <div class="flex items-center gap-2 text-emerald-600">
      <TrendingUp class="w-4 h-4" />
      <span>Community powered</span>
    </div>
  </div>

  <!-- Payment return banner (Chapa / Telebirr) -->
  {#if paymentState}
    <div class="flex items-center gap-3 rounded-xl border p-4 text-sm {paymentState === 'success'
      ? 'border-emerald-200 bg-emerald-50 text-emerald-800'
      : paymentState === 'pending'
        ? 'border-amber-200 bg-amber-50 text-amber-800'
        : paymentState === 'verifying'
          ? 'border-ifa-border bg-ifa-card text-ifa-text-secondary'
          : 'border-red-200 bg-red-50 text-red-700'}">
      {#if paymentState === 'verifying'}
        <Loader2 class="w-4 h-4 animate-spin shrink-0" />
      {:else if paymentState === 'success'}
        <BadgeCheck class="w-4 h-4 shrink-0" />
      {:else}
        <AlertCircle class="w-4 h-4 shrink-0" />
      {/if}
      <span>{paymentState === 'verifying' ? 'Verifying your payment…' : paymentMessage}</span>
    </div>
  {/if}

  <!-- Course Grid -->
  {#if error}
    <p role="alert" class="rounded-xl border border-red-200 bg-red-50 p-4 text-sm text-red-700 whitespace-pre-line">{error} <button type="button" on:click={loadCatalog} class="underline">Reload Catalog</button></p>
  {/if}
  {#if notice}<p role="status" class="rounded-xl border border-ifa-border bg-ifa-card p-4 text-sm text-ifa-text-primary">{notice}</p>{/if}
  {#if loading}
    <p class="py-12 text-center text-sm text-ifa-text-secondary">Loading public courses from the Witforge API…</p>
  {:else if filteredCourses.length === 0}
    <div class="text-center py-12">
      <BookOpen class="w-12 h-12 text-ifa-text-muted mx-auto mb-4" />
      <p class="text-ifa-text-secondary">{error ? 'The public course catalog is unavailable.' : 'No public courses found matching your criteria.'}</p>
    </div>
  {:else}
    <div class="grid grid-cols-1 {viewMode === 'grid' ? 'md:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4' : 'md:grid-cols-2'} gap-4">
      {#each filteredCourses as course}
        <PublicCourseCard
          course={course}
          onEnroll={() => handleEnroll(course)}
          busy={enrollingId !== null}
          enrollmentLabel={course.isOwner ? 'Preview Your Course' : $session.role === 'Educator' ? 'View Course' : course.enrollmentStatus === 'Rejected' ? 'Request Rejected' : course.enrollmentStatus === 'PendingApproval' ? 'Awaiting Approval' : course.enrollmentStatus === 'PendingPayment' ? 'Continue Checkout' : course.enrollmentStatus === 'Active' ? 'Continue Learning' : course.enrollmentMode === 'ApprovalRequired' ? 'Request Enrollment' : ''}
        />
      {/each}
    </div>
  {/if}
</div>
