# Ledger

Ledger is a **server-side Vintage Story mod** that periodically exports player data to **JSON**, allowing bots,
dashboards, and external tools to consume game information **without directly interacting with the game engine**.

The project focuses on **simplicity, predictability, and minimal server impact**.

Ledger is intentionally limited in scope: it exports **raw, reliable server-side data** and leaves all further
processing, visualization, and integrations to external tools.

## ⚠️ Project Status: Unmaintained

> **Ledger is no longer actively maintained.**

The mod is considered **feature-complete for its intended scope** and will not receive further updates,
new features, or long-term support.

### What this means

* ✔ The mod **works as-is** and can be safely used
* ✔ Existing functionality will not be intentionally broken
* ❌ No new features will be added
* ❌ No guarantees of compatibility with future Vintage Story versions
* ❌ Bug fixes will only happen if absolutely critical and trivial

The codebase remains available for:

* personal use
* learning/reference
* forks and community maintenance

If you wish to extend or maintain Ledger, feel free to fork the project.

## What Ledger Does

* Runs **exclusively on the server**
* Generates **one JSON file per player**
* Updates data at configurable intervals
* Tracks players whether they are:

  * online
  * offline
  * dead
* Preserves previously valid data when the game state becomes temporarily invalid
* Uses **Unix timestamps** for easy consumption by bots (e.g. Discord)

The generated files are suitable for:

* Discord bots
* Web dashboards
* Monitoring systems
* External scripts and integrations

## File Structure

By default, Ledger writes files to:

```shell
<VintagestoryData>/ModData/ledger/
```

File naming format:

```shell
<uid>.json
```

> File names use **base64url encoding** of the player UID to remain filesystem-safe and reversible.

Each player always has **exactly one file**, which is updated in place.

## Example Output

```json
{
  "SchemaVersion": 2,
  "Uid": "Q9aZ2Lw7nJpM3F6cUeR0xA",
  "Name": "harukadev",
  "Online": true,
  "Meta": {
    "FirstJoin": 1765814109,
    "LastJoin": 1765814109,
    "LastSeen": 1765814145
  },
  "Location": {
    "X": -20,
    "Y": 114,
    "Z": -53
  },
  "Stats": {
    "Health": {
      "Current": 4.744039535522461,
      "Max": 16.48400115966797
    },
    "Hunger": {
      "Current": 0.0,
      "Max": 1500.0
    },
    "Deaths": 0,
    "PlaytimeSeconds": 36,
    "PingMs": 35,
    "Privileges": [
      "announce",
      "areamodify",
      "attackcreatures",
      "attackplayers",
      "ban",
      "build",
      "buildblockseverywhere",
      "chat",
      "controlplayergroups",
      "controlserver",
      "worldedit"
    ]
  },
  "Equipment": {
    "Armor": [
      "clothes-upperbody-commoner-shirt",
      "armor-body-scale-steel",
      "armor-legs-scale-steel"
    ],
    "HeldItem": "redmeat-cooked",
    "Hotbar": []
  },
  "World": {
    "AmbientTemperature": 4.646189212799072,
    "ClimateTag": "arid"
  }
}
```

## ⚠️ Important Notes for Consumers

### Optional / Missing Fields

Ledger **may omit fields from the JSON output** when data is unavailable or not applicable.

Consumers **must treat all fields as optional** and apply their own defaults.

### Name Field (`Name`)

If the player name cannot be resolved, `Name` will be set to:

```text
"Unknown"
```

This value is a sentinel and will be replaced automatically when the player joins again.

### Equipment Handling

* `Equipment.Armor` is a **flexible list**
* Supports:

  * vanilla armor
  * layered clothing
  * mods adding additional armor slots
* The order is **not positional**

Consumers must not assume fixed slot indices.

### SchemaVersion

* Missing `SchemaVersion` ⇒ assume version `1`
* Future versions may add or remove fields
* Consumers must tolerate forward-compatible changes

## Commands

### `/ledger reload`

Reloads configuration without restarting the server.

* Applies new `Capture.*` options immediately
* Does not change `IntervalSeconds`
* Requires `controlserver` privilege

## Safe File Writing

Ledger uses atomic writes:

1. Write to temporary file
2. Replace final file

Temporary files:

```text
._<uid>.json.tmp
```

Ignore in watchers:

```text
*.tmp
._*
```

## Configuration

Config file:

```text
<VintagestoryData>/ModConfig/ledgerconfig.json
```

Example:

```json
{
  "IntervalSeconds": 5,
  "BasePath": "",
  "EnableJson": true,
  "Capture": {
    "Vitals": true,
    "Tiredness": false,
    "Ping": true,
    "Privileges": true,
    "Location": true,
    "World": true,
    "Equipment": true,
    "Hotbar": false
  }
}
```

## What Ledger Does **Not** Do

* ❌ No Discord integration
* ❌ No HTTP requests
* ❌ No external dependencies
* ❌ No inferred or guessed data

## License

MIT License — fork, modify, reuse freely.
Attribution appreciated.
