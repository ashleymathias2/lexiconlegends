# LEXICON LEGENDS — Game Design Document

Mobile hybrid-casual word-combat game.

## 1. Game Overview

**Genre:** Mobile hybrid-casual word-combat game. Players form words from a letter grid to cast spells against an enemy.

**Core Concept:** Players form words from a letter grid to cast spells against an enemy in real time. Word length and letter rarity determine spell power; word category (vowel-heavy, consonant-heavy, rare-letter-included) determines spell type. The enemy's threat escalates based on the player's own pace of play — shown through proximity and expression rather than a visible clock or bar — and the player must out-damage, out-heal, or out-last the enemy before their own HP runs out.

**Platform & Orientation:** Mobile, portrait orientation.

## 2. Screen Layout (Portrait)

Three vertical zones:

- **Top Zone — Enemy Area (~35–40% of screen height):** Enemy sprite (position reflects current Aggression stage: far / stepped-in / stepped-in-further / adjacent), emoji reaction bubble above the enemy, enemy HP bar, background/environment art.
- **Middle Zone — HUD Strip (~10% of screen height):** Player HP bar, combo streak indicator, lives/hearts icon and count, pause button, level/score indicator.
- **Bottom Zone — Word Grid Area (~50–55% of screen height):** Currently-selected word preview row above the grid, 4×5 letter tile grid, Confirm and Clear/Reshuffle action buttons.

Design note: grid stays in thumb reach at the bottom; enemy feedback stays unobstructed above.

## 3. Core Loop

1. Player views the letter grid and selects tiles to form a word.
2. Word is confirmed → tiles destroyed → a spell is cast (damage + possible side effect, see Section 5).
3. The cast adds fill to the enemy's Aggression Meter (Section 6); board refills and re-validates (Section 4).
4. Once the Aggression Meter reaches its threshold, the enemy attacks the player and the meter resets.
5. Loop continues until enemy HP reaches 0 (win) or player HP reaches 0 (loss).

## 4. The Word Grid

- 4×5 grid of letter tiles (20 letters total).
- Tile selection is **free selection**: any tile, anywhere on the grid, in any order — tiles do not need to be adjacent.
- Each tile may be used once per word; once a word is confirmed, its tiles are destroyed.
- Minimum word length: 3. Maximum word length: 10.
- Each confirmed word = one spell cast.

> Free selection (rather than adjacency/Boggle-style) is deliberate: on a 4×5 grid, requiring adjacency would make 7–10 letter words nearly impossible to find. It also keeps board validation computationally cheap (Section 8).

## 5. Letter Spawn System

### Board Lifecycle
1. Generate initial board.
2. Player selects letters to form a word.
3. Selected tiles are destroyed.
4. Tiles above fall down (gravity).
5. Empty tiles are refilled using spawn logic.
6. Board is validated to ensure playability.
7. Player continues forming words.

### Initial Board Generation
The initial board must guarantee at least one good word for the player.

**Step 1 — Select a Seed Word**

| Word Length | Probability |
|---|---|
| 4 letters | 50% |
| 5 letters | 30% |
| 6 letters | 15% |
| 7+ letters | 5% |

**Step 2 — Choose Random Grid Positions:** Randomly select N grid cells, where N = length of the seed word.

**Step 3 — Place Word Letters:** Insert the seed word's letters into the chosen positions.

**Step 4 — Fill Remaining Tiles:** Fill empty tiles using the Weighted Letter Distribution below.

### Letter Spawn Distribution

| Tier | Letters | Spawn Chance |
|---|---|---|
| Vowels | A, E, I, O, U | 35% |
| Common | R, T, L, N, S | 30% |
| Medium | D, G, H, M, P | 20% |
| Rare | B, C, F, W, Y | 10% |
| Legendary | J, Q, X, Z | 5% |

### Spawn Rules

- **Vowel Balance Rule:** Minimum vowels on board: 6. Maximum vowels on board: 9. If below minimum during spawning, force-spawn a vowel.
- **Rare Letter Cooldown:** If a Legendary letter (J, Q, X, Z) spawns, disable Legendary spawning for the next 6 tile spawns.
- **Large Word Reward:** If the player forms a word with 6+ letters, spawn 1 Rare or Epic letter in the next refill cycle.

### Difficulty Scaling

| Stage | Vowel Spawn | Legendary Spawn |
|---|---|---|
| Early Game | 40% | 3% |
| Mid Game | 35% | 5% |
| Late Game | 30% | 7% |

### Player Word Selection Rules
- Minimum word length: 3. Maximum word length: 10.
- Tiles may be selected in any order; each tile used once per word.
- Confirming a word destroys the selected tiles and applies its spell effect (Section 6).

### Tile Gravity & Refill
- Tiles fall downward to fill destroyed gaps.
- Empty spaces refilled from the top of the grid.
- Refill order: Weighted Letter Distribution → Vowel Balance Check → Rare Letter Cooldown → Board Playability Check.
- After refill, the board must contain at least 3 valid words (see Section 8 for efficient checking).

### Dictionary Requirements
- Word lengths: 3–10 letters.
- Extremely obscure words removed.
- Target dictionary size: 20,000–30,000 words.

## 6. Spellcasting & Damage System

**Damage = Length Multiplier × Average Letter Rarity Weight × Combo Multiplier**

Average Letter Rarity Weight is the mean of each letter's rarity value across all letters in the word — not the maximum, and not a product.

### Length Multiplier

| Word Length | Multiplier |
|---|---|
| 3 letters | 1.0 |
| 4 letters | 1.3 |
| 5 letters | 1.7 |
| 6 letters | 2.2 |
| 7 letters | 3.0 |
| 8+ letters | 4.0 |

### Letter Rarity Weight

| Tier | Letters | Weight |
|---|---|---|
| Common | E, A, R, T | 1.0 |
| Uncommon | D, L, N, S | 1.2 |
| Rare | C, M, P, W | 1.5 |
| Epic | B, F, V, Y | 2.0 |
| Legendary | J, Q, X, Z | 3.0 |

**Worked Example — "QUARTZ"** (Q, U, A, R, T, Z):
- Length Multiplier (6 letters) = 2.2
- Letter weights: Q=3.0, U=1.0, A=1.0, R=1.0, T=1.0, Z=3.0 → average = 10.0 ÷ 6 = 1.67
- Damage = 2.2 × 1.67 = 3.67, before Combo Multiplier

### Combo Multiplier

| Streak | Multiplier |
|---|---|
| 1–2 words | ×1.0 |
| 3–4 words | ×1.15 |
| 5+ words | ×1.3 |

Streak resets if more than 4 seconds pass without a confirmed word.

### Spell Types

| Word Category | Spell Type | Effect |
|---|---|---|
| ≥60% vowels | Restoration | Heals the player for a portion of the word's damage value |
| ≥60% consonants | Strike | Standard damage, no side effect |
| Contains a Rare/Epic letter | Burn | Damage + small damage-over-time tick for 3 turns |
| Contains a Legendary letter | Stagger | Damage + pushes the Aggression Meter back, delaying the next enemy attack |

## 7. Combat System

### Combatant Stats
- Player HP: flat 100, does not scale with level.
- Enemy HP: set per level/enemy type.

### Aggression Meter
The enemy's attack cadence is driven entirely by the player's own actions, not a real-time clock.
- Meter starts at 0 each cycle.
- Each confirmed word adds fill based on word length — shorter words add more fill, longer words add less.
- A Stagger-type word subtracts additional fill on top of its normal contribution.
- When the meter reaches the Attack Threshold, the enemy attacks immediately and the meter resets to 0.

**Aggression Meter Fill by Word Length**

| Word Length | Meter Fill |
|---|---|
| 3 letters | 1.0 |
| 4 letters | 0.85 |
| 5 letters | 0.70 |
| 6 letters | 0.55 |
| 7 letters | 0.45 |
| 8+ letters | 0.40 |

Attack Threshold: **2.5**

### Visual Presentation: Enemy Proximity & Emoji Escalation
The Aggression Meter is never shown as a number or bar — only via enemy proximity + emoji, in thirds of the threshold:

| Stage | Meter Fill | Enemy Position | Emoji |
|---|---|---|---|
| 1 | ~33% | Steps closer (1st step) | Angry 😠 |
| 2 | ~66% | Steps closer (2nd step) | Shaking angry 😡 (tremble animation) |
| 3 (threshold) | 100% | Reaches the player | Steaming angry 🤬 (steam puffs) |

On Stage 3: enemy attacks immediately, meter resets to 0, enemy steps back to starting distance with neutral expression. A Stagger word can visibly knock the enemy back a step and downgrade its emoji.

### Enemy Damage Scaling

**Enemy Hit Damage = BaseHit × (1 + 0.08 × (Level − 1))**

- BaseHit: 12–15.
- Level 1: ~12–15 damage per hit. Level 10: ~22–27 damage per hit.
- Linear-ish scaling by design (avoids "wall" feeling of exponential curves). Sharper spikes reserved for boss levels specifically.
- Uses the same Level variable as the spawn-weight Difficulty Scaling table.

### Win / Loss Conditions
- Win: enemy HP reaches 0.
- Loss: player HP reaches 0.
- All player damage comes from Aggression Meter attacks — no separate timer-based loss condition.

## 8. Meta Loop & Progression

- Lives/hearts economy: capped attempts per session, refilling over time or via rewarded ads/IAP.
- Level map: enemies of increasing difficulty, gating spawn-weight and damage scaling to level number.
- Daily seeded board: one fixed daily challenge board, shareable score.
- Power-ups: one-time "reveal a valid word" hint, one-time board reshuffle — IAP/ad-reward candidates.
- Enemy variety: vary Attack Threshold and BaseHit per enemy type for distinct combat "feels."

## 9. Technical Implementation Notes

### Efficient Board Validation
With free selection and 20 tiles, a naive subset search against the dictionary is exponential and not viable in real time on-device.
- Maintain a running letter-frequency count of the current board (26 integers).
- Precompute each dictionary word's own letter-frequency signature offline.
- A word is formable if, for every letter, the board's count ≥ the word's required count — O(1) check per word against a precomputed signature.
- Index the dictionary by length and letter-signature buckets so validity checks only scan plausible candidates.

### Damage Formula — Reference Implementation
```
lengthMult = lookup(word.length)        // capped at 10
avgRarity  = sum(letterWeight(l) for l in word) / word.length
comboMult  = lookup(currentStreak)
damage     = lengthMult * avgRarity * comboMult
```

### Aggression Meter — Reference Implementation
```
meterFill = lookup(word.length)
if word.isStaggerType:
    meterFill -= staggerBonus     // suggested starting value: 0.5
meter += max(meterFill, 0)
if meter >= attackThreshold:
    triggerEnemyAttack()
    meter = 0
enemyHitDamage = baseHit * (1 + 0.08 * (level - 1))
```

## 10. Parameter Reference Sheet

All tunable values, consolidated. Starting points for prototyping, not final balance.

### Grid & Word Rules
| Parameter | Value |
|---|---|
| Grid size | 4 rows × 5 columns (20 tiles) |
| Minimum word length | 3 letters |
| Maximum word length | 10 letters |
| Tile selection rule | Free selection (no adjacency required) |
| Minimum valid words after refill | 3 |

### Letter Spawn Distribution
| Tier | Letters | Spawn Chance |
|---|---|---|
| Vowels | A, E, I, O, U | 35% |
| Common | R, T, L, N, S | 30% |
| Medium | D, G, H, M, P | 20% |
| Rare | B, C, F, W, Y | 10% |
| Legendary | J, Q, X, Z | 5% |

### Board Rules & Difficulty Scaling
| Parameter | Value |
|---|---|
| Minimum vowels on board | 6 |
| Maximum vowels on board | 9 |
| Legendary spawn cooldown | 6 tile spawns after a Legendary letter spawns |
| Large word reward trigger | 6+ letter word → spawn 1 Rare/Epic letter next cycle |
| Vowel spawn % — Early/Mid/Late | 40% / 35% / 30% |
| Legendary spawn % — Early/Mid/Late | 3% / 5% / 7% |
| Dictionary size | 20,000–30,000 words |
| Dictionary word length range | 3–10 letters |

### Damage System
| Parameter | Value |
|---|---|
| Length Multiplier — 3/4/5/6/7/8+ | 1.0 / 1.3 / 1.7 / 2.2 / 3.0 / 4.0 |
| Rarity Weight — Common/Uncommon/Rare/Epic/Legendary | 1.0 / 1.2 / 1.5 / 2.0 / 3.0 |
| Combo Multiplier — 1-2/3-4/5+ streak | ×1.0 / ×1.15 / ×1.3 |
| Streak reset gap | 4 seconds without a confirmed word |
| Restoration heal amount | Portion of word's damage value (tune in playtesting) |
| Burn damage-over-time duration | 3 turns |

### Combat & Aggression Meter
| Parameter | Value |
|---|---|
| Player HP | 100 (flat, does not scale with level) |
| Enemy BaseHit | 12–15 |
| Enemy hit damage growth rate | +8% per level above 1 |
| Aggression Meter Fill — 3/4/5/6/7/8+ letters | 1.0 / 0.85 / 0.70 / 0.55 / 0.45 / 0.40 |
| Attack Threshold | 2.5 |
| Stagger meter reduction bonus | 0.5 (in addition to word's normal fill) |
| Emoji/proximity stage breakpoints | ~33% / ~66% / 100% of Attack Threshold |

### Screen Layout
| Zone | Height Allocation |
|---|---|
| Top — Enemy Area | 35–40% |
| Middle — HUD Strip | ~10% |
| Bottom — Word Grid Area | 50–55% |

## 11. Future Considerations

- Enemy resistances/weaknesses by spell type (Phase 2 — boss variety via enemy data alone).
- Whether Attack Threshold should drop slightly as level increases (frequency scaling, separate from severity).
- Whether boss-type levels should use a distinct Aggression Meter curve.
- Whether emoji escalation stages should carry animation/sound stings at each transition vs. only at the final attack.
