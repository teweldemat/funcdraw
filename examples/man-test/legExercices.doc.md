# Leg Exercise Scene

Snapshot scene that lifts each leg in place and swings both hands in circles. Use it to check IK targets and the new path guides in `art/legExercise.fs`.

- **Run:** `npm run play -- --exp art.legExercise --t 0` (add `--dump` to inspect payloads; change `--t` to scrub time)
- **Inputs:** `constants.legExercise` controls `heroPosition`, foot lift/offset scales, hand attachment/offset/lengths, and `groundHalfSpan`.
- **Paths:** Dashed green segments show planned foot arcs; cyan rings show the hand effector orbits around their anchors.
- **Pose hooks:** Leg targets come from `leftLegTarget`/`rightLegTarget`; hand targets orbit with `handReach` at the configured attachment height.

Use the dump output to confirm effector coordinates and path geometry while tweaking constants.***
