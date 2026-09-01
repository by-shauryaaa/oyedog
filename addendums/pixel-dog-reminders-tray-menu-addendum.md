# Pixel Dog Reminders — Spec Addendum
### Tray Icon: Today's Schedule Menu

---

## 1. Core Idea

Clicking the tray icon currently does nothing beyond whatever default tray behavior exists, and the existing right-click menu (Open Oye Dog / "Dogu, Walk In!" / Pause Reminders / Exit Oye Dog) is separate from that. **Collapse these into one single unified menu**, triggered by **either left-click or right-click** on the tray icon — no more split behavior between the two.

**No custom pixel-art styling needed for this menu** — a standard OS-native context menu/popup is fine and expected here, consistent with how other tray apps (Notion Calendar, etc.) present this kind of glanceable info.

---

## 2. Menu Contents

In chronological order, for **today only**:

- **Today's classes** (from the Timetable tab's data) — each shown as time + subject name (+ room, if set), same info already used elsewhere in the app.
  - **The currently in-progress class (if any) is shown in bold**, so it's immediately obvious what's happening right now versus what's still upcoming.
- **Today's sports events** (from the Matches tab's data), if any Barca match or F1 session falls on today's date — shown in the same chronological list alongside classes, not a separate section, so the whole day reads as one simple timeline.
- If there is genuinely nothing scheduled today (no classes, no sports events), show a simple empty-state line (e.g. "Nothing scheduled today") rather than an empty-looking blank menu.

---

## 3. Standard Menu Items (Below the Schedule List)

Carried over from the previous separate right-click menu, now living in this single unified menu, separated from the schedule list by a divider:

- **Open Oye Dog** — opens/focuses the main app window.
- **"Dogu, Walk In!"** — the existing test action that manually triggers the walk-in greeting animation on demand, unchanged from its current behavior.
- **Pause Reminders** — unchanged from its current behavior.
- **Exit Oye Dog** — fully exits the app (stops the tray-resident background process).

---

## 4. Behavior Notes

- This menu is **read-only** — no interaction with individual schedule items (no editing, no dismissing) from the tray menu itself; it's purely a glanceable summary. Any actual management still happens in the full app window via the Reminders/Timetable/Matches tabs.
- The list should reflect the **same underlying data** already driving the Timetable and Matches tabs — no separate data source or duplicated logic, just a filtered "today only" view of what's already being tracked.
- Menu should refresh its contents each time it's opened (not cached from an earlier point in the day), so a class that just started shows as bold immediately rather than only updating on next app launch.

---

## 5. Open Items — Left to Antigravity's Judgment

- Exact ordering/grouping if a class and a sports event happen to be at the exact same time (edge case, low priority).
- Whether "Open Oye Dog" specifically navigates to the Home screen or just restores whatever tab was last open.
