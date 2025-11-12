# Live Demo Script

## Scene 1 — “Let’s draw a line” (t = 0s → ~15s)
1. Show the target line expression `{ type: 'line'; from: [0,0]; to: [10,10]; }` (without `eval`) and place the caret at the start.
2. Insert a blank line and type the helper variables:
   - `teta: 0.1;`
   - `r: 10;`
   - `x: r*math.cos(teta);`
   - `y: r*math.sin(teta);`
3. To parameterize the line, prepend `eval ` and edit the expression to `{ type: 'line'; from: [0,0]; to: [x,y]; }`. The info panel subtitle switches to “Let’s parameterize the expression.”
4. Update `teta` twice:
   - Replace `0.1` with `math.pi` (caret jumps to the value, delete, type the new expression).
   - Replace `math.pi` with `math.pi/2` using the same sequence.
5. The preview switches from the literal `[10,10]` line to the computed `[x,y]` as soon as each edit completes.

## Scene 2 — “Let’s draw ten lines” (t ≈15s → ~30s)
1. Continue from Scene 1’s finished code and move the caret to the top of the entry field.
2. Insert `range(0,10) map (i)=>{` on a new line above the helper definitions.
3. Jump to the `teta` value, delete `math.pi/2`, and type `2*i*math.pi/10` so each iteration rotates evenly around the circle.
4. Append a closing `}` after the `eval { … }` block.
5. When the wrapper is complete, the subtitle flashes “Voilà,” the preview renders all ten lines fanning out, and the entry continues to show the mapped expression.

## Scene 3 — “Reinforce the sticks” (t ≈30s → ~45s)
1. Continue from Scene 2’s finished code.
2. Lift `r:10;` out of the mapped block: cut it from inside `range(0,10)…{}` and paste it above the range call so `r` becomes a shared value.
3. Tag the mapped expression with `sticks:` (insert the label at the very start of the block).
4. After the closing `}`, add the circle primitive `ri: { type: 'circle'; center: [0,0]; radius: r; };` so it sits outside the sticks definition but still reuses `r` (matching the stick length).
5. Append `eval [sticks, ri];` to render both primitives together; the subtitle switches to “Now it’s better.”

## Scene 4 — “Let’s make it spin, because why not?” (t ≈45s → ~60s)
1. Keep Scene 3’s expression visible and insert a new helper `spin:t*math.pi/4;` so time drives the animation.
2. Update the `teta` line inside the `sticks` map to `teta:2*i*math.pi/10 + spin;` so each frame adds the live phase.
3. Preview now shows the circle of sticks slowly rotating; the entry stays focused on the `sticks` definition while the subtitle callout highlights the playful “why not” attitude.

## Scene 5 — “Now you saw how easy it is…” (t ≈60s → wrap)
1. Fade into a hero message: overlay a translucent panel centered on the preview with the text “Now you saw how easy it is, check out the other examples and start creating your own animation! Enjoy!”.
2. Leave the spinning sticks animation from Scene 4 running in the background so the motion keeps playing behind the translucent panel.
3. Keep the final expression visible in the entry field so viewers know exactly what produced the effect.
