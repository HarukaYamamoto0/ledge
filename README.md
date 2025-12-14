# Ledger

Ledger is a **server-side Vintage Story mod** that periodically exports player data to **JSON**, allowing bots,
dashboards, and external tools to consume game information **without directly interacting with the game engine**.

The project focuses on **simplicity, predictability, and minimal server impact**.

## What Ledger Does

* Runs **exclusively on the server**
* Generates **one JSON file per player**
* Updates data at configurable intervals
* Maintains a consistent state even when:
    * players are offline
    * players are dead
    * chunks are unloaded
* Uses **Unix timestamps** for easy consumption by bots (e.g. Discord)

The generated files are suitable for:

* Discord bots
* Web dashboards
* Monitoring systems
* External integrations and scripts

## File Structure

By default, Ledger writes files to:

```shell
<VintagestoryData>/ModData/ledger/
````

File naming format:

```shell
<uid>.json
```

> File names use **base64url encoding** of the player UID to remain filesystem-safe and reversible.

## Example Output

```json
{
  "SchemaVersion": 2,
  "Uid": "Te6I6bIpn69Pu5V52kVw-s3s",
  "Name": "harukadev",
  "Online": true,
  "Meta": {
    "FirstJoin": 1765672656,
    "LastJoin": 1765672797,
    "LastSeen": 1765673706
  },
  "Location": {
    "X": 25,
    "Y": 4,
    "Z": -6
  },
  "Stats": {
    "Health": {
      "Current": 15.09333324432373,
      "Max": 15.09333324432373
    },
    "Hunger": {
      "Current": 750.0,
      "Max": 1500.0
    },
    "Deaths": 0,
    "PlaytimeSeconds": 1044,
    "PingMs": 34,
    "Privileges": [
      "announce",
      "areamodify",
      "attackcreatures",
      "attackplayers",
      "ban",
      "time",
      "...",
      "useblockseverywhere",
      "whitelist",
      "worldedit"
    ]
  },
  "Equipment": {
    "Armor": [
      "armor-head-scale-iron",
      "armor-body-brigandine-blackbronze",
      "armor-legs-tailored-black-linen"
    ],
    "HeldItem": "termitemound-harvested-chert-large",
    "Hotbar": [
      "termitemound-harvested-chert-large",
      "none",
      "none",
      "none",
      "none",
      "none",
      "none",
      "none",
      "none",
      "none"
    ]
  },
  "World": {
    "AmbientTemperature": 25.06141471862793,
    "ClimateTag": "arid"
  }
}
```

## ⚠️ Important Notes for Consumers

### Optional / Missing Fields

Ledger **may omit fields from the JSON output** when data is unavailable or not applicable.

This is intentional.

Consumers **must treat all fields as optional** and apply their own defaults when needed.

Examples:

* Offline players may not have updated world or equipment data
* Some fields may be absent instead of explicitly set to `null`

### Name Field (`Name`)

* When the player name cannot be resolved (e.g., offline with no prior data), `Name` will be set to:

```
"Unknown"
```

This value is a **sentinel**, not a real username.

When the player joins again, the correct name will automatically replace it.

### Equipment Fields

* `HeldItem`
  Represents the item currently held in the **active hotbar slot**.

* `Hotbar`
  A fixed-size list (10 entries) representing the player's hotbar.
  Empty slots are exported as `"none"`.

### SchemaVersion

`SchemaVersion` indicates the structure/semantics version of the JSON document.

* Missing `SchemaVersion` should be treated as version `1`
* Future versions may add, rename, or remove fields
* Consumers are expected to tolerate forward-compatible changes

## Commands

Ledger provides a small set of **server-side chat commands** for administration and runtime control.

### `/ledger reload`

Reloads the Ledger configuration **without restarting the server**.

This command re-reads `ledgerconfig.json` and immediately applies the new `Capture` settings to later snapshots.

**Behavior:**

* Applies changes to `Capture.*` options immediately
* Does **not** change the snapshot interval (`IntervalSeconds`)
* Does **not** reset or rewrite existing player data
* Safe to run while the server is online

**Usage:**

```text
/ledger reload
```

**Requirements:**

* Server-side only
* Requires the `controlserver` privilege

**Notes for administrators:**

* If you change `IntervalSeconds`, a server restart is still required
* This command is intended for fine-tuning what data is captured without interrupting gameplay

## Safe File Writing (Important)

To avoid partial reads, Ledger **never writes directly to the final file**.

The process is:

1. Write data to a temporary file
2. Atomically replace the final file

Temporary file format:

```
._<uid>.json.tmp
```

### If you use watchdog / file watchers

Ignore files that:

* end with `.tmp`
* start with `._`

Example ignore rules:

```
*.tmp
._*
```

## Configuration

Configuration file location:

```
<VintagestoryData>/ModConfig/ledgerconfig.json
```

Example configuration:

```json
{
  "IntervalSeconds": 60,
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

### Options

* `IntervalSeconds`
  Update interval in seconds.

* `EnableJson`
  Enables JSON file export.

* `BasePath`
  Optional custom output directory.
  Leave empty to use the default ModData path.

### Capture Filters

The `Capture` section controls which fields are collected and written to the JSON snapshot.

When a capture option is disabled, Ledger will generally **keep the previous value** (instead of overwriting with
defaults).

* `Vitals`
  Health and hunger.

* `Tiredness`
  Tiredness value (if available).

* `Ping`
  Player ping in milliseconds (when online).

* `Privileges`
  List of privileges for the player.

* `Location`
  Player coordinates (X/Y/Z).

* `World`
  Ambient temperature and climate tag.

* `Equipment`
  Armor and held item.

* `Hotbar`
  When enabled, exports the full hotbar list (10 entries).

## What Ledger Does **Not** Do (By Design)

* ❌ Does not send data to Discord
* ❌ Does not perform HTTP requests
* ❌ Does not depend on external APIs
* ❌ Does not attempt to infer data not exposed by the engine

Ledger **only exports raw, reliable server-side data**.
How this data is used is entirely up to the external tool.

## Project Status

* Status: **Experimental but functional**
* JSON structure may evolve
* Backward compatibility is considered but not guaranteed
* Feedback is welcome, but the scope is intentionally limited

## License

Licensed under the **MIT License**.
Do whatever you want — just give credit.