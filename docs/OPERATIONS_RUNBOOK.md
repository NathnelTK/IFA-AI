# Witforge Operations Runbook

## Release Order

1. Rotate any credentials that have ever existed in ignored local environment files.
2. Back up PostgreSQL and record the R2 object inventory.
3. Deploy additive schema with the one-shot command `dotnet worker/IFA.Worker.dll migrate`.
4. Deploy API, worker, ClamAV, and frontend with all new feature flags disabled.
5. Confirm `/api/health`, `/api/health/live`, worker logs, and the operations queue view.
6. Enable features in this dependency order: `OpaqueSessions`, `Workspaces`, `UnifiedAuthoring`, `AssetUploads`, `ImmutableReleases`, `PublicPresence`, then independent product flags.
7. Enable `CustomDomains`, `Certificates`, and `Offers` only after their external dependencies pass production smoke checks.

The API and long-running worker never run migrations during normal startup. Only the pre-deploy command owns DDL, guarded by a PostgreSQL advisory lock.

## Required Secrets

- API: database runtime credential, JWT legacy-exchange key, BFF HMAC key, auth data-protection key, context pepper, Chapa credentials when offers are enabled, R2 credentials when uploads are enabled.
- Worker: database runtime credential, auth data-protection key, AI provider credentials, R2 credentials, email provider key, Vercel domain token, optional OCR key.
- Frontend/BFF: private API origin and BFF HMAC key. Only `PUBLIC_*` values may reach browser bundles.

Never reuse the JWT key, BFF HMAC key, data-protection key, or context pepper.

## Database Backup And Restore

Create a logical backup before every migration:

```bash
pg_dump --format=custom --no-owner --no-privileges "$DATABASE_URL" --file witforge.dump
pg_restore --list witforge.dump
```

Restore into a new database first, run the migration command against it, and exercise login, authoring, enrollment, release progress, and payment reconciliation. Do not overwrite production as the first restore test.

## R2 Policy

- Browser CORS permits `PUT` only from the fixed Witforge app/auth origins and approved custom-site origins, with `Content-Type` headers and `ETag` exposed for multipart completion.
- Quarantine objects expire automatically after seven days.
- Clean private objects and certificate PDFs have no public bucket access.
- Public marketing derivatives use a separate `public/` prefix and immutable cache keys.
- Incomplete multipart uploads, when enabled, expire after one day.

## Queue Recovery

- Inspect leased and failed work through `/api/analytics/platform/operations`.
- Inspect poison outbox messages through `/api/operations/dead-letters`.
- Retry a reviewed outbox message with `POST /api/operations/dead-letters/{id}/retry`.
- Retry an authoring job with `POST /api/operations/authoring-jobs/{id}/retry`.
- Never edit lease tokens manually. Expired leases are reclaimed with fencing tokens.

All operations endpoints require `PlatformAdmin` plus an MFA-authenticated session.

## Payment Incident

1. Disable `Features__Offers` to stop new offer checkout.
2. Keep webhook ingestion and the worker online so signed events and provider verification can reconcile existing transactions.
3. Compare Chapa amount, currency, transaction reference, offer, release, and cohort snapshots before granting access.
4. Do not infer payment success from a browser callback.
5. Re-enable checkout only after webhook signature validation and reconciliation lag return to normal.

## Custom Domain Incident

1. Set the domain record to disabled or disable `Features__CustomDomains`.
2. Preserve the workspace slug route as the fallback.
3. Check TXT ownership, exact CNAME target, Vercel project registration, and certificate status.
4. Never wildcard an unverified hostname and never use external sites as authentication or payment-return origins.

## Rollback

- Prefer disabling the affected feature flag over reversing an additive migration.
- API and worker versions can be rolled back while additive columns and tables remain.
- Do not run migration `Down` in production unless a rehearsed restore proves it is safe.
- Public UUIDs, share codes, release IDs, payment references, certificate serials, and slug aliases must remain stable.
