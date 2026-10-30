# Asking the user

Read when: the request involves a device, addresses, thresholds, commands, or anything you would otherwise have to guess.

The user is often a field engineer, not a developer.

## How to ask

- Plain words, one question at a time. Offer 2–3 choices with your recommendation first and one line of reason, so the user can simply accept.
- Ask about the user's world (ranges, limits, what they watch, what they press, what happened before), not about software (framework, polling, byte order, threads). Ask for experience, not numbers: "Has it ever stopped because it got too hot?"
- Ask for what the user has: the device manual's address table, IP/port/unit (or serial settings), model name. Take any format (PDF, photo, spreadsheet, typed list); do not ask them to reformat it. Korean CSV files are often EUC-KR (CP949).
- When the user gives a list and says it is complete, do not add items to it.

## Never invent these (high risk)

If missing: ask. If the user does not know either: propose the safe default (commands-and-safety.md) and build it after they agree. The default is written in `spec.md` as a `가정:` line (an assumption the user signs and checks on site) — once the requirements are signed, adding it is a change: show it and sign it, and named in your report.

- addresses, raw format, scale, units, display ranges
- warning and danger limits, alarm list and priorities, what acknowledging means
- commands written to the device: address, how (value / hold on-off / pulse), when blocked
- what the HMI does automatically on a condition (interlock) and who releases it
- start-up behaviour, stop during a sequence, behaviour on comm loss
- what is recorded, for how long, and whether it survives a restart

Safe default for anything that can move equipment: locked (the most restrictive state).

Low-risk details (sizes, colors, refresh period, small delays, names in code): decide yourself and mention them briefly in the report.

## Address tables

- Fill what you can. Values you **interpreted** (scale, signed/unsigned, word order, address base, write method) are still assumptions even if they look obvious: show them together once and let the user correct only what is wrong. If the user does not know either, the person to confirm is whoever programs the device.
- Keep the manual's notation (`address`) next to your interpretation (`type`, `scale`, `how`, `note`) in the memory map (`.dv-hmi/design/devices.json`, modbus.md).
- Items that are only in the table and were not requested: do not add them; ask if they look needed.

## Reporting

In assistant mode, end each request with: what you did, what you assumed (safety-related ones first), what you could not check (workflow mode reports at the end of each stage instead). Short, in the user's language, no code terms unless the user uses them.
