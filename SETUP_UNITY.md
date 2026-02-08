# Nexus Prime – Setup e avvio in Unity

## 1. Struttura cartelle (già creata)

Le cartelle e gli script sono già organizzati sotto **Assets/Scripts/** come segue. Se apri il progetto in Unity, troverai tutto al posto giusto.

```
Assets/
└── Scripts/
    ├── Core/
    │   ├── GameManager.cs
    │   ├── GameState.cs
    │   ├── PlayerData.cs
    │   └── SaveSystem.cs
    ├── Economy/
    │   ├── ResourceManager.cs
    │   ├── Resource.cs
    │   ├── ResourceProducer.cs
    │   ├── ResourceConsumer.cs
    │   └── ResourceType.cs
    ├── Units/
    │   ├── UnitMovement.cs
    │   ├── UnitStats.cs
    │   ├── CombatUnit.cs
    │   ├── SelectableUnit.cs
    │   └── UnitFactory.cs
    ├── Building/
    │   ├── BuildingSystem.cs
    │   ├── Building.cs
    │   ├── BuildingGhost.cs
    │   ├── BuildingType.cs
    │   └── BuildingFactory.cs
    ├── Technology/
    │   ├── TechTreeSystem.cs
    │   ├── Technology.cs
    │   ├── TechTreeData.cs
    │   └── ResearchManager.cs
    ├── AI/
    │   ├── AICommandManager.cs
    │   ├── AIFactionController.cs
    │   ├── AIprofile.cs
    │   └── BehaviorTree/
    │       ├── AIBehaviorTree.cs
    │       ├── AIActionNode.cs
    │       └── AIConditionNode.cs
    ├── UI/
    │   ├── UIManager.cs
    │   ├── UIAnimator.cs
    │   ├── ResourcePanel.cs
    │   ├── UnitCommandPanel.cs
    │   ├── SelectionManager.cs
    │   ├── SelectionPanel.cs
    │   ├── NotificationPanel.cs
    │   ├── ObjectivesPanel.cs
    │   ├── ChatPanel.cs
    │   ├── TechTreeUI.cs
    │   ├── Minimap.cs
    │   ├── BuildMenu.cs
    │   ├── GameOverScreen.cs
    │   ├── DamageNumberUI.cs
    │   ├── ContextMenuUI.cs
    │   ├── ConfirmationDialogUI.cs
    │   └── DialogueSystem.cs
    ├── Campaign/
    │   ├── CampaignManager.cs
    │   ├── Mission.cs
    │   ├── MissionObjective.cs
    │   └── MissionReward.cs
    ├── Factions/
    │   ├── FactionManager.cs
    │   ├── Faction.cs
    │   └── FactionData.cs
    ├── Audio/
    │   ├── AudioManager.cs
    │   ├── MusicManager.cs
    │   └── SFXManager.cs
    └── Utils/
        ├── ObjectPool.cs
        ├── Extensions.cs
        ├── MathHelper.cs
        └── Singleton.cs
```

**Nota:** Se per ora tieni tutti gli script in una sola cartella (es. `Assets/Scripts/`), il gioco funziona lo stesso. La struttura sopra serve per ordine e manutenzione.

---

## 2. Come avviare il progetto in Unity

### Aprire il progetto

1. Avvia **Unity Hub**.
2. **Aggiungi** il progetto: punta alla cartella che contiene **Nexus Prime** (quella con i file `.cs` e, se presente, la cartella `Assets`).
3. Seleziona una **versione di Unity** compatibile (es. **2021.3 LTS** o **2022.3 LTS**).
4. Clicca sul progetto per aprirlo.

### Prima apertura

- Se non esiste ancora un progetto Unity nella cartella:
  - Crea un **nuovo progetto** (3D o 2D, a tua scelta).
  - Copia tutti i file `.cs` nella cartella **Assets/Scripts/** del nuovo progetto (crea la cartella se non c’è).
- Se la cartella **Nexus Prime** è già un progetto Unity (contiene `Assets`, `ProjectSettings`, ecc.):
  - Apri quella cartella come progetto da Unity Hub.

### Scena di gioco minima

1. **File → New Scene** (o salva la scena di default come “Sandbox” o “MainMenu”).
2. Crea un **GameObject vuoto** e rinominalo **GameManager**.
3. Aggiungi al GameManager gli script:
   - **GameManager** (NexusPrime.Core)
   - **ResourceManager**
   - **TechTreeSystem**
   - **BuildingSystem**
   - **FactionManager**
   - **CampaignManager**
4. Crea un altro GameObject **BuildingSystem** e aggiungi:
   - **BuildingSystem**
   - **BuildingFactory** (lascia la lista “Definitions” vuota; aggiungerai BuildingDefinition dopo aver creato prefab).
5. Per la **UI**:
   - Crea una **Canvas** (UI → Canvas).
   - Aggiungi **UIManager** a un GameObject sotto la Canvas (o a un “UIManager” sotto la Canvas).
   - Assegna nell’Inspector i riferimenti richiesti (Main Canvas, pannelli, ecc.). I campi opzionali possono restare vuoti; il gioco gestirà i null.

### Avviare il gioco

1. Salva la scena (**Ctrl+S**).
2. Premi **Play** (triangolo in alto nell’Editor).
3. Il **GameManager** si inizializza e crea/collega i manager mancanti; in Console potrebbero apparire messaggi tipo “Game Manager Initialized”.

---

## 3. Dipendenze opzionali

- **TextMeshPro:** usato da molti script UI. Se Unity chiede di importare TMP, clicca **Import TMP Essential Resources**.
- **DOTween (DG.Tweening):** usato da `UIAnimator` e `TechTreeUI`. Se non lo usi, puoi commentare le righe che usano DOTween o installare il pacchetto dall’Asset Store / Package Manager.

---

## 4. Scena MainMenu

`GameManager` e `UIManager` fanno riferimento alla scena **"MainMenu"** (es. per tornare al menu). Crea una scena chiamata **MainMenu** e aggiungila in **File → Build Settings → Scenes In Build** (e, se usi “Sandbox” come scena di gioco, aggiungi anche quella). Così il pulsante “Torna al menu” e il flusso di gioco funzionano correttamente.

---

## Riepilogo

| Cosa fare | Dove / Come |
|-----------|-------------|
| Cartelle script | **Assets/Scripts/** con sottocartelle Core, Economy, Units, Building, UI, AI, Campaign, Factions, Audio, Utils (vedi schema sopra). |
| Aprire il gioco | Unity Hub → apri cartella progetto Nexus Prime → Play nella scena con GameManager. |
| Scena minima | GameObject con GameManager (+ altri manager) e Canvas con UIManager; scena “MainMenu” in Build Settings se usi ritorno al menu. |
