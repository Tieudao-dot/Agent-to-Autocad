// Appends the "Điểm mới" block of docs/ghi-chu-phat-hanh-nhap.md to the release description CI generates.
//
//   node bundle/release-highlights.mjs <version> <release-notes file> [notes file]
//
// The block is the first fenced code block of section "## C. …" of the notes file. It is only used when the
// notes file's title names the version being released, so notes left over from an earlier version never end up
// on a newer release. Anything missing, empty or unreadable leaves the release description as it is: this
// script never fails a release (it always exits 0).
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const DEFAULT_NOTES = path.join(path.dirname(fileURLToPath(import.meta.url)), '..', 'docs', 'ghi-chu-phat-hanh-nhap.md');

/** The highlights for `version` in the notes markdown, or '' when there are none. */
export function extractHighlights(markdown, version) {
  const lines = String(markdown).replace(/^﻿/, '').split(/\r?\n/);

  const title = lines.find((l) => /^#\s+\S/.test(l));
  if (!title || !version) return '';
  const escaped = String(version).replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
  if (!new RegExp(`(^|[^0-9.])v?${escaped}($|[^0-9.]|\\.(?![0-9]))`).test(title)) return '';

  const start = lines.findIndex((l) => /^##\s+C\.\s/.test(l));
  if (start < 0) return '';
  let end = lines.findIndex((l, i) => i > start && /^##\s/.test(l));
  if (end < 0) end = lines.length;

  const open = lines.findIndex((l, i) => i > start && i < end && /^```/.test(l));
  if (open < 0) return '';
  const close = lines.findIndex((l, i) => i > open && i < end && /^```\s*$/.test(l));
  if (close < 0) return '';

  return lines.slice(open + 1, close).join('\n').trim();
}

function main(argv) {
  const [version, target, notes = DEFAULT_NOTES] = argv;
  if (!version || !target) {
    console.log('release highlights: usage: release-highlights.mjs <version> <release-notes file> [notes file]');
    return;
  }
  try {
    if (!fs.existsSync(notes)) {
      console.log(`release highlights: ${notes} not found, nothing added`);
      return;
    }
    const block = extractHighlights(fs.readFileSync(notes, 'utf8'), version);
    if (!block) {
      console.log(`release highlights: no section C for ${version} in ${notes}, nothing added`);
      return;
    }
    const current = fs.readFileSync(target, 'utf8');
    fs.appendFileSync(target, `${current.endsWith('\n') ? '' : '\n'}\n${block}\n`, 'utf8');
    console.log(`release highlights: added ${block.split('\n').length} line(s) for ${version}`);
  } catch (e) {
    console.log(`release highlights: skipped (${e && e.message ? e.message : e})`);
  }
}

if (process.argv[1] && path.resolve(process.argv[1]) === fileURLToPath(import.meta.url)) main(process.argv.slice(2));
