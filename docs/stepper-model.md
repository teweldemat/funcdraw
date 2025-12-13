# FuncDraw Stepper Framework — Formal Specification

## 1. Overview

The FuncDraw stepper framework extends the stateless FuncDraw evaluation model into a stateful execution model.  
A stepper model uses externally stored state and an event-driven step function to update that state and produce new renderable output.

---

## 2. Model Semantics

### 2.1 Standard Model

A standard FuncDraw model is a pure function:

```
model : HookedVars → RenderData
```

- **HookedVars** — inputs such as time `t` or canvas metadata  
- **RenderData** — the static expression rendered for the frame  

The standard model is stateless and referentially transparent.

---

### 2.2 Stepper Model

A stepper model is defined as:

```
model : (HookedVars × State) → (RenderData × StepF)
```

Where:

- **State** — externally stored and persistent  
- **RenderData** — the current frame’s render output  
- **StepF** — the event-handling function returned by the model  

The host reads the stepper function from the top-level `step` entry when the evaluated root is a map. If the root is not a map (or lacks `step`), the retained stepper is cleared.

#### Step Function

```
step_f : Event → (NextState × EventList) | null
```

- **Event** — an input event dequeued from the event queue  
- **NextState** — updated state  
- **EventList** — output events emitted during processing  

**Return shape (reference implementation contract)**  
- Supported shape: a map `{ state, events }`. `state` must be non-null whenever `events` is non-null; a null `state` with emitted events is rejected.  
- `events` may be a single value or an FsList. A single non-null value is enqueued once; list contents are enqueued in order.  
- Any non-null return value that is **not** a map is treated as the next state and **does not** emit events.  
- A `null` return means the event was ignored: the host keeps `State` unchanged, emits no events, and does not re-evaluate the model for that event.  

> Tuple/list forms like `[nextState, events]` are **not** recognized by this reference host.

The model and step function are pure; all mutability is isolated to external state and event queues.  
The returned step function closes over the state it was created with; hosts call it with only the event payload.

---

## 3. Execution Lifecycle

The system proceeds through the following lifecycle:

### 0. Initial State
- `State = null`  
- `StepF = null`

### 1. Model Load
- The model definition is loaded or reloaded.

### 2. Evaluation
- `model(HookedVars, State) → (RenderData, StepF)`  
- `RenderData` produced.  
- `StepF` stored for subsequent event processing.

### 3. Queue Processing
For each event popped from the event queue:

```
StepResult ← StepF(Event)
if StepResult is null
    continue
(NextState, OutEvents) ← normalize(StepResult)
State ← NextState
Push OutEvents into the queue
Re-evaluate to get new StepF and RenderData
```

If `StepF(Event)` returns `null`, the event is ignored: `State` is unchanged, no events are enqueued, and the host does not re-evaluate the model for that event.

After each **applied** event (non-null step result):

```
model(HookedVars, State) → (RenderData, StepF_new)
StepF ← StepF_new
```

The newly evaluated step function replaces the previous one.

### 5. Rendering
- The most recent `RenderData` is emitted to the renderer.

### 6. Wait
- The engine sleeps until a change occurs.

### 7. Event Arrival
- If a new event enters the queue → return to **Step 3**.

### 8. Hook Variable Change
- If any hooked variable changes → return to **Step 2**.

### 9. Model Change
- If the model definition changes → return to **Step 1**.


## The FuncDraw API

### Initialization
Loading and evaluating the initial rendering data
scene = FuncDraw.LoadScene(initExpression?, listOf([name, valueHook]))
- `valueHook` functions are provided **once** at scene creation; FuncDraw calls them on demand.  
- No hook values are passed to `evaluate` or `pushEvent` calls.
- No hooks are injected by default; starting from an empty hook set is valid.
scene.evaluate() -> RenderData
scene.pushEvent(event) -> RenderData | null
scene.reset() -- clears state and timeline

When evaluation returns a stepper function, that function is retained in the scene and used to handle subsequent events.

### When an event is received

If a stepper function is retained, the event is processed through the stepper until the event queue is cleared. The final RenderData produced after processing all pending events is returned.
If every processed step returns `null`, the model state is unchanged and `pushEvent` returns `null`.

### When used hooks change

scene.evaluate() -> RenderData

When any used hook value changes, the consumer calls evaluate again to produce updated RenderData similar to initialization.
