# AR ↔ Web integracija — Digitron Buje

> **Status dokumenta:** Nacrt — čeka usklađivanje s web timom
> **Zadnje ažuriranje:** 2026-03-20
> **Namjena:** Web tim, agenti koji rade na Digitron Buje webu

---

## 1. Što je AR aplikacija

AR aplikacija je zasebna Unity WebGL aplikacija koja korisniku omogućuje da u svom fizičkom prostoru — putem kamere mobitela — vidi 3D model Digitron DB-800/DB-801 kalkulatora iz 1970-ih.

**Što korisnik doživljava:**
1. Otvori AR aplikaciju u mobilnom browseru
2. Usmjeri kameru prema podu — aplikacija traži ravnu površinu
3. Tapne da postavi kalkulator u prostor
4. Model se pojavljuje u prostoru — može ga gledati iz svih strana
5. Tapne "Otvori" — animacija otvori kalkulator
6. Na unutarnjim komponentama pojavljuju se hotspot oznake s opisima (kućište, tipkovnica, elektronička ploča, čipovi, baterije, zaslon)
7. "Zatvori" vraća u zatvoreno stanje, "Reset" vraća na početak

**Desktop browser (bez kamere):**
Na desktopu se automatski prikazuje 3D preview bez AR-a. Korisnik može rotirati model mišem i zoomirati. Sve ostale funkcije (Otvori, hotspotovi) rade identično.

---

## 2. Kako se pokreće iz weba

**Preporučeni flow:**

```
Korisnik je na Digitron Buje webu
         ↓
Web zna koji jezik koristi (HR / EN / IT)
         ↓
Korisnik klikne na AR gumb / AR sekciju
         ↓
Web otvara AR aplikaciju s jezičnim parametrom u URL-u
         ↓
AR aplikacija se učita odmah na ispravnom jeziku
```

**Primjeri URL-a:**
```
https://kristijankauric.github.io/kalkulator-ar/?lang=hr
https://kristijankauric.github.io/kalkulator-ar/?lang=en
https://kristijankauric.github.io/kalkulator-ar/?lang=it
```

**Preporuka:** Web neka otvara AR URL u novom tabu (`target="_blank"`). AR aplikacija zauzima cijeli ekran i nema navigacije natrag na web, pa korisnik ionako zatvara tab kad završi.

---

## 3. Jezični flow

### A) Korisnik dolazi s glavnog weba

```
Web link: /kalkulator-ar/?lang=en
         ↓
AR aplikacija čita ?lang= parametar
         ↓
Sav sadržaj prikazan odmah na engleskom
         ↓
Nema language picker ekrana
```

- AR aplikacija **ne pita** za jezik — već ga zna
- Korisnik ulazi direktno u iskustvo

### B) Korisnik dolazi direktnim linkom (bez ?lang=)

```
Direktni URL: /kalkulator-ar/   (bez ?lang=)
         ↓
AR aplikacija NE zna jezik
         ↓
Prikaže se language picker (HR / EN / IT)
         ↓
Korisnik odabere jezik
         ↓
Iskustvo se pokreće na odabranom jeziku
```

- Language picker je **jednostavan ekran** — tri zastavice ili nazivi jezika
- Ne prikazuje se ako `?lang=` parametar postoji i validan je

### Tablica ponašanja

| Ulaz | Ponašanje |
|------|-----------|
| `?lang=hr` | Direktno HR iskustvo |
| `?lang=en` | Direktno EN iskustvo |
| `?lang=it` | Direktno IT iskustvo |
| Bez `?lang=` | Language picker ekran |
| Nepoznati `?lang=xyz` | Language picker ekran (fallback) |

> **Status implementacije:** Jezični parametar i language picker **nisu još implementirani** (stanje: 2026-03-20). Sve je trenutno hardcode HR. Implementacija planirana u Fazi 3 (vidjeti `ar-project-plan.md`).

---

## 4. Preporučeni URL / entry model

**Arhitektura:**

```
Digitron Buje web          AR aplikacija
(zasebni repozitorij)      (zasebni repozitorij)
       |                          |
       |    ?lang=hr              |
       |------------------------->|
       |    (link/redirect)       |
```

- Svaki projekt je **zaseban deploy** na zasebnom URL-u
- Web ne embedira AR u iframe — AR je zasebna stranica / zasebna aplikacija
- Web samo linkuje prema AR URL-u s jezičnim parametrom
- Prihvatljivo je da AR iskustvo otvori u novom tabu

**Zašto ne iframe:**
- AR zahtijeva kamera pristup (camera permissions su problematični u iframeu)
- AR zauzima cijeli ekran i treba puni browser context
- Zasebni tab je bolji UX za AR na mobitelu

---

## 5. Što web tim treba znati

**Sažetak za web tim (ne treba poznavati Unity):**

| Tema | Detalj |
|------|--------|
| Što je AR app | Zasebna Unity WebGL aplikacija, hostana na GitHub Pages |
| Tko je hostira | AR tim (zasebni repozitorij, zasebni deploy) |
| Kako linkati | Anchor tag s ?lang= parametrom |
| Gdje ide link | Otvara se u novom tabu ili redirecta |
| Što korisnik vidi | Fullscreen AR iskustvo, nema navigacije natrag na web |
| Platforme | Mobitel (kamera + AR), Desktop (3D preview bez AR) |
| Jezici | Trenutno samo HR. EN i IT dolaze u kasnijoj fazi |
| Deploy URL | Potvrditi s AR timom (GitHub Pages URL) |

**Web timu nisu potrebni:**
- Detalji Unity arhitekture
- Detalji o 3D modelima ili scenama
- Pristup AR repozitoriju (osim za dogovor o URL-u)

---

## 6. GDPR / Privacy napomena

### Situacija
Glavni Digitron Buje web ima vlastiti GDPR / consent sloj. AR aplikacija se međutim može otvoriti i direktno (bookmarkom, dijeljenjem linka, QR kodom) — bez prolaska kroz web.

### Zahtjev
**AR aplikacija mora imati vlastiti minimalni privacy / camera permission entry.**

To ne znači puni GDPR banner — dovoljno je:
- Kratko obavještenje da aplikacija koristi kameru
- Jasno traženje camera permission (browser to ionako radi, ali UI treba biti jasan)
- Opcija za odustajanje (npr. "Nastavi bez kamere" → desktop preview)

### Što se NE smije pretpostaviti
- Da je korisnik uvijek došao kroz web koji ima consent
- Da je korisnik već vidio privacy/GDPR informacije
- Da browser camera permission automatski pokriva sve pravne zahtjeve

> **Status implementacije:** Camera permission entry postoji kroz browser default dialog. Minimalni privacy/consent UI u AR aplikaciji **nije još implementiran**. Planirati u Fazi 4 (vidjeti `ar-project-plan.md`).

---

## 7. Preporuka za web tim — što implementirati

### Gumb / link prema AR aplikaciji

```html
<!-- Jednostavan primjer -->
<a href="https://kristijankauric.github.io/kalkulator-ar/?lang=hr"
   target="_blank"
   rel="noopener">
  Istraži AR iskustvo
</a>
```

- `target="_blank"` — otvori u novom tabu
- `rel="noopener"` — sigurnosna preporuka za external link
- `?lang=hr` — zamijeniti s aktivnim jezikom weba

### Dinamično slanje jezika (JavaScript primjer)

```javascript
const lang = getCurrentWebsiteLanguage(); // 'hr', 'en', ili 'it'
const arUrl = `https://kristijankauric.github.io/kalkulator-ar/?lang=${lang}`;
document.getElementById('ar-link').href = arUrl;
```

### Preview AR iskustva na webu

Preporuka je da web prikaže preview AR iskustva (screenshot ili kratki video) **prije** nego korisnik klikne na gumb. Razlog: AR aplikacija se učitava sporo (Unity WebGL) i korisnik treba znati što ga čeka.

Potrebno od AR tima:
- [ ] 2–3 screenshotova (model u prostoru, otvoren kalkulator, hotspot detalj)
- [ ] Kratki video (15–30 sekundi) koji prikazuje iskustvo
- [ ] Finalni deploy URL

### Ako korisnik otvara direktni link (bez ?lang=)

Web tim ne mora posebno ništa raditi za ovaj slučaj — AR aplikacija sama prikazuje language picker. Jedino što web tim treba osigurati je da linkovi s weba uvijek nose `?lang=` parametar.

---

## 8. Za agente koji rade na webu

Kratki sažetak za Claude agente ili developere koji rade na Digitron Buje web projektu:

1. **AR je zasebni projekt** — zasebni Unity repozitorij, zasebni GitHub Pages URL, ne dijeli kod s webom
2. **Jezik se šalje query parametrom** — format: `?lang=hr`, `?lang=en`, `?lang=it`
3. **Direktni link bez ?lang= otvara language picker** — AR aplikacija sama rješava taj slučaj
4. **Ne embedirati AR u iframe** — koristiti redirect ili `target="_blank"` link
5. **AR ima vlastiti camera permission / privacy entry** — ne pretpostavljati da web consent pokriva AR
6. **Trenutno je samo HR implementiran** — EN i IT dolaze u kasnijoj fazi; ne slati ?lang=en dok AR tim ne potvrdi da je spreman
7. **Koristiti screenshotove/video za preview** — Unity WebGL se učitava sporo, korisniku treba context prije klika
8. **Deploy URL potvrditi s AR timom** — URL se može promijeniti, ne hardcodirati bez dogovora
9. **Ne pretpostavljati da web i AR dijele isti repozitorij ili isti deploy** — to nisu iste aplikacije
10. **Sva komunikacija o URL-u i jezičnom flowu ide kroz ovaj dokument** — promjene dogovoriti s AR timom i ažurirati ovdje
