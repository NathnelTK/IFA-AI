<script lang="ts">
  import SitePage from '../../../_site/[...path]/+page.svelte';
  export let data: { site: any; basePath?: string };
</script>

<SitePage {data} />
