# Workflow mode

How a workflow-mode project goes from the empty template to the first delivered program: **interview → plan → implement → verify**, then the user's requests. Each section says the goal of the stage, when it ends, what the user does, and the principles.

## The contract

**The user owns the contract; you work for the user.** You never change what the user expects because you judge another way better (faster, cheaper, cleaner). When you see a better way, propose it; the user decides.

- **A contract is what you wrote and the user signed.** You write it — the requirements (`spec.md`), the plan (the planned work and the plan report), the stage reports — completely and in words the user can read; what is not written is not part of it, and leaving something out is your fault. The user signs in chat in their own words ("좋아", "계획 승인"); you record it with `dv-hmi-cli log sign …`, which keeps the user's last message word for word (`dv-hmi-cli -h`). Nothing else is asked of the user.
- **Write it as results the user can check by running the app**: what is on each screen (the controls and the information shown), what pressing something does, how the device moves, what is kept. Whatever the user can tell apart by running the app is part of the contract once written; how you build it (code structure, names, files, threads) is yours, as long as those results do not change. When an implementation choice changes a result (an alarm timer that follows the PC clock: changing the clock stops the alarm), that is a contract matter. Positions, sizes and wording on screens are not written in the contract (the drafts show them): you may adjust them, and you report notable changes.
- **Everything written in `spec.md` and in a signed report or plan is in the contract** — including how a turn is built (split between subagents or not: it changes the user's time and cost).
- **Changing a signed thing is the user's decision.** Write the change first — the new version of the work (`log revise`), the edited `spec.md`, the new report — then show it in chat with what changes and the real reason you have at that moment, and stop that part until the user answers (keep working on what does not depend on it; collect several proposals into one message). On yes, sign it with their answer (`log sign …`); on no, take it back (`log revise <n> --withdraw`; for `spec.md`, restore the signed version in `.dv-hmi/spec/<version>.md`). When the user asked for the change themselves, their request is the signature: write it, then `log sign … --asked`. Every change is a new version; earlier versions and signatures stay. Taking work out is a change too (`log revise <n> --drop`).
- **Sign only a plain yes.** An approval with a condition ("좋아, 근데 버튼 색은 바꿔줘") is not a signature yet: make the change, show it, and sign the next answer.
- **A chosen option is a signature.** When you offer options, write each one as the complete sentence that will go into the contract ("1. 휴일에는 켜기도 끄기도 하지 않음"), never "as recommended". When the user picks one, put that sentence in word for word, sign with `--asked`, and quote the line in your next message ("이렇게 적었습니다: …"). If you reworded it, decided further details, or changed anything else with it, show it and sign a plain yes instead.
- **If signed documents disagree** (one section says "hold to stop", another "stops at one press"), do not pick one: ask.
- **Reasons are the real ones.** In chat and in reports, write the reason you actually had at that moment — never one found afterwards to fit.
- **When the user signs, open questions come first.** If the user approves while a `확인 필요` line or a 꼭 볼 것 item has no answer from them, ask it before you record the signature; what they cannot answer becomes a `가정:` line (an assumption to check on site), shown to them in the version they sign. After the answer, write it in, show the result, and wait for a new plain yes — `--asked` never signs an answer you wrote in afterwards. `log sign` prints the words it recorded: if they were not a plain yes, the signature is wrong — tell the user and ask again.

## Working rules of this mode

- The stage is in `dv-hmi-cli status` (`stage …`, `spec …`, `work …`, `open <n> …`). In a new session, read it first and continue from there.
- **Continuing after a break** (the user may not read code; they just say "continue"): recover the state yourself from the files — `status`, the open work's notes and what it waited for, `spec.md`, the stage report, the code, uncommitted changes and unmerged worktrees (the session-start hook names them). Then tell the user in plain words where it stopped and what you will do next ("The alarm screen was in progress: saving alarms is done, the reset button is next"), and **repeat what was decided before the break** ("Before the break you decided …") so they can correct it.
- **What is decided in chat goes into a file at once**: what the program does → `spec.md` (a change to signed requirements is signed again); progress and decisions while work is open → `dv-hmi-cli log note [<n>] "<text>"`. Chat alone is lost when the session ends.
- **Commit** (when the project folder is its own git repository — setup made it) after every signature and after every `log done` that succeeds, and before you split a turn between subagents. The user allowed this in advance for this project; do not push. One line in the user's words per commit. Changing files with git (commit, merge, revert) is allowed; other shell commands never change the app's files.
- Move to the next stage with `dv-hmi-cli stage <next>`. It checks the signatures (conditions: `dv-hmi-cli -h`). There is no going back to an earlier stage: a large change is a new version of the requirements and new planned work, both signed.
- The user talks to you in chat. The dashboard is for watching: the user gives content only in its 참고 파일 tab (files saved into the project's `inputs/` folder), and decisions only in chat; its 실행/중지 buttons only run or stop the app.
- From the stage `requests` on, the project works in assistant mode: `assistant.md` next to this file.

## 1. Interview

**Goal**: know what program to build, mostly by drafting its screens with the user. The result is the requirements file `.dv-hmi/spec.md`, signed by the user. There is no other design document.

**Ends** when the screens are drafted, no question is left, and the user signs the requirements (`log sign spec`). Then run `dv-hmi-cli stage plan`.

**The user**: describes the program, uploads what they have, looks at the drafts and asks for changes, answers the few questions left, approves.

### Opening

Say this in the user's language (adapt the wording, keep the content):

> Now let's talk about what program to build.
> If you have reference files (existing screens, address tables, record formats, specifications …), please upload them in the dashboard's 참고 파일 tab. If not, that's fine.
> Then please describe briefly what the program is for.

- Files uploaded (`inputs/`): read them. Say what you understood ("I understood it like this: …") instead of asking what they already answer. Korean CSV files are often EUC-KR (CP949).
- No files: say first that without reference material the interview may take a little longer, then start from the core of each large part.

### Devices first

Always ask these, one at a time; then ask whatever else you need from the answers and the files. If the user starts with a screen, draft it first (below) and come back to these right after:

1. Which devices does the HMI connect to (PLC, inverter, temperature controller …, not the whole machine)? If several, the user lists them; if it is unclear whether there are more, ask.
2. The protocol. Check in the Devinno.Communication docs whether the library supports it; if not, ask whether there is a protocol document.
3. Whether there is a memory map (address table). **Never assume there is one.** If there is, fill `.dv-hmi/design/devices.json` from it (format: knowledge file `modbus.md`); the user sees it on the dashboard's 통신 tab. If not, write that down and leave communication as not done yet.

### Screens — the main part

When the user describes a screen ("main screen with temperature 1, 2, humidity and flow, and a piping diagram"), draft it **right away** as `.dvml` and let the user see it on the dashboard (화면 tab; it is redrawn about 2 s after the file changes).

- Drafts are the real pages: `src/<name>/Pages/<Name>Page.dvml`, named as the page will be named. Implementation finishes these same files.
- Write values directly in the markup (`value="82.4"`, `text="Running"`). No bindings, view model or `.cs` yet. During the interview only `Pages/*.dvml` and the memory map can be edited (the hook refuses the rest).
- Data the markup cannot hold — the points of a trend, sparkline or chart — goes into `.dv-hmi/screens/<Page>.values.json` as example data (format: `dv-hmi-cli -h`, "values JSON"; bind the control to that name). Only that data: everything else stays written in the markup. An empty graph does not show what the screen will look like.
- Bars shared by every page (top bar with the menu, status or alarm bar, side bar) are their own `Pages/<Name>.dvml`. Start each with `<!-- area: top -->` (`bottom`, `left`, `right`): without the app code that places it, this is how the dashboard draws every page inside its bars, as the app will show it. Implementation places them in code (`app.Top = new TopBar()` …).
- The window is 1280 x 800 unless the screens need another size (a large diagram on a Full HD monitor). Then write `<!-- window: 1920x1080 -->` in a page (`dv-hmi-cli -h`, render, "window"): the dashboard draws every draft at that size. Implementation sets the same size in the app code (the dashboard warns while they differ).
- Tables and lists get example rows (an alarm history with a few alarms, a file list with a few files), consistent with the counts shown elsewhere on the same screen — an empty table does not show what the screen will look like.
- Before the first draft, tell the user it takes a little longer (you read the library docs and the knowledge first). Read `screens.md` and the library docs the working rules point to.
- Before you show a draft, open its picture (`.dv-hmi/screens/<Page>.png`) and read it as the user will: lines that overlap or seem to join the wrong parts, flow that does not match the situation shown (flow behind a closed valve), anything cut off or crowded. Fix what you find first; a library warning in the picture's list is a problem to read on the picture, not only a size to change.
- Say which parts you invented (e.g. the piping layout) so the user can correct them.
- When you cannot draw what the user asked for in the form they asked (a diagram they asked for becomes cards), that is not an invented part: say it separately, with the real reason and what is possible later — see `screens.md`, "When the controls cannot do it".
- Re-read section 12 of the requirements (design concept and intent) before each draft.
- What the user adds while talking about a screen ("red above 90 degrees", "this button must be held") fills other sections: write it down there at once.
- Watch for the device behaviour behind buttons (what start, stop, reset do; what is locked when): it is easy to spend the time on colours and layout instead. Ask about it.

### The requirements file

`.dv-hmi/spec.md` has 12 numbered sections (setup wrote them; keep the numbers — the dashboard finds sections by them). Write it **while** the user talks, not at the end.

- Each section: a few lines in the user's words and language. No sub-structure, no tables, no code or file names. It must stay light — it is not a design document.
- Section 3 (screens) says for each screen what is on it and what the user presses there — the controls (buttons, inputs, and how they are pressed: once, held, with a confirmation) and the information (values and states shown). Positions, sizes and wording are not listed: the drafts show them.
- A line starting with `확인 필요:` (`Needs confirmation:` when the user writes English) is something you still need to ask. These lines and the empty sections **are** the question list; there is no other. A line starting with `가정:` (`Assumption:`) is what the user could not confirm — it is signed as an assumption and goes into the test list.
- Empty section = not talked about yet. When the user says it is not needed, write `없음` (`None`).
- Section 10 holds functions only this program has (production count, a special process …). Section 12 holds what is not a function: design concept and intent, special wishes, delivery date, site information.
- The large parts (screens, alarms, trends, logging, settings, recipes, schedule) are a **checklist** to see that nothing is missing, not questions to ask one by one.

### Questions

- Ask only what the screens and files did not answer. One question per message.
- **Safety first** (section 11: what moves or stops the device, interlocks, behaviour on communication loss), then what changes the result most.
- Plain words about the user's world; offer a recommendation when the user may not know (`asking-the-user.md`).

### Finishing

When nothing is left, say (in the user's language): "I have no more questions. Please check the requirements on the dashboard and approve them; then I'll plan the work and start building." If the user approves while a question is still open, ask it first ("The contract" above). On approval run `dv-hmi-cli log sign spec`, then `dv-hmi-cli stage plan`.

## 2. Plan

**Goal**: picture how the program will be built and bring out the decisions the interview could not reach (they appear only when you think about *how*: does a setting go to the device at once, are old alarms ever deleted, what happens to a command while the link is down …). Decisions about what the program does go into `spec.md` (a new version, signed with the plan). Then split the requirements into work, each listed with `dv-hmi-cli log plan "<work>" [--test "<check>" …] [--after <n>,…] [--area "<paths>"]`.

**Ends** when every requirement has its work, the plan report is written, and the user signs the plan (`log sign plan`, `log sign report plan`, and `log sign spec` if the requirements changed). Then run `dv-hmi-cli stage implement`.

**The user**: reads the report (dashboard: 계획 → 보고·승인) and the work list (작업 내역), answers or corrects in chat, approves.

- **Size**: the smallest piece that has its own check (it builds and its tests run). Fold preparation into the work that needs it. Split only where a piece could be accepted or rejected on its own. Each ends with something you can check yourself. You decide how fine.
- **Coverage**: when the list is done, go through the requirements line by line and check that each line (except non-functional section 12) belongs to some work.
- **Checks** (`--test`): what the work must prove, in the user's words, input → expected ("챔버 온도 원시값 1652 → 165.2 ℃", "응축탱크 온도가 기준을 넘으면 알람", "통신이 끊기면 알람을 판정하지 않음"). **Every logic and safety requirement is a check** — what the user decided about start, stop, locks, communication loss, what is kept or deleted. They fix the expected values before the code, from the requirements and the memory map. Screens, text and layout get none. No test code in the plan. When the work is closed, every check must be paired with a passing unit test or a reason (implement).
- **Order** (`--after`, `--area`): the simulator comes first (all unit tests talk to it). Then what other work depends on (communication and device model, usually). Work that changes the same files is not in the same turn; work that needs another's result comes after it. `--area` lists the files and folders the work changes (paths from the project folder, comma-separated) — including its test file (`tests/<name>.Tests/<Feature>Tests.cs`) and, for a screen, its text files (`src/<name>/Strings/<Page>.en.json`, `.ko.json`) and its render values (`.dv-hmi/screens/<Page>.values.json`): it shows which work can be done at the same time, and a subagent may edit only inside its work's area. Every work in a split turn has an area.
- **No memory map yet** (the interview left communication as not done): the simulator and the communication work wait for it — plan them, say in the plan report that they start when the address table arrives, and test the logic directly (`simulator-and-checks.md`) until then. When it arrives (`inputs/`): read it, write what it changes into `spec.md` section 2 and the communication work's checks (`log revise`), show both and sign them; then open the work and fill `.dv-hmi/design/devices.json` as part of it.
- **A change to the drafts or the memory map in this stage** ("move that button"): it changes the requirements — write it in `spec.md` first (the drafts can then be edited), show both, and it is signed with the plan.
- **How each turn is built** is decided here and written in the plan report, concretely ("차례 3: 화면 4개를 서브에이전트 넷이 동시에", "차례 1: 혼자 차례로"); once signed it is part of the contract. A turn with two or more works whose `--area`s do not overlap is split between subagents, one work each — except a turn of small works only (smaller than writing a brief and merging), which you build yourself, and any turn when the project folder is not its own git repository (`status` prints `git none`, or `git rev-parse --show-toplevel` is another folder): then work is not split. Write the reason for a turn you do not split.
- Work item text is in the user's language and words, what the user will see or what the device does.

**The plan report** `.dv-hmi/report/plan.md` (the user's language, plain words, no code terms — see "Stage reports" below):
- `## 한 일`: the work in a few lines, and the order in the user's terms, with how each turn is built ("After the first turn you can run the program with the simulator", "Turn 3: the four screens are built at the same time — faster, but more usage").
- `## 꼭 볼 것`, `## 더한 것`, `## 바꾼 것`, `## 그 밖에 정한 것`: the decisions you made while picturing the build.

Write the decisions about what the program does into `spec.md` before you ask. Then tell the user in chat, briefly: the work is listed, the report is on the dashboard, what changed in the requirements, and the 꼭 볼 것 items in one line each; ask them to approve or correct. Changes they ask for are made here (the list, the report, `spec.md`), shown, and approved again. On a plain yes: `log sign plan`, `log sign report plan`, and `log sign spec` if it changed.

**Changing how a turn is built later**: edit the plan report (the turn's line), show it, and sign it again (`log sign report plan`) — before you open that turn.

## 3. Implement

**Goal**: build every signed planned work.

**Ends** when nothing is planned or open, the implementation report is written, and the user signs it (`log sign report implement`, and `log sign spec` for what it added or changed). Then run `dv-hmi-cli stage verify`.

**The user**: watches the progress on the dashboard; reads the report at the end and approves in chat.

- Open each signed work with `dv-hmi-cli log start <number>` (refused while it is not signed or its `--after` work is not done) and close it with `log done <n> "<one thing you did>" …` (items in the user's words and language, what the user will see or what the device does). A work is done when it closes: its paired unit tests pass. `log done` refuses at the first failed build or test; never weaken a test to pass (`simulator-and-checks.md`).
- **Checks and tests**: the plan's checks are the tests to write. Close with `log done <n> "<item>" … --check <k> "<test DisplayName>"` for every check (it must run and pass now), or `--skip <k> "<why>"` when a check cannot be a unit test — the skip and its reason go into the report's 꼭 볼 것. Here `log done` runs only the paired tests (the full run comes in verify), so it stays fast however much work there is. Removing or renaming a test is allowed when it merged into another; the dashboard shows it. Unit test rules: `simulator-and-checks.md`.
- **Decisions while building** — there will be new ones; do not stop for each, but sort them:
  - what the requirements do not say and the user would see (an error message, a confirmation dialog, how a button is pressed): decide, build, and put it in the report's `## 더한 것`;
  - **except** anything that moves or stops the device or gives the operator a new refusal, lock or condition: propose it first and build it after the user answers;
  - anything that differs from what was signed: propose it first ("The contract");
  - everything else (code structure, names): yours; notable ones go into `## 그 밖에 정한 것`.
  Note each on the open work (`log note`).
- **Fixes**: a failing test or a defect in what was built is a fix — `log fix "<what>" --for <n>`; `log done` runs every unit test for it. A fix makes the signed promise hold; when making it hold would change what was signed, it is a proposal, not a fix.
- **Changing the plan**: when a work turns out different from the plan (bigger, smaller, overlapping, a check's expected value is wrong, a turn would be better built another way), write the new version (`log revise`) before you open that turn, show it, and sign it with the user's yes (or `--withdraw` on no) — "The contract". New work the user asks for: `log plan`, then `log sign <n> --asked`.
- **Commit**: as the working rules of this mode say (above); `log done` prints `note uncommitted: …` when you forgot.
- **Splitting between subagents** (the turns the plan split; you lead):
  1. Commit. Open the turn's works (`log start <n>` each — several can be open at once in this stage) and mark each as waiting for its subagent: `log wait <n> "<subagent>"` (the edit hook lets a worktree edit only inside the area of work that waits for a subagent).
  2. Give each work to a subagent in its own worktree, with a short brief: the work text, its checks, its `--area`, a finished page to follow when there is one (built earlier in this project), and the knowledge files and library doc sections for this work (the working rules' index). The working rules' section "Working as a subagent" tells it the rest — do not repeat it. Context several subagents need (the managers' methods, screen rules) can go into one file the briefs point to.
  3. While they work, do not edit the files in their areas. Before ending a turn while they work, write `log wait` again (it holds for one turn).
  4. When a subagent reports: merge its worktree, add what it listed for files outside its area, then `log done <n> …` yourself (it builds and runs that work's tests) and commit.
  - After merging, a build is enough; the full test run is in verify.
- **Waiting**: before you end a turn with work open — waiting for a subagent, or for the user's answer to a proposal or question — write what you wait for: `log wait [<n>] "<the question you sent, word for word | the subagent>"`. It holds for this turn; the stop hook asks again after the user's next message.
- The screen drafts become the real pages: keep their look. Move the literal values into bindings and the literal text into text keys (`Strings/<Page>.en.json`, `.ko.json`), and put the same values into `.dv-hmi/screens/<Page>.values.json` so the picture stays comparable with the draft. Notable changes of positions or wording go into the report's `## 그 밖에 정한 것`.
- **A change to `spec.md` here** (a proposal the user signed, a `가정:` line added, a 더한 것) also checks the planned work it touches: a done work whose text or checks no longer match gets a new version (`log revise`, signed with the change) and is built again.
- Planned work is never dropped by you: write `log revise <n> --drop "<why>"`, show it, and `log sign <n>` with the user's yes.

**The implementation report** `.dv-hmi/report/implement.md`: `## 한 일` (a few lines), `## 꼭 볼 것`, `## 더한 것`, `## 바꾼 것` (each change with the user's decision), `## 그 밖에 정한 것` — including skipped checks. Put 더한 것 and 바꾼 것 into `spec.md` too, before you ask. Tell the user in chat (the 꼭 볼 것 in one line each, and what changed in the requirements) and ask them to approve or correct. On a plain yes: `log sign report implement`, and `log sign spec` if it changed.

## 4. Verify

**Goal**: your own final inspection of the whole program — the full unit test run, then a code review — and one list for the user of what to test by running it. Running the app — on the device or the simulator — is the user's, always: a wrong command moves equipment and can damage it or hurt someone, so it is tested by a person who sees the machine and can stop it at once. That testing comes after verify, as requests.

**Ends** when the review's problems are closed, the full run passes, the test list and the report are written, and the user signs the report. Then run `dv-hmi-cli stage requests`.

**The user**: follows the review on the dashboard (검증 → 보고·승인), approves; afterwards runs the app and tests on the device, and reports problems or new wishes in chat (requests).

1. **Full test run**: `dv-hmi-cli test` (build + every unit test, against the simulator). A failure is a fix (`log fix "<what is wrong, in the user's words>" --for <n>`).
2. **Code review by a subagent** that did not write the code: give it the source and the requirements only, and ask for bugs the tests do not cover — error handling, communication loss and reconnecting, shutdown, threads and timing, resources, values out of range — and for every place where the program differs from the signed requirements and is not in a report's `## 바꾼 것`.
3. **Open a fix per real problem** (`log fix "<problem, plain words>" --for <n>`), then write `.dv-hmi/report/verify.md` (the dashboard tab lights up): `## 리뷰` with one line per real problem, in plain words about what the user would run into, not code ("통신이 끊긴 채 프로그램을 끄면 멈출 수 있었어요"), ending with its fix number: `- … (#14)`. Mere improvements ("this could be cleaner") are not fixed and not listed.
4. **Fix**:
   - fix them all first, run `dv-hmi-cli test` once, then close each fix with `log done` — it reuses that passing run while nothing changed since;
   - a fix that makes the program do what was signed: fix it and close it as usual;
   - a fix that would change what was signed — or give the operator a new refusal, lock or condition — is a proposal: collect these after the review and propose them in one message; write the change into `spec.md` first and show it; on yes sign it (`log sign spec`) and fix accordingly; on no, restore the signed `spec.md` and close the fix with `log done <n> --declined` (the user's words are recorded) or fix it within what was signed;
   - a finding that is not a defect: `log done <n> --not-a-defect "<the signed version and section, or the test that shows no problem>"`; one that is the same as another fix, or is covered by new planned work the user decided: `log done <n> --merged <fix or work>`;
   - new planned work the user decides here sends the project back to implement when signed; build it, add it to the implementation report (`## 한 일`, sign it again), then verify again from step 1 — the review, the test list and the verify report are redone.
5. `dv-hmi-cli test` again after the last fix (`stage requests` checks that it passed after the last closed work).
6. **Test list for the user** `.dv-hmi/verify.md`: what to test by running, from the signed requirements, in the user's language, **safety first**, then by screen or function. Each item: what to do and what should happen ("운전 버튼을 길게 누르면 히터가 켜지고, 통신을 끊으면 운전 버튼이 잠긴다"). Include what the simulator could not show (assumptions — the `가정:` lines —, assumed device behaviour, skipped checks). Headings (`##`) and list items (`-`) only.
7. Finish the report (`## 한 일`, `## 꼭 볼 것`, `## 더한 것`, `## 바꾼 것`, `## 그 밖에 정한 것`) and put its 더한 것 and 바꾼 것 into `spec.md`; tell the user in chat: the review result in a line or two, the 꼭 볼 것, where the test list is (검증 → 시험 목록), and that from now on problems and wishes are handled one request at a time. On a plain yes: `log sign report verify` (and `log sign spec` if it changed), then `dv-hmi-cli stage requests`.

Never start the app yourself.

## Stage reports

At the end of plan, implement and verify you write `.dv-hmi/report/<stage>.md`; the user approves it in chat and you sign it (`log sign report <stage>`). The dashboard shows it in the stage's 보고·승인 tab. A report is not itself the contract: its `## 더한 것` and `## 바꾼 것` go into `spec.md` when signed. In the user's language and plain words, no code or file names:

- `## 한 일` — what was done, a few lines.
- `## 꼭 볼 것` — what you decided yourself that meets one of the **three criteria**, one line each: what you decided and why the user should look.
  1. **Safety**: what state the device is left in when someone makes a mistake or something goes wrong — it becomes dangerous, cannot easily be undone, or changes without anyone noticing. Writing to the device is not enough by itself (a setting entered in an input box and confirmed is not one).
  2. **Against what the user said**, or it would make the user reverse a decision.
  3. **Hard for you to judge** (the manual does not say, the answer could go either way).
- `## 더한 것` — what you added that the requirements did not say and the user will see (one line each).
- `## 바꾼 것` — what now differs from the signed requirements, each with the user's decision.
- `## 그 밖에 정한 것` — every other decision the user may want to know (one line each).
- Verify only: `## 리뷰` (above).

Write every decision down; the user may not read the whole report, so the criteria decide what stands out, not what is hidden.
