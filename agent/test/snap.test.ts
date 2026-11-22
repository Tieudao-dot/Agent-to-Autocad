/**
 * Snap references as point parameters (docs/ke-hoach-tu-kiem.md §9, layer A): accepted by every point
 * parameter, plain [x, y] still accepted, and the same snap kinds as the plugin.
 */
import assert from 'node:assert/strict';
import fs from 'node:fs';
import path from 'node:path';
import { describe, it } from 'node:test';
import { z } from 'zod';
import { SYSTEM_PROMPT } from '../src/agent/systemPrompt.js';
import { SNAP_KINDS, point } from '../src/tools/common.js';
import { buildTools } from '../src/tools/index.js';

const tools = buildTools({ call: async () => ({ id: 'x', ok: true, result: {} }), status: () => {} });
const schemaOf = (name: string) => z.object(tools.find((t) => t.name === name)!.inputSchema as z.ZodRawShape);

describe('snap references', () => {
  it('plain points still parse', () => {
    assert.equal(point.safeParse([4000, 3600]).success, true);
    assert.equal(point.safeParse([1, 2, 3]).success, true);
    assert.equal(point.safeParse([1]).success, false);
  });

  it('a snap reference parses wherever a point does', () => {
    const ref = { snap: 'end', of: '2F1A', near: [3890, 2550] };
    assert.equal(point.safeParse(ref).success, true);
    assert.equal(schemaOf('draw_line').safeParse({ start: ref, end: { snap: 'per', of: '2F1C', from: 'start' } }).success, true);
    assert.equal(schemaOf('draw_polyline').safeParse({ vertices: [[0, 0], ref, { snap: 'mid', of: '2F1C' }] }).success, true);
    assert.equal(schemaOf('draw_arc').safeParse({ center: { snap: 'end', of: '2F1A', near: [0, 0] }, radius: 900, start_angle: 0, end_angle: 90 }).success, true);
  });

  it('an object without "snap" is not a point', () => {
    assert.equal(point.safeParse({ of: '2F1A' }).success, false);
  });

  it('the point schema stays small (it is repeated in every point parameter)', () => {
    assert.ok(JSON.stringify(z.toJSONSchema(point)).length < 450);
  });

  it('knows the same snap kinds as the plugin', () => {
    const cs = fs.readFileSync(path.resolve(import.meta.dirname, '..', '..', '..', 'plugin', 'ClaudeCAD.Plugin', 'Core', 'SnapRules.cs'), 'utf8');
    const block = /Kinds\s*=\s*\{([^}]*)\}/.exec(cs)?.[1] ?? '';
    assert.deepEqual([...block.matchAll(/"([a-z]+)"/g)].map((m) => m[1]), [...SNAP_KINDS]);
  });

  it('get_snap_points is declared', () => {
    assert.equal(schemaOf('get_snap_points').safeParse({ handles: ['2F1A', '2F1B'] }).success, true);
    assert.equal(schemaOf('get_snap_points').safeParse({ handles: [] }).success, false);
  });

  it('the system prompt explains the format once', () => {
    assert.match(SYSTEM_PROMPT, /# Snapping/);
    for (const k of SNAP_KINDS) assert.match(SYSTEM_PROMPT, new RegExp(`"${k}"`));
    assert.match(SYSTEM_PROMPT, /"@n"/);
  });
});
