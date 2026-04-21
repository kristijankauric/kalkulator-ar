# Imagine WebAR – World Tracking: Vodič za postavljanje

Ovaj dokument opisuje kako ispravno postaviti Imagine WebAR World Tracking plugin u Unity projektu.  
Baziran je na radnom projektu koji koristi isti plugin u 12 build scena.

---

## 1. Što je World Tracking

World Tracking ne prati sliku (image target) – prati fizički prostor korisnika koristeći senzore uređaja (žiroskop, akcelerometar, kamera). AR objekt se pojavljuje u stvarnom prostoru ispred korisnika.

Plugin radi **isključivo na WebGL buildu** i zahtijeva HTTPS u pregledniku.  
U Unity Editoru radi u debug modu (tipkovnica za simulaciju kamere – WASD + strelice).

---

## 2. Struktura Scene – Obavezni elementi

Svaka scena mora imati točno ova dva objekta:

### 2.1 ARCamera
- Dodaj iz menija: `Assets > Imagine WebAR > Create > AR Camera`  
  ili ručno: instanciraj prefab `Assets/Imagine/WorldTracker/Prefabs/ARCamera.prefab`
- Objekt mora imati komponentu `ARCamera` (namespace: `Imagine.WebAR`)
- **Ne koristi se standardna Unity Main Camera za AR** – ARCamera ju zamjenjuje

Ključna polja na `ARCamera` komponenti:

| Polje | Opis |
|---|---|
| `Video Plane Mode` | Ostavi na `TEXTURE_PTR` (default) |
| `Video Plan Mat` | Material za prikaz feed-a kamere u pozadini |
| `Pause On Application Lost Focus` | Preporučuje se uključiti za mobilne uređaje |

### 2.2 WorldTracker
- Dodaj iz menija: `Assets > Imagine WebAR > Create > WorldTracker`  
  ili instanciraj prefab `Assets/Imagine/WorldTracker/Prefabs/WorldTracker.prefab`
- Objekt mora imati komponentu `WorldTracker` (namespace: `Imagine.WebAR`)

---

## 3. Konfiguracija WorldTracker komponente

### 3.1 Tracker Camera
- Povuci `ARCamera` objekt u polje `Tracker Camera`
- Ako ostane prazno, sustav ga automatski traži u sceni putem `FindObjectOfType<ARCamera>()`, ali bolje je eksplicitno dodijeliti

### 3.2 Tracking Mode – odabir načina praćenja

```
TrackingMode.MODE_3DOF        ← preporučeno za većinu projekata
TrackingMode.MODE_6DOF        ← napredno, za walk-around iskustvo
TrackingMode.MODE_3DOF_ORBIT  ← objekt stoji u sredini, kamera orbita oko njega
```

**3DOF (3 stupnja slobode)**  
Prati rotaciju glave korisnika. Objekt stoji na jednom mjestu, korisnik može hodati okolo ali objekt ne pomiče poziciju. Najstabilniji i najkompatibilniji mode.

Postavke (`s3dof`):
- `armLength` (default: 0.4) – udaljenost placement indikatora od kamere
- `useExtraSmoothing` – uključi za glađe kretanje, ali unosi malo kašnjenja
- `smoothenFactor` (1–50) – brzina smoothinga, viša = brže

**6DOF (6 stupnjeva slobode)**  
Koristi OpenCV feature tracking za procjenu dubine. Zahtjevnije za uređaj, može biti nestabilno na slabijim telefonima.

Postavke (`s6dof`):
- `depthMode` – `SCALE_AS_DEPTH` (preporučeno) ili `Z_AS_DEPTH_EXPERIMENTAL`
- `maxPixels` (300–600) – rezolucija za feature detection, niže = brže ali manje točno
- `maxPoints` (50–200) – broj tracking točaka

**3DOF_ORBIT**  
Kamera orbita oko centralnog objekta. Korisnik swipe-om rotira pogled, pinch-om zumira.

Postavke (`s3dof_orbit`):
- `orbitDistance` – početna udaljenost kamere od centra
- `centerTransform` – **obavezno dodijeliti** transform objekta oko kojeg kamera orbita
- `swipeSensitivity` – osjetljivost swipe rotacije
- `minDist` / `maxDist` – min/max zoom raspon

### 3.3 Main Object
- Povuci GameObject koji sadrži tvoje AR sadržaje (3D modeli, UI, efekti) u polje `Main Object`
- Ovaj objekt se automatski sakrije dok korisnik ne postavi origin, i prikaže nakon postavljanja

### 3.4 Camera Start Height
- Default: `1.25` (metara)
- Simulira visinu kamere na početku – ne mijenjaj ako nemaš poseban razlog

### 3.5 Placement Indicator (preporučeno uključiti)

`Use Placement Indicator = true` – korisnik vidi indikator gdje će se objekt pojaviti i sam ga potvrditi.

Polja:
- `Placement Indicator` – povuci GameObject koji služi kao vizualni indikator (npr. krug na tlu)
- `minZ` / `maxZ` – minimalna/maksimalna udaljenost indikatora od kamere (default: 0.5–5 metara)

Ako je isključeno (`false`), objekt se automatski postavi na zadanu poziciju bez interakcije korisnika.

### 3.6 Event Settings

Ovi UnityEvent-i su korisni za UI i feedback:

| Event | Kada se okida |
|---|---|
| `OnPlacedOrigin` | Kad korisnik postavi origin (tap) |
| `OnResetOrigin` | Kad korisnik resetira i vrati indikator |
| `ShowGameObjectsWhenPlaced` | Lista objekata koji se prikazuju nakon postavljanja |
| `ShowGameObjectsWhenReset` | Lista objekata koji se prikazuju dok čeka postavljanje |

Tipična upotreba: gumb "Postavi" koji poziva `PlaceOrigin()` spoji na `OnResetOrigin`, a gumb "Resetiraj" koji poziva `ResetOrigin()` pokaži tek nakon postavljanja.

---

## 4. Pozivi iz koda / UI gumba

```csharp
// Postavi origin na lokaciju placement indikatora
worldTracker.PlaceOrigin();

// Resetiraj i vrati korisnika u "placement" mode
worldTracker.ResetOrigin();

// Pokretanje/zaustavljanje trackera (rijetko potrebno)
worldTracker.StartTracker();
worldTracker.StopTracker();
```

---

## 5. Build Settings – WebGL

### 5.1 Obavezno
- Platform: **WebGL**
- Compression: `Disabled` ili `Gzip` (NE Brotli – blokira neke preglednike)
- Publishing Settings → `Decompression Fallback`: **uključiti**

### 5.2 HTTPS
WebAR **ne radi na HTTP**. Kamera neće dobiti dozvolu. Potreban je HTTPS hosting.

### 5.3 Rendering Pipeline
Ovisno o projektu:
- **Built-In RP** – default, ne treba ništa posebno
- **URP** – pokreni `Assets > Imagine WebAR > Update Plugin to URP`  
  Ovo dodaje `IMAGINE_URP` define symbol i mijenja shadere

---

## 6. Česti razlozi zašto tracking ne radi

### Kamera se ne pokreće
- Stranica nije na HTTPS-u
- Korisnik nije dao dozvolu za kameru u browseru
- Provjeri u konzoli preglednika: `WebGLStartCamera` / `OnStartWebcamFail`

### AR objekt se ne vidi
- `Main Object` nije dodijeljen na WorldTracker komponenti
- `Main Object` ostaje skriven jer `Use Placement Indicator = true` a korisnik još nije taknuo ekran – to je ispravno ponašanje, nije bug
- Provjeri je li `PlaceOrigin()` pozvan (ili da postoji gumb koji ga poziva)

### Tracking "skače" ili je nestabilan (3DOF)
- Uključi `Use Extra Smoothing = true` i postavi `Smoothen Factor` na 15–25
- Provjeri da je `Camera Start Height` realan (1.0–1.5)

### Tracking ne radi u Editoru
- U Editoru **nema kamera feed-a** – to je normalno
- Koristi tipkovnicu za debug simulaciju kamere:  
  `W/A/S/D` = pomicanje, `strelice` = rotacija, `R/F` = gore/dolje

### 6DOF je nestabilan
- Smanji `maxPoints` na 80–100
- Osiguraj dobro osvjetljenje scene – OpenCV treba feature točke (rubovi, teksture)
- Na ravnim, jednobojnim površinama 6DOF neće raditi dobro – koristi 3DOF

### Compass (experimentalno)
- `Use Compass = true` rotira objekt prema sjeveru
- Neki browseri trebaju do 20 sekundi za inicijalizaciju kompasa
- Ne radi u 6DOF modu
- Na iOS zahtijeva eksplicitnu dozvolu korisnika u Safariju

---

## 7. Geolocation (GPS) mod – napomene

Ako koristite `Use Geolocation = true`:
- Preporučuje se uz `Use Compass = true`
- Radi samo u 3DOF modu (ne 6DOF!)
- Scena se mora dodati u `WorldTrackerGlobalSettings` listu geolocationScenes – Editor to radi automatski kad uključiš checkbox
- Post-build skripta `PostProcessBuild_WT.cs` automatski aktivira GPS u `index.html`

---

## 8. Provjera ispravne postavljenosti (checklist)

```
[ ] ARCamera objekt postoji u sceni s ARCamera komponentom
[ ] WorldTracker objekt postoji u sceni s WorldTracker komponentom
[ ] TrackerCamera polje na WorldTrackerima pokazuje na ARCamera objekt
[ ] MainObject je dodijeljen
[ ] PlacementIndicator je dodijeljen (ako Use Placement Indicator = true)
[ ] Postoji gumb ili event koji poziva PlaceOrigin()
[ ] Build target je WebGL
[ ] Hosting je HTTPS
[ ] Compression nije Brotli
[ ] Decompression Fallback je uključen
[ ] Za URP projekte: pokrenut "Update Plugin to URP" korak
```

---

## 9. Namespace i skripte

Sve klase su u `Imagine.WebAR` namespaceu. Ako pišeš vlastite skripte koje komuniciraju s trackerom:

```csharp
using Imagine.WebAR;

public class MojController : MonoBehaviour {
    [SerializeField] WorldTracker worldTracker;

    public void OnTap() {
        worldTracker.PlaceOrigin();
    }
}
```

---

*Dokument napisan na temelju izvornog koda projekta jankec-mihanovic-tuheljske-toplice.*
