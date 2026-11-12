<script lang="ts">
  import { createEventDispatcher } from 'svelte';
  import { UploadCloud, Loader2, CheckCircle2, AlertTriangle } from 'lucide-svelte';
  import { assetsApi } from '$lib/api';

  export let purpose: 'CourseCover' | 'CourseMaterial' | 'AssignmentSubmission';
  export let workspaceId: string | undefined = undefined;
  export let courseId: string | undefined = undefined;
  export let accept = '';
  export let label = 'Upload file';

  const dispatch = createEventDispatcher<{ complete: { assetId: string; fileName: string } }>();
  let progress = 0;
  let status: 'idle' | 'hashing' | 'uploading' | 'scanning' | 'complete' | 'error' = 'idle';
  let message = '';

  async function selectFile(event: Event) {
    const input = event.currentTarget as HTMLInputElement;
    const file = input.files?.[0];
    if (!file) return;
    progress = 0;
    message = '';
    try {
      status = 'hashing';
      const sha256 = await digest(file);
      const session = await assetsApi.createUpload({
        workspaceId,
        courseId,
        purpose,
        fileName: file.name,
        contentType: file.type || 'application/octet-stream',
        sizeBytes: file.size,
        sha256
      });
      status = 'uploading';
      let parts: Array<{ partNumber: number; eTag: string }> = [];
      try {
        if (session.multipart) {
          parts = await uploadMultipart(
            session.uploadSessionId,
            file,
            session.partSizeBytes ?? 10 * 1024 * 1024,
            session.partCount ?? 0,
            (value) => (progress = value)
          );
        } else if (session.uploadUrl) {
          await upload(session.uploadUrl, session.requiredHeaders ?? {}, file, (value) => (progress = value));
        } else {
          throw new Error('Storage did not return an upload target.');
        }
      } catch (cause) {
        await assetsApi.abortUpload(session.uploadSessionId).catch(() => undefined);
        throw cause;
      }
      const queued = await assetsApi.completeUpload(session.uploadSessionId, parts);
      status = 'scanning';
      await waitForScan(queued.id);
      status = 'complete';
      message = `${file.name} is clean and ready.`;
      dispatch('complete', { assetId: queued.id, fileName: file.name });
    } catch (cause) {
      status = 'error';
      message = cause instanceof Error ? cause.message : 'The upload could not be completed.';
    } finally {
      input.value = '';
    }
  }

  async function digest(file: File): Promise<string> {
    const buffer = await file.arrayBuffer();
    const hash = await crypto.subtle.digest('SHA-256', buffer);
    return Array.from(new Uint8Array(hash)).map((value) => value.toString(16).padStart(2, '0')).join('');
  }

  function upload(url: string, headers: Record<string, string>, file: Blob, onProgress: (value: number) => void): Promise<string> {
    return new Promise((resolve, reject) => {
      const request = new XMLHttpRequest();
      request.open('PUT', url);
      for (const [name, value] of Object.entries(headers)) request.setRequestHeader(name, value);
      request.upload.onprogress = (event) => {
        if (event.lengthComputable) onProgress(Math.round(event.loaded * 100 / event.total));
      };
      request.onerror = () => reject(new Error('The direct storage upload failed.'));
      request.onload = () => request.status >= 200 && request.status < 300
        ? resolve(request.getResponseHeader('ETag')?.replaceAll('"', '') ?? '')
        : reject(new Error(`Storage rejected the upload (${request.status}).`));
      request.send(file);
    });
  }

  async function uploadMultipart(
    sessionId: string,
    file: File,
    partSize: number,
    partCount: number,
    onProgress: (value: number) => void
  ): Promise<Array<{ partNumber: number; eTag: string }>> {
    if (partCount < 1) throw new Error('The multipart upload plan is invalid.');
    const completed: Array<{ partNumber: number; eTag: string }> = [];
    let uploadedBytes = 0;
    for (let partNumber = 1; partNumber <= partCount; partNumber++) {
      const start = (partNumber - 1) * partSize;
      const end = Math.min(file.size, start + partSize);
      const chunk = file.slice(start, end);
      const signed = await assetsApi.createPartUrl(sessionId, partNumber);
      const eTag = await upload(signed.uploadUrl, signed.requiredHeaders, chunk, (partProgress) => {
        const currentBytes = chunk.size * partProgress / 100;
        onProgress(Math.round((uploadedBytes + currentBytes) * 100 / file.size));
      });
      if (!eTag) throw new Error(`Storage did not expose an ETag for part ${partNumber}.`);
      completed.push({ partNumber, eTag });
      uploadedBytes += chunk.size;
      onProgress(Math.round(uploadedBytes * 100 / file.size));
    }
    return completed;
  }

  async function waitForScan(assetId: string): Promise<void> {
    for (let attempt = 0; attempt < 20; attempt++) {
      await new Promise((resolve) => setTimeout(resolve, Math.min(5000, 750 * (attempt + 1))));
      const asset = await assetsApi.get(assetId);
      if (asset.status === 'Clean') return;
      if (['Infected', 'Rejected', 'Deleted'].includes(asset.status)) {
        throw new Error(asset.rejectionCode === 'malware_detected'
          ? 'The upload was rejected by malware scanning.'
          : 'The upload failed integrity or safety checks.');
      }
    }
    throw new Error('Scanning is taking longer than expected. The asset remains quarantined; check again later.');
  }
</script>

<div class="rounded-xl border border-dashed border-ifa-border bg-ifa-card-muted/40 p-4">
  <label class="flex cursor-pointer items-center gap-3 text-sm font-semibold text-ifa-text-primary">
    <span class="grid h-10 w-10 place-items-center rounded-lg bg-ifa-card text-ifa-pine"><UploadCloud class="h-5 w-5" /></span>
    <span>{label}</span>
    <input type="file" {accept} on:change={selectFile} disabled={status !== 'idle' && status !== 'complete' && status !== 'error'} class="sr-only" />
  </label>
  {#if status !== 'idle'}
    <div class="mt-3" aria-live="polite">
      {#if status === 'uploading'}
        <div class="h-2 overflow-hidden rounded-full bg-ifa-border"><div class="h-full bg-ifa-pine" style={`width:${progress}%`}></div></div>
        <p class="mt-1 text-xs text-ifa-text-secondary">Uploading directly to private storage: {progress}%</p>
      {:else if status === 'hashing' || status === 'scanning'}
        <p class="flex items-center gap-2 text-xs text-ifa-text-secondary"><Loader2 class="h-3.5 w-3.5 animate-spin" />{status === 'hashing' ? 'Calculating checksum…' : 'Scanning in quarantine…'}</p>
      {:else if status === 'complete'}
        <p class="flex items-center gap-2 text-xs text-emerald-700"><CheckCircle2 class="h-4 w-4" />{message}</p>
      {:else if status === 'error'}
        <p role="alert" class="flex items-start gap-2 text-xs text-red-700"><AlertTriangle class="mt-0.5 h-4 w-4 shrink-0" />{message}</p>
      {/if}
    </div>
  {/if}
</div>
