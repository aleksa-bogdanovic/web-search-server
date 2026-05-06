# WebSearchServer

Projekat iz predmeta **Sistemsko Programiranje** — Web server za pretraživanje tekstualnih fajlova.

## Opis

Konzolna serverska aplikacija u C# koja omogućava pretraživanje reči u tekstualnim fajlovima putem browser-a. Server prima GET zahteve, pretražuje `.txt` fajlove u svom root direktorijumu i vraća HTML stranicu sa rezultatima pretrage.

### Primer poziva

```
http://localhost:5050/sistemsko&programiranje&elfak&projekat
```

## Arhitektura

```
Browser GET ──► HttpListener (HttpServer)
                    │
                    ▼
              Queue<Request>  ← lock/Monitor
                    │
                    ▼
              ThreadPool workers
                    │
              ┌─────┴──────┐
              ▼            ▼
          Cache?        FileSearcher
          (hit)─────────(miss→pretraga)
              │            │
              └─────┬──────┘
                    ▼
             ResponseBuilder
                    │
                    ▼
             HTML → Browser
```

### Komponente

- **HttpServer** — prima zahteve klijenata putem `HttpListener`, stavlja ih u red čekanja
- **RequestQueue** — thread-safe red čekanja (FIFO) za pristigle zahteve
- **ThreadPoolWorker** — uzima zahteve iz reda i prosleđuje ih `ThreadPool`-u na obradu
- **FileSearcher** — pretražuje `.txt` fajlove po ključnim rečima i broji pojavljivanja
- **SearchCache** — LRU keš sa ograničenjem veličine, thread-safe
- **ResponseBuilder** — generiše HTML stranicu sa rezultatima pretrage u obliku tabele
- **Logger** — thread-safe logovanje svih događaja i grešaka u konzolu i fajl

## Tehnologije

- C# / .NET 8
- `System.Net.HttpListener`
- `System.Threading` (ThreadPool, Monitor, lock)

## Pokretanje

1. Klonirati repozitorijum:
   ```bash
   git clone https://github.com/aleksa-bogdanovic/web-search-server.git
   cd web-search-server
   git checkout dev
   ```
2. Otvoriti solution u Visual Studio / Rider / VS Code
3. Pokrenuti projekat:
   ```bash
   cd WebSearchServer
   dotnet run
   ```
4. U browser-u otvoriti:
   ```
   http://localhost:5050/rec1&rec2&rec3
   ```

## Testiranje

Za testiranje konkurentnosti koristiti PowerShell:
```powershell
1..50 | ForEach-Object { Start-Job { Invoke-WebRequest -Uri "http://localhost:5050/sistemsko&programiranje" -UseBasicParsing } }
```

Test `.txt` fajlovi nalaze se u folderu `TextFiles/`.

## Analiza sinhronizacionih mehanizama

### Kritične sekcije

| Klasa | Kritična sekcija | Mehanizam |
|---|---|---|
| `Logger` | Pisanje u konzolu i fajl | `lock` |
| `RequestQueue` | Dodavanje/uzimanje iz reda | `lock` + `Monitor.Wait/PulseAll` |
| `SearchCache` | Čitanje/pisanje keša, LRU lista | `lock` + `Monitor.Wait/PulseAll` |

### Mehanizmi sinhronizacije

**`lock`**
Koristi se za zaštitu kritičnih sekcija — garantuje da samo jedna nit u datom trenutku pristupa deljenom resursu. Korišćen u svim trima klasama za osnovnu zaštitu podataka.

**`Monitor.Wait`**
Kada uslov nije ispunjen (red je prazan, red je pun, pretraga je u toku), nit otpušta lock i prelazi u stanje čekanja bez trošenja CPU resursa. Ovo je blokirajuća sinhronizacija kako se zahteva u specifikaciji.

**`Monitor.PulseAll`**
Nakon promene stanja (novi zahtev u redu, pretraga završena), sve čekajuće niti se bude i ponovo proveravaju uslov.

### Cache Stampede zaštita

Problem cache stampede nastaje kada veliki broj niti istovremeno traži isti resurs koji nije u kešu — sve bi krenule da pretražuju fajlove paralelno. Rešenje:

1. Prva nit koja detektuje cache miss poziva `TryMarkInProgress` — dodaje ključ u `HashSet<string> _inProgress` i dobija `true`
2. Sve ostale niti dobijaju `false` i pozivaju `WaitForResult` — blokiraju se sa `Monitor.Wait`
3. Kada prva nit završi pretragu, poziva `MarkDone` — uklanja ključ iz `_inProgress` i budi ostale niti sa `Monitor.PulseAll`
4. Ostale niti se bude i uzimaju rezultat direktno iz keša

### LRU keš strategija

Keš koristi ograničenje veličine (size-limited) sa LRU (Least Recently Used) strategijom upravljanja:
- Maksimalan broj unosa je 50
- Svaki pristup kešu pomera unos na početak `LinkedList`-e
- Kada je keš pun, unos sa kraja liste (najduže nekorišćen) se izbacuje
- `LinkedListNode` referenca u svakom `CacheEntry` omogućava O(1) operacije na listi

### Ponašanje sistema pod opterećenjem

Testiranje sa 50 paralelnih zahteva pokazalo je stabilno ponašanje:
- Svaka jedinstvena pretraga se izvršava tačno jednom
- Sve ostale niti dobijaju rezultat iz keša
- Nema race condition-a, nema deadlock-a
- Server ostaje stabilan i responzivan

## Autori

- Aleksa Bogdanović
- Nikola Stojković

## Predmet

Sistemsko Programiranje — Elektrotehnički fakultet
