# AGENTS.md

## Svrha
Ovaj file definira kako AI agent treba raditi na projektu `digitron-ar-kalkulator` tako da promjene budu konzistentne s trenutnom arhitekturom i ciljem projekta.

## Ogranicenja konteksta
- Agent nema pristup starim chatovima osim onoga sto je vec zapisano u ovom razgovoru i u repozitoriju.
- Ako nesto nije zapisano u kodu, sceni ili ovom fileu, tretira se kao pretpostavka i treba biti jasno oznaceno.
- Kad korisnik referencira "kako smo prije dogovorili", agent treba prvo provjeriti postoji li to u repou ili u trenutnoj niti.

## Trenutni projektni kontekst
- Engine: Unity `2022.3.36f1`.
- Glavna build scena trenutno je `Assets/Scenes/Digitron AR Base.unity`.
- Projekt koristi Imagine WebAR / WorldTracker paket iz `Assets/Imagine/Common` i `Assets/Imagine/WorldTracker`.
- Glavna runtime kamera u sceni je `ARCamera`.
- U sceni postoji `WorldTracker` objekt koji je povezan s `ARCamera` i eventima `OnPlacedOrigin` i `OnResetOrigin`.
- Trenutni fokus projekta je editor-friendly preview Digitrona u Unity Editoru.
- Mobilni / WebGL AR tok se ne smije nepotrebno razbijati dok se radi editor preview.

## Trenutni funkcionalni cilj
- U Unity Editoru treba biti moguce jasno vidjeti model, kameru i UI bez oslanjanja na pravi AR feed s mobitela.
- Kasnije se mora moci vratiti AR prikaz s mobitela / WebGL builda.
- Zato editor fallback mora biti izoliran iza `UNITY_EDITOR` ili slicnih sigurnih granica.

## Poznati relevantni scriptovi
- `Assets/Scripts/MainController.cs`
  - Trenutno koordinira editor preview i Digitron spawn.
  - U editor Play modu trenutno priprema fallback scenu, gasi tracker komponente i kadrira kameru.
- `Assets/Scripts/DigitronCalculatorController.cs`
  - Upravlja Digitron iskustvom nakon spawna.
  - Ima stanja `Unplaced`, `PlacedClosed`, `Opening`, `Opened`.
  - Koristi `OnGUI` za gumb "Otvori", hotspotove i info panel.
- `Assets/Imagine/Common/Scripts/ARCamera.cs`
  - WebGL / AR camera bridge.
- `Assets/Imagine/WorldTracker/Scripts/WorldTracker.cs`
  - Upravljanje placementom, resetom origin-a i debug pomicanjem kamere u editoru.

## Pravila rada na projektu

### 1. Editor preview ima prioritet dok korisnik eksplicitno ne trazi povratak na pravi AR tok
- Ako korisnik kaze da zeli "vidjeti sto se dogada u Unity editoru", prednost imaju editor-safe rjesenja.
- U toj fazi je prihvatljivo privremeno iskljuciti `WorldTracker`, gesture skripte ili placement UI unutar `UNITY_EDITOR`.
- Ne smije se lomiti WebGL ili mobilni kod bez izricitog razloga.

### 2. AR i editor logika moraju biti jasno odvojene
- Editor fallback logiku drzati u `#if UNITY_EDITOR` blokovima ili u zasebnim helperima.
- WebGL / mobilne pozive ne mijenjati ako promjena nije direktno vezana uz AR runtime.
- Kad je moguce, editor preview treba biti additive fallback, ne zamjena za pravi AR flow.

### 3. Ne mijenjati scenski setup napamet
- Prije promjene scene ili prefabova treba procitati postojece reference u `.unity` / `.prefab` assetima ili odgovarajucim scriptama.
- Posebno paziti na:
  - `WorldTracker`
  - `ARCamera`
  - `MainObject`
  - `Jankec Anchor`
  - `Placement Indicator`
  - `Placement Canvas`

### 4. Digitron iskustvo je trenutno primarni feature ovog repoa
- Ako postoji konflikt izmedu starog Jankec sadrzaja i novog Digitron previewa, ne brisati stari sustav bez korisnikove potvrde.
- Radije uvoditi lokalne, jasno ogranicene promjene koje omogucuju Digitron flow.
- Kod koji sluzi Digitronu treba ostati citljiv i samostalan.

### 5. Koristiti postojecu arhitekturu prije uvodenja nove
- Za spawn, anchor i kameru prvo koristiti postojece objekte i evente.
- Ako vec postoji `OnPlacedOrigin` / `OnResetOrigin` hook, nova logika se treba nadovezati na to prije nego se uvodi paralelni lifecycle.
- Ako postoji editor preview mehanizam, doraditi njega prije stvaranja potpuno novog sustava.

### 6. Kod mora biti reverzibilan
- Privremena rjesenja za editor moraju biti jednostavna za kasnije uklanjanje ili gasenje.
- Izbjegavati "hard fork" pristup gdje se duplicira cijeli AR flow samo za editor.

### 7. Kod stil
- Preferirati kratke i jasne MonoBehaviour komponente.
- Ne dodavati kompleksnu apstrakciju bez stvarne potrebe.
- Ako se uvodi editor-only helper, ime mora jasno reci da je za editor / preview / debug.
- Komentare koristiti samo kad objasnjavaju zasto je nesto napravljeno, ne sto linija radi.

## Workflow za buduce promjene
- Prvo identificirati je li problem:
  - editor preview problem
  - AR runtime problem
  - scene wiring problem
  - UI / UX problem
  - asset / import problem
- Prvo citati:
  - glavnu scenu
  - `MainController.cs`
  - `DigitronCalculatorController.cs`
  - relevantni Imagine / WorldTracker skript
- Tek nakon toga mijenjati kod.
- Ako promjena utjece i na editor i na AR runtime, jasno odvojiti ta dva toka.

## Sto agent treba eksplicitno provjeriti prije vece promjene
- Je li scena `Assets/Scenes/Digitron AR Base.unity` i dalje jedina build scena.
- Je li `WorldTracker` i dalje aktivan dio produkcijskog flowa.
- Gdje je stvarni source-of-truth za Digitron model:
  - `Assets/Resources/Digitron/DB_801_03.fbx`
  - `Assets/Models/DIGITRON stara animacija/DB_801_03.fbx`
- Postoje li u worktreeju korisnikove nerijesene izmjene koje se ne smiju pregaziti.

## Sto agent ne smije raditi bez potvrde
- Ne brisati ili resetirati velike kolicine asseta.
- Ne dirati build scene ili globalne project settings bez razloga.
- Ne raditi destruktivne git operacije.
- Ne pretpostavljati da su brojni obrisani fileovi u git statusu "greska"; korisnik mozda svjesno restrukturira projekt.

## Trenutno poznate rupe u dokumentaciji
Ove informacije jos nedostaju i trebale bi se kasnije dopuniti:
- Koji je tocno dugorocni scope projekta: samo Digitron ili i dalje siri Jankec AR framework.
- Je li finalni target WebGL, Android app, iOS app ili vise targeta.
- Kako se definira "gotovo" za editor preview fazu.
- Koji asseti i folderi su naslijedeni legacy sadrzaj, a koji su aktivni dio projekta.
- Postoji li zeljeni coding style za C# naming, serialize field pristup i organizaciju MonoBehaviour skripti.
- Kako se testira:
  - samo rucno u Unity Editoru
  - WebGL build
  - mobilni uredaj
- Koji su tocni acceptance kriteriji za povratak na pravi AR prikaz.

## Predlozena dopuna od korisnika
Da bi ovaj file postao stvarno dobar, korisnik bi trebao potvrditi ili dopuniti:
- Koji je glavni cilj aplikacije u jednoj recenici.
- Koja platforma je primarna.
- Sto trenutno mora raditi odmah, a sto moze cekati.
- Koji dijelovi starog projekta se smatraju legacy i ne treba ih dirati.
- Kako zelis da agent tretira scene:
  - mijenjati direktno glavnu scenu
  - raditi kroz prefabove i skripte
  - izbjegavati scene osim kad je nuzno
- Zelis li da agent automatski odrzava editor preview dok radimo na funkcionalnostima.

## Pravilo azuriranja ovog filea
- Kad se donese nova stabilna projektna odluka, treba je zapisati u `AGENTS.md`.
- Ako je odluka privremena, treba je oznaciti kao privremenu.
- Ako se promijeni glavni smjer projekta, ovaj file treba azurirati prije vecih novih zahvata.
