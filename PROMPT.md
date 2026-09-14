# Recreate Pixel Unicorn Runner

Create a complete, playable 2D side-view endless runner called **Pixel Unicorn Runner**. A bright ivory unicorn gallops automatically to the right through dark, moss-covered castle ruins. The player jumps across gaps, collects gold relics, and dashes through runestones to earn points. Reproduce the following gameplay and presentation. Make all necessary technical choices for the target engine; this prompt specifies the player experience only.

## Core experience

- Start directly in gameplay on a long, safe, flat platform. The unicorn is already running; there is no title screen or menu.
- Continue through an endless, varied sequence of stone platforms separated by gaps. There is no finish line or victory state.
- Let the player control only jumping and dashing. Forward movement is automatic, with no steering or braking.
- A single fatal collision or fall ends the run. Show the final score and an immediate keyboard retry option.
- Keep the scope focused: no enemies beyond stationary runestones, combat systems, health meter, lives, upgrades, progression, saved scores, music, or sound effects.

## Controls and movement

| Action | Keys | Behavior |
| --- | --- | --- |
| Jump / double jump | Space or Z | A fresh press jumps; a second press while airborne grants one additional jump. |
| Short jump | Release the jump key early | Cut upward motion so a tap produces a lower jump than a hold. |
| Dash | Left Shift, Right Shift, or X | Burst horizontally forward, on the ground or in the air. |
| Retry | R | After death, immediately begin a fresh run. |

Use the following tuning as the reference feel. A gameplay unit corresponds to 16 logical pixels, so the values can be translated consistently to another coordinate system.

- Increase normal forward speed linearly from **5 to 9 units per second over the first 120 seconds**, then hold it at 9.
- Each jump launches upward at **9 units per second**, with downward acceleration of **28 units per second squared**. The second jump resets upward speed to the same launch speed.
- Releasing jump while rising reduces upward speed to **45%** of its current value.
- Allow a **0.10-second grace period** for jumping after leaving a platform and remember a jump press for **0.12 seconds** when it cannot yet take effect.
- Landing restores both jumps. Once the ledge grace period expires, walking off a platform leaves only the one airborne recovery jump.
- A dash lasts **0.20 seconds**, travels at **twice the current running speed**, and temporarily suspends vertical movement. Resume the previous vertical motion after the dash.
- Begin a **0.70-second dash cooldown after the dash ends**. Dashing does not replenish jumps, and landing does not bypass the cooldown. A new run starts with dash available.
- Keep controls responsive and movement consistent at different display frame rates.

## Platforms, hazards, and difficulty

- Begin at ground level with approximately **24 units of safe runway ahead** of the starting position. Place a first row of five relics about 4 units ahead, one unit apart.
- Subsequent platforms are **14, 16, or 18 units long**, with thick gray masonry bodies, flat moss-covered tops, and decorative ruined stonework.
- Introduce runestones after the opening grace period, around the first few seconds of travel. Place them well inside platforms, approximately **6 units after the leading edge**, leaving clear takeoff and landing space.
- Keep early gaps near **1.25 units**. After roughly 8 seconds, vary gaps from **1.5 units** up to a gradually increasing maximum: approximately **2 units initially**, reaching **3.5 units at 120 seconds**.
- Keep early terrain level. After roughly 20 seconds, vary successive platform elevations in **0.5-unit steps**, staying within **1 unit above or below the initial ground level**.
- Ensure every gap and elevation transition is comfortably clearable with a well-timed, fully held single jump at the expected running speed. Preserve takeoff and landing margins; double jump is a recovery option.
- Separate consecutive runestones by at least approximately **12.9 units**, allowing the dash to recover even at maximum speed.
- A runestone is a tall ochre obelisk with a readable rune. Touching one without an active dash is fatal. Dashing through one breaks it, removes it, and awards points. Jumping over it is also valid.
- Hitting the front face of a platform is fatal, including during a dash. Landing on a platform top is safe. Falling approximately **6 units below the initial ground level** is fatal.
- Produce a new arrangement on retry. Keep the course continuous and fair during extended play, with no visible gaps caused by missing scenery, sudden terrain changes, or camera jumps.

## Relics and scoring

- Use small floating gold diamond-shaped relics, collected on contact and awarded only once.
- Place five relics on each platform, alternating randomly between a horizontal row and a shallow arch. On normal platforms, begin approximately **1.25 units after the leading edge**, spacing relics about **0.85 units apart**.
- Horizontal rows sit about **0.9 units above the platform**. Arches start and end near **0.8 units**, rising to about **2.3 units** in the middle.
- Keep relic trails clear of runestones so a reward never conceals a hazard.
- Award **10 points per unit of forward distance**, rounded down for the distance contribution, **25 points per relic**, and **100 points per broken runestone**. Dash travel contributes to distance normally.
- Display both total score and the number of relics collected. Stop accumulating score when the run ends.

## Art direction and camera

- Use original, coarse pixel art with hard square edges, binary transparency, and no gradients, smoothing, or antialiasing.
- Compose the game at a logical **320 × 180** resolution. Enlarge it in whole-number steps with sharp pixels; center the play area with black borders when the display dimensions do not fit exactly. Keep the HUD aligned to the same play area.
- Design the unicorn around a **32 × 32-pixel frame**, with an approximately 28-pixel-wide, 24-pixel-tall silhouette. It faces right, has a prominent horn, an ivory body, and a flowing pale-green mane and tail.
- Use an eight-pose galloping cycle, playing at about **12 poses per second at the starting speed** and accelerating with running speed. Include distinct rising, falling, horizontal magical dash, and stumbling/death poses.
- Give the environment a dark, ancient castle atmosphere: mossy gray brick platforms, arches, pillars, broken masonry, and vines. The unicorn and gold collectibles must remain easy to read against the background.
- Follow the unicorn horizontally with a fixed camera height. Keep it approximately one quarter of the way across the screen from the left, leaving substantial visibility ahead. Do not track its jumps vertically.
- Create depth with distant arches scrolling at **15%** of camera travel and nearer background ruins at **40%**.
- Restrict the artwork and interface to this palette:

| Color | Hex |
| --- | --- |
| Near-black | `#101419` |
| Charcoal | `#252d32` |
| Stone gray | `#59615d` |
| Dark brown | `#594b36` |
| Ochre gold | `#b58a43` |
| Moss green | `#628548` |
| Pale green | `#b5cf83` |
| Ivory | `#f3efd9` |

## Interface and feedback

- Use a compact, crisp **5 × 7-style pixel font**.
- Place a dark strip across the top, with `SCORE 000000` at the left and gold `RELICS 000` at the right. Use these as minimum digit widths, allowing larger totals to remain readable.
- At the lower left, show `DASH READY` when available and `DASH` while active or cooling down. Add a thin charge bar that empties during the dash and refills over its cooldown.
- For the first **6 seconds** of each run, show the lower-right hint `SPACE / Z  JUMP` and `SHIFT / X  DASH` on separate lines.
- Leave brief moss-colored unicorn afterimages during a dash, appearing about every **0.045 seconds** and lasting about **0.13 seconds**.
- Collecting a relic or breaking a runestone produces a small gold pixel burst lasting about **0.35 seconds**.
- On death, stop the unicorn immediately, show its stumbling pose briefly, and emit a larger ivory pixel burst lasting about **0.75 seconds**. Hide the unicorn after about **0.65 seconds**.
- Show a compact, dark, centered panel reading `RUN ENDED`, followed by `R TO RETRY`. Keep the final score and relic count visible.
- Retrying resets distance, score, relic count, running speed, jump availability, dash charge, visual effects, and camera position, then starts immediately on a safe opening platform.

## Completion criteria

Deliver the playable game with a cohesive pixel-art presentation. Confirm that tap and held jumps differ, only one extra airborne jump is allowed, dash duration and cooldown are consistent, runestones break only during a dash, terrain collisions and falls end the run, each reward scores once, and retry fully resets the experience. Verify that generated transitions remain fair as speed increases, extended runs remain stable, and the game and HUD stay sharp and correctly aligned at different window sizes.
