# FuncDraw Web Player Events Reference

This document describes the event payloads emitted by the FuncDraw web player (FuncDraw Play browser client) and delivered to a scene stepper via `pushEvent`.

## Delivery & Ignore Semantics

- Events are sent to the scene endpoint as JSON: `POST /__funcdraw/scene { events: [ ... ] }`.
- The stepper is called once per queued event.
- If the stepper returns `null`, the event is ignored and the model state does not change.
- When all provided events are ignored, the scene endpoint returns `null` and the web player does not update the current scene.

## Event Types

### Pointer Event

Sent for pointer interactions on the canvas when the current scene exposes a stepper (`step` marker is `"<step>"`).

```json
{
  "type": "pointer",
  "action": "down",
  "pointer": {
    "id": 1,
    "type": "mouse",
    "isPrimary": true,
    "down": true,
    "captured": true,
    "pressure": 0.5,
    "tangentialPressure": 0,
    "tiltX": 0,
    "tiltY": 0,
    "twist": 0,
    "width": 1,
    "height": 1
  },
  "button": 0,
  "buttons": 1,
  "modifiers": { "alt": false, "ctrl": false, "meta": false, "shift": false },
  "point": { "x": 1.25, "y": -0.5 },
  "time": 0.0
}
```

- `action`: `"down" | "up" | "move" | "enter" | "leave" | "over" | "out" | "cancel" | "gotcapture" | "lostcapture" | "rawupdate"`.
- `point.x/y`: world coordinates in the current viewbox coordinate system.
- `time`: current timeline `t` (seconds) at the time of the event.
- `pointer`: pointer metadata (`id`, `type`, `isPrimary`, `down`, `captured`, `pressure`, `tangentialPressure`, `tiltX`, `tiltY`, `twist`, `width`, `height`). `width`/`height` are world units.
- The web player calls `setPointerCapture(pointer.id)` on `"down"` so you keep receiving pointer events during drags; `pointer.captured` reflects that state.

## FuncScript Stepper Pattern

Return `null` for events you don’t handle:

```funcscript
step: (event) =>
  eval if event.type == "pointer" && event.action == "down" then
  {
    state: { /* next state */ };
    events: [];
  }
  else null;
```
