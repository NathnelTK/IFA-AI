<script lang="ts">
  import { onMount } from 'svelte';
  import { Users, CheckCircle2, Globe2, Loader2, GraduationCap, TrendingUp, ShieldAlert } from 'lucide-svelte';
  import { educatorApi, type EducatorDashboardDto, type EducatorEnrollmentDto } from '$lib/api';
  import { session } from '$lib/stores/sessionStore';

  let dashboard: EducatorDashboardDto | null = null;
  let enrollments: EducatorEnrollmentDto[] = [];
  let loading = true;
  let error = '';
  let actingOn: string | null = null;
  let notice = '';

  onMount(async () => {
    try {
      const [dash, pending] = await Promise.all([
        educatorApi.dashboard(),
        educatorApi.enrollments('PendingApproval').catch(() => [])
      ]);
      dashboard = dash;
      enrollments = pending;
    } catch (cause) {
      error = cause instanceof Error ? cause.message : 'Could not load the educator dashboard.';
    } finally {
      loading = false;
    }
  });

  async function handleDecision(enrollmentId: string, approve: boolean) {
    if (actingOn) return;
    actingOn = enrollmentId;
    notice = '';
    try {
      if (approve) await educatorApi.approveEnrollment(enrollmentId);
      else await educatorApi.rejectEnrollment(enrollmentId);
      enrollments = enrollments.filter((e) => e.enrollmentId !== enrollmentId);
      notice = approve ? 'Enrollment approved.' : 'Enrollment rejected.';
      // Refresh the totals so the pending counter stays accurate.
      dashboard = await educatorApi.dashboard().catch(() => dashboard);
    } catch (cause) {
      error = cause instanceof Error ? cause.message : 'Could not process the request.';
    } finally {
      actingOn = null;
    }
  }

  /** Max activity across all courses — used to scale the mini traffic bars. */
  $: maxTraffic = dashboard
    ? Math.max(1, ...dashboard.courses.flatMap((c) => c.traffic.map((t) => t.activity + t.enrollments)))
    : 1;
</script>

<div class="p-6 md:p-8 space-y-6 max-w-[1400px] mx-auto">
  {#if $session.role !== 'Educator'}
    <div class="flex items-center gap-3 rounded-xl border border-amber-200 bg-amber-50 p-5 text-sm text-amber-800">
      <ShieldAlert class="w-5 h-5 shrink-0" />
      <span>The Educator Studio is only available to Educator accounts.</span>
    </div>
  {:else}
    <div>
      <h1 class="text-2xl font-bold text-ifa-text-primary">Studio Dashboard</h1>
      <p class="text-sm text-ifa-text-secondary mt-1">Enrollments, progress and pending approvals across your courses.</p>
    </div>

    {#if error}
      <p role="alert" class="rounded-xl border border-red-200 bg-red-50 p-4 text-sm text-red-700">{error}</p>
    {/if}
    {#if notice}
      <p class="rounded-xl border border-emerald-200 bg-emerald-50 p-4 text-sm text-emerald-800">{notice}</p>
    {/if}

    {#if loading}
      <div class="flex items-center justify-center py-16">
        <Loader2 class="w-6 h-6 text-ifa-pine animate-spin" />
      </div>
    {:else if dashboard}
      <!-- Totals cards -->
      <div class="grid grid-cols-1 sm:grid-cols-3 gap-4">
        <div class="bg-ifa-card rounded-2xl border border-ifa-border p-5 shadow-card flex items-center gap-4">
          <span class="w-10 h-10 rounded-xl bg-emerald-100 text-emerald-700 flex items-center justify-center">
            <Users class="w-5 h-5" />
          </span>
          <div>
            <p class="text-2xl font-extrabold text-ifa-text-primary">{dashboard.totals.enrollments}</p>
            <p class="text-xs text-ifa-text-secondary">Total enrollments</p>
          </div>
        </div>
        <div class="bg-ifa-card rounded-2xl border border-ifa-border p-5 shadow-card flex items-center gap-4">
          <span class="w-10 h-10 rounded-xl bg-amber-100 text-amber-700 flex items-center justify-center">
            <CheckCircle2 class="w-5 h-5" />
          </span>
          <div>
            <p class="text-2xl font-extrabold text-ifa-text-primary">{dashboard.totals.pendingApprovals}</p>
            <p class="text-xs text-ifa-text-secondary">Pending approvals</p>
          </div>
        </div>
        <div class="bg-ifa-card rounded-2xl border border-ifa-border p-5 shadow-card flex items-center gap-4">
          <span class="w-10 h-10 rounded-xl bg-blue-100 text-blue-700 flex items-center justify-center">
            <Globe2 class="w-5 h-5" />
          </span>
          <div>
            <p class="text-2xl font-extrabold text-ifa-text-primary">{dashboard.totals.publishedCourses}</p>
            <p class="text-xs text-ifa-text-secondary">Published courses</p>
          </div>
        </div>
      </div>

      <!-- Per-course rows -->
      <section class="space-y-3">
        <h2 class="text-sm font-bold text-ifa-text-primary">Your courses</h2>
        {#if dashboard.courses.length === 0}
          <div class="text-center py-12 bg-ifa-card rounded-2xl border border-ifa-border">
            <GraduationCap class="w-10 h-10 text-ifa-text-muted mx-auto mb-3" />
            <p class="text-sm text-ifa-text-secondary">No courses yet. Approve a blueprint in a Studio project to create one.</p>
          </div>
        {:else}
          {#each dashboard.courses as course (course.courseId)}
            <div class="bg-ifa-card rounded-2xl border border-ifa-border p-5 shadow-card">
              <div class="flex flex-wrap items-start justify-between gap-3 mb-3">
                <div>
                  <h3 class="text-sm font-bold text-ifa-text-primary">{course.title}</h3>
                  <div class="flex flex-wrap items-center gap-2 mt-1 text-[11px]">
                    <span class="px-2 py-0.5 rounded-full font-semibold {course.isPublic ? 'bg-emerald-100 text-emerald-800' : 'bg-ifa-card-muted text-ifa-text-secondary border border-ifa-border'}">
                      {course.isPublic ? 'Published' : 'Private'}
                    </span>
                    {#if course.isPaid}
                      <span class="px-2 py-0.5 rounded-full font-semibold bg-ifa-pine text-white">
                        ETB {Number(course.price ?? 0).toLocaleString()}
                      </span>
                    {:else}
                      <span class="px-2 py-0.5 rounded-full font-semibold bg-ifa-card-muted text-ifa-text-secondary border border-ifa-border">Free</span>
                    {/if}
                    {#if course.pendingApprovals > 0}
                      <span class="px-2 py-0.5 rounded-full font-semibold bg-amber-100 text-amber-800">
                        {course.pendingApprovals} pending
                      </span>
                    {/if}
                  </div>
                </div>
                <div class="flex items-center gap-5 text-xs text-ifa-text-secondary">
                  <span><strong class="text-ifa-text-primary">{course.enrollments}</strong> enrolled</span>
                  <span><strong class="text-ifa-text-primary">{course.activeEnrollments}</strong> active</span>
                </div>
              </div>

              <div class="grid grid-cols-1 lg:grid-cols-[1fr_auto] gap-4 items-center">
                <!-- Avg progress -->
                <div>
                  <div class="flex items-center justify-between text-[11px] text-ifa-text-secondary mb-1">
                    <span class="flex items-center gap-1"><TrendingUp class="w-3 h-3" /> Avg progress</span>
                    <span class="font-bold text-ifa-pine">{Math.round(course.avgProgress)}%</span>
                  </div>
                  <div class="h-2 rounded-full bg-ifa-card-muted overflow-hidden">
                    <div class="h-full rounded-full bg-ifa-pine transition-all" style="width: {Math.min(100, Math.max(0, course.avgProgress))}%"></div>
                  </div>
                </div>

                <!-- Mini traffic chart (simple SVG bars) -->
                <div class="flex items-end gap-1 h-12 min-w-[160px]">
                  {#each course.traffic as point (point.date)}
                    <div class="flex-1 flex flex-col items-center gap-0.5" title="{point.date}: {point.enrollments} enrollments · {point.activity} activity">
                      <div class="w-full rounded-t bg-ifa-pine/80" style="height: {Math.max(4, ((point.activity + point.enrollments) / maxTraffic) * 100)}%"></div>
                    </div>
                  {/each}
                </div>
              </div>
            </div>
          {/each}
        {/if}
      </section>

      <!-- Pending approvals queue -->
      <section class="space-y-3">
        <h2 class="text-sm font-bold text-ifa-text-primary">Pending approvals</h2>
        {#if enrollments.length === 0}
          <p class="text-xs text-ifa-text-muted py-3">No enrollments awaiting approval.</p>
        {:else}
          <div class="bg-ifa-card rounded-2xl border border-ifa-border divide-y divide-ifa-border-light overflow-hidden">
            {#each enrollments as enrollment (enrollment.enrollmentId)}
              <div class="flex flex-wrap items-center justify-between gap-3 p-4">
                <div class="min-w-0">
                  <p class="text-sm font-semibold text-ifa-text-primary truncate">{enrollment.learnerName}</p>
                  <p class="text-[11px] text-ifa-text-muted truncate">{enrollment.courseTitle} · requested {new Date(enrollment.enrolledAt).toLocaleDateString()}</p>
                </div>
                <div class="flex items-center gap-2">
                  <button
                    type="button"
                    on:click={() => handleDecision(enrollment.enrollmentId, true)}
                    disabled={actingOn !== null}
                    class="px-3 py-1.5 rounded-lg bg-ifa-pine text-white text-xs font-semibold hover:bg-ifa-pine-light transition disabled:opacity-50"
                  >
                    {#if actingOn === enrollment.enrollmentId}
                      <Loader2 class="w-3 h-3 animate-spin" />
                    {:else}
                      Approve
                    {/if}
                  </button>
                  <button
                    type="button"
                    on:click={() => handleDecision(enrollment.enrollmentId, false)}
                    disabled={actingOn !== null}
                    class="px-3 py-1.5 rounded-lg bg-ifa-card-muted border border-ifa-border text-ifa-text-secondary text-xs font-semibold hover:text-red-700 hover:border-red-200 transition disabled:opacity-50"
                  >
                    Reject
                  </button>
                </div>
              </div>
            {/each}
          </div>
        {/if}
      </section>
    {/if}
  {/if}
</div>
