<script lang="ts">
  import { Terminal, Database, Network, KeyRound, CheckCircle2, Code2, Palette } from 'lucide-svelte';

  export let categoryName = '';
  export let skills: any[] = [];

  const iconMap: Record<string, any> = {
    'C#': Terminal,
    'Databases': Database,
    'APIs': Network,
    'Authentication': KeyRound,
    'Testing': CheckCircle2,
    'JavaScript': Code2,
    'React': Code2,
    'CSS/Tailwind': Palette
  };

  function getIcon(skillName: string) {
    return iconMap[skillName] || Terminal;
  }
</script>

<div class="bg-ifa-card rounded-2xl border border-ifa-border p-6">
  <h3 class="text-sm font-bold text-ifa-text-primary mb-4">{categoryName}</h3>
  <div class="space-y-3">
    {#each skills as skill}
      <div class="flex items-center gap-4">
        {#if getIcon(skill.name) === Terminal}
          <div
            class="w-10 h-10 rounded-lg flex items-center justify-center shrink-0"
            style="background-color: {skill.color}15; color: {skill.color};"
          >
            <Terminal class="w-5 h-5" />
          </div>
        {:else if getIcon(skill.name) === Database}
          <div
            class="w-10 h-10 rounded-lg flex items-center justify-center shrink-0"
            style="background-color: {skill.color}15; color: {skill.color};"
          >
            <Database class="w-5 h-5" />
          </div>
        {:else if getIcon(skill.name) === Network}
          <div
            class="w-10 h-10 rounded-lg flex items-center justify-center shrink-0"
            style="background-color: {skill.color}15; color: {skill.color};"
          >
            <Network class="w-5 h-5" />
          </div>
        {:else if getIcon(skill.name) === KeyRound}
          <div
            class="w-10 h-10 rounded-lg flex items-center justify-center shrink-0"
            style="background-color: {skill.color}15; color: {skill.color};"
          >
            <KeyRound class="w-5 h-5" />
          </div>
        {:else if getIcon(skill.name) === CheckCircle2}
          <div
            class="w-10 h-10 rounded-lg flex items-center justify-center shrink-0"
            style="background-color: {skill.color}15; color: {skill.color};"
          >
            <CheckCircle2 class="w-5 h-5" />
          </div>
        {:else if getIcon(skill.name) === Code2}
          <div
            class="w-10 h-10 rounded-lg flex items-center justify-center shrink-0"
            style="background-color: {skill.color}15; color: {skill.color};"
          >
            <Code2 class="w-5 h-5" />
          </div>
        {:else if getIcon(skill.name) === Palette}
          <div
            class="w-10 h-10 rounded-lg flex items-center justify-center shrink-0"
            style="background-color: {skill.color}15; color: {skill.color};"
          >
            <Palette class="w-5 h-5" />
          </div>
        {:else}
          <div
            class="w-10 h-10 rounded-lg flex items-center justify-center shrink-0"
            style="background-color: {skill.color}15; color: {skill.color};"
          >
            <Terminal class="w-5 h-5" />
          </div>
        {/if}

        <div class="flex-1">
          <div class="flex items-center justify-between mb-1">
            <span class="text-sm font-semibold text-ifa-text-primary">{skill.name}</span>
            <div class="flex items-center gap-2">
              <span class="text-sm font-bold text-ifa-pine">{skill.percentage}%</span>
              {#if skill.improvement}
                <span class="text-xs font-semibold text-emerald-600 bg-emerald-50 px-2 py-0.5 rounded-full">
                  {skill.improvement}
                </span>
              {/if}
            </div>
          </div>
          <div class="w-full h-2 bg-ifa-card-muted rounded-full">
            <div
              class="h-full rounded-full transition-all duration-1000"
              style="width: {skill.percentage}%; background-color: {skill.color}"
            ></div>
          </div>
        </div>
      </div>
    {/each}
  </div>
</div>