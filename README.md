# ledge
Exports player stats and world info to JSON on the server for use in bots, dashboards, and external tools.

Ledger writes to a temporary file `._<uid>.json.tmp` and then replaces <uid>.json to avoid partial reads.
If you use watchdog, ignore files ending in .tmp or starting with `._.`