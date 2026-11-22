/**
 * Self-check before a turn reports done (docs/ke-hoach-tu-kiem.md): the tracker, the Stop hook decision and
 * its wiring in AgentSession, and the verify_drawing tool schema.
 */
import assert from 'node:assert/strict';
import fs from 'node:fs';
import os from 'node:os';
import path from 'node:path';
import { describe, it } from 'node:test';
import { z } from 'zod';
import type { SDKUserMessage } from '@anthropic-ai/claude-agent-sdk';
import { MAX_FIX_ROUNDS, MAX_VERIFY_NAGS, SelfCheckTracker } from '../src/agent/selfCheck.js';
import { AgentSession, type QueryFn } from '../src/agent/session.js';
import { summarizeResult } from '../src/agent/content.js';
import { SYSTEM_PROMPT } from '../src/agent/systemPrompt.js';
import { buildTools } from '../src/tools/index.js';
import { VERIFY_CODES, VERIFY_TOOL } from '../src/tools/verify.js';

const ok = (result: unknown = {}) => ({ ok: true, result });
const clean = ok({ checked: 4, ok: true, issues: [] });
const dirty = (...codes: string[]) => ok({ checked: 4, ok: false, issues: codes.map((code) => ({ code, objects: ['2A'], message: 'x' })) });

describe('self-check tracker', () => {
  it('lets a read-only turn end at once', () => {
    const t = new SelfCheckTracker();
    t.note('get_drawing_info', {}, ok({ entities: 10 }));
    t.note('list_entities', { layers: ['A-WALL'] }, ok({ entities: [], total: 0 }));
    t.note('capture_view', {}, ok({ width: 100, height: 100 }));
    assert.deepEqual(t.stopDecision(false), { block: false });
  });

  it('holds back a turn that drew without verify_drawing', () => {
    const t = new SelfCheckTracker();
    t.note('draw_line', { start: [0, 0], end: [1, 0] }, ok({ handle: '2A' }));
    const d = t.stopDecision(false);
    assert.equal(d.block, true);
    assert.match((d as { reason: string }).reason, /verify_drawing/);
  });

  it('counts batch creations and changes of existing objects as writes, failed calls not', () => {
    const t = new SelfCheckTracker();
    t.note('draw_line', {}, { ok: false });
    assert.equal(t.writes, 0);
    t.note('create_layer', { name: 'A-WALL' }, ok({ name: 'A-WALL' }));
    assert.equal(t.writes, 0, 'a layer is not a drawn object');
    t.note('batch_execute', { calls: [{ tool: 'draw_line', params: {} }] }, ok({ results: [{ ok: true, result: { handle: '2B' } }] }));
    assert.equal(t.writes, 1);
    t.note('move_entities', { handles: ['1F'], displacement: [10, 0] }, ok({}));
    assert.equal(t.writes, 2);
  });

  it('a clean verify after the last change ends the turn', () => {
    const t = new SelfCheckTracker();
    t.note('draw_rectangle', {}, ok({ handle: '2A' }));
    t.note(VERIFY_TOOL, {}, clean);
    assert.deepEqual(t.stopDecision(false), { block: false });
  });

  it('a change after the verify needs a new verify', () => {
    const t = new SelfCheckTracker();
    t.note('draw_rectangle', {}, ok({ handle: '2A' }));
    t.note(VERIFY_TOOL, {}, clean);
    t.note('add_text', {}, ok({ handle: '2B' }));
    const d = t.stopDecision(false);
    assert.equal(d.block, true);
    assert.match((d as { reason: string }).reason, /changed after/);
  });

  it('verify_drawing auto_fix is not a new change', () => {
    const t = new SelfCheckTracker();
    t.note('draw_line', {}, ok({ handle: '2A' }));
    t.note(VERIFY_TOOL, { auto_fix: true }, ok({ checked: 1, ok: true, issues: [], fixed: [{ code: 'GAP', objects: ['2A'] }] }));
    assert.deepEqual(t.stopDecision(false), { block: false });
  });

  it('remaining issues hold the turn back for at most MAX_FIX_ROUNDS rounds', () => {
    const t = new SelfCheckTracker();
    t.note('draw_line', {}, ok({ handle: '2A' }));
    for (let i = 0; i < MAX_FIX_ROUNDS; i++) {
      t.note(VERIFY_TOOL, {}, dirty('GAP', 'LAYER_0'));
      const d = t.stopDecision(false);
      assert.equal(d.block, true, `round ${i + 1}`);
      assert.match((d as { reason: string }).reason, /GAP, LAYER_0/);
      t.note('set_entity_props', { handles: ['2A'], layer: 'A-WALL' }, ok({}));
    }
    t.note(VERIFY_TOOL, {}, dirty('GAP'));
    assert.deepEqual(t.stopDecision(false), { block: false });
    assert.equal(t.endsWithIssues, true);
  });

  it('the last fix round asks to list what is still open', () => {
    const t = new SelfCheckTracker();
    t.note('draw_line', {}, ok({ handle: '2A' }));
    let last = '';
    for (let i = 0; i < MAX_FIX_ROUNDS; i++) {
      t.note(VERIFY_TOOL, {}, dirty('STRAY'));
      last = (t.stopDecision(false) as { reason: string }).reason;
    }
    assert.match(last, /last fix round/);
  });

  it('counts omitted issues too', () => {
    const t = new SelfCheckTracker();
    t.note('draw_line', {}, ok({ handle: '2A' }));
    t.note(VERIFY_TOOL, {}, ok({ checked: 40, ok: false, issues: [], more: { ZERO_LEN: 11 } }));
    assert.equal(t.issues, 11);
    assert.equal(t.stopDecision(false).block, true);
  });

  it('stops nagging after MAX_VERIFY_NAGS when Claude never verifies', () => {
    const t = new SelfCheckTracker();
    t.note('draw_line', {}, ok({ handle: '2A' }));
    for (let i = 0; i < MAX_VERIFY_NAGS; i++) assert.equal(t.stopDecision(false).block, true);
    assert.equal(t.stopDecision(false).block, false);
  });

  it('never holds back a cancelled turn or after verify_drawing failed', () => {
    const t = new SelfCheckTracker();
    t.note('draw_line', {}, ok({ handle: '2A' }));
    assert.deepEqual(t.stopDecision(true), { block: false });
    t.note(VERIFY_TOOL, {}, { ok: false });
    assert.deepEqual(t.stopDecision(false), { block: false });
  });

  it('a check of objects the user named is not the self-check of an unchanged turn', () => {
    const t = new SelfCheckTracker();
    t.note(VERIFY_TOOL, { handles: ['1F'] }, dirty('GAP'));
    assert.equal(t.issues, 0);
    assert.deepEqual(t.stopDecision(false), { block: false });
  });

  it('reset starts a new turn', () => {
    const t = new SelfCheckTracker();
    t.note('draw_line', {}, ok({ handle: '2A' }));
    t.stopDecision(false);
    t.reset();
    assert.deepEqual(t.stopDecision(false), { block: false });
    assert.equal(t.verifyNags, 0);
  });
});

function idleQuery(): QueryFn {
  return ((params: { prompt: AsyncIterable<SDKUserMessage> }) => {
    void params;
    const gen = (async function* () {
      await new Promise(() => {});
    })();
    return Object.assign(gen, { interrupt: async () => {}, close: () => {} });
  }) as unknown as QueryFn;
}

describe('Stop hook in the session', () => {
  function session() {
    const logs: string[] = [];
    const s = new AgentSession({
      emit: () => {},
      bridge: { call: async (id) => ({ id, ok: true, result: {} }), status: () => {} },
      version: 'test',
      workDir: fs.mkdtempSync(path.join(os.tmpdir(), 'claudecad-selfcheck-')),
      log: (m) => logs.push(m),
      queryFn: idleQuery(),
    });
    return { s, logs };
  }

  it('is registered in the query options', async () => {
    const { s } = session();
    const opts = s.buildOptions();
    const stop = opts.hooks?.Stop;
    assert.ok(stop && stop.length === 1 && stop[0]!.hooks.length === 1);
    const out = await stop[0]!.hooks[0]!({ hook_event_name: 'Stop' } as never, undefined, { signal: new AbortController().signal });
    assert.deepEqual(out, {}, 'no turn running: never blocks');
  });

  it('blocks a turn that drew, until verify_drawing ran clean, then a new message starts fresh', () => {
    const { s, logs } = session();
    s.send('vẽ phòng 4x3m');
    s.noteResult('draw_rectangle', {}, ok({ handle: '2A' }));
    const out = s.onStop() as { decision?: string; reason?: string };
    assert.equal(out.decision, 'block');
    assert.match(out.reason ?? '', /verify_drawing/);
    assert.ok(logs.some((l) => l.includes('self-check: turn held back')));
    s.noteResult(VERIFY_TOOL, {}, clean);
    assert.deepEqual(s.onStop(), {});
    // next turn: nothing drawn yet
    s.send('đếm số layer');
    assert.deepEqual(s.onStop(), {});
  });

  it('does not hold back a stopped turn', async () => {
    const { s } = session();
    s.send('vẽ');
    s.noteResult('draw_line', {}, ok({ handle: '2A' }));
    await s.cancel();
    assert.deepEqual(s.onStop(), {});
  });
});

describe('verify_drawing tool', () => {
  const bridge = { call: async () => ({ id: 'x', ok: true as const, result: {} }), status: () => {} };
  const tool = buildTools(bridge).find((t) => t.name === VERIFY_TOOL)!;

  it('is declared to Claude with optional parameters only', () => {
    assert.ok(tool);
    const schema = z.object(tool.inputSchema as z.ZodRawShape);
    assert.equal(schema.safeParse({}).success, true);
    assert.equal(schema.safeParse({ expect: { 'A-WALL': 4 }, gap: 10, ignore: ['TEXT_OVERLAP'], auto_fix: false }).success, true);
    assert.equal(schema.safeParse({ ignore: ['NOT_A_CODE'] }).success, false);
    assert.equal(schema.safeParse({ gap: 0 }).success, false);
    assert.equal(schema.safeParse({ expect: { 'A-WALL': -1 } }).success, false);
  });

  it('knows the same codes as the plugin', () => {
    const cs = fs.readFileSync(path.resolve(import.meta.dirname, '..', '..', '..', 'plugin', 'ClaudeCAD.Plugin', 'Core', 'VerifyRules.cs'), 'utf8');
    const block = /Codes\s*=\s*\{([^}]*)\}/.exec(cs)?.[1] ?? '';
    const csCodes = [...block.matchAll(/"([A-Z_0-9]+)"/g)].map((m) => m[1]);
    assert.deepEqual(csCodes, [...VERIFY_CODES]);
  });

  it('has a Vietnamese activity summary', () => {
    assert.deepEqual(summarizeResult(VERIFY_TOOL, { checked: 12, ok: true, issues: [] }), { summary: 'kiểm 12, không lỗi' });
    assert.deepEqual(
      summarizeResult(VERIFY_TOOL, { checked: 12, ok: false, issues: [{ code: 'GAP' }], more: { STRAY: 2 }, fixed: [{ code: 'GAP' }] }),
      { summary: 'kiểm 12, 3 lỗi, tự sửa 1' },
    );
  });

  it('is required by the system prompt before reporting done', () => {
    assert.match(SYSTEM_PROMPT, /BEFORE you tell the user it is done, call verify_drawing/);
    assert.match(SYSTEM_PROMPT, /MUST list the issues still open/);
  });
});
