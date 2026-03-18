# Claude Handoff - Digitron AR (Unity WebAR)

## Project Goal
Build and stabilize a thin Unity WebAR experience for Digitron DB-800/DB-801, based on existing Imagine WebAR tracking and placement flow (do not replace AR stack).

## Tech + Runtime Constraints
- Unity: `2022.3.36f1`
- Primary target: `WebGL` + mobile browser WebAR
- AR stack: `Assets/Imagine/Common` + `Assets/Imagine/WorldTracker`
- Active working scene: `Assets/Scenes/Digitron AR Base 3.unity`

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
  - Placement/reset hooks, Web runtime split (mobile AR vs desktop preview), lighting and spawn parent resolution.
- `Assets/Scripts/DigitronCalculatorController.cs`
  - State machine: `Unplaced`, `PlacedClosed`, `Opening`, `Opened`
  - Open action + hotspot/info behavior.
- `Assets/Scripts/WebDesktopOrbitZoom.cs`
  - Desktop preview drag rotate + zoom controls.
- Web template:
  - `Assets/WebGLTemplates/wTracker/index.html`
  - `Assets/WebGLTemplates/wTracker/navigation.js`
  - `Assets/WebGLTemplates/wTracker/TemplateData/navigation.css`

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
1. Desktop issue (critical):
   - Current behavior: desktop URL ostaje na pozadinskom/loading ekranu i ne prikazuje model.
   - Expected: desktop auto-preview bez kamere, model odmah vidljiv, orbit+zoom aktivni.
2. Mobile issue (critical):
   - Current behavior: model je i dalje krivo orijentiran nakon placementa.
   - Expected: model mora biti +180° prema kameri (tipke/zaslon prema korisniku).
3. Re-verify open/hotspot runtime:
   - `Otvori` dostupno odmah nakon prvog placementa.
   - Animacija 100-150 -> `Opened` i hotspotovi vidljivi.
4. Keep WebAR placement/reset flow intact:
   - indikator (strelica) vidljiv odmah na startu i nakon svakog reseta.

## Latest Commits (chronological)
- `4d626cc` Add GitHub Pages preview deployment for WebGL build
- `3e242f0` Fix GitHub Pages loading by adding uncompressed WebGL fallback
- `ad08965` Stabilize WebAR flow and add auto desktop preview mode
- `e06eead` Fix desktop startup by initializing wTracker before Unity runtime

## Current User-Reported Regression (must reproduce first)
- Desktop: shows only background/loading state, no usable preview UI.
- Mobile: placement radi, ali model je i dalje krivo okrenut.

## Done Criteria
- Editor Play Mode: model visible, open button usable, hotspots clickable.
- WebAR runtime: placement + reset work, open sequence works once, hotspots stable.
- Desktop Web: no camera prompt, immediate model preview, mouse rotate + zoom works.
- No compile errors, no accidental AR stack replacement.
