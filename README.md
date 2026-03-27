# 🚛 ETS2 Tachograph

> 🇬🇧 [English](#english) &nbsp;|&nbsp; 🇭🇺 [Magyar](#magyar) &nbsp;|&nbsp; 🇩🇪 [Deutsch](#deutsch)

---

<a name="english"></a>
# 🇬🇧 English

A realistic **VDO DTCO digital tachograph** overlay for **Euro Truck Simulator 2**, enforcing real EU Regulation 561/2006 driving time rules in real-time.

![Windows](https://img.shields.io/badge/platform-Windows-blue)
![Language](https://img.shields.io/badge/language-C%23-purple)
![ETS2](https://img.shields.io/badge/game-Euro%20Truck%20Simulator%202-orange)

## Features

- **Live telemetry** — reads directly from ETS2's shared memory via the SCS SDK plugin
- **EU 561/2006 compliance engine** — tracks continuous drive time, mandatory breaks, daily and weekly limits in real wall-clock seconds
- **Multi-page LCD display** — cycle through Speed, Drive Time, Daily/Weekly, Job Info, and Violations screens
- **Violation detection & fines** — flags rule breaches with severity levels (Minor / Serious / Very Serious) and EUR fine amounts
- **Persistent session state** — driving time data is saved to `tacho_cache.dat` and restored on next launch, accounting for real time elapsed while the app was closed
- **Tachograph printout** — generates a formatted `.txt` report and sends it to a physical printer (virtual/PDF printers are automatically filtered out)
- **Authentic VDO DTCO skin** — dark LCD panel, status bar, navigation buttons, and corner screws

### EU 561/2006 Limits Enforced

| Rule | Limit |
|---|---|
| Max continuous driving before break | 4h 30m |
| Mandatory break | 45 min (or 15 + 30 split) |
| Daily driving — normal | 9h |
| Daily driving — extended (max 2×/week) | 10h |
| Weekly driving | 56h |
| Fortnightly driving | 90h |
| Daily rest | 11h (reduced 9h max 3×/week) |
| Weekly rest | 45h (reduced 24h allowed) |

---

## Requirements

- **Windows 10/11** (64-bit)
- **Euro Truck Simulator 2**
- **`ets2-telemetry.dll`** plugin installed in ETS2 (see Step 1 below)

---

## Installation

### Step 1 — Install the ETS2 telemetry plugin

The tachograph communicates with ETS2 through a plugin DLL that writes live vehicle data into shared memory. **This step is required and only needs to be done once.**

1. Download **`ets2-telemetry.dll`** from the [**Releases**](../../releases) page of this repository
2. Navigate to your ETS2 installation folder and open the `plugins` subfolder:
   ```
   C:\Program Files (x86)\Steam\steamapps\common\Euro Truck Simulator 2\bin\win_x64\plugins\
   ```
   > If the `plugins` folder does not exist, create it manually.
3. Copy `ets2-telemetry.dll` into that folder
4. Launch ETS2 — the plugin loads automatically on startup, no extra in-game configuration needed

### Step 2 — Install the tachograph app

**Option A — Precompiled (recommended)**

1. Go to the [**Releases**](../../releases) page
2. Download `TachographForm.exe`
3. Place it anywhere on your PC — no installer needed
4. Start ETS2 first, then double-click `TachographForm.exe`

> The app shows **NO CONNECTION** in the status bar until ETS2 is running with the plugin loaded.

**Option B — Compile it yourself**

You need the **C# compiler** (`csc.exe`) included with .NET Framework 4.x, found at:
```
C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe
```
Visual Studio is **not** required.

1. Clone or download this repository
2. Open **Command Prompt** in the folder containing `TachographForm.cs`
3. Run:

```cmd
csc TachographForm.cs /target:winexe /platform:x64 /r:System.Windows.Forms.dll /r:System.Drawing.dll /r:System.dll /out:TachographForm.exe
```

4. `TachographForm.exe` will appear in the same folder
5. Start ETS2 first, then run `TachographForm.exe`

**Compiler flags explained**

| Flag | Purpose |
|---|---|
| `/target:winexe` | Builds a Windows GUI app (no console window) |
| `/platform:x64` | Targets 64-bit — required to match ETS2's shared memory |
| `/r:System.Windows.Forms.dll` | UI framework |
| `/r:System.Drawing.dll` | GDI+ rendering (LCD display, buttons) |
| `/r:System.dll` | Core .NET runtime types |
| `/out:TachographForm.exe` | Output file name |

---

## Usage

### Navigation buttons

| Button | Action |
|---|---|
| `◄` / `▲` | Previous page |
| `▼` | Next page |
| `OK` | Reset all driving time counters (and save) |
| `PRINT` | Generate a tachograph printout |

### Pages

| Page | What it shows |
|---|---|
| **Speed** | Live speed, RPM, gear, fuel level, speed limit, cruise control indicator |
| **Drive Time** | Continuous drive elapsed, break due countdown, current rest |
| **Daily / Weekly** | Daily and weekly drive totals with remaining time (turns red when low) |
| **Job Info** | Origin city, destination, route distance, truck make, odometer |
| **Violations** | Active violations with fine amounts — press OK to reset after serving rest |

### Printout

Clicking **PRINT** saves a `.txt` report (`tacho_YYYYMMDD_HHMMSS.txt`) next to the exe and sends it to your physical printer. If no physical printer is found, only the file is saved.

---

## File structure

```
TachographForm.exe     — main application
tacho_cache.dat        — auto-saved session state (created on first run)
tacho_YYYYMMDD_*.txt   — printout files (auto-deleted after 3 minutes)

ETS2 plugins folder:
└── ets2-telemetry.dll — SCS SDK telemetry plugin (required)
```

---

## How it works

`ets2-telemetry.dll` is loaded by ETS2 at startup and writes live vehicle data into a Windows shared memory segment (`Local\SimTelemetryETS2`). The tachograph app opens that mapping every 100ms and reads speed, gear, fuel, job data and more directly from the raw struct layout. `EU561Engine` then runs a real-time state machine — incrementing drive counters when speed exceeds 1 km/h and the game is unpaused, and crediting rest when stopped. Session state is saved on exit and restored with real elapsed time applied on next launch.

---

## License

MIT — see [LICENSE](LICENSE)

---
---

<a name="magyar"></a>
# 🇭🇺 Magyar

Valósághű **VDO DTCO digitális tachográf** overlay az **Euro Truck Simulator 2**-höz, amely valós időben érvényesíti az EU 561/2006/EK rendelet menetidő-szabályait.

## Funkciók

- **Élő telemetria** — közvetlenül az ETS2 megosztott memóriájából olvas az SCS SDK plugin segítségével
- **EU 561/2006 szabálymotor** — valódi falióra-másodpercekben követi a folyamatos vezetési időt, a kötelező szüneteket, napi és heti korlátokat
- **Többoldalas LCD kijelző** — Sebesség, Menetidő, Napi/Heti, Fuvar adatok és Szabálysértések oldalak
- **Szabálysértés-észlelés és bírságok** — automatikusan jelzi a jogsértéseket súlyossági szinttel (Enyhe / Súlyos / Nagyon súlyos) és EUR összegekkel
- **Munkamenet-mentés** — a menetidő adatok mentésre kerülnek, és indításkor visszatöltődnek (figyelembe véve az eltelt valódi időt)
- **Tachográf nyomtatvány** — formázott `.txt` jelentést generál és fizikai nyomtatóra küld

---

## Követelmények

- **Windows 10/11** (64-bit)
- **Euro Truck Simulator 2**
- **`ets2-telemetry.dll`** plugin telepítve az ETS2-be (lásd 1. lépés)

---

## Telepítés

### 1. lépés — Az ETS2 telemetria plugin telepítése

A tachográf egy plugin DLL-en keresztül kommunikál az ETS2-vel, amely megosztott memórián át teszi elérhetővé a jármű adatait. **Ez a lépés kötelező, és csak egyszer kell elvégezni.**

1. Töltsd le az **`ets2-telemetry.dll`** fájlt a jelen repository [**Releases**](../../releases) oldaláról
2. Nyisd meg az ETS2 telepítési mappáját, majd a `plugins` almappát:
   ```
   C:\Program Files (x86)\Steam\steamapps\common\Euro Truck Simulator 2\bin\win_x64\plugins\
   ```
   > Ha a `plugins` mappa nem létezik, hozd létre kézzel.
3. Másold az `ets2-telemetry.dll` fájlt ebbe a mappába
4. Indítsd el az ETS2-t — a plugin automatikusan betöltődik, nincs szükség további beállításra

### 2. lépés — A tachográf alkalmazás telepítése

**A lehetőség — Előre lefordított (ajánlott)**

1. Menj a [**Releases**](../../releases) oldalra
2. Töltsd le a `TachographForm.exe` fájlt
3. Helyezd el bárhol a számítógépeden — telepítő nem szükséges
4. Először indítsd el az ETS2-t, majd kattints duplán a `TachographForm.exe`-re

> Az alkalmazás **NO CONNECTION** feliratot mutat, amíg az ETS2 nem fut a betöltött pluginnal.

**B lehetőség — Saját fordítás**

Szükséged van a .NET Framework 4.x-szel települt **C# fordítóra** (`csc.exe`):
```
C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe
```
A Visual Studio **nem szükséges**.

1. Klónozd vagy töltsd le a repository-t
2. Nyiss **Parancssort** a `TachographForm.cs` fájlt tartalmazó mappában
3. Futtasd:

```cmd
csc TachographForm.cs /target:winexe /platform:x64 /r:System.Windows.Forms.dll /r:System.Drawing.dll /r:System.dll /out:TachographForm.exe
```

4. A `TachographForm.exe` megjelenik ugyanabban a mappában
5. Először indítsd el az ETS2-t, majd futtasd a `TachographForm.exe`-t

---

## Használat

### Navigációs gombok

| Gomb | Funkció |
|---|---|
| `◄` / `▲` | Előző oldal |
| `▼` | Következő oldal |
| `OK` | Összes menetidő-számláló visszaállítása (és mentése) |
| `PRINT` | Tachográf nyomtatvány generálása |

### Oldalak

| Oldal | Tartalom |
|---|---|
| **Sebesség** | Élő sebesség, fordulatszám, fokozat, üzemanyagszint, sebességkorlát |
| **Menetidő** | Folyamatos vezetési idő, kötelező szünetig hátralévő idő, aktuális pihenő |
| **Napi / Heti** | Napi és heti vezetési összesítők, hátralévő idő (piros ha kevés) |
| **Fuvar adatok** | Indulási és célváros, útvonalhossz, jármű típusa, kilométeróra |
| **Szabálysértések** | Aktív szabálysértések bírságösszegekkel — OK gombbal törölhető pihenő után |

---

## Fájlstruktúra

```
TachographForm.exe     — főalkalmazás
tacho_cache.dat        — automatikusan mentett munkamenet-adatok
tacho_YYYYMMDD_*.txt   — nyomtatványfájlok (3 perc után automatikusan törlődnek)

ETS2 plugins mappa:
└── ets2-telemetry.dll — SCS SDK telemetria plugin (kötelező)
```

---

## Licenc

MIT — lásd [LICENSE](LICENSE)

---
---

<a name="deutsch"></a>
# 🇩🇪 Deutsch

Ein realistisches **VDO DTCO Digitaltachograph**-Overlay für **Euro Truck Simulator 2**, das die Lenk- und Ruhezeiten gemäß EU-Verordnung 561/2006 in Echtzeit überwacht.

## Funktionen

- **Live-Telemetrie** — liest direkt aus dem geteilten Speicher von ETS2 über das SCS SDK Plugin
- **EU 561/2006 Regelwerk** — verfolgt kontinuierliche Lenkzeit, Pflichtpausen sowie Tages- und Wochenlimits in Echtzeit (Wanduhr-Sekunden)
- **Mehrseitiges LCD-Display** — Geschwindigkeit, Lenkzeit, Täglich/Wöchentlich, Auftragsdaten und Verstöße
- **Verstöße & Bußgelder** — erkennt automatisch Regelverstöße mit Schweregrad (Geringfügig / Schwerwiegend / Sehr schwerwiegend) und EUR-Beträgen
- **Sitzungsspeicherung** — Lenkzeitdaten werden gespeichert und beim nächsten Start wiederhergestellt (mit echter verstrichener Zeit)
- **Tachografenausdruck** — erstellt einen formatierten `.txt`-Bericht und sendet ihn an einen physischen Drucker

---

## Voraussetzungen

- **Windows 10/11** (64-Bit)
- **Euro Truck Simulator 2**
- **`ets2-telemetry.dll`** Plugin in ETS2 installiert (siehe Schritt 1)

---

## Installation

### Schritt 1 — ETS2 Telemetrie-Plugin installieren

Der Tachograf kommuniziert mit ETS2 über eine Plugin-DLL, die Fahrzeugdaten über gemeinsam genutzten Speicher bereitstellt. **Dieser Schritt ist einmalig erforderlich.**

1. Lade **`ets2-telemetry.dll`** von der [**Releases**](../../releases)-Seite dieses Repositories herunter
2. Navigiere zum ETS2-Installationsordner und öffne den `plugins`-Unterordner:
   ```
   C:\Program Files (x86)\Steam\steamapps\common\Euro Truck Simulator 2\bin\win_x64\plugins\
   ```
   > Falls der `plugins`-Ordner nicht existiert, erstelle ihn manuell.
3. Kopiere `ets2-telemetry.dll` in diesen Ordner
4. Starte ETS2 — das Plugin wird beim Start automatisch geladen, keine weitere Konfiguration nötig

### Schritt 2 — Tachograf-App installieren

**Option A — Vorkompiliert (empfohlen)**

1. Gehe zur [**Releases**](../../releases)-Seite
2. Lade `TachographForm.exe` herunter
3. Lege die Datei beliebig auf deinem PC ab — kein Installer erforderlich
4. Starte zuerst ETS2, dann doppelklicke auf `TachographForm.exe`

> Die App zeigt **NO CONNECTION** in der Statusleiste, solange ETS2 nicht mit geladenem Plugin läuft.

**Option B — Selbst kompilieren**

Du benötigst den **C#-Compiler** (`csc.exe`), der mit .NET Framework 4.x mitgeliefert wird:
```
C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe
```
Visual Studio ist **nicht erforderlich**.

1. Klone oder lade dieses Repository herunter
2. Öffne die **Eingabeaufforderung** im Ordner mit `TachographForm.cs`
3. Führe aus:

```cmd
csc TachographForm.cs /target:winexe /platform:x64 /r:System.Windows.Forms.dll /r:System.Drawing.dll /r:System.dll /out:TachographForm.exe
```

4. `TachographForm.exe` erscheint im selben Ordner
5. Starte zuerst ETS2, dann führe `TachographForm.exe` aus

---

## Bedienung

### Navigationstasten

| Taste | Funktion |
|---|---|
| `◄` / `▲` | Vorherige Seite |
| `▼` | Nächste Seite |
| `OK` | Alle Lenkzeitzähler zurücksetzen (und speichern) |
| `PRINT` | Tachografenausdruck erstellen |

### Seiten

| Seite | Inhalt |
|---|---|
| **Geschwindigkeit** | Echtzeit-Geschwindigkeit, Drehzahl, Gang, Tankstand, Tempolimit |
| **Lenkzeit** | Bisherige Lenkzeit, Zeit bis zur Pflichtpause, aktuelle Ruhezeit |
| **Täglich / Wöchentlich** | Tages- und Wochenlenkzeiten mit Restkontingent (rot bei niedrigem Stand) |
| **Auftragsdaten** | Abfahrts- und Zielort, Streckenlänge, Fahrzeugmarke, Kilometerstand |
| **Verstöße** | Aktive Verstöße mit Bußgeldbeträgen — nach Ruhezeit mit OK zurücksetzen |

---

## Dateistruktur

```
TachographForm.exe     — Hauptanwendung
tacho_cache.dat        — automatisch gespeicherte Sitzungsdaten
tacho_YYYYMMDD_*.txt   — Ausdruckdateien (automatisch nach 3 Minuten gelöscht)

ETS2 plugins-Ordner:
└── ets2-telemetry.dll — SCS SDK Telemetrie-Plugin (erforderlich)
```

---

## Lizenz

MIT — siehe [LICENSE](LICENSE)
