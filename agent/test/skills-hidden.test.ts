/** v0.9.6: skills the owner hid from the Google Sheet ("hidden": true in skills.json) are never loaded. */
import assert from 'node:assert/strict';
import fs from 'node:fs';
import os from 'node:os';
import path from 'node:path';
import { describe, it } from 'node:test';
import { enabledSkills, hiddenSkills, librarySkills, prepareSkillsPlugin } from '../src/agent/skills.js';

function library(): string {
  const dir = fs.mkdtempSync(path.join(os.tmpdir(), 'claudecad-hidden-'));
  for (const n of ['cty-a', 'cty-b', 'mine']) {
    fs.mkdirSync(path.join(dir, n));
    fs.writeFileSync(path.join(dir, n, 'SKILL.md'), `---\nname: ${n}\ndescription: test skill for hidden skills, long enough.\n---\n`);
  }
  fs.writeFileSync(path.join(dir, 'skills.json'), JSON.stringify({
    version: 1,
    skills: [
      { name: 'cty-a', enabled: true, origin: 'admin', adminName: 'cty-a', rev: '1', hidden: true },
      { name: 'cty-b', enabled: true, origin: 'admin', adminName: 'cty-b', rev: '2' },
      { name: 'mine', enabled: true },
    ],
  }));
  return dir;
}

describe('skills hidden by the owner', () => {
  it('are not in the library nor among the enabled skills', () => {
    const dir = library();
    assert.deepEqual([...hiddenSkills(dir)], ['cty-a']);
    assert.deepEqual(librarySkills(dir), ['cty-b', 'mine']);
    assert.deepEqual(enabledSkills(dir), ['cty-b', 'mine']);
  });

  it('are not copied even when a chat still lists them', () => {
    const dir = library();
    const work = fs.mkdtempSync(path.join(os.tmpdir(), 'claudecad-hidden-work-'));
    const p = prepareSkillsPlugin(dir, work, ['cty-a', 'cty-b'])!;
    assert.deepEqual(p.skills, ['cty-b']);
    assert.ok(!fs.existsSync(path.join(p.pluginDir, 'skills', 'cty-a')));
  });

  it('a registry without the new fields works as before', () => {
    const dir = library();
    fs.writeFileSync(path.join(dir, 'skills.json'), JSON.stringify({ version: 1, skills: [{ name: 'mine', enabled: false }] }));
    assert.deepEqual(hiddenSkills(dir).size, 0);
    assert.deepEqual(enabledSkills(dir), ['cty-a', 'cty-b']);
  });
});
