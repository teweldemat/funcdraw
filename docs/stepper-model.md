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

#### Step Function

```
step_f : Event → (NextState × EventList)
```

- **Event** — an input event dequeued from the event queue  
- **NextState** — updated state  
- **EventList** — output events emitted during processing  

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
StepF(Event) → (NextState, OutEvents)
State ← NextState
Push OutEvents into the queue
```

After **each** event:

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
scene.pushEvent(event) -> RenderData
scene.reset() -- clears state and timeline

When evaluation returns a stepper function, that function is retained in the scene and used to handle subsequent events.

### When an event is received

If a stepper function is retained, the event is processed through the stepper until the event queue is cleared. The final RenderData produced after processing all pending events is returned.

### When used hooks change

scene.evaluate() -> RenderData

When any used hook value changes, the consumer call evaluate again to produced update RenderData similar to initialization.
