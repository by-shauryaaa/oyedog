# Pixel Dog Reminders — Spec Addendum
### New Tab: Timetable (Class Schedule + Pre-Class Reminders)

Tab bar becomes: **Reminders | Matches | Timetable | Home | Settings** (ordering flexible — Timetable can sit wherever feels natural in the flow).

---

## 1. Core Idea

A weekly, recurring class schedule (not date-bound like the Matches tab) that fires a **small, quick "mini reminder"** shortly before each class starts — lighter-weight than the standard habit-reminder popup, since this is a heads-up, not a task to complete.

---

## 2. Data Model — Subjects & Slots

Data is organized **by subject**, not by individual one-off class entries. A subject is created once, and can have **multiple recurring weekly slots** attached to it.

- **Subject** has:
  - **Name** (e.g. "Data Structures")
  - **Class duration** (in minutes — prefilled from the global default duration set in Settings, editable per subject)
  - **Room** (optional)
  - **Color** — automatically assigned a unique color when the subject is created (no manual color picker needed; just needs to stay consistent and visually distinct from other subjects)
- **Slot** (belongs to a subject) has:
  - **Day of week**
  - **Start time**
  - **End time** — always **derived** from the slot's start time + the parent subject's duration, never entered directly.
- **Adding a subject flow:**
  1. User taps **"+ Add Subject"**.
  2. Enters **name**, **duration** (prefilled with the global default, editable), and optional **room**.
  3. Taps **"+ Add Slot"** — this opens a small picker for **day of week** + **start time**, and creates one recurring weekly slot for that subject.
  4. The "+ Add Slot" action can be repeated to attach **multiple slots to the same subject** (e.g. a lab that recurs Tuesday and Thursday) — all slots inherit the subject's name/duration/room/color.
  5. **Overlap validation happens before the slot is saved.** Using the day + start time + the subject's duration, check the new slot's time range against every existing slot across all subjects on that day. If any overlap is found, block the save and show a warning telling the user to change the time — the slot picker should stay open so they can adjust and retry, rather than losing their entry.
- Slots repeat weekly indefinitely (no semester date range in v1 — see §7).

---

## 3. Timetable Tab UI — Two Views

A view switcher at the top of the tab toggles between:

### 3a. Week Grid view
- A **fixed 5×10 grid**: columns = **Monday–Friday**, rows = **hourly slots from 8 AM to 6 PM**.
- Each subject's slots render as colored blocks in their corresponding day column at their start-time row, sized/spanning according to duration (e.g. a 90-minute class visually spans one and a half row-heights).
- Block shows subject name (and room, if space allows) in the assigned subject color.
- Tapping a block opens that **subject's** edit view (not just the single slot — editing surfaces the subject with all its attached slots listed, so multiple slots can be managed from one place).
- **Scrollable** — if the grid doesn't fully fit the available window height/width, it scrolls rather than compressing/cutting off (consistent with the general anti-truncation UI principle already established for the rest of the app).

### 3b. Schedule view
- A simple **upcoming list**, showing the next classes in chronological order (today's remaining classes, then rolling into the next school day), each row showing subject name, color swatch, day/time, and room if set.
- No grid — just a clean **scrollable** list, useful for a quick "what's next" glance.

### Shared
- **"+ Add Subject"** button accessible from either view.
- **Delete** option available from the subject edit view (removes the subject and all its slots together — deleting a single slot from a multi-slot subject should also be possible without deleting the whole subject).

---

## 4. Class Reminder — "Flag" Style (Distinct From Other Reminder Types)

This is a **new, separate visual pattern** from both the standard habit-reminder popup and the startup walk-in greeting — no dog character, no speech bubble.

- **Visual:** a **flag/banner graphic flies across the screen, left to right**, at a fixed vertical position, then exits off the right edge.
- **Content on the flag:** two pieces of text rendered directly on the flag graphic —
  - The **countdown**, e.g. *"in 5 min"*
  - The **subject name**, e.g. *"DBS Lab"*
- **No buttons, no interaction required** — this is a fly-by notification, not something the user responds to. It simply animates across and disappears on its own once it exits the screen (no separate auto-dismiss timer needed since the animation itself is the exit).
- **Flight duration: ~5 seconds** total, left edge to right edge exit.
- **Trigger:** fires once per scheduled slot, **X minutes before start time**, where X = the global lead time set in Settings (per-slot override not required given the simplified flag format — global default only, unless a future revision wants per-subject overrides).
- Styling should stay visually consistent with the app's overall pixel-art aesthetic (blocky flag shape, pixel font for the text — same "Press Start 2P" family used elsewhere) even though it doesn't use the dog character.

---

## 5. Settings Additions

- **"Timetable Reminders"** — on/off toggle button, same pattern as the Matches tab's master toggle.
- **Default class duration** — a duration value (e.g. dropdown or numeric field, minutes) used to prefill the duration field whenever a new subject is created. Editable per-subject afterward regardless of this default.
- **Lead time before class** — how many minutes before a slot's start time the flag reminder fires (e.g. dropdown: 5 / 10 / 15 / 30 minutes).

---

## 6. Explicitly Deferred (Out of Scope for v1)

- Semester/term date ranges — slots repeat every week indefinitely once added; pausing during a break just means toggling "Timetable Reminders" off manually.
- Attendance tracking / "did you go to class" logging — consistent with the rest of the app having no history tracking in v1.
- Syncing with any external calendar or university timetable system — manual entry only.
- Per-subject lead-time overrides — global lead time only for v1, given the simplified flag-notification format.
- Conflict handling for overlapping slots in the same time block — grid can visually overlap if the user schedules it that way; no blocking validation in v1.

---

## 7. Build for Extensibility (Important — Read Before Implementing)

This Timetable tab is explicitly likely to grow into a **standalone, full scheduling app** later. Build the data model and core logic in a way that doesn't need a rewrite when that happens:

- **Keep the Subject/Slot data model decoupled from the specific 8am–6pm, Mon–Fri grid.** The grid is a *view/rendering constraint*, not a data constraint — a slot should just be stored as (day, start time, duration), with no assumption baked into the data layer that days are limited to Mon–Fri or hours to 8–6. That range restriction should live purely in the Week Grid view's rendering logic, so loosening it later (e.g. adding Saturday, or extending to 7am–9pm) is a display change, not a data migration.
- **Keep the reminder-firing logic generic** (subject name + time + lead-time → fire a flag) rather than hardcoding "class reminder" assumptions throughout — so if this ever expands beyond just classes (e.g. any recurring weekly event type), the same underlying trigger engine can be reused rather than duplicated.
- **Isolate the flag-notification renderer** as its own self-contained component (separate from the dog-based popup system) — since it may end up reused for other notification types later, it shouldn't be tightly coupled to timetable-specific logic beyond the text it receives.
- **Subject color assignment** should be a small standalone utility (e.g. deterministic hash-to-palette or next-available-color-in-sequence) rather than inlined into the subject-creation flow, so it's easy to reuse if other entity types ever need auto-coloring too.
- None of this needs to be over-engineered for v1 — just avoid hardcoding grid bounds or "class"-specific naming deep into shared logic where it'd need to be untangled later.

---

## 8. Open Items to Decide Before Build

- Exact flag animation vertical screen position (top/middle/bottom of screen) — flight duration (~5s) and no-interaction behavior are now settled per §4, but placement still needs a decision.
- Visual treatment for the "conflict" warning shown during slot creation (modal/toast/inline message) — the *behavior* (block save, keep picker open) is settled per §2, just needs a visual form.
