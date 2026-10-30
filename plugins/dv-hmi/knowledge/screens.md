# Screens

Read when: making or changing a screen.

## Whose design

- The design follows the user's intent. If the user describes or shows how it should look, build exactly that.
- A box sketch (labelled rectangles) fixes **where** things go and what each area holds, not how they look. Keep its layout, and fill each box as if the look were left to you: the control that fits the kind of value (table below), not one plain card per box.
- If the user leaves it to you, work out the design that is most beautiful and communicates best **for this program**. Start from what the operator must notice first, what kind of equipment it is (a process flow, a few key values, a sequence of steps), and what the operator does most often; choose the layout and the controls from that.
- **The frame comes first.** Before the first page, decide how the operator moves between screens and where what every screen needs lives — the menu, comm state, unacknowledged alarms, the clock, the language switch (EN / 한국어): bars at the top, bottom, left or right (`DvBar`, Devinno.UI `dvml-ui-markup.md` §16.5–16.6), inside the pages, or no bars at all. setup places no bars on purpose, so the frame is yours to choose.
- **Find the options in this program, not in a finished screen.** Ask what the operator must notice first and from how far away, touch or mouse, how many screens and how often they switch, how wide the main content is. Different answers point to different frames and layouts (a dock under the thumb, a narrow rail beside a wide diagram, one screen with no menu at all). Compare at least two for this program in your head (no draft of the others), pick one, and tell the user that direction in one sentence as you draft it — the interview drafts right away.
- **Then** look through the reference program's screens (`reference/HotAirDryerHmi/Pages/`) for controls and techniques you can use — process symbols, gauges, tanks, trends, state colors, hold buttons, how a bar highlights the current page or acknowledges alarms — and pick the ones that fit your screen. Take controls and techniques, not its layout: its frame and layouts were reasoned for a hot air dryer, and your layout stays the direction you chose.
- Why the direction comes first: a screen with the same role sits finished in the reference; seen before you have your own idea, it becomes the idea (a default is not a choice). Decided first, the reference becomes a source of controls instead. If your reason would fit any program, it is not a reason yet.

## Principles

Fill the window — HMI screens rarely leave empty regions (spacing between elements is fine, a blank half of a screen is not). Fill it by giving the screen's main content more room (a longer list, a bigger diagram), not with extra cards that repeat what another part of the screen already shows. Use the library's controls richly and pick them by the kind of value:

| Kind of value | Controls (Devinno.UI `dvml-ui-markup.md` §7–§12) |
|---|---|
| Key values | `DvStat` (big number, unit, tone), with a trend or sparkline nearby |
| Level, load, percentage | `DvGauge`, `DvRing`, `DvTank`, `DvProgress` |
| Setpoints | `DvKnob`, `DvSlider`, `DvInput`, `App.InputNumber` |
| States | `DvStatusItem`, `DvLamp`, `DvBadge`, `DvStepBar` |
| Flow of the process | `DvProcess` (`dvml-process.md`) |
| Over time, counts | `DvTrend`, `DvSparkline`, `DvBarChart`, `DvPieChart` |
| Lists | `DvDataGrid`, `DvListBox` |

- Read `src/<app>/.devinno/Devinno.UI/llm-rules.md` before writing markup — many mistakes are silently ignored (`%`, `auto` and `em` sizes become 0; unknown attributes vanish).
- Fill the window with `grow="1"`; giving cards `grow="1"` with `style="height:0"` (or `width:0`) makes them share the space.
- Decide how each screen looks on comm loss and alarms, and render those states too (a values file per state).
- The screen set comes from the request. If your design goes beyond the number of screens the user asked for, say so before building.
- Colors: theme tokens (`tone-*`, `state-*`, `alarm-*`), never hex. Color shows state and abnormal conditions, not decoration, and never alone (add text or an icon).
- Touch panel (Senvas, OpenTK): larger targets (`size="lg"`, rows 40–44 high), nothing that depends on hover; a soft keyboard appears for inputs.
- After every change: build (no DVML warnings), then render and look at the PNGs (the working rules, "Checking screens").

## When the controls cannot do it

1. If the built-in controls cannot show what the user asked for (a symbol the process diagram lacks, a widget made of several), look at the library's extensions first: user symbols (`dvml-ui-markup.md`, "User symbols (DvSymbol)") and user components ("User components (DvComponent)"). Both are registered in C# code.
2. If it still cannot be done **at this stage**, say so with the real reason and what is possible instead or later — e.g. "In the interview drafts I cannot draw user symbols (they need C# code, which comes in implementation); for now I drew cards with arrows, and in implementation it may be possible with symbols." Not "it is hard with the piping diagram" when the real limit is something else.
3. Changing the form the user asked for goes against what they said (the second of the three criteria): tell it on its own, not inside the list of parts you invented, and offer the later option.
