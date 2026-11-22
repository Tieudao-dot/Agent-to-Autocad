/**
 * Self-check before a turn reports done (docs/ke-hoach-tu-kiem.md).
 *
 * The tracker follows the tool calls of one turn: did the turn change the drawing, did verify_drawing run after
 * the last change, and how many issues it left. The Stop hook asks `stopDecision` whether Claude may end the
 * turn; when not, Claude gets the reason as feedback and the turn goes on.
 */
import { VERIFY_TOOL } from '../tools/verify.js';
import { affectedCount, createdHandles } from './confirmation.js';

/** Fix rounds: blocks because verify_drawing still reported issues. After that the turn may end (issues listed). */
export const MAX_FIX_ROUNDS = 2;
/** Blocks because verify_drawing did not run after the last change (Claude forgot / changed things again). */
export const MAX_VERIFY_NAGS = 2;

export type StopDecision = { block: false } | { block: true; reason: string };

export class SelfCheckTracker {
  /** Successful calls of this turn that drew / changed / erased drawing objects. */
  writes = 0;
  /** `writes` at the last verify_drawing (-1 = never ran this turn). */
  verifiedAt = -1;
  /** Issues left by the last verify_drawing. */
  issues = 0;
  /** Codes of the issues left (for the feedback). */
  codes: string[] = [];
  fixRounds = 0;
  verifyNags = 0;
  /** verify_drawing itself failed (tool error): do not insist. */
  verifyFailed = false;

  reset(): void {
    this.writes = 0;
    this.verifiedAt = -1;
    this.issues = 0;
    this.codes = [];
    this.fixRounds = 0;
    this.verifyNags = 0;
    this.verifyFailed = false;
  }

  /** After each tool call of the turn (successful or not). */
  note(tool: string, params: Record<string, unknown>, resp: { ok: boolean; result?: unknown }): void {
    if (tool === VERIFY_TOOL) {
      if (!resp.ok) {
        this.verifyFailed = true;
        return;
      }
      // a check of objects the user named (handles) is not the turn's own self-check
      if (Array.isArray(params.handles) && params.handles.length > 0 && this.writes === 0) return;
      const r = (resp.result ?? {}) as { issues?: unknown; more?: unknown };
      const list = Array.isArray(r.issues) ? (r.issues as Array<{ code?: unknown }>) : [];
      const more = r.more && typeof r.more === 'object' ? Object.values(r.more as Record<string, unknown>).reduce<number>((n, v) => n + (typeof v === 'number' ? v : 0), 0) : 0;
      this.issues = list.length + more;
      this.codes = [...new Set(list.map((i) => (typeof i.code === 'string' ? i.code : '')).filter(Boolean))];
      this.verifiedAt = this.writes;
      this.verifyFailed = false;
      return;
    }
    if (!resp.ok) return;
    if (createdHandles(tool, params, resp.result).length > 0 || affectedCount(tool, params) > 0) this.writes++;
  }

  /**
   * May Claude end the turn now? `cancelled`: the user pressed Dừng (never held back).
   * Each block counts, so a turn can be held back at most MAX_VERIFY_NAGS + MAX_FIX_ROUNDS times.
   */
  stopDecision(cancelled: boolean): StopDecision {
    if (cancelled || this.writes === 0 || this.verifyFailed) return { block: false };
    if (this.verifiedAt < this.writes) {
      if (this.verifyNags >= MAX_VERIFY_NAGS) return { block: false };
      this.verifyNags++;
      return {
        block: true,
        reason:
          this.verifiedAt < 0
            ? `Self-check required: this turn changed the drawing but verify_drawing has not run. Call ${VERIFY_TOOL} now (no handles needed: it checks this turn's objects), fix what it reports, and only then report to the user.`
            : `Self-check required: the drawing changed after the last ${VERIFY_TOOL}. Call it again before reporting to the user.`,
      };
    }
    if (this.issues > 0) {
      if (this.fixRounds >= MAX_FIX_ROUNDS) return { block: false };
      this.fixRounds++;
      const left = MAX_FIX_ROUNDS - this.fixRounds;
      return {
        block: true,
        reason:
          `${VERIFY_TOOL} still reports ${this.issues} issue(s) (${this.codes.join(', ')}). Fix them with the normal tools, then call ${VERIFY_TOOL} again. ` +
          (left === 0
            ? 'This is the last fix round: after it, list every issue still open in your reply.'
            : `Fix rounds left after this one: ${left}.`),
      };
    }
    return { block: false };
  }

  /** Issues the turn ends with although it was held back MAX_FIX_ROUNDS times (for the log). */
  get endsWithIssues(): boolean {
    return this.writes > 0 && this.verifiedAt === this.writes && this.issues > 0;
  }
}
