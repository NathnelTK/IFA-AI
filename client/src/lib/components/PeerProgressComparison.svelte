<script lang="ts">
  import { Users, TrendingUp, Award, BarChart3, BookOpen, Trophy, Star, Zap } from 'lucide-svelte';

  const peerData = {
    currentLearner: {
      name: 'Nathnel',
      avatar: 'N',
      overallProgress: 72,
      coursesCompleted: 3,
      skills: {
        'C#': 84,
        'Databases': 61,
        'APIs': 55,
        'Authentication': 45,
        'Testing': 32
      }
    },
    peers: [
      {
        name: 'Ermiyas',
        avatar: 'E',
        overallProgress: 61,
        coursesCompleted: 2,
        skills: {
          'C#': 72,
          'Databases': 58,
          'APIs': 48,
          'Authentication': 52,
          'Testing': 38
        }
      },
      {
        name: 'Negede',
        avatar: 'N',
        overallProgress: 85,
        coursesCompleted: 4,
        skills: {
          'C#': 90,
          'Databases': 75,
          'APIs': 68,
          'Authentication': 60,
          'Testing': 45
        }
      }
    ]
  };

  function getProgressColor(value: number) {
    if (value >= 80) return 'bg-emerald-500';
    if (value >= 60) return 'bg-blue-500';
    if (value >= 40) return 'bg-yellow-500';
    return 'bg-red-500';
  }
</script>

<div class="bg-ifa-card rounded-2xl border border-ifa-border p-6">
  <h3 class="text-sm font-bold text-ifa-text-primary mb-4 flex items-center gap-2">
    <Users class="w-5 h-5 text-ifa-pine" />
    Peer Progress Comparison
  </h3>

  <!-- Your Progress -->
  <div class="mb-6 p-4 bg-gradient-to-r from-ifa-pine/10 to-emerald-50 rounded-xl">
    <div class="flex items-center gap-3 mb-3">
      <div class="w-10 h-10 rounded-full bg-ifa-pine flex items-center justify-center text-white font-bold">
        {peerData.currentLearner.avatar}
      </div>
      <div>
        <p class="text-sm font-bold text-ifa-text-primary">{peerData.currentLearner.name}</p>
        <p class="text-xs text-ifa-text-secondary">You</p>
      </div>
      <div class="ml-auto text-right">
        <p class="text-2xl font-black text-ifa-pine">{peerData.currentLearner.overallProgress}%</p>
        <p class="text-xs text-ifa-text-muted">Overall</p>
      </div>
    </div>
    <div class="flex items-center gap-2 text-xs text-ifa-text-secondary">
      <Award class="w-3 h-3" />
      <span>{peerData.currentLearner.coursesCompleted} courses completed</span>
    </div>
  </div>

  <!-- Peers -->
  <div class="space-y-3">
    {#each peerData.peers as peer}
      <div class="p-4 bg-ifa-card-muted rounded-xl">
        <div class="flex items-center gap-3 mb-3">
          <div class="w-8 h-8 rounded-full bg-gray-200 flex items-center justify-center text-gray-600 font-bold text-sm">
            {peer.avatar}
          </div>
          <div class="flex-1">
            <p class="text-sm font-semibold text-ifa-text-primary">{peer.name}</p>
            <div class="flex items-center gap-2 mt-1">
              <div class="flex-1 h-2 bg-gray-300 rounded-full">
                <div
                  class="h-full rounded-full {getProgressColor(peer.overallProgress)}"
                  style="width: {peer.overallProgress}%"
                ></div>
              </div>
              <span class="text-xs font-bold text-ifa-text-primary">{peer.overallProgress}%</span>
            </div>
          </div>
          <div class="text-right">
            {#if peer.overallProgress > peerData.currentLearner.overallProgress}
              <TrendingUp class="w-4 h-4 text-emerald-600" />
            {:else}
              <TrendingUp class="w-4 h-4 text-red-500 rotate-180" />
            {/if}
          </div>
        </div>

        <!-- Skill Comparison -->
        <div class="grid grid-cols-5 gap-1 mt-3">
          {#each Object.entries(peer.skills) as [skill, value]}
            <div class="text-center">
              <div class="h-16 bg-white rounded flex items-end justify-center pb-1">
                <div
                  class="w-2 rounded-t {getProgressColor(value)}"
                  style="height: {value}%"
                ></div>
              </div>
              <p class="text-[10px] text-ifa-text-secondary mt-1 truncate">{skill}</p>
            </div>
          {/each}
        </div>
      </div>
    {/each}
  </div>

  <button
    type="button"
    class="w-full mt-4 py-2 text-xs font-semibold text-ifa-pine hover:text-emerald-700 transition flex items-center justify-center gap-2"
  >
    <BarChart3 class="w-4 h-4" />
    <span>View Detailed Comparison</span>
  </button>
</div>
