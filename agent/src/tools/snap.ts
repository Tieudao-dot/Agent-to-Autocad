import { z } from 'zod';
import { cadTool, handle, type AnyTool, type ToolBridge } from './common.js';

/** Object snap points (docs/ke-hoach-tu-kiem.md §9, layer A). */
export function snapTools(b: ToolBridge): AnyTool[] {
  return [
    cadTool(b, 'get_snap_points',
      'Exact characteristic points of up to 50 objects, like AutoCAD object snaps: end (line / arc ends, polyline vertices), mid (middle of ' +
        'each segment), cen (arc / circle centre), ins (text / block insertion point), plus every real intersection between the given objects. ' +
        'Use it when you need the numbers; to draw from such a point, pass a snap reference ({"snap": "end", "of": handle, "near": [x, y]}) ' +
        'directly as the point parameter instead. Returns {objects: [{handle, type, layer, points: {end: [[x, y], ...], mid, cen, ins}}], ' +
        'intersections?: [{of: [h1, h2], point}]}.',
      { handles: z.array(handle).min(1).max(50).describe('Objects to read the points of (and intersect with each other).') },
      { readOnly: true }),
  ];
}
