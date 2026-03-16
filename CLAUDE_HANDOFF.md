# Claude Handoff - Digitron AR (Unity WebAR)

## Project Goal
Build and stabilize a thin Unity WebAR experience for Digitron DB-800/DB-801, based on existing Imagine WebAR tracking and placement flow (do not replace AR stack).

## Tech + Runtime Constraints
- Unity: `2022.3.36f1`
- Primary target: `WebGL` + mobile browser WebAR
- AR stack: `Assets/Imagine/Common` + `Assets/Imagine/WorldTracker`
- Active working scene: `Assets/Scenes/Digitron AR Base.unity`

## Required User Flow
1. User enters AR scene.
2. User taps `Postavi u prostor`.
3. Calculator is placed at selected point.
4. User taps `Otvori`.
5. Only animation segment `100-150` plays once.
6. Calculator stays open (no auto-close, no loop).
7. Hotspots become active and show one shared info panel.
8. `Reset` returns full initial state.

## Current Key Scripts
- `Assets/Scripts/MainController.cs`
  - Placement/reset hooks and editor fallback bridge.
- `Assets/Scripts/DigitronCalculatorController.cs`
  - State machine: `Unplaced`, `PlacedClosed`, `Opening`, `Opened`
  - Open action + hotspot/info behavior.

## Known Risks To Handle Carefully
- Editor Game view can appear black when AR feed path stays active in Play Mode.
- Model may exist in both:
  - `Assets/Models/DIGITRON stara animacija/DB_801_03.fbx`
  - `Assets/Resources/Digitron/DB_801_03.fbx`
- Scene wiring is fragile around:
  - `WorldTracker`, `ARCamera`, `MainObject`, `Placement Indicator`, `Placement Canvas`

## What NOT To Do
- Do not replace Imagine WebAR / WorldTracker.
- Do not do destructive bulk cleanup without user approval.
- Do not break WebAR runtime while improving editor preview.

## Priority Work (Next Agent)
1. Stabilize editor preview framing so model/UI are easy to test in Play Mode.
2. Validate open clip behavior (`100-150`) is deterministic and one-shot.
3. Resolve model source-of-truth (Models vs Resources) to avoid drift.
4. Verify hotspot anchor positions on actual model geometry.
5. Keep WebAR placement/reset flow intact.

## Done Criteria
- Editor Play Mode: model visible, open button usable, hotspots clickable.
- WebAR runtime: placement + reset work, open sequence works once, hotspots stable.
- No compile errors, no accidental AR stack replacement.
