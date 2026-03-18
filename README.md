# digitron-ar-kalkulator

Unity WebAR projekt za prezentaciju Digitron DB-800/DB-801 modela kroz Imagine WebAR plugin.

## Cilj projekta
Napraviti "thin" AR bazu koja zadrzava postojece tracking i placement mehanizme, ali uklanja legacy narativni sloj.

Korisnicki flow:
1. Korisnik ulazi u AR scenu.
2. Klikne `Postavi u prostor`.
3. Digitron se prikaze na odabranoj poziciji.
4. Klikne `Otvori`.
5. Odigra se animacija segment `100-150`.
6. Nakon toga su aktivni hotspotovi s info panelom.
7. `Reset` vraca stanje na pocetak.

## Tehnologija
- Unity `2022.3.36f1`
- WebGL build target
- Imagine WebAR / WorldTracker (lokalno u `Assets/Imagine`)

## Trenutni glavni asseti
- Scena: `Assets/Scenes/Digitron AR Base 3.unity`
- Runtime kontroler: `Assets/Scripts/MainController.cs`
- Digitron logika: `Assets/Scripts/DigitronCalculatorController.cs`
- Model source: `Assets/Models/DIGITRON stara animacija/DB_801_03.fbx`
- Runtime load kopija: `Assets/Resources/Digitron/DB_801_03.fbx`

## Vazno za sljedece agente
- Source-of-truth za nacin rada i handoff je `AGENTS.md`.
- Ne mijenjati AR stack bez dogovora.
- Editor fallback i WebAR runtime moraju ostati odvojeni.
- Prije vecih promjena provjeriti Console i scene wiring (`WorldTracker`, `ARCamera`, `MainObject`, `Placement Canvas`).

## Brzi test checklist
- U Editor Play modu model je vidljiv i klikabilan.
- `Otvori` mijenja stanje u `Opened`.
- Hotspot klik prikazuje ispravan info box.
- U WebAR toku placement/reset ostaju funkcionalni.
- U WebGL desktop browseru (bez mobitela) ucita se desktop preview bez kamere.

## GitHub Pages preview (mobitel)
Za brzi preview na mobitelu koristimo `docs/` + GitHub Pages deployment workflow.

1. Napravi svjezi Unity WebGL build u lokalni `Build/`.
2. Sinkroniziraj build u `docs/`:
   - `powershell -ExecutionPolicy Bypass -File .\tools\sync-pages-build.ps1`
3. Commit + push (`docs/` i workflow).
4. U GitHub repo settings ukljuci Pages source: `GitHub Actions`.
5. Nakon uspjesnog workflowa otvori URL:
   - `https://<github-user>.github.io/<repo-name>/`
