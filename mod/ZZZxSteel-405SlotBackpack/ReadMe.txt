🇩🇪 ANLEITUNG (Deutsch)

📦 Installation
1. Entpacke die Mod in deinen Mods-Ordner
2. Stelle sicher, dass nur eine Version aktiv ist

⚙️ Einstellungen (config.json)
Öffne die Datei config.json und passe die Werte an:

{
  "TabsCount": 9,
  "SlotsPerPage": 45
}

▶️ Anwendung
Doppelklicke auf apply_config.bat

Das Skript passt automatisch an:
- windows.xml
- entityclasses.xml
- progression.xml (perkPackMule)

🛠 Was in der progression.xml geändert wird
Das Skript passt den Eintrag für perkPackMule automatisch an.
Dabei wird dieser Wert neu berechnet:

- CarryCapacity

Dieser Wert legt fest, wie viele Slots durch das Perk "Pack Mule" freigeschaltet werden.
Die Werte werden automatisch an die eingestellte Backpack-Größe angepasst.

Beispiel bei 9 Tabs und 45 Slots pro Seite:
- Gesamtgröße: 405 Slots
- progression.xml wird so angepasst, dass perkPackMule zu dieser Größe passt

Dadurch bleibt das Perk-System mit der eingestellten Backpack-Größe synchron.

🔒 Hinweise
- TabsCount: 1–10
- SlotsPerPage: 9–45
- SlotsPerPage sollte ein Vielfaches von 9 sein (9, 18, 27, 36, 45)

Falls ein ungültiger Wert eingetragen wird, wird er automatisch korrigiert.

💾 Backups
Beim Ausführen werden automatisch Sicherungskopien erstellt:
- windows.xml.bak
- entityclasses.xml.bak
- progression.xml.bak

❗ Fehlerbehebung
Falls nichts passiert:
- Rechtsklick auf apply_config.bat und "Als Administrator ausführen"
- Prüfen, ob PowerShell auf dem System blockiert wird


----------------------------------------

🇬🇧 INSTRUCTIONS (English)

📦 Installation
1. Extract the mod into your Mods folder
2. Make sure only one version is active

⚙️ Configuration (config.json)
Open config.json and adjust the values:

{
  "TabsCount": 9,
  "SlotsPerPage": 45
}

▶️ Apply
Double-click apply_config.bat

The script automatically updates:
- windows.xml
- entityclasses.xml
- progression.xml (perkPackMule)

🛠 What is changed in progression.xml
The script automatically updates the perkPackMule entry.
It recalculates this value:

- CarryCapacity

This value defines how many slots are unlocked through the "Pack Mule" perk.
The values are automatically adjusted to match the configured backpack size.

Example with 9 tabs and 45 slots per page:
- Total size: 405 slots
- progression.xml is updated so perkPackMule matches that size

This keeps the perk system synchronized with the configured backpack size.

🔒 Notes
- TabsCount: 1–10
- SlotsPerPage: 9–45
- SlotsPerRows should be a multiple of 9 (9, 18, 27, 36, 45)

If an invalid value is entered, it will be corrected automatically.

💾 Backups
The script automatically creates backup files:
- windows.xml.bak
- entityclasses.xml.bak
- progression.xml.bak

❗ Troubleshooting
If nothing happens:
- Right-click apply_config.bat and choose "Run as Administrator"
- Check whether PowerShell execution is blocked on the system
