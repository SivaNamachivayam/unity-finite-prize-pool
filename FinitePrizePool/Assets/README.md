# Finite Prize Pool
### Unity-Based Finite Prize Pool Game

## 1. Project Overview

Finite Prize Pool is a Unity-based game built in C# that uses a **pre-generated finite ticket pool** instead of generating a new random outcome on every spin.

Each deal contains a fixed sequence of Regular prize tickets, Big prize tickets, and Loss tickets. The deal is generated and validated before gameplay begins. Each accepted spin consumes the next ticket in the active deal.

The main objective is to provide a reliable and auditable deal engine that maintains the configured Return to Player (RTP) target of exactly 90% over a fully completed deal.

**Key objectives**
- Implement the finite prize pool deal-generation system.
- Support Inner and Outer game modes.
- Support 20 bet levels from $0.25 to $5.00.
- Calculate wagers, payouts, and RTP using exact monetary arithmetic.
- Validate ticket placement and preserve prize-cycle order.
- Maintain player balances, deal progress, spin history, and persistence.
- Support deal lifecycle management and retirement auditing.

## 2. Technology Stack

- **Engine:** Unity 6.3
- **Language:** C#
- **Testing:** Unity Test Framework — EditMode
- **Configuration:** Unity ScriptableObject
- **Persistence:** Local JSON save files
- **Randomisation:** `System.Security.Cryptography.RandomNumberGenerator`

The deal engine is implemented in C# within Unity. The assignment does not require a separate server or database.

## 3. Game Configuration

The game is designed around the following assignment requirements:

| Setting | Configuration |
|---|---|
| Game modes | Inner and Outer |
| Bet levels | 20 |
| Minimum bet | $0.25 |
| Maximum bet | $5.00 |
| Bet increment | $0.25 |
| Target RTP | Exactly 90% |
| Bonus unit value | $1.00 |
| Maximum deal-generation attempts | 1,000 |

The bet levels are configured in `GameConfig`, a Unity ScriptableObject. Prize bag values and placement settings are also managed through configuration.

**Prize configuration note:** The final Regular and Big bag values must be checked against the Mathematics PDF referenced in the client brief before the final submission.

## 4. Architecture and Implementation

The project separates deal generation, validation, gameplay operations, configuration, and persistence into dedicated components.

### GameConfig

Stores the game configuration, including the configuration version, target RTP, bonus unit value, bet levels, prize bag payouts, and placement settings.

### DealBuilder

Responsible for generating and preparing deals. Its implementation includes:

- Secure Fisher–Yates shuffling.
- Independent shuffling of Regular and Big prize cycles.
- Preservation of prize-cycle order.
- Construction and validation of ticket sequences.
- Exact RTP verification before a deal is accepted.
- A bounded generation process with a maximum of 1,000 attempts.

A deal that cannot be generated within the configured limits should be rejected rather than silently relaxing the rules.

### TicketPlacementValidator

Calculates and validates placement limits for Loss, Regular, and Big tickets.

The configured formulas are:

- Maximum consecutive Loss tickets: `2 × ceil(L / (P + 1))`
- Maximum Regular gap: `2 × ceil(N / (R + 1))`
- Maximum Big gap: `2 × ceil(N / (B + 1))`

Where:

- `L` = number of Loss tickets.
- `P` = total number of prize slots.
- `N` = total number of spins in the deal.
- `R` = number of Regular slots.
- `B` = number of Big slots.

The validator checks consecutive Loss runs, gaps between prize categories, and the circular gap across the end and start of the deal.

### DealService

Manages the core gameplay operations, including:

- Player registration and balance tracking.
- Active and Ready deal management.
- Spin processing and ticket consumption.
- Wager deductions and prize awards.
- Deal totals and spin history.
- Deal completion and successor activation.
- Deal retirement and retirement history.
- Pending spin result tracking.

### Persistence

The persistence layer stores game state locally using JSON. It supports saving and loading player records, deals, ticket progress, spin history, and retirement history.

The save-storage implementation also maintains a temporary save file and a backup to help recover from certain save failures.

## 5. RTP and Deal-Size Calculations

RTP represents the percentage of total wagered money returned as prizes over a completed deal.

**Formula:**

`RTP (%) = (Total Payout / Total Wager) × 100`

For example, if a completed deal has total wagers of $10.00 and total payouts of $9.00:

`RTP = ($9.00 / $10.00) × 100 = 90%`

The engine uses integer cents for monetary calculations to avoid floating-point rounding errors.

The deal-size calculation must determine the smallest valid number of paired cycles that produces a whole number of paid spins and exactly 90% RTP. Configurations that cannot satisfy the required constraints must be rejected.

The assignment requires a deal-size table covering all 40 combinations: two game modes multiplied by 20 bet levels.

## 6. How to Run the Project

1. Install the Unity version used by the project.
2. Open Unity Hub and select **Add project from disk**.
3. Select the root folder of the Finite Prize Pool repository.
4. Open the project in Unity.
5. Allow Unity to finish importing assets and compiling scripts.
6. Open the project's gameplay scene.
7. Press the **Play** button in the Unity Editor to launch the game.

If the project does not open correctly, check the Unity Console for compilation errors before testing gameplay.

## 7. How to Play

### Step 1 — Select a Player

Choose one of the available player profiles. Each player should maintain their own balance and game progress.

### Step 2 — Select a Game Mode

Choose either:

- **Inner**
- **Outer**

The selected mode determines which configured deal and prize rules are used.

### Step 3 — Select a Bet

Choose one of the 20 available bet levels, ranging from $0.25 to $5.00 in $0.25 increments.

### Step 4 — Spin

Press the **Spin** button to play the next ticket in the active deal.

The game checks whether the selected player has sufficient balance. If the balance is insufficient, the spin is rejected and the ticket must remain unused.

For an accepted spin, the wager is processed, the next ticket is consumed, the prize or Loss result is applied, and the player's balance and deal totals are updated.

### Step 5 — Review the Result

Review the displayed result, prize type, award amount, and updated balance.

Continue spinning to consume the remaining tickets in the active deal.

### Step 6 — Complete the Deal

When the final ticket is consumed, the current deal should transition to Completed and gameplay should continue using its Ready successor.

### Step 7 — Verify Persistence

Stop and restart the game to verify that saved player balances and deal progress are restored. Pending-result recovery should also be tested by closing the game after a spin is recorded but before its result is displayed.

## 8. Validation and Error Handling

The implementation includes checks for invalid deal structures, missing or empty ticket collections, invalid configuration, invalid ticket placement, and insufficient player balance.

The deal engine also verifies the total wager and payout against the target RTP before accepting a generated or activated deal.

The generation process is bounded to avoid an endless search for a valid deal.

## 9. Automated Testing

The project uses the Unity Test Framework for EditMode tests.

**Latest reported test result: 68/68 EditMode tests passed.**

The existing tests cover multiple areas, including:

- Deal creation and activation.
- Ticket generation and shuffling.
- Prize-cycle ordering.
- RTP validation for the test configuration.
- Placement validation.
- Invalid configuration and ticket handling.
- Player balance and spin processing.
- Deal completion and successor activation.
- Persistence and retirement-history scenarios.

Passing these tests demonstrates the tested behaviour; it does not by itself confirm every client acceptance scenario.

## 10. Known Limitations and Final Verification

Before the final client handoff, the following items must be verified or completed:

- Confirm the exact Regular and Big prize bag values against the referenced Mathematics PDF.
- Implement and test the rule that prize slots must not follow a fixed repeating interval.
- Add and run the required test for rejecting mathematically impossible placement settings, including the specified Inner $1.00 case.
- Verify pending-result recovery by closing and reopening the game mid-spin.
- Generate and include the deal-size table for all 40 mode/bet combinations.
- Complete the simulation report covering at least 10 complete Inner $1.00 deals and one complete Outer $1.00 deal, with actual RTP recorded for each.
- Complete the required screen recording and the 1–2 page design note.
- Verify the remaining lifecycle, configuration-change, and retirement acceptance scenarios.

Any assumptions or skipped requirements should be documented explicitly rather than represented as completed.

## 11. Design Principles

The project follows these principles:

- **Deterministic deal progression:** A spin consumes the next ticket rather than generating a new random prize.
- **Exact monetary arithmetic:** Money is calculated in integer cents.
- **Secure randomisation:** Cryptographic random-number generation is used for shuffling.
- **Validation before use:** Deals are checked before activation.
- **Bounded generation:** Invalid or impossible deals should be rejected without endless retries.
- **Persistent state:** Player and deal progress are saved locally.
- **Auditability:** Spin history and retirement records support later review.
- **Separation of responsibilities:** Deal generation, validation, gameplay, configuration, and persistence are handled by separate components.

## 12. Project Status

The core implementation and gameplay flow have been exercised, and the current EditMode test suite has been reported as passing 68/68 tests.

The project should be considered ready for final submission only after the outstanding requirements in Section 10 have been verified, the required deliverables have been added to the repository, and the final project has been tested from a clean checkout.

---

**Finite Prize Pool — Unity / C#**

A finite-ticket game implementation focused on exact RTP calculations, validated deal generation, persistent gameplay state, and auditable deal progression.