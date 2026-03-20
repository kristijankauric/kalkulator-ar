# AR Projekt Plan — Digitron Kalkulator

> **Status dokumenta:** Aktivan radni plan
> **Zadnje ažuriranje:** 2026-03-20
> **Namjena:** Interno — timovi, Claude agenti, developeri

---

## 1. Sažetak projekta

**Što je ovo?**
Unity WebGL AR aplikacija koja korisnicima omogućuje da u augmented reality iskustvu vide i istraže Digitron DB-800/DB-801 kalkulator iz 1970-ih. Korisnik postavlja 3D model kalkulatora u vlastiti prostor putem kamere mobitela, može ga "otvoriti" i pogledati unutarnju elektroniku kroz interaktivne hotspot oznake s opisima.

**Cilj aplikacije:**
Edukativno-muzejski AR doživljaj vezan uz Muzej Digitron Buje. AR aplikacija je komponenta šireg web projekta Digitron Buje, ali funkcionira i kao samostalna cjelina.

**Platforme:**
- Mobitel (WebAR — Chrome/Safari mobilni browser) — primarna platforma
- Desktop browser — automatski desktop preview bez kamere, s orbit/zoom kontrolama

**Tehnički stack:**
- Unity 2022.3.36f1
- WebGL build target
- Imagine WebAR / WorldTracker plugin
- GitHub Pages deploy (iz `docs/` foldera)

**Trenutni status:**
Aplikacija funkcionira end-to-end na desktopu i mobitelu. Svi ključni elementi rade:
- placement flow
- open animacija (frame 100–150)
- hotspot sustav s info panelima
- desktop orbit/zoom preview
- grafički identitet (font, boje, teksture)

Projekt je u fazi fine-tuninga i stabilizacije — nema novih velikih feature zahtjeva.

---

## 2. Trenutno stanje

### Što radi
- WebAR placement na mobitelu (kamera, postavljanje u prostor, reset)
- Desktop preview s orbit/zoom mišem i touchom
- Otvaranje kalkulatora s animacijom
- 6 hotspotova s opisima na hrvatskom
- LED display s digital-7 fontom (crvena boja)
- UI grafika: naslov, trakica, papir podloga
- GitHub Pages deploy workflow

### Sitnice koje još treba provjeriti / dovršiti
- Hotspot background veličina — bila persistentni problem, zadnji fix u kodu ali nije potvrđen u buildu
- Mobile UX — rotacija/orijentacija kalkulatora na uređajima
- Info box text veličina na desktopu
- Provjera jesu li sve texture ispravno učitane u WebGL runtime (Resources/ folder)
- Lokalizacija nije napravljena (sve je hardcode HR)
- Nema language picker/handoffa s weba

### Legacy teret
Projekt je nastao iz starijeg projekta koji je imao širi scope. Zbog toga u projektu vjerojatno postoji:
- stari modeli i prefabovi koji se ne koriste
- scene koje nisu produkcijska scena
- asseti u krivim folderima (npr. duplikati u `Assets/Models/` i `Assets/Resources/`)
- stare skripte ili komponente koje su zamijenila nova rješenja

### Rizik krivog modela
Postoje najmanje dva mjesta gdje je model: `Assets/Models/DIGITRON stara animacija/NOVI-OBJEKT/` i `Assets/Resources/Digitron/`. **Source-of-truth** je model dodeljen u Inspectoru na `MainController`, ne Resources fallback. Ako agent ili developer promijeni wiring, može se pokrenuti krivi model.

---

## 3. Preporučeni redoslijed dovršavanja

### FAZA 1 — Stabilizacija i content lock

**Cilj:** Potvrditi da je sve što postoji ispravno i zamrznuti sadržaj.

- [ ] Testirati build na stvarnom mobitelu — potvrditi orijentaciju, placement, animaciju
- [ ] Potvrditi hotspot background veličinu u buildu
- [ ] Potvrditi info box čitljivost na desktopu i mobitelu
- [ ] Potvrditi da su sve teksture (naslov, trakica, podloga) učitane u WebGL
- [ ] Potvrditi da digital-7 font radi u WebGL (nije samo editor)
- [ ] Pregledati svih 6 hotspot opisa — jesu li finalni? Odobreni?
- [ ] Odluka: je li HR verzija sadržaj finalan? → **Content lock**
- [ ] Zabilježiti sve otvorene bugove ako postoje

**Izlaz iz Faze 1:** Stabilan, testiran, sadržajno zaključan HR build.

---

### FAZA 2 — Audit i čišćenje projekta

**Cilj:** Smanjiti legacy teret, jasno odvojiti produkcijski od arhivskog sadržaja.

> ⚠️ **Pravilo:** Ova faza se radi NAKON content locka. Nikad paralelno s promjenama sadržaja.

- [ ] Asset audit — popis svih foldera, scena, modela, prefabova, skripti
- [ ] Identificirati koje scene su produkcijske, koje su legacy
- [ ] Identificirati koji model/prefab je zaista source-of-truth
- [ ] Odvojiti assete u jasnu strukturu (vidjeti Sekciju 4)
- [ ] Označiti ili premjestiti legacy sadržaj u `_Archive/` subfoldere
- [ ] Ažurirati `AGENTS.md` s novim putevima

**Izlaz iz Faze 2:** Čist projekt s jasnom strukturom, minimiziran rizik krivog modela.

---

### FAZA 3 — Priprema lokalizacije

**Cilj:** Pripremiti sustav za HR / EN / IT bez mijenjanja sadržaja.

> ⚠️ **Pravilo:** Lokalizacija se radi NAKON što je HR sadržaj konačno zaključan. Ne ranije.

- [ ] Identificirati sva hardcode mjesta s HR tekstom u kodu
- [ ] Definirati minimalni lokalizacijski sustav (JSON fajlovi ili ScriptableObject)
- [ ] Izvući tekstove iz koda u lokalizacijske fajlove
- [ ] Implementirati čitanje jezika iz URL query parametra (`?lang=hr`)
- [ ] Implementirati language picker za direktni entry (bez query parametra)
- [ ] Dostaviti HR tekstove kao bazu za prijevod EN / IT
- [ ] Implementirati EN i IT verzije nakon dostave prijevoda
- [ ] Testirati sve tri jezične verzije

**Izlaz iz Faze 3:** Višejezična aplikacija, jezik se može odabrati ili primiti od weba.

---

### FAZA 4 — Priprema web integracije

**Cilj:** Definirati i implementirati vezu između Digitron Buje weba i AR aplikacije.

- [ ] Definirati entry URL format (vidjeti `ar-web-integration.md`)
- [ ] Implementirati čitanje `?lang=` parametra u AR aplikaciji
- [ ] Definirati što se prikazuje ako `?lang=` nije prisutan
- [ ] Testirati flow: web → AR aplikacija s jezikom
- [ ] Testirati flow: direktni link → AR aplikacija bez jezika
- [ ] Provjera GDPR / camera permission entry (vidjeti Sekciju 6 ovog dokumenta)
- [ ] Uskladiti s web timom — koje URL-ove koristiti, što linkati

**Izlaz iz Faze 4:** Dokumentiran i testiran entry flow iz weba u AR aplikaciju.

---

### FAZA 5 — Završni polish i release priprema

**Cilj:** Spreman za produkcijski deploy.

- [ ] Finalni testovi na iOS i Android (Chrome + Safari)
- [ ] Finalni testovi desktop (Chrome, Firefox, Edge)
- [ ] Provjera svih jezičnih verzija end-to-end
- [ ] Provjera mobile UX-a (placement, animacija, hotspotovi, zatvori)
- [ ] Provjera privacy/camera permission entry flowa
- [ ] Provjera GitHub Pages deploya i URL-a
- [ ] Finalni handoff dokumentacija ažurirana
- [ ] Odobrenje za produkcijski deploy

---

## 4. Preporučena struktura projekta

Trenutna struktura je organično rasla. Preporučena ciljna struktura:

```
Assets/
  _Production/              ← sve što je finalno i aktivno
    Models/
    Textures/
    Materials/
    Prefabs/
    Animations/
    Scenes/
    Audio/

  _Archive/                 ← legacy, staro, nekorišteno — ne brisati, samo odvojiti
    Models_old/
    Scenes_old/
    Prefabs_old/

  Imagine/                  ← WebAR plugin (ne dirati)
  Resources/                ← Unity Resources/ (runtime load — samo što se stvarno koristi)
    Digitron/
    digital-7 (mono).ttf
    digitron naslov.png
    solo trakica.png
    papir podloga kvadrati.jpg

  Scripts/                  ← C# skripte (ostaju gdje su)
  Localization/             ← (nova — Faza 3)
    hr.json
    en.json
    it.json

  Plugins/                  ← JS bridge i slično (ne dirati)

Docs/                       ← projektna dokumentacija (ovaj folder)
  ar-project-plan.md
  ar-web-integration.md

docs/                       ← GitHub Pages build output (ne editirati ručno)
```

> **Napomena:** Ne premještati assete dok projekt nije content-locked (Faza 1). Premještanje prije toga uzrokuje wiring probleme u scenama.

---

## 5. Rizici

| Rizik | Vjerojatnost | Utjecaj | Mitigation |
|-------|-------------|---------|------------|
| Agent ili developer koristi krivi 3D model | Visoka | Visok | Jasno dokumentirati source-of-truth u AGENTS.md; cleanup u Fazi 2 |
| Legacy asseti ostaju i usporavaju projekt | Visoka | Srednji | Faza 2 audit — eksplicitno označiti i odvojiti |
| Lokalizacija se počne raditi prije content locka | Srednja | Visok | Content lock je prerequisite za Fazu 3, nikad ranije |
| Web integracija bez dogovorenog jezičnog flowa | Srednja | Srednji | Dokumentirati u ar-web-integration.md PRIJE implementacije |
| GDPR / privacy nije definiran za standalone entry | Srednja | Srednji | Minimalni camera permission / privacy entry u Fazi 3–4 |
| Build se radi s krivim Unity settings | Niska | Visok | Uvijek koristiti WebGL + Disabled compression za GitHub Pages |
| Scene wiring se razbije promjenom prefaba | Srednja | Visok | Obavezna provjera Console i scene wiringa prije commitanja |

---

## 6. Projektne odluke

Ove odluke su donesene i ne trebaju se preispitivati bez eksplicitnog razgovora:

**Odvojenost projekata**
AR aplikacija i Digitron Buje web su odvojeni projekti u odvojenim repozitorijima. Ne postoji plan za unifikaciju.

**Model source-of-truth**
Aktivni produkcijski model je `db801-novo-odvojene-tipke.fbx` dodijeljen u Inspectoru na `MainController`. Resources fallback (`Assets/Resources/Digitron/DB_801_03.fbx`) je stari model i ne smije biti primarni.

**Lokalizacija dolazi nakon content locka**
Ne implementirati višejezičnost dok HR sadržaj nije finalno odobren i zaključan.

**Web integracija se definira dokumentirano**
Jezični flow i entry URL logika moraju biti dogovoreni i dokumentirani (vidjeti `ar-web-integration.md`) prije nego web tim počne implementirati linkove prema AR aplikaciji.

**Privacy / camera permission entry**
AR aplikacija mora imati vlastiti minimalni privacy/camera consent entry. Ne smije se pretpostavljati da korisnik uvijek dolazi s glavnog weba koji već ima consent.

**Platforma deploy**
GitHub Pages iz `docs/` foldera je trenutni deploy mehanizam. Prihvatljivo rješenje za ovu fazu.

---

## 7. Next actions

Neposredni sljedeći koraci (Faza 1):

- [ ] **Build + test na pravom mobitelu** — potvrditi orijentaciju, hotspot bg, font, teksture
- [ ] **Pregledati 6 hotspot opisa s klijentom / urednikom** — jesu li finalni?
- [ ] **Odluka: content lock** — zapisati u ovaj dokument kad je donesen
- [ ] **Dokumentirati krive modele u AGENTS.md** — dodati eksplicitno upozorenje o DB_801_03 vs db801-novo

Nakon Faze 1:
- [ ] Pokrenuti asset audit (Faza 2)
- [ ] Dogovoriti s web timom jezični flow (priprema za Fazu 3–4)
