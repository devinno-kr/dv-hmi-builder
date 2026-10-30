# Trends, records and recipes

Read when: showing values over time, writing logs, saving recipes.

Ask first: what to record, when (period or on change), where, how long to keep it, and whether it must survive a restart (asking-the-user.md).

## Trends

`TrendBuffer` + `<DvTrend>` (Devinno.UI `dvml-ui-markup.md` §12, `TrendBuffer` remarks in `Devinno.UI.xml`):
- Capacity is a count of points: capacity × record interval = time kept. Check it covers the longest window you show.
- `double.NaN` breaks the line — record NaN while a value is invalid.
- One shared vertical axis for all channels: put values with different units in separate trends.
- Channel names must match exactly (case-sensitive). Call `App.Invalidate()` after adding.
- Memory only: a trend is empty after a restart unless you store and reload it.

Pattern: `reference/HotAirDryerHmi/Managers/LogicManager.cs` (`Record`), and for the markup only the `<DvTrend>` element in `Pages/MonitorPage.dvml` (its channels and window binding) — not that page's layout (screens.md).

## Files

The library writes no logs. Records are your code, in `DataManager` (file paths only there):
- The logic timer only queues a line; a dedicated writer thread writes the file, so a slow disk or network folder never delays judgement. On write failure: one alarm, drop the line, continue.
- Where: by default a `data` folder under `DataManager.UserDataFolder` (next to `config`; Windows `%LOCALAPPDATA%\<name>`, touch panel `~/.local/share/<name>`). Not the program folder: the dashboard's deploy replaces it (on the touch panel the whole folder is deleted and unpacked again), so records and settings there are lost. Not Documents or Desktop — they are often synced to the cloud (OneDrive), which can lock a file the program writes every minute and uploads the records. If the user needs another place (a network share, another drive), ask, and make it a setting.
- `CultureInfo.InvariantCulture` for numbers and times. Save settings and recipes atomically (write a temp file, then move it over).

## Recipes

Not in the library. Pattern: `reference/HotAirDryerHmi/Models/Recipe.cs`, `Pages/RecipePage.cs`, `Managers/LogicManager.cs` (`ApplyRecipe`) — a list, edit a copy, validate, save, and send to the device as a separate step. It counts as applied only when the device confirmed the writes (modbus.md "Writes"); a schedule starts the machine only after that.
