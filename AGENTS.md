# AGENTS.md

## 1) Projekt u jednoj recenici
Gradimo Unity WebAR iskustvo za Digitron DB-800/DB-801 koristeci Imagine WebAR plugin, s fokusom na stabilan placement flow, open animaciju i hotspot info sloj.

## 2) Tehnicki stack i platforma
- Engine: Unity `2022.3.36f1`
- Primarna platforma: `WebGL` + mobilni browser (WebAR)
- AR sloj: `Assets/Imagine/Common` i `Assets/Imagine/WorldTracker`
- Kljucna scena: `Assets/Scenes/Digitron AR Base 3.unity`
- JS bridge: `Assets/Scripts/LibraryManager.cs` + `Assets/Plugins/Jankec.jslib`

## 3) Sto tocno gradimo (scope ove faze)
- Jedan glavni 3D objekt: digitron `DB_801_03.fbx`
- Placement UI: "Postavi u prostor"
- Nakon placementa:
  - prikaz modela
  - gumb `Otvori`
  - animacija samo segment `100-150`
  - nakon animacije stanje ostaje otvoreno
  - hotspotovi s info boxom
- `Reset` vraca stanje na pocetak

## 4) Kljucna pravila koja agent mora postovati
- Ne mijenjati Imagine WebAR / WorldTracker arhitekturu bez izricitog razloga.
- Ne uvoditi novi AR framework.
- AR runtime i Editor fallback moraju biti odvojeni.
- Editor-only logika ide u `#if UNITY_EDITOR`.
- Ne raditi destruktivne cleanup promjene bez potvrde korisnika.
- Ako je worktree neocekivano promijenjen, pauzirati i pitati korisnika.

## 5) Trenutno stanje koda (za handoff)
- `MainController` koordinira:
  - placement hookove (`OnPlacedOrigin`, `OnResetOrigin`)
  - runtime spawn digitrona
  - editor preview fallback
- `DigitronCalculatorController` vodi:
  - stanja: `Unplaced`, `PlacedClosed`, `Opening`, `Opened`
  - `Otvori` UI
  - hotspotove i info panel
- Build scena je postavljena na:
  - `ProjectSettings/EditorBuildSettings.asset` -> `Assets/Scenes/Digitron AR Base 3.unity`
- Model source-of-truth (aktivni):
  - Scene-assigned prefab na `MainController`:
    `Assets/Models/DIGITRON stara animacija/NOVI-OBJEKT/db801-novo-odvojene-tipke.fbx`
- Runtime model source-of-truth (WebGL + Editor Play):
  - prvo koristi scene template `Digitron Editor Preview` (klon/instanca u runtimeu)
  - tek ako to nije dostupno pada natrag na `MainController.m_DigitronPrefab`
- Open animacija source-of-truth:
  - `Assets/Models/DIGITRON stara animacija/NOVI-OBJEKT/CalculatorOpen.anim`
  - klip je serijaliziran na `MainController` kao runtime referenca i prosljeđuje se `DigitronCalculatorController` umjesto editor-only `AssetDatabase` lookupa
- Runtime load put (trenutno):
  - scene template `Digitron Editor Preview` ili `MainController.m_DigitronPrefab`
  - `Digitron Editor Preview` u sceni je postavljen kao neaktivan template (ne sluzi kao stalno vidljiv editor model)
- Napomena za model import:
  - scene preview instanca trenutno nosi bitne active-state overrideove dobrog modela; zato je ne treba brisati iz scene bez zamjene istim prefab variant setupom
- Editor fallback model load put:
  - `Assets/Models/DIGITRON stara animacija/NOVI-OBJEKT/db801-novo-odvojene-tipke.fbx`
- Web runtime mode:
  - mobile: puni WebAR flow (kamera, placement, reset, otvori + hotspot)
  - desktop: auto desktop preview bez kamere, s rotacijom i zoom kontrolama; Unity se pokrece neovisno o `wTracker` inicijalizaciji
- GitHub Pages preview flow (novo):
  - deploy source: `docs/` (kopija lokalnog Unity `Build/`)
  - sync skripta: `tools/sync-pages-build.ps1`
  - workflow: `.github/workflows/deploy-pages.yml`

## 6) Poznati operativni rizici
- U Editor `Game` pogledu AR kamera moze davati crn ekran ako ostane aktivan AR feed flow.
- Mogu postojati razlike izmedju modela u `Assets/Models/...` i kopije u `Assets/Resources/...`.
- Ako `MainController` opet bude usmjeren na `Assets/Resources/Digitron/DB_801_03.fbx`, WebGL ce se vratiti na krivi/plain fallback model.
- Scene wiring se lako razbije ako se dira `WorldTracker`, `MainObject`, `Placement Canvas`, `Placement Indicator`.

## 7) Obavezna provjera prije vecih promjena
- Provjeri `ProjectSettings/EditorBuildSettings.asset`.
- Provjeri da je `WorldTracker` i dalje povezan na `MainController` evente.
- Provjeri koji DB_801 model se stvarno koristi u runtimeu.
- Provjeri da u Console nema compile errors prije daljnjeg rada.

## 8) Jasni acceptance kriteriji
- WebAR runtime:
  - placement radi
  - `Reset` radi
  - `Otvori` pokrece segment 100-150 i ostaje otvoreno stanje
  - hotspot klik prikazuje ispravan info
- Unity editor fallback:
  - model je vidljiv
  - `Game` nije crn
  - moguce je testirati `Otvori` i hotspotove bez pravog AR feeda

## 9) Handoff zadatak za Claude (sljedeci agent)
Claude preuzima fine tuning i stabilizaciju, ne novi redesign:
1. Stabilizirati editor fallback kadar tako da je klik test ugodan.
2. Potvrditi da se animacija `100-150` pouzdano okida i zavrsava.
3. Uskladiti model source (Models vs Resources) da nema duplih izvora istine.
4. Potvrditi hotspot anchor pozicije na stvarnom modelu.
5. Sacuvati kompatibilnost sa WebAR runtime flowom.

## 10) Pravilo azuriranja ovog dokumenta
Kad se donese nova stabilna odluka (platforma, scena, runtime flow, model source), odmah azurirati `AGENTS.md` u istoj promjeni.
