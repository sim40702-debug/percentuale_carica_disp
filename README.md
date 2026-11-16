# Batterie in Barra

Mini batterie per cuffie, tastiera, mouse e controller Bluetooth **direttamente sulla barra di Windows**,
a sinistra delle icone vicino all'orologio. Con un clic si apre un widget con cerchi, tempo stimato e grafico
delle ultime 24 ore.

Scritta in **C# (WPF, .NET 10)**: leggera, senza PowerShell né browser dentro.

## Come si usa

1. Scarica `BatterieInBarra-x.y.z.exe` dalle **Releases**
2. Mettilo in una cartella fissa (es. Documenti) e aprilo
3. **Clic** sulle batterie: widget. **Tasto destro**: Aggiorna, Impostazioni, Sposta, Esci

## Come pubblicare una versione nuova

1. Scrivi le novità in cima a `docs/CHANGELOG.md`, in una sezione `## 0.2.1 — data (titolo)`
2. Dal terminale:
   ```
   git pull && git add . && git commit -m "Versione 0.2.1" && git push && git tag v0.2.1 && git push origin v0.2.1
   ```
3. GitHub compila da solo e crea la release "Batterie in Barra 0.2.1" con l'exe

A ogni push GitHub controlla anche che il codice compili (Actions → "Controlla che compili").

## I file

- `App.xaml.cs` - il cervello: legge le batterie, aggiorna barra e widget, avvisi
- `Batterie.cs` - chiede a Windows la batteria dei dispositivi Bluetooth collegati
- `Finestre.cs` - funzioni di Windows: posizione della barra, schermo intero, finestre sempre davanti
- `Impostazioni.cs` - legge e salva le impostazioni
- `Cronologia.cs` - si ricorda la batteria nel tempo (grafico e tempo stimato)
- `Avvisi.cs` - le notifiche di Windows
- `Grafica.cs` - colori, tema chiaro/scuro, icone e mini batterie
- `BarraWindow` - le mini batterie sulla barra
- `WidgetWindow` - il widget con cerchi, lista e grafico
- `ImpostazioniWindow` - la finestra Impostazioni

Le impostazioni e la cronologia vengono salvate in `%APPDATA%\BatterieInBarra`.

## Compilare sul PC Windows (facoltativo)

Serve il .NET 10 SDK. Poi nella cartella del progetto:
```
dotnet run
```
