import { z } from 'zod';
import { cadTool, handles, type AnyTool, type ToolBridge } from './common.js';

/** Codes verify_drawing reports (docs/ke-hoach-tu-kiem.md); kept in sync with VerifyRules.Codes (C#). */
export const VERIFY_CODES = [
  'GAP',
  'OVERSHOOT',
  'OPEN_POLY',
  'ZERO_LEN',
  'DUPLICATE',
  'ARC_MISMATCH',
  'LAYER_0',
  'TEXT_OVERLAP',
  'STRAY',
  'COUNT',
] as const;

export const VERIFY_TOOL = 'verify_drawing';

/** Self-check before a turn reports done: measured by the plugin, not by looking at a picture. */
export function verifyTools(b: ToolBridge): AnyTool[] {
  return [
    cadTool(b, VERIFY_TOOL,
      'Self-check what THIS TURN drew or changed, measured on the real drawing (not a picture). Call it after drawing / modifying and BEFORE you report ' +
        'that the task is done; the turn cannot end with unchecked or unfixed changes. Default: checks every object this turn created or changed, ' +
        'against its neighbours. Codes: GAP (two ends closer than gap_mm but not touching), OVERSHOOT (a line runs a few mm past an intersection), ' +
        'OPEN_POLY (polyline whose ends meet but is not closed), ZERO_LEN, DUPLICATE (identical object on the same layer), ARC_MISMATCH (an arc end ' +
        'touches nothing, e.g. a door swing not reaching the jamb), LAYER_0 (objects on layer 0 / Defpoints), TEXT_OVERLAP (texts / dimension texts ' +
        'overlapping), STRAY (an object far from the rest of what this turn drew), COUNT (new objects per layer differ from "expect"). ' +
        'auto_fix (default true) repairs the safe ones on this turn\'s objects only, in the same undo step: joins GAPs, closes OPEN_POLYs, erases ' +
        'DUPLICATE / ZERO_LEN copies. Fix the remaining issues with the normal tools and call it again. An issue that is intentional: pass its code ' +
        'in "ignore" and say why in your reply. Returns {checked, ok, issues: [{code, objects, message, fix, at}], fixed?, more?}.',
      {
        handles: handles
          .max(5000)
          .optional()
          .describe('Check these objects instead of the ones this turn created / changed (e.g. when the user asks to check existing objects; they are never auto-fixed).'),
        expect: z
          .record(z.string(), z.number().int().min(0))
          .optional()
          .describe('Planned number of NEW objects per layer, written down before drawing, e.g. {"A-WALL": 4, "A-DOOR": 2, "A-DIM": 7}. Enables COUNT.'),
        gap: z
          .number()
          .min(0.1)
          .max(1000)
          .optional()
          .describe('Largest gap / overshoot / open-polyline distance treated as a mistake, in mm. Default 10. Wider gaps are intentional (openings).'),
        ignore: z
          .array(z.enum(VERIFY_CODES))
          .optional()
          .describe('Codes not to report, because they are intentional here or the skill says so (explain it in the reply).'),
        auto_fix: z.boolean().optional().describe('Repair the safe issues automatically (default true). Read-only drawings are never changed.'),
      }),
  ];
}
