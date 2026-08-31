# Pixel Dog Reminders — Spec Addendum
### Home Screen Redesign: Interactive Scene + Sidebar Widget Fixes

---

## 1. Sidebar Dog Widget — Fixes

- **Increase the sidebar dog sprite size** noticeably from its current small scale — it should feel like a real presence in the sidebar, not a tiny icon-adjacent afterthought.
- **Remove the "DOGU" nameplate** text entirely from beneath the sidebar sprite. The sprite + small clock (as already speced) is enough — no name label needed here.

---

## 2. Home Screen Entrance — Walk-to-Center Interaction

- Clicking the dog widget in the sidebar navigates to the Home screen **and triggers an entrance animation**: the dog **walks from its sidebar position into the center of the main window**, using the same walk-cycle sprite variant already built for the startup morning-greeting feature (§5 of the earlier Home Tab / Startup Walk-In addendum) — reused here rather than building a second walk animation.
- Once the dog reaches center and settles into its idle loop, the **action buttons animate into place around it** — positioned to the left/top/bottom of the dog (arranged like a radial/contextual action menu in a game, not a plain horizontal row underneath as it is currently). Exact arrangement (semi-circle, stacked to one side, etc.) left to Antigravity's judgment, but the buttons should feel like they're "popping into" the scene around the character rather than sitting in a static toolbar.
- **This entrance animation only plays when navigating to Home fresh from the sidebar widget** (or from the startup walk-in, if that happens to land on Home) — it shouldn't replay every time the Home screen simply re-renders or the app briefly loses/regains focus while already on that screen.

---

## 3. Greeting Text — De-boxed

- The "Good morning, Abhishek" greeting + subtext keeps its content and behavior (time-of-day-based, birthday override on Aug 25) exactly as speced before, but **loses its bordered box container** — it should sit directly on the scene background as free-floating text, consistent with removing visual clutter now that the background itself is doing more visual work.

---

## 4. Background — Time-of-Day Scenery

- Replace the current blank/flat background with a **full-window pixel-art scene**, anchored around a small doghouse, that **changes based on time of day**:
  - **Morning** — soft sunrise palette, maybe a rising sun and warm light.
  - **Noon/Day** — bright, clear sky, sun high.
  - **Evening** — orange/pink dusk tones, sun low.
  - **Night** — dark sky, moon, stars.
- The scene should **cover the entire Home screen background**, behind the greeting text, clock, dog, and buttons.
- Same pixel-grid-as-data art method used for the sprites should extend to this scenery for visual consistency (crisp, nearest-neighbor-scaled, same overall palette family as the dog).
- **Transitions between time-of-day variants crossfade** smoothly rather than instantly swapping — this applies to the base scenery art itself as the clock crosses into a new period.

---

## 5. Ambient Scenery Motion

Each time-of-day variant gets its own small, unique piece of ambient motion, so the background feels alive rather than a static painted backdrop:

- **Morning** — a couple of small birds drifting/flying across the sky.
- **Day/Noon** — soft clouds slowly drifting across the background.
- **Evening** — the doghouse's porch lantern flickers/glows on as dusk settles in, giving the scene a warm focal point as the light fades.
- **Night** — fireflies drifting near the doghouse, plus gently twinkling stars in the sky.

These are small, low-cost looping details layered into each scenery variant — not full mini-animations requiring their own complex rigs, just subtle motion to sell "living scene" rather than "still image."

---

## 6. Explicitly Out of Scope

- No new persistent state/tracking introduced by any of this (feed/pet/walk interactions remain cosmetic-only, consistent with the rest of the app's no-history-tracking v1 scope).
- No live weather-based scenery (time-of-day only, no external weather data) — keeps this fully offline/deterministic like the rest of the app's local-clock-based logic.

---

## 7. Open Items — Left to Antigravity's Judgment

- Exact button arrangement around the centered dog (semi-circle vs. stacked to one side vs. other layout).
- Whether the ambient scenery elements (birds, clouds, lantern glow, fireflies/stars) also crossfade in/out with the time-of-day transition, or simply appear/disappear as their variant becomes active.
