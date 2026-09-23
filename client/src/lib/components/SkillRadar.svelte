<script lang="ts">
  export let skills: any[] = [];
  export let title = 'Skills';

  // Calculate polygon points for radar chart
  function getRadarPoints() {
    const numSkills = skills.length;
    const angleStep = (Math.PI * 2) / numSkills;
    const radius = 80;
    const centerX = 100;
    const centerY = 100;

    return skills.map((skill, index) => {
      const angle = index * angleStep - Math.PI / 2;
      const value = skill.percentage / 100;
      const x = centerX + Math.cos(angle) * radius * value;
      const y = centerY + Math.sin(angle) * radius * value;
      return `${x},${y}`;
    }).join(' ');
  }

  function getBackgroundPoints() {
    const numSkills = skills.length;
    const angleStep = (Math.PI * 2) / numSkills;
    const radius = 80;
    const centerX = 100;
    const centerY = 100;

    return skills.map((_, index) => {
      const angle = index * angleStep - Math.PI / 2;
      const x = centerX + Math.cos(angle) * radius;
      const y = centerY + Math.sin(angle) * radius;
      return `${x},${y}`;
    }).join(' ');
  }

  function getLabelPositions() {
    const numSkills = skills.length;
    const angleStep = (Math.PI * 2) / numSkills;
    const radius = 95;
    const centerX = 100;
    const centerY = 100;

    return skills.map((skill, index) => {
      const angle = index * angleStep - Math.PI / 2;
      const x = centerX + Math.cos(angle) * radius;
      const y = centerY + Math.sin(angle) * radius;
      return { x, y, skill };
    });
  }
</script>

<div class="bg-ifa-card rounded-2xl border border-ifa-border p-6">
  <h3 class="text-sm font-bold text-ifa-text-primary mb-4">{title}</h3>
  <div class="flex items-center justify-center">
    <svg viewBox="0 0 200 200" class="w-full max-w-xs">
      <!-- Background polygon -->
      <polygon
        points={getBackgroundPoints()}
        fill="none"
        stroke="#E5E7EB"
        stroke-width="1"
      />

      <!-- Grid lines -->
      {#each [0.25, 0.5, 0.75, 1] as level}
        {@const levelRadius = level * 80}
        {@const levelPoints = skills.map((_, i) => {
          const angle = i * ((Math.PI * 2) / skills.length) - Math.PI / 2;
          const x = 100 + Math.cos(angle) * levelRadius;
          const y = 100 + Math.sin(angle) * levelRadius;
          return `${x},${y}`;
        }).join(' ')}
        <polygon
          points={levelPoints}
          fill="none"
          stroke="#E5E7EB"
          stroke-width="0.5"
        />
      {/each}

      <!-- Skill polygon -->
      <polygon
        points={getRadarPoints()}
        fill="rgba(42, 157, 104, 0.2)"
        stroke="#2A9D68"
        stroke-width="2"
      />

      <!-- Labels -->
      {#each getLabelPositions() as label}
        <text
          x={label.x}
          y={label.y}
          text-anchor="middle"
          dominant-baseline="middle"
          class="text-[8px] font-semibold fill-ifa-text-primary"
        >
          {label.skill.name}
        </text>
      {/each}
    </svg>
  </div>

  <!-- Legend -->
  <div class="mt-4 grid grid-cols-2 gap-2">
    {#each skills as skill}
      <div class="flex items-center gap-2">
        <div
          class="w-3 h-3 rounded-full"
          style="background-color: {skill.color}"
        ></div>
        <span class="text-xs text-ifa-text-secondary">{skill.name}: {skill.percentage}%</span>
      </div>
    {/each}
  </div>
</div>
