# Pixel Dog Reminders — Spec Addendum
### Navigation Overhaul: Sidebar Replaces Top Tab Bar

---

## 1. Core Idea

Replace the horizontal top tab row with a **vertical sidebar** on the left edge of the window, styled like a retro RPG pause-menu list rather than a standard modern app sidebar (blocky bordered rows, thick-border/inverted-color highlight on the active item — consistent with the app's existing pixel aesthetic).

The dog no longer needs its own tab — it becomes a **permanent mini-widget pinned at the bottom of the sidebar**, and tapping it is how the user reaches the Home screen (rather than Home being a peer tab alongside the others).

---

## 2. Sidebar Structure

**Top-to-bottom layout:**

1. **Collapse/expand toggle button**, pinned at the very top of the sidebar — a small button (e.g. a chevron or hamburger-style pixel icon) that collapses the sidebar down to icon-only width, and expands it back to icon+label width when tapped again. This should persist as a real, built-in v1 feature (not deferred), since it directly helps reclaim space once the header strip below is removed and content area becomes more prominent.
   - **Expanded width:** comfortable enough to give the dog sprite room to breathe (it's not just icon+label text, there's a full mini dog widget at the bottom) — but not so wide that it competes with the Timetable grid's need for horizontal space. Antigravity should pick a specific pixel width that balances these two constraints at implementation time.
2. **Nav items** (in this order): **Reminders → Matches → Timetable → Settings**
   - Each rendered as a blocky bordered row, icon + label, matching the existing tab styling (just rotated from horizontal to vertical).
   - Active item gets the same visual treatment currently used for the selected top tab (solid highlight/inverted color), just applied to a vertical row instead of a horizontal one.
   - When collapsed, rows shrink to icon-only (label hidden), while active-state highlighting still applies to whichever icon is selected.
3. **A visual divider/gap** separating the nav list from the widget below it, so it doesn't read as a 5th nav item.
4. **Dog + Clock mini-widget**, pinned at the bottom of the sidebar **when expanded**:
   - A **small idle-looping dog sprite** (scaled down from the Home page's full-size version, but still animated — not a static icon; the personality/motion is part of what makes this a "companion" rather than a nav icon).
   - The **live clock**, shown small beneath or beside the mini sprite.
   - The entire widget (sprite + clock together) is **one tappable region** that navigates to the Home screen.
   - When the Home screen is the active view, this widget should get its own highlighted/selected treatment (consistent with how nav items show active state), so it's clear "you are here" even though it's visually distinct from the list above it.
5. **Collapsed state — dog widget is replaced, not shrunk:** when the sidebar is collapsed, the dog sprite and clock are **hidden entirely** (not shown in miniature — the collapsed sidebar is icon-only for nav, full stop). In their place, show the **"System Tray Resident" status chip, rotated to a vertical orientation** running down that same bottom portion of the sidebar. This relocates the status indicator (previously slated for the Settings tab) to double as the collapsed sidebar's bottom element instead.
   - Note: since Home is no longer reachable via the dog widget while collapsed, Antigravity should decide how the Home screen stays reachable in the collapsed state (e.g. adding it back as a small icon in the nav list only when collapsed, or another approach) — left open per item 3 below.

---

## 3. Top Header Bar — Removed

- The existing brown header strip (Oye Dog logo, tagline, "System Tray Resident" status pill) is being **removed entirely**. That vertical space is reclaimed and given to tab content instead.
- **Branding relocation:** the app name/logo shouldn't just disappear — fold a small version of it into the top of the sidebar itself (above or alongside the collapse toggle), so there's still a clear "this is Oye Dog" identifier somewhere in the UI, just not as a dedicated full-width strip anymore.
- **Tray status relocation:** the "System Tray Resident" indicator moves into the sidebar itself — specifically, it appears as the vertical chip shown in the sidebar's collapsed state (per §2, item 5), rather than persisting as a full-width strip. When the sidebar is expanded, Antigravity can decide whether this chip still shows somewhere or is only present while collapsed.
- Net effect: content area starts right at the top of the window (minus whatever minimal space the relocated branding takes in the sidebar), maximizing room for Reminders/Matches/Timetable content especially the Timetable grid view.

---

## 3. Home Screen — Now a Dedicated Destination, Not a Tab

- All existing Home page content stays as-is (greeting text, live clock, "tap me to walk across your screen" prompt, full-size dog sprite, Feed Dog / Pet-Play / Walk Screen buttons, tray-resident status footer).
- The only change is **how it's reached** — via the sidebar's dog+clock widget instead of a "Home" tab in the top row.
- The **top header bar** (Oye Dog logo, tagline, tray status pill) stays exactly where it is, unaffected — only the tab row beneath it is being replaced by the sidebar.

---

## 4. Why This Grouping (Rationale, Not Just Layout)

- Reminders / Matches / Timetable / Settings are all **content/configuration screens** — places you go to look at or manage something.
- Home is fundamentally different — it's the **"hang out with the companion"** screen, not a content list. Giving it a distinct entry point (the living, animated dog widget) rather than lumping it in as just another equally-weighted tab reinforces that distinction and gives the dog a permanent, always-visible presence in the UI even while browsing other tabs — which fits the "companion" framing better than the current setup, where the dog disappears entirely unless you're on the Home tab.

---

## 5. Scalability Note

Per the extensibility principle already established for the Timetable tab: a vertical sidebar list scales far better than a horizontal tab bar as more sections get added later (it just grows downward with more rows, rather than competing for shrinking horizontal space). This is a good structural move independent of the visual/vibe reasoning, since more tabs are already anticipated.

---

## 6. Open Items — Left to Antigravity's Judgment

The following details are intentionally left unspecified; implement using best judgment consistent with the rest of the app's style:

- Exact expanded/collapsed pixel widths for the sidebar.
- Whether the collapse state persists between app sessions or resets on launch.
- How the Home screen stays reachable while the sidebar is collapsed (since the dog widget that normally opens it is hidden in that state — see §2 item 5).
- Mini dog sprite size (expanded state) — should read clearly as an animated dog, not shrunk past legibility.
- Exact treatment for the relocated logo/branding within the sidebar (icon only vs. small wordmark), and whether it appears in both collapsed and expanded states or only expanded.
