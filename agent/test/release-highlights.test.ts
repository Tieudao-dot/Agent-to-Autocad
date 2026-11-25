import assert from 'node:assert/strict';
import { execFileSync } from 'node:child_process';
import fs from 'node:fs';
import os from 'node:os';
import path from 'node:path';
import { describe, it } from 'node:test';
import { fileURLToPath } from 'node:url';

const repo = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..', '..', '..');
const script = path.join(repo, 'bundle', 'release-highlights.mjs');
const BASE = 'ClaudeCAD 1.2.3\n\n- SHA-256: ABC\n';

/** Runs the script the way CI does and returns the release description afterwards. */
function run(version: string, notes: string | null): string {
  const dir = fs.mkdtempSync(path.join(os.tmpdir(), 'claudecad-notes-'));
  try {
    const target = path.join(dir, 'release-notes.md');
    const notesFile = path.join(dir, 'notes.md');
    fs.writeFileSync(target, BASE, 'utf8');
    if (notes !== null) fs.writeFileSync(notesFile, notes, 'utf8');
    execFileSync(process.execPath, [script, version, target, notesFile], { stdio: 'pipe' });
    return fs.readFileSync(target, 'utf8');
  } finally {
    fs.rmSync(dir, { recursive: true, force: true });
  }
}

const notes = (title: string, sectionC: string) =>
  `# ${title}\n\n## A. Điểm mới\n\n\`\`\`text\nnot this block\n\`\`\`\n\n${sectionC}\n## Thử thật\n\n\`\`\`markdown\nnor this one\n\`\`\`\n`;
const C = '## C. Mô tả bản phát hành\n\n```markdown\n### Điểm mới\n- **Một:** tiếng Việt có dấu.\n- Hai.\n```\n';

describe('release highlights (section C of docs/ghi-chu-phat-hanh-nhap.md)', () => {
  it('appends the block of section C under the generated description', () => {
    const out = run('1.2.3', notes('Ghi chú phát hành ClaudeCAD 1.2.3', C));
    assert.equal(out, `${BASE}\n### Điểm mới\n- **Một:** tiếng Việt có dấu.\n- Hai.\n`);
  });

  it('accepts CRLF notes and a draft marker after the version', () => {
    const out = run('1.2.3', notes('Ghi chú phát hành ClaudeCAD 1.2.3 — BẢN NHÁP', C).replace(/\n/g, '\r\n'));
    assert.match(out, /\n### Điểm mới\n- \*\*Một:\*\* tiếng Việt có dấu\.\n- Hai\.\n$/);
  });

  it('leaves the description alone when the notes are for another version', () => {
    assert.equal(run('1.2.4', notes('Ghi chú phát hành ClaudeCAD 1.2.3', C)), BASE);
    assert.equal(run('1.2', notes('Ghi chú phát hành ClaudeCAD 1.2.3', C)), BASE);
    assert.equal(run('1.2.3', notes('Ghi chú phát hành ClaudeCAD 1.2.30', C)), BASE);
  });

  it('leaves the description alone without section C, with an empty or an unclosed block, or without a notes file', () => {
    assert.equal(run('1.2.3', notes('ClaudeCAD 1.2.3', '')), BASE);
    assert.equal(run('1.2.3', notes('ClaudeCAD 1.2.3', '## C. Mô tả\n\n```markdown\n\n```\n')), BASE);
    assert.equal(run('1.2.3', notes('ClaudeCAD 1.2.3', '## C. Mô tả\n\nchỉ có chữ\n')), BASE);
    assert.equal(run('1.2.3', '# ClaudeCAD 1.2.3\n\n## C. Mô tả\n\n```markdown\n- chưa đóng khối\n'), BASE);
    assert.equal(run('1.2.3', null), BASE);
  });

  it('exits 0 even when the release description cannot be written', () => {
    const dir = fs.mkdtempSync(path.join(os.tmpdir(), 'claudecad-notes-'));
    try {
      const notesFile = path.join(dir, 'notes.md');
      fs.writeFileSync(notesFile, notes('ClaudeCAD 1.2.3', C), 'utf8');
      execFileSync(process.execPath, [script, '1.2.3', path.join(dir, 'missing', 'release-notes.md'), notesFile], { stdio: 'pipe' });
      execFileSync(process.execPath, [script], { stdio: 'pipe' });
    } finally {
      fs.rmSync(dir, { recursive: true, force: true });
    }
  });

  it('the notes in the repository, when they have a section C block, yield highlights for their version', () => {
    const file = path.join(repo, 'docs', 'ghi-chu-phat-hanh-nhap.md');
    if (!fs.existsSync(file)) return;
    const text = fs.readFileSync(file, 'utf8');
    if (!/^##\s+C\.\s[^\n]*\n(?:(?!^##\s)[\s\S])*?^```[^\n]*\n\s*\S/m.test(text)) return;
    const version = /\d+\.\d+\.\d+/.exec(text.split(/\r?\n/).find((l) => /^#\s/.test(l)) ?? '')?.[0];
    assert.ok(version, 'the notes title names the version they are written for');
    const dir = fs.mkdtempSync(path.join(os.tmpdir(), 'claudecad-notes-'));
    try {
      const target = path.join(dir, 'release-notes.md');
      fs.writeFileSync(target, BASE, 'utf8');
      execFileSync(process.execPath, [script, version, target], { stdio: 'pipe' });
      assert.notEqual(fs.readFileSync(target, 'utf8'), BASE);
    } finally {
      fs.rmSync(dir, { recursive: true, force: true });
    }
  });
});
