# Ledger

Ledger is a **server-side Vintage Story mod** that periodically exports player data to **JSON**, allowing bots, dashboards, and external tools to consume game information without directly interacting with the game engine.

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

```
<VintagestoryData>/ModData/ledger/
```

File naming format:

```
<uid>.json
```

Example output:

```json
{
  "Uid": "TB1I6bIpn66PuXV5pkVw-s3s",
  "Name": "harukadev",
  "Online": true,
  "Stats": {
    "Health": {
      "Current": 20.0,
      "Max": 21.0
    },
    "Hunger": {
      "Current": 950.0,
      "Max": 1900.0
    },
    "Deaths": 2,
    "PlaytimeSeconds": 627
  },
  "Equipment": {
    "Armor": [
      "armor-head-plate-iron",
      "armor-body-chain-blackbronze",
      "armor-legs-tailored-yellow-linen"
    ],
    "Weapon": "blade-longsword-admin"
  },
  "World": {
    "AmbientTemperature": 18.03624153137207,
    "ClimateTag": "arid"
  },
  "FirstJoin": 1765592936,
  "LastJoin": 1765593661
}
```

## Safe File Writing (Important)

To avoid partial reads, Ledger **never writes directly to the final file**.

The process is:

1. Write data to a temporary file
2. Atomically replace the final file

Temporary file format:

```
._<uid>.json.tmp
```

### ⚠️ If you use watchdog / file watchers

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
  "EnableSqlite": false
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
* Feedback is welcome, but the scope is intentionally limited

## License

Licensed under the **MIT License**.
Do whatever you want — just give credit.
