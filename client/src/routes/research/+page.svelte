<script lang="ts">
  import { Search, BookOpen, Youtube, ExternalLink, Plus, Clock, Filter } from 'lucide-svelte';
  import ResearchResults from '$lib/components/ResearchResults.svelte';
  import ResearchSourceCard from '$lib/components/ResearchSourceCard.svelte';
  import ResearchSummary from '$lib/components/ResearchSummary.svelte';

  let searchQuery = '';
  let activeFilter = 'all'; // 'all' | 'scholarxiv' | 'web' | 'youtube'

  const researchHistory = [
    {
      id: 'res-1',
      title: 'C# REST API Research',
      courseTitle: 'C# Backend Development',
      timestamp: '2 hours ago',
      status: 'completed',
      scholarxivCount: 12,
      webCount: 8,
      youtubeCount: 6
    },
    {
      id: 'res-2',
      title: 'Clean Architecture Patterns',
      courseTitle: 'ASP.NET Core Fundamentals',
      timestamp: '1 day ago',
      status: 'completed',
      scholarxivCount: 8,
      webCount: 15,
      youtubeCount: 4
    },
    {
      id: 'res-3',
      title: 'SQL Database Optimization',
      courseTitle: 'SQL for Developers',
      timestamp: '3 days ago',
      status: 'completed',
      scholarxivCount: 6,
      webCount: 10,
      youtubeCount: 3
    }
  ];

  const currentResearch = {
    id: 'res-1',
    title: 'C# REST API Research',
    scholarxivCount: 12,
    webCount: 8,
    youtubeCount: 6,
    scholarxivSources: [
      {
        id: 'src-1',
        title: 'RESTful Web API Design Patterns',
        authors: 'Fielding, R.',
        year: 2000,
        type: 'academic',
        abstract: 'This paper presents the architectural style and design guidelines for RESTful web services.',
        credibility: 'high',
        url: 'https://scholarxiv.org/paper/123'
      },
      {
        id: 'src-2',
        title: 'API Security Best Practices',
        authors: 'Smith, J., Johnson, A.',
        year: 2022,
        type: 'academic',
        abstract: 'Comprehensive analysis of security patterns for modern web APIs including OAuth and JWT.',
        credibility: 'high',
        url: 'https://scholarxiv.org/paper/456'
      }
    ],
    webSources: [
      {
        id: 'web-1',
        title: 'Microsoft REST API Guidelines',
        url: 'https://docs.microsoft.com/en-us/azure/architecture/best-practices/api-design',
        type: 'documentation',
        description: 'Official Microsoft documentation on REST API design principles and implementation.',
        credibility: 'high'
      },
      {
        id: 'web-2',
        title: 'REST API Tutorial',
        url: 'https://restfulapi.net/',
        type: 'tutorial',
        description: 'Comprehensive tutorial covering REST concepts, HTTP methods, and best practices.',
        credibility: 'medium'
      }
    ],
    youtubeResources: [
      {
        id: 'yt-1',
        title: 'Building REST APIs with ASP.NET Core',
        channel: 'Nick Chapsas',
        videoId: 'abc123',
        duration: '45:30',
        thumbnail: 'https://img.youtube.com/vi/abc123/default.jpg',
        relevance: 'high'
      },
      {
        id: 'yt-2',
        title: 'REST API Fundamentals',
        channel: 'FreeCodeCamp',
        videoId: 'def456',
        duration: '1:20:00',
        thumbnail: 'https://img.youtube.com/vi/def456/default.jpg',
        relevance: 'medium'
      }
    ],
    summary: 'REST APIs are commonly structured around resource-based URLs, standard HTTP methods (GET, POST, PUT, DELETE), and stateless communication. Academic research emphasizes the importance of proper HTTP status codes, content negotiation, and HATEOAS for hypermedia-driven APIs. Industry best practices focus on versioning, authentication, rate limiting, and comprehensive error handling.'
  };

  function handleAddToLearningPath(sourceId: string) {
    console.log('Adding source to learning path:', sourceId);
  }

  function handleOpenSource(url: string) {
    console.log('Opening source:', url);
  }

  function handleNewResearch() {
    console.log('Starting new research');
  }
</script>

<div class="p-6 md:p-8 space-y-6 max-w-[1600px] mx-auto">
  <!-- Header -->
  <div class="flex items-center justify-between">
    <div>
      <h1 class="text-2xl font-bold text-ifa-text-primary">Research</h1>
      <p class="text-sm text-ifa-text-secondary mt-1">Academic research and external resources for your courses</p>
    </div>
    <button
      type="button"
      on:click={handleNewResearch}
      class="px-4 py-2 bg-ifa-pine text-white rounded-lg text-sm font-semibold flex items-center gap-2 hover:bg-emerald-800 transition"
    >
      <Plus class="w-4 h-4" />
      <span>New Research</span>
    </button>
  </div>

  <!-- Search -->
  <div class="relative">
    <Search class="absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-ifa-text-muted" />
    <input
      type="text"
      bind:value={searchQuery}
      placeholder="Search research history..."
      class="w-full pl-10 pr-4 py-2.5 rounded-xl bg-ifa-card-muted border border-ifa-border text-sm text-ifa-text-primary placeholder-ifa-text-muted focus:outline-none focus:ring-1 focus:ring-ifa-pine"
    />
  </div>

  <!-- Research History -->
  <div>
    <h2 class="text-lg font-bold text-ifa-text-primary mb-4">Research History</h2>
    <ResearchResults
      researchHistory={researchHistory}
      onSelect={(id) => console.log('Selected research:', id)}
    />
  </div>

  <!-- Current Research Details -->
  <div class="space-y-6">
    <h2 class="text-lg font-bold text-ifa-text-primary">{currentResearch.title}</h2>

    <!-- Research Summary -->
    <ResearchSummary summary={currentResearch.summary} />

    <!-- Source Counts -->
    <div class="flex items-center gap-6 text-sm">
      <div class="flex items-center gap-2 text-ifa-text-secondary">
        <BookOpen class="w-4 h-4" />
        <span>{currentResearch.scholarxivCount} Academic Sources</span>
      </div>
      <div class="flex items-center gap-2 text-ifa-text-secondary">
        <ExternalLink class="w-4 h-4" />
        <span>{currentResearch.webCount} Web Resources</span>
      </div>
      <div class="flex items-center gap-2 text-ifa-text-secondary">
        <Youtube class="w-4 h-4" />
        <span>{currentResearch.youtubeCount} YouTube Videos</span>
      </div>
    </div>

    <!-- Scholarxiv Sources -->
    <div>
      <h3 class="text-md font-bold text-ifa-text-primary mb-3 flex items-center gap-2">
        <BookOpen class="w-5 h-5 text-ifa-pine" />
        Scholarxiv Academic Sources
      </h3>
      <div class="grid grid-cols-1 md:grid-cols-2 gap-4">
        {#each currentResearch.scholarxivSources as source}
          <ResearchSourceCard
            source={source}
            onAddToPath={() => handleAddToLearningPath(source.id)}
            onOpen={() => handleOpenSource(source.url || '#')}
          />
        {/each}
      </div>
    </div>

    <!-- Web Sources -->
    <div>
      <h3 class="text-md font-bold text-ifa-text-primary mb-3 flex items-center gap-2">
        <ExternalLink class="w-5 h-5 text-blue-500" />
        Web Resources
      </h3>
      <div class="grid grid-cols-1 md:grid-cols-2 gap-4">
        {#each currentResearch.webSources as source}
          <ResearchSourceCard
            source={source}
            onAddToPath={() => handleAddToLearningPath(source.id)}
            onOpen={() => handleOpenSource(source.url)}
          />
        {/each}
      </div>
    </div>

    <!-- YouTube Resources -->
    <div>
      <h3 class="text-md font-bold text-ifa-text-primary mb-3 flex items-center gap-2">
        <Youtube class="w-5 h-5 text-red-500" />
        YouTube Resources
      </h3>
      <div class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
        {#each currentResearch.youtubeResources as source}
          <div class="bg-ifa-card rounded-xl border border-ifa-border p-4 hover:shadow-elevated transition">
            <div class="aspect-video bg-gray-200 rounded-lg mb-3 relative overflow-hidden">
              <img
                src={source.thumbnail}
                alt={source.title}
                class="w-full h-full object-cover"
              />
              <div class="absolute bottom-2 right-2 bg-black/70 text-white text-xs px-2 py-1 rounded">
                {source.duration}
              </div>
            </div>
            <h4 class="text-sm font-semibold text-ifa-text-primary mb-1">{source.title}</h4>
            <p class="text-xs text-ifa-text-secondary mb-2">{source.channel}</p>
            <div class="flex items-center justify-between">
              <span class="text-[10px] font-semibold px-2 py-0.5 rounded-full {source.relevance === 'high' ? 'bg-emerald-100 text-emerald-700' : 'bg-gray-100 text-gray-700'}">
                {source.relevance}
              </span>
              <button
                type="button"
                on:click={() => handleAddToLearningPath(source.id)}
                class="text-xs font-semibold text-ifa-pine hover:text-emerald-700 transition"
              >
                Add to Path
              </button>
            </div>
          </div>
        {/each}
      </div>
    </div>
  </div>
</div>
