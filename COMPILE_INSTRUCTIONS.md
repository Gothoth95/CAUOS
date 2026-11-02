# 🔨 Istruzioni di Compilazione - Classic Assistant HUD Gotica

## Prerequisiti

- **Windows 10/11** (consigliato)
- **Visual Studio 2019 o successivo** (Community edition è sufficiente)
- **.NET Framework 4.8** (solitamente preinstallato)
- **Git** (opzionale, per controllo versione)

---

## 📥 Step-by-Step Compilazione

### 1. Apri Visual Studio
```
Avvia Visual Studio 2019/2022
```

### 2. Apri la Soluzione
```
File → Open → Project/Solution
Apri il file ClassicAssist.sln nella cartella principale del repository CAUOS
Clicca "Open"
```

### 3. Attendi il Caricamento
```
Soluzione si caricherà (potrebbe impiegare 30-60 secondi)
Aspetta che tutti i progetti si carichino
```

### 4. Verifica i Progetti
```
Solution Explorer (a destra) dovrebbe mostrare:
- ClassicAssist (Progetto principale) ✓
- ClassicAssist.Shared
- ClassicAssist.Controls
- ClassicAssist.Launcher
- ClassicAssist.Updater
```

### 5. Seleziona Configurazione
```
In alto a sinistra:
Debug dropdown → "Release" (consigliato per performance)
Platform dropdown → "x64" (oppure "Any CPU")
```

### 6. Ripulisci (Opzionale ma Consigliato)
```
Build → Clean Solution
(Aspetta che termini)
```

### 7. Compila
```
Build → Rebuild Solution
Oppure: Ctrl + Shift + B

Attendi il completamento (2-5 minuti)
Dovresti vedere: "Build succeeded" nel messaggio finale
```

### 8. Esegui
```
Debug → Start Without Debugging (Ctrl + F5)
Oppure: Debug → Start Debugging (F5) per debug

L'applicazione si avvierà con il nuovo tema gotico!
```

---

## ✅ Verifica della Compilazione

Quando la compilazione è completata, dovresti vedere:

```
========== Build: X succeeded, 0 failed, 0 up-to-date, 0 skipped ==========
```

Se vedi **errori**, controlla:
- [ ] Visual Studio è aggiornato?
- [ ] .NET Framework 4.8 è installato?
- [ ] I file modificati sono salvati correttamente?

---

## 📍 File Compilati

Dopo una compilazione riuscita, i file .exe si troveranno in:

```
ClassicAssist\bin\Release\net48\ClassicAssist.dll
Output\net48\ClassicAssist.dll
```

Puoi copiare/creare scorciatoia da uno di questi percorsi per facilità d'accesso.

---

## 🎨 Verifica del Tema Gotico

Dopo l'avvio, dovresti vedere:

✅ **Header Nero** con bordo viola (#9333EA)  
✅ **Tab Buttons Viola Scuro** con highlight viola al hover  
✅ **Background Nero Profondo** (#0F0C0F)  
✅ **Testo Bianco Freddo** per contrasto massimo  
✅ **Effetti Ombra** sotto gli elementi  
✅ **Animazioni Pulsanti** su hover/select  

Se il tema non è visibile, controlla che i file XAML siano stati salvati correttamente:
- `ClassicAssist.Shared/Resources/Colours.xaml`
- `ClassicAssist.Shared/Resources/DarkTheme.xaml`
- `ClassicAssist/UI/Views/MainWindow.xaml`

---

## 🔧 Troubleshooting

### Compilazione Fallisce
```
Soluzione: Pulisci e ricompila
1. Build → Clean Solution
2. Attendi 30 secondi
3. Build → Rebuild Solution
```

### Tema Non Appare
```
Soluzione: Cancella la cache
1. Chiudi l'applicazione
2. Elimina: bin/Release (cartella)
3. Ricompila completamente
```

### Errori di XAML
```
Soluzione: Verifica sintassi
1. Apri i file .xaml modificati
2. Controlla eventuali errori (la riga sarà segnalata rosso)
3. Correggi la sintassi XML
4. Ricompila
```

---

## 💾 Backup (Consigliato)

Prima di compilare, fai un backup:

```
Copia l'intera cartella del repository CAUOS
In una cartella separata con il suffisso _BACKUP
```

Così avrai una copia pulita se serve revertare.

---

## 📞 Assistenza

Se riscontri problemi:

1. **Controlla i messaggi di errore** nella finestra Error List (Ctrl + Backspace)
2. **Verifica che i file siano salvati** (non devono avere asterisco in Visual Studio)
3. **Prova Clean + Rebuild** (a volte risolve)
4. **Riavvia Visual Studio** se persiste

---

## ⏱️ Tempo Previsto

- **Prima compilazione:** 5-10 minuti
- **Compilazioni successive:** 1-2 minuti
- **Avvio applicazione:** 2-5 secondi

---

## 🎯 Al Completamento

Una volta compilato, avrai:

✅ Classic Assistant con **tema gotico completo**  
✅ **Animazioni fluide** su tutti i controlli  
✅ **Colori drammatici** nero/viola/argento  
✅ **Look medievale** sofisticato e elegante  
✅ **Performance ottimale** (versione Release)

Goditi il nuovo look! 🖤

---

**Ultima modifica:** 29 Aprile 2026  
**Versione:** 1.0  
**Status:** Pronto per compilazione
