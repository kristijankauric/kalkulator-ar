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
    `Assets/Resources/Digitron/db801-novo-odvojene-tipke.fbx` (guid `5af528a9cfea01e49bfb8f0c0993d6e7`)
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
  - `Assets/Resources/Digitron/db801-novo-odvojene-tipke.fbx`
- Web runtime mode:
  - mobile: puni WebAR flow (kamera, placement, reset, otvori + hotspot)
  - desktop: auto desktop preview bez kamere, s rotacijom i zoom kontrolama; Unity se pokrece neovisno o `wTracker` inicijalizaciji
  - mobile spawn rotacija je runtime-forced na `X=180, Z=180`, uz automatski fallback korekciju ako model ostane naglavacke
  - mobile pinch zoom-out je blago prosiren kroz `PinchToScale.minScale` u sceni
- Hotspot vizualna pravila (stabilizirano):
  - odabrani hotspot dio ostaje potpuno vidljiv; svi ostali dijelovi se boje u cvrstu zutu (podloga stil) radi jasnog fokusa
  - hotspot naslov se renderira u jednom redu, s vecom podlogom od teksta
  - hotspot naslov i podloga koriste depth test (bez probijanja kroz 3D model)
- Info panel pravilo:
  - podloga info boxa je posvijetljena dodatnim 50% white overlay slojem preko papir teksture
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

## 11) Status display brojeva (handoff)
- Trenutno stanje:
  - brojke se ponovno prikazuju u runtimeu (Editor + Build)
  - računanje radi (`DigitronRuntime` i key input logovi pokazuju ispravan `display=...`)
  - brojke se ne prikazuju sa stražnje strane (front-side uvjet aktivan)
  - poravnanje je desno i punjenje ide ulijevo (novi broj ulazi s lijeve strane)
  - boja brojeva je crvena
- Implementacija je u:
  - `Assets/Scripts/DigitronCalculatorController.cs`
  - ključne metode: `EnsureDisplayAnchor()`, `EnsureDisplayText()`, `UpdateDisplayText()`, `UpdateDisplayTextVisibility()`
  - modelski `TextMesh` (`TextPlus`) se koristi kao primarni display path
  - pozicija se pokušava zaključati na `plocica_01b/plocica_01` bounds (display surface)
- Status:
  - pozicija brojeva je stabilizirana (brojke sjedaju u svjetliji display prozor)
  - vidljivost je stabilizirana (brojke se ne prikazuju sa straznje strane i ne probijaju kroz kuciste iz gornjih kutova)
  - `digital-7 (mono)` je ujednacen u Editor + WebGL buildu
- Ako se ponovno razbije:
  - pogledati sekciju `12) BRZI RESTORE - display brojke i font`

## 12) BRZI RESTORE - display brojke i font (stabilno stanje 2026-03-30)
- Glavni file:
  - `Assets/Scripts/DigitronCalculatorController.cs`
- Potvrdene vrijednosti za dobar polozaj brojki:
  - `KDisplaySurfaceRightFactor = 0.36f`
  - `KDisplaySurfaceUpFactor = 0.62f`
  - `KGeneratedDisplayTextLocalPosition = new Vector3(-0.061f, 0.151f, -0.001f)`
- Gdje se primjenjuje pozicija:
  - metoda `EnsureDisplayText()`
  - runtime-generated `Digitron Display Text` koristi `KGeneratedDisplayTextLocalPosition`
  - model-surface pozicioniranje koristi `KDisplaySurfaceRightFactor` i `KDisplaySurfaceUpFactor`
- Font koji mora biti isti u Editor + Build:
  - konstanta: `KDisplayFontResourcesPath = "digital-7 (mono)"`
  - ucitavanje ide kroz `EnsureDisplayFont()`
  - prvo: `Resources.Load<Font>(KDisplayFontResourcesPath)` (radi u buildu)
  - fallback samo u editoru: `AssetDatabase.LoadAssetAtPath<Font>("Assets/Models/digital-7 (mono).ttf")`
  - GUI style takoder koristi isti `KDisplayFontResourcesPath`
- Brza provjera nakon promjene:
  - u Play modu upisi vise znamenki (`555555...`) i provjeri da sjede u svjetlijem display pravokutniku
  - provjeri da je font `digital-7 (mono)` i u Editoru i u WebGL buildu

## 13) Otvoreno - hotspot label clipping/culling (2026-03-30)
- Status: NIJE potpuno rijeseno.
- Simptom:
  - pod odredenim rotacijama i dalje djelomicno nestaju hotspot naslovi/podloge i/ili linije
  - vidljivost je bolja nego prije, ali problem nije 100% uklonjen
- Pogodeni fileovi za daljnji rad:
  - `Assets/Scripts/DigitronHotspotMarker.cs`
  - `Assets/Scripts/DigitronCalculatorController.cs`
  - `Assets/Scripts/MainController.cs`
- Napomena:
  - prije novih zahvata testirati ekstremne kutove (gore + lateralno) u desktop preview modu

## 14) Potvrdeni baseline za novi chat (2026-03-31)
- Prihvacena "OK" verzija za nastavak rada:
  - branch: `dorade`
  - commit: `e6482b6` (`Revert "Improve mobile placement lock and hide initial indicator flicker"`)
- Lokalni workspace je uskladen s deployanom verzijom (`origin/dorade`, clean `git status`).
- U sljedecem chatu tretirati `e6482b6` kao polaznu stabilnu tocku za daljnji tuning trackinga.

## 15) Hotspot natpisi - stabilizacija (2026-03-31)
- Implementirano:
  - hotspot natpisi koriste eksplicitni `targetCamera` (fallback: `Camera.main`) umjesto implicitnog oslanjanja samo na `Camera.main`
  - billboard je prebacen na `LookRotation` prema kameri (bez random rotiranja i bez zrcaljenja teksta)
  - podloga naslova je povecana na tocno 2x (sirina i visina), tekst ostaje iste velicine
  - marker root scale je vracen na mali faktor (`0.045`) radi citljive velicine natpisa
  - podloga hotspot naslova je dodatno povecana (multiplier `2.8`) bez promjene velicine teksta
  - `LabelBackground` koristi puni `BoxCollider` tako da je cijela trakica klikabilna (button zona = cijela podloga)
  - uklonjen je legacy root quad renderer za marker (marker root je sada prazan GO; vizual je samo label background + text)
  - zadrzan je overlay pristup (`ZTest Always`, `ZWrite Off`, `Cull Off`) i prosireni text bounds radi smanjenja clipping/culling artefakata
  - u desktop/editor preview modu runtime gasi scene objekte `Shadow Plane` i `Shadow` radi cisce vidljivosti labela tijekom debuga
  - mobile WebGL runtime forsira siri pinch zoom-out raspon preko `PinchToScale.minScale = 0.03` (runtime override)
- Pogođeni fileovi:
  - `Assets/Scripts/DigitronHotspotMarker.cs`
  - `Assets/Scripts/DigitronCalculatorController.cs`

## 16) Pravilo verzioniranja builda (od 2026-03-31)
- Svaki novi deploy (`commit + push + build/deploy`) mora dobiti novu verziju.
- Verzija se prikazuje na dnu ekrana malim slovima kao `vX.XX` (build badge u `docs/index.html`).
- Početna tocka ovog pravila je verzija `0.31`.
- Svaki sljedeci deploy povecava broj verzije (npr. `0.32`, `0.33`, ...), i isti broj se koristi u:
  - `APP_BUILD_VERSION` u `docs/index.html`
  - `productVersion` u Unity loader konfiguraciji (u istom fileu)

## 17) Hotspot label background + mobile pinch (2026-04-01)
- Hotspot label background vise ne koristi paper teksturu (`solo trakica`), nego cisti bijeli rectangle (`Color.white`) iza teksta.
- Velicina rectangle podloge je sada adaptivna po platformi:
  - desktop/editor: `KBackgroundScaleMultiplierDesktop = 2.4`, `KBackgroundMinLocalWidthDesktop = 3.8`, `KBackgroundMinLocalHeightDesktop = 1.4`
  - mobile: `KBackgroundScaleMultiplierMobile = 3.1`, `KBackgroundMinLocalWidthMobile = 5.4`, `KBackgroundMinLocalHeightMobile = 1.9`
- Sirina hotspot podloge je dodatno smanjena za ~25% (`KBackgroundWidthScale = 0.75`), dok visina ostaje nepromijenjena.
- Selektirani hotspot sada boji i podlogu natpisa u narancasto (isti ton kao selektirana linija).
- Label i background ostaju na billboard pristupu prema aktivnoj kameri (`targetCamera`, fallback `Camera.main`) i rotiraju se kao cjelina.
- Mobile pinch runtime override sada postavlja i `minScale` i `maxScale`:
  - `PinchToScale.minScale = 0.03` (vise zoom-out)
  - `PinchToScale.maxScale = 2.5` (kontrolirani zoom-in)
- Pogodeni fileovi:
  - `Assets/Scripts/DigitronHotspotMarker.cs`
  - `Assets/Scripts/MainController.cs`
  - `Assets/Scripts/DigitronCalculatorController.cs`

## 18) WorldTracker baseline uskladen s Jankec scenom (2026-04-01)
- U `Assets/Scenes/Digitron AR Base 3.unity` uskladene su kljucne pocetne vrijednosti s `Jankec_Mihanovic_Tuheljske_Toplice`:
  - `PinchToScale.minScale` postavljen na `0.1` (ranije `0.08`)
  - `WorldTracker.placementIndicatorSettings.placed` postavljen na `1` (ranije `0`)
- Event hookovi (`OnPlacedOrigin`/`OnResetOrigin`) i dalje ostaju runtime-bound kroz `MainController.EnsureWorldTrackerHooks()` radi postojece stabilne arhitekture.

## 19) Mobile placement snap stabilizacija (2026-04-01)
- U `Assets/Scripts/MainController.cs` mobile placement snap vise ne korigira samo `Y`, nego poravnava 3D kontakt tocku modela (`bounds.center.xz + bounds.min.y`) na placement origin.
- Dodan je kratki post-placement re-snap kroz nekoliko frameova (`MobilePlacementSnapRoutine`) kako bi se smanjilo "plutanje" dok se tracking i bounds stabiliziraju.
- Primjena je ogranicena na WebGL mobile runtime (desktop preview ostaje netaknut).

## 20) Hotfix - nestajanje modela nakon placementa (2026-04-01)
- Uoceno: na mobile buildu model se nakon placementa ponekad ne pojavi (pomak iz kadra).
- Fix:
  - `SnapMobileDigitronToSurface` vracen na sigurni `Y-only` ground snap (bez X/Z korekcije).
  - `Assets/Scenes/Digitron AR Base 3.unity` -> `WorldTracker.placementIndicatorSettings.placed` vracen na `0`.
- Zadrzan je post-placement re-snap kroz nekoliko frameova, ali samo vertikalno.

## 21) Hotfix - placement readiness fallback (2026-04-01)
- Uoceno: na nekim mobitelima `OnPlacedOrigin` moze ostati trajno suppressan ako `m_MobileReady` ne postane `true` na vrijeme.
- Fix u `Assets/Scripts/MainController.cs`:
  - `OnPlacedOrigin` sada suppressa samo prvi prerani event, a zatim aktivira fallback readiness i dopusta spawn.
  - `ForceInitialMobileResetRoutine` postavlja `m_MobileReady = true` fallback i kad `WorldTracker` privremeno nije pronaden.

## 22) Hotfix - placement watchdog (2026-04-01)
- Dodan mobile runtime watchdog u `Assets/Scripts/MainController.cs`:
  - prati stanje `WorldTracker.mainObject.activeInHierarchy`
  - ako je placement aktivan, a `Digitron` instanca nije spawnana, watchdog forsira `OnPlacedOrigin()` flow.
- `ForceInitialMobileResetRoutine` sada preskace `ResetOrigin()` ako je korisnik vec uspio postaviti objekt prije isteka odgode.
