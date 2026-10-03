-- =============================================================================
-- cleanup-duplicate-courses.sql
--
-- Deletes duplicate Courses that share the same Title and CreatorLearnerId.
-- Keeps the MOST MATERIALIZED copy: the one with the most ready modules, then
-- the most lessons, then the oldest. (A later duplicate can be the richer one —
-- e.g. if a JIT "generate module" call ran against it — so age alone is unsafe.)
--
-- Created for the two identical demo courses left behind when a create call
-- persisted before response serialization failed and the second create succeeded.
--
-- Safe:
--   * scoped to Courses only; you review the SELECT output before the DELETE
--   * deleting a Course cascades its Modules -> Lessons/Quizzes/Questions and
--     its CourseEnrollments; ResearchPackages.CourseId is set to NULL
--   * wrapped in a transaction; nothing is committed until the final COMMIT
--
-- Usage:
--   psql "<connection-string>" -f scripts/cleanup-duplicate-courses.sql
--   (or paste into the Supabase SQL editor)
-- =============================================================================

BEGIN;

-- Shared ranking: for each (Title, CreatorLearnerId) group, pick the keeper.
CREATE TEMP TABLE _dup_ranked ON COMMIT DROP AS
WITH stats AS (
    SELECT
        c."Id",
        c."Title",
        c."CreatorLearnerId",
        c."CreatedAt",
        (SELECT COUNT(*) FROM "Modules" m
          WHERE m."CourseId" = c."Id" AND m."GenerationStatus" = 2) AS ready_modules,
        (SELECT COUNT(*)
           FROM "Lessons" l
           JOIN "Modules" m ON m."Id" = l."ModuleId"
          WHERE m."CourseId" = c."Id") AS lessons
    FROM "Courses" c
)
SELECT
    "Id",
    "Title",
    "CreatedAt",
    ready_modules,
    lessons,
    ROW_NUMBER() OVER (
        PARTITION BY "Title", "CreatorLearnerId"
        ORDER BY ready_modules DESC, lessons DESC, "CreatedAt" ASC, "Id" ASC
    ) AS rn
FROM stats;

-- 1) Preview: rn = 1 is KEPT, rn > 1 will be DELETED.
SELECT "Id", "Title", "CreatedAt", ready_modules, lessons,
       CASE WHEN rn = 1 THEN 'KEEP' ELSE 'DELETE' END AS action
FROM _dup_ranked
WHERE rn > 1 OR rn = 1
ORDER BY "Title", rn;

-- 2) Delete the duplicates (every row that is not the keeper).
DELETE FROM "Courses" c
USING _dup_ranked r
WHERE c."Id" = r."Id"
  AND r.rn > 1;

-- 3) Confirm what remains.
SELECT "Id", "Title", "CreatedAt"
FROM "Courses"
ORDER BY "CreatedAt";

-- Change to ROLLBACK; if you want to inspect without deleting.
COMMIT;
