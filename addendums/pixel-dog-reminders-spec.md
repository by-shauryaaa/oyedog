# Pixel Dog Reminders — Windows Desktop App
### Full Product & Implementation Spec (Birthday Gift Build)

---

## 1. Overview

A lightweight native Windows desktop app that delivers daily habit reminders — plus a personal touch of Barcelona match reminders and F1 race reminders — through a pixel-art dog companion instead of generic system toasts. The dog slides onto the screen, says what it wants in a pixel-styled speech bubble, and the user responds with **Snooze** or **Okii**. Built as a personalized birthday gift for a specific friend (Abhishek), with Hindi reminder copy and Barca/F1 fandom baked in.

**Target hardware context:** recipient's laptop runs a Ryzen 7 AI 350 (Zen 5 / RDNA 3.5, modern mid-2025+ silicon) — this is a capable, modern chip, so there's no need to over-optimize for constrained hardware. The "lightweight" requirement below is about good background-app hygiene (fast launch, low idle CPU/RAM, no bloated runtime sitting in the tray all day) rather than working around weak hardware. Any modern native-Windows framework is fine as long as it doesn't idle like a browser tab.

---

## 2. Platform & Framework

- **Native Windows desktop app.** Framework is not locked — choose whichever of the following gives the best lightweight + polish tradeoff:
  - WinUI 3 (Windows App SDK) — modern, fine if unpackaged deployment is used to avoid MSIX/tray friction.
  - WPF — mature, very predictable for custom-shaped/transparent always-on-top windows, low overhead.
  - Tauri (Rust backend + web-rendered UI) — smallest binary/lowest idle footprint, good if a more web-flavored UI stack is preferred for the tab pages.
  - Avoid UWP (legacy/deprecated path).
- **No dependency on the hardware's NPU or GPU acceleration** — nothing in this app (sprite rendering, popups, API polling) needs AI acceleration or heavy graphics; keep the stack simple.
- **Final deliverable: a single installer or standalone `.exe`.** Since this is a gift, first-run polish matters — double-click, install/launch, done. No manual dependency installs, no config files to hand-edit.
- **System tray resident.** Closing the main window does not stop reminders — app keeps running from the tray as long as it's open. Tray icon should have at minimum: Open, Pause reminders, Quit.

---

## 3. Sprite System

- **Character: a dog** (not a cat), rendered **full body** (not just a face/head).
- **Art method:** pixel art built as raw grid data — a 2D array where each cell holds a color code (fur, shading, outline, eyes, nose, etc.), composed from simple primitives (ellipses for head/body, triangles for ears). A render script paints the grid to an image and scales it with **nearest-neighbor scaling** for crisp, non-blurry pixels. This is deliberate: it keeps proportions and style identical across every variant/frame, which is the actual reason to avoid AI image generation for this asset (consistency across frames is the hard part AI gen fails at).
- **Resolution:** target **256×256**, allowed to go higher if full-body detail looks cramped at that size once the dog + limbs + props are all in frame. Resolution is a means to an end (readable full-body detail) not a fixed constraint.
- **Animation:** **5 frames per variant** (up from a bare idle/blink loop) so each pose reads as genuinely animated, not just a two-frame flicker.
- **Variants required, all sharing the same base rig (head size, body proportions, palette) so switching variants never looks jarring:**
  1. **Idle/Default** — neutral sitting dog, used for any reminder without a specific category.
  2. **Water** — dog holding/near a small cup or droplet shape, sipping motion across frames.
  3. **Food** — dog beside a small food bowl, looking → mid-bite across frames.
  4. **Sleep** — drowsy dog with a floating "Z" icon, eyes progressively closing, Z growing.
  5. **Rest/Break** — dog mid-stretch, paws forward → full arched stretch.
  6. **Barca** — dog in Barcelona team colors (blaugrana red/blue), used for match reminders.
  7. **F1** — dog posed with a small racecar/wheel prop, used for race-session reminders.

---

## 4. Reminder Popup — Visual & Interaction Behavior

- **No window chrome.** The popup is a transparent, always-on-top, layered/composited window sized only to its contents — no title bar, no background panel, no visible rectangular container. It should read as a character floating directly over the desktop/whatever app is in focus, not a notification box.
- **Position:** configurable to one of six screen anchors — top-left, top-right, top-center, bottom-left, bottom-right, bottom-center. **Default: bottom-right.**
- **Visual stack (top to bottom):**
  1. **Speech bubble** — pixel/blocky-bordered bubble (not smooth/rounded), reminder text set in a pixel game font (**"Press Start 2P"** or equivalent).
  2. **Dog sprite** — full body, playing its 5-frame loop for the relevant variant.
  3. **Shadow** — soft-edged but visually prominent ellipse beneath the dog's feet. This is what sells the "floating over the desktop" illusion given there's no container — needs enough opacity/blur to stay legible over both light and dark backgrounds/wallpapers/app windows.
  4. **Buttons**, pixel-styled (not default OS buttons), below the sprite:
     - **Snooze** — re-fires after the configured snooze duration.
     - **Okii** — dismisses (the "done" action; intentionally cute microcopy, not "Done"/"Dismiss").
- **Motion:** slides in from the edge nearest its configured screen anchor, plays its idle loop while visible, slides back out on dismiss/snooze.
- **Interaction model:** click-through everywhere except the sprite/bubble/buttons themselves, so it never blocks interaction with whatever's underneath it.

---

## 5. App Structure — Tabs: Reminders | Matches | Settings

### 5.1 Reminders tab
- **Three default reminders, pre-loaded on first launch:**
  1. **Food** — message: *"kuch khaya?"*
  2. **Water** — message: *"paani pi le"*
  3. **Sleep** — message: *"abe soja ab"* — **enabled by default**, fires daily at **11:00 PM**
- **"+ Add custom reminder"** button/entry.
- **A card linking to the Matches tab**, prompting the user to opt into sports reminders (e.g. "Want match day reminders too?" → navigates to Matches tab).
- **Clicking any reminder** (default or custom) opens a config popup/dialog with:
  - Name
  - Time — **supports multiple time slots** for a single reminder (e.g. water reminder firing at several times a day)
  - Message text
  - Character variant picker, **with a live sprite preview** of the selected variant
  - (For interval-based custom reminders, "every X minutes" should remain an available trigger type alongside fixed time-of-day, per the original reminder engine design.)
- **Easter egg:** if the system date is **August 25**, display **"Happy Birthday Abhishek ♥"** at the top of the Reminders tab.

### 5.2 Matches tab
- Card-based schedule feed, showing the next **30 days** of:
  - **Barcelona first-team fixtures** across La Liga, UCL, and any other competitions they're active in.
  - **F1 sessions** — Sprint, Qualifying, and Race — for upcoming race weekends.
- **Master toggle at the top of the page: "Match Reminders"** — **on by default** — controls whether these events actually fire as popups (the schedule cards themselves are always visible/browsable regardless of toggle state).

### 5.3 Settings tab
- Reminder popup position picker (the six anchors from §4) — **default: bottom-right**.
- Snooze duration setting — **default: 5 minutes**.
- Footer text: **"made for abhishek"**

---

## 6. Data Sources — Barca & F1 Schedules

- **Football (Barcelona fixtures):** [Football-Data.org](https://www.football-data.org) — free tier, requires a free API key (self-serve, quick registration). Covers La Liga, Champions League, and other major competitions Barca plays in.
- **F1 (race weekend sessions):** [Jolpica-F1](https://api.jolpi.ca/ergast/f1) — free, open, **no authentication required**, Ergast-schema-compatible successor API, actively maintained through the current season.
- **Fetch strategy:** fetch both schedules **on app launch and once daily thereafter** (a 30-day-window fixture list is small — this is a trivial, infrequent request, not a live-polling concern). Cache the result locally so the Matches tab and reminder engine work fine offline between refreshes, with the cached data simply going stale (harmless for a fixtures-only, no-live-score v1) if there's no internet on a given day.
- **Explicitly out of scope for this build:** live scores, live race timing/results. Only kickoff/session start times are used, purely for firing reminder popups and populating the schedule cards. Live tracking is a clearly deferred future feature.

---

## 7. Explicitly Deferred (Not in This Build)

- Habit history/logging or streak tracking (an earlier idea worth revisiting later, but **not** part of this scope).
- Live match/race scores or in-progress updates.
- Cloud sync / multi-device support.
- Any settings beyond popup position, snooze duration, and the sports-reminder toggle.

---

## 8. Summary of Key Design Decisions (for implementation reference)

| Area | Decision |
|---|---|
| Framework | Any lightweight native Windows stack (WinUI 3 / WPF / Tauri) — no UWP |
| Deployment | Single `.exe` / installer, no manual setup |
| Background behavior | Runs from system tray after window close |
| Character | Full-body pixel dog, 256×256+ grid-based, 5 frames/variant |
| Popup style | Chromeless, transparent, floating sprite + shadow + pixel speech bubble, no box/window look |
| Font | Press Start 2P (or equivalent pixel font) for reminder text |
| Buttons | "Snooze" / "Okii" |
| Default position | Bottom-right (6 positions configurable) |
| Default snooze | 5 minutes |
| Default reminders | Food / Water / Sleep (Sleep on by default, 11:00 PM) |
| Sports data | Football-Data.org (Barca) + Jolpica-F1, fetched on launch + daily, cached locally |
| Sports reminders | Off the schedule cards' visibility, but firing controlled by a default-on toggle |
| Personalization | Hindi reminder copy, "made for abhishek" footer, Aug 25 birthday easter egg |
