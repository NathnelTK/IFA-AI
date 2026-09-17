#!/usr/bin/env node
/**
 * STARK Changelog tooling for IFA.
 *
 * The STARK hackathon requires every meaningful milestone to be traceable.
 * This script appends standardized entries to docs/stark-changelog.md and
 * verifies that every existing entry follows the required format.
 *
 * Usage:
 *   node scripts/update-changelog.js add --title "..." [options]
 *   node scripts/update-changelog.js verify
 *
 * Add options:
 *   --title         (required) Short milestone title.
 *   --type          ENTRY_TYPE, default: CHORE.
 *   --phase         Milestone phase, default: "Phase 1".
 *   --author        Entry author, default: "Team XOR".
 *   --summary       One or two sentence description.
 *   --files         Comma separated list of touched paths.
 *   --verification  How the change was verified.
 *   --problem       Optional problem / solution note.
 *   --date          YYYY-MM-DD, default: today.
 */
'use strict';

const fs = require('fs');
const path = require('path');

const ROOT = path.resolve(__dirname, '..');
const CHANGELOG_PATH = path.join(ROOT, 'docs', 'stark-changelog.md');

const ENTRY_TYPES = [
  'FEATURE',
  'FIX',
  'REFACTOR',
  'PERFORMANCE',
  'DOCS',
  'CHORE',
  'ARCHITECTURE',
  'SECURITY'
];

const REQUIRED_FIELDS = ['Author', 'Phase', 'Type', 'Summary', 'Verification'];
const OPTIONAL_FIELDS = ['Files', 'Problem / Solution'];
const HEADING_PATTERN = /^## \[(\d{4}-\d{2}-\d{2})\] ([A-Z]+): (.+)$/;
const MAX_SUMMARY_LENGTH = 500;

function fail(message) {
  console.error(`\x1b[31m[changelog]\x1b[0m ${message}`);
  process.exitCode = 1;
}

function ok(message) {
  console.log(`\x1b[32m[changelog]\x1b[0m ${message}`);
}

function parseArgs(argv) {
  const args = { _: [] };
  for (let i = 0; i < argv.length; i += 1) {
    const token = argv[i];
    if (!token.startsWith('--')) {
      args._.push(token);
      continue;
    }
    const key = token.slice(2);
    const next = argv[i + 1];
    if (next === undefined || next.startsWith('--')) {
      args[key] = true;
    } else {
      args[key] = next;
      i += 1;
    }
  }
  return args;
}

function today() {
  return new Date().toISOString().slice(0, 10);
}

function isValidDate(value) {
  if (!/^\d{4}-\d{2}-\d{2}$/.test(value)) return false;
  return !Number.isNaN(Date.parse(`${value}T00:00:00Z`));
}

function readChangelog() {
  if (!fs.existsSync(CHANGELOG_PATH)) {
    fail(`Changelog not found at ${path.relative(ROOT, CHANGELOG_PATH)}.`);
    return null;
  }
  return fs.readFileSync(CHANGELOG_PATH, 'utf8');
}

/** Splits the document into entry blocks, ignoring the header/legend and fenced code. */
function extractEntries(content) {
  const lines = content.split(/\r?\n/);
  const entries = [];
  let current = null;
  let inFence = false;

  for (const line of lines) {
    if (/^\s*(```|~~~)/.test(line)) {
      inFence = !inFence;
    }

    if (inFence) {
      if (current) current.push(line);
      continue;
    }

    if (line.startsWith('## [')) {
      if (current) entries.push(current);
      current = [line];
    } else if (current) {
      current.push(line);
    }
  }
  if (current) entries.push(current);

  return entries.map((block) => block.join('\n').trimEnd());
}

function validateEntry(entry) {
  const problems = [];
  const lines = entry.split(/\r?\n/);
  const heading = lines[0] || '';
  const match = HEADING_PATTERN.exec(heading);

  if (!match) {
    problems.push(
      `Invalid heading "${heading}". Expected "## [YYYY-MM-DD] TYPE: Title".`
    );
    return problems;
  }

  const [, date, type, title] = match;

  if (!isValidDate(date)) problems.push(`Invalid date "${date}". Use a real calendar date.`);
  if (!ENTRY_TYPES.includes(type)) {
    problems.push(`Unknown type "${type}". Allowed: ${ENTRY_TYPES.join(', ')}.`);
  }
  if (title.trim().length < 5) problems.push('Title must be at least 5 characters.');

  const fieldValues = {};
  for (const field of [...REQUIRED_FIELDS, ...OPTIONAL_FIELDS]) {
    const pattern = new RegExp(`^- \\*\\*${field.replace(/[/\\]/g, '\\$&')}:\\*\\* (.+)$`, 'm');
    const fieldMatch = pattern.exec(entry);
    if (fieldMatch) {
      fieldValues[field] = fieldMatch[1].trim();
    }
  }

  for (const field of REQUIRED_FIELDS) {
    if (!fieldValues[field]) problems.push(`Missing or empty required field "**${field}:**".`);
  }

  if (fieldValues.Type && fieldValues.Type !== type) {
    problems.push(`Field "**Type:**" (${fieldValues.Type}) does not match heading type (${type}).`);
  }

  if (fieldValues.Summary && fieldValues.Summary.length > MAX_SUMMARY_LENGTH) {
    problems.push(`"**Summary:**" exceeds ${MAX_SUMMARY_LENGTH} characters.`);
  }

  return problems;
}

function verify() {
  const content = readChangelog();
  if (content === null) return;

  const entries = extractEntries(content);

  if (entries.length === 0) {
    fail('No changelog entries found. Add one with "npm run changelog:add".');
    return;
  }

  let invalid = 0;
  entries.forEach((entry, index) => {
    const problems = validateEntry(entry);
    if (problems.length > 0) {
      invalid += 1;
      const heading = entry.split(/\r?\n/)[0];
      fail(`Entry ${index + 1} "${heading}" is invalid:`);
      problems.forEach((problem) => console.error(`           - ${problem}`));
    }
  });

  if (invalid > 0) {
    fail(`${invalid} of ${entries.length} changelog entries are invalid.`);
    return;
  }

  ok(`Verified ${entries.length} changelog entr${entries.length === 1 ? 'y' : 'ies'}. Format is STARK-compliant.`);
}

function buildEntry(options) {
  const type = (options.type || 'CHORE').toUpperCase();
  const date = options.date || today();

  const lines = [
    `## [${date}] ${type}: ${options.title.trim()}`,
    `- **Author:** ${options.author || 'Team XOR'}`,
    `- **Phase:** ${options.phase || 'Phase 1'}`,
    `- **Type:** ${type}`,
    `- **Summary:** ${options.summary || options.title.trim()}`,
    `- **Verification:** ${options.verification || 'Build and type-check pass locally.'}`
  ];

  if (options.files) {
    lines.push(`- **Files:** ${options.files}`);
  }
  if (options.problem) {
    lines.push(`- **Problem / Solution:** ${options.problem}`);
  }

  return lines.join('\n');
}

function add(args) {
  if (!args.title || args.title === true) {
    fail('Missing required option "--title".');
    console.error('Example: npm run changelog:add -- --title "Add X" --type FEATURE --phase "PR 2.1"');
    return;
  }

  const content = readChangelog();
  if (content === null) return;

  const entry = buildEntry(args);
  const problems = validateEntry(entry);
  if (problems.length > 0) {
    problems.forEach((problem) => fail(problem));
    return;
  }

  const separator = '\n\n';
  fs.writeFileSync(CHANGELOG_PATH, `${content.trimEnd()}${separator}${entry}\n`, 'utf8');
  ok(`Added ${args.type ? String(args.type).toUpperCase() : 'CHORE'} entry: "${args.title}".`);}

function main() {
  const args = parseArgs(process.argv.slice(2));
  const command = args._[0];

  switch (command) {
    case 'add':
      add(args);
      break;
    case 'verify':
      verify();
      break;
    default:
      fail('Unknown command. Use "add" or "verify".');
      console.error('  node scripts/update-changelog.js add --title "..."');
      console.error('  node scripts/update-changelog.js verify');
      break;
  }
}

main();
