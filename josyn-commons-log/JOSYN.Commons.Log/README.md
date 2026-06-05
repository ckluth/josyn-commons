# JOSYN.Commons.Log

Part of the **JOSYN** (JobSystem Next) ecosystem — member of the `josyn-commons` utility layer.

`JOSYN.Commons.Log` stellt einen prozess-lokalen Datei-Logger bereit, der von allen
JOSYN-EXE-Prozessen (`JOSYN.JobHost`, `JOSYN.Jap.JAPServer`) und Backend-Komponenten
(`JOSYN.Backend.ErrorHandler`) genutzt wird.

Vormalig bekannt als `JOSYN.Jap.Shared.Log` — gemäß ADR-008 nach `josyn-commons` verschoben,
da kein Bezug zum JAP-Protokoll besteht und der Logger eine plattformweite Infrastruktur-Utility ist.

---

## Überblick

Jeder JOSYN-Prozess schreibt sein eigenes Log in:

```
<LogDirectory>\<yyyy-MM-dd>.log
```

Einträge werden **sofort auf Platte geflusht** (kein Puffer). Schreibfehler werden
stillschweigend ignoriert — der Logger darf den Host-Prozess niemals zum Absturz bringen.
Mit gesetztem Flag `LocalLog.EnableConsoleOutput = true` wird zusätzlich auf die Konsole
geschrieben.

---

## API

```csharp
// Fehlereintrag — String-Variante
LocalLog.WriteError("Verbindung fehlgeschlagen.", callStack: "...", exceptionDetails: "...");

// Fehlereintrag — Result-Variante (extrahiert Message, CallStack, Exception automatisch)
LocalLog.WriteError(result);

// Fehlereintrag — mit Causer (schreibt in Unterordner)
LocalLog.WriteError("JAPServer", "Verbindung fehlgeschlagen.");

// Info-Eintrag
LocalLog.WriteInfo("Server terminiert.");
```

---

## Log-Format

```
[2026-06-05 11:43:12 +02:00] [ERROR]
Verbindung fehlgeschlagen.
--- CallStack ---
  → Host.RunServer  (Host.cs:43)
  → ...
--- Exception ---
  System.IO.IOException: ...
--------------------------------------------------------------------------------
```

---

## Abhängigkeiten

| Paket | Rolle |
|---|---|
| `JOSYN.Foundation.ResultPattern` | `Result`-Überladung von `WriteError` |

---

*JOSYN.Commons.Log — © 2026 HAEVG AG — MIT License*
