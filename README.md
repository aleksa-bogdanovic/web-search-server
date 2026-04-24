# WebSearchServer

Projekat iz predmeta **Sistemsko Programiranje** — Web server za pretraživanje tekstualnih fajlova.

## Opis

Konzolna serverska aplikacija u C# koja omogućava pretraživanje reči u tekstualnim fajlovima putem browser-a. Server prima GET zahteve, pretražuje `.txt` fajlove u svom root direktorijumu i vraća HTML stranicu sa rezultatima pretrage.

### Primer poziva

```
http://localhost:5050/sistemsko&programiranje&elfak&projekat
```

## Arhitektura

- **HttpServer** — prima zahteve klijenata putem `HttpListener`
- **RequestQueue** — thread-safe red čekanja za pristigle zahteve
- **ThreadPoolWorker** — raspodela obrade zahteva putem `ThreadPool`-a
- **FileSearcher** — pretraga `.txt` fajlova po ključnim rečima
- **SearchCache** — keš sa ograničenjem veličine (size-limited LRU strategija)
- **ResponseBuilder** — generisanje HTML odgovora
- **Logger** — thread-safe logovanje događaja i grešaka

## Tehnologije

- C# / .NET 8
- `System.Net.HttpListener`
- `System.Threading` (ThreadPool, Monitor, lock)

## Pokretanje

1. Klonirati repozitorijum:
   ```bash
   git clone https://github.com/aleksa-bogdanovic/web-search-server.git
   ```
2. Otvoriti solution u Visual Studio / Rider / VS Code
3. Pokrenuti projekat (`F5` ili `dotnet run`)
4. U browser-u otvoriti:
   ```
   http://localhost:5050/rec1&rec2&rec3
   ```

## Testiranje

Za testiranje API-a preporučuje se [Postman](https://www.postman.com/).  
Test `.txt` fajlovi nalaze se u folderu `TextFiles/`.

## Autori

- Aleksa Bogdanović
- Nikola Simonović

## Predmet

Sistemsko Programiranje — Elektrotehnički fakultet
