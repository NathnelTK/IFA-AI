<script lang="ts">
  export let title = 'Something went wrong';
  export let message: string;
</script>

<section role="alert" class="rounded-2xl border border-red-200 bg-red-50 p-5 text-red-900 dark:border-red-900 dark:bg-red-950/40 dark:text-red-200">
  <h2 class="font-bold">{title}</h2>
  <p class="mt-1 text-sm">{message}</p>
  <div class="mt-4"><slot /></div>
</section>
