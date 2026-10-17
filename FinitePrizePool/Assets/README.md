
# Finite Prize Pool — Unity Project

A Unity/C# implementation of a finite-ticket prize game. Deals are generated and validated before play. Each accepted spin consumes the next ticket in the active deal. The project focuses on exact monetary calculations, configurable prize pools, persistent gameplay state, and auditable spin records.

> **Project Status:** The latest reported test result is 68/68 EditMode tests passed. Not every client acceptance requirement has been verified. See the Outstanding Verification section before final submission.

## 1. Project Overview

Finite Prize Pool uses a pre-generated finite ticket pool instead of generating a new random prize on every spin.

Each deal contains a fixed sequence of Regular prizes, Big prizes, and Loss tickets. The deal is generated and validated before gameplay begins. Each accepted spin consumes the next ticket in the active deal.

The main objectives are:

- Support Inner and Outer game modes.
- Support 20 bet levels from $0.25 to $5.00.
- Calculate wagers, payouts, and RTP using integer cents.
- Target exactly 90% RTP over a completed deal when the configuration permits it.
- Validate ticket placement and preserve prize-cycle order.
- Maintain player balances, deal progress, spin history, and persistence.
- Support deal lifecycle management and retirement auditing.

## 2. Technology Stack

- **Engine:** Unity 6.3
- **Language:** C#
- **Testing:** Unity Test Framework — EditMode
- **Configuration:** Unity ScriptableObject
- **Persistence:** Local JSON save files
- **Randomisation:** System.Security.Cryptography.RandomNumberGenerator

The project uses local persistence and does not require a separate server or database for the described assignment.

## 3. Game Configuration

The GameConfig ScriptableObject stores the game configuration.

| Setting | Configuration |
|---|---|
| Game modes | Inner and Outer |
| Bet levels | 20 |
| Minimum bet | $0.25 |
| Maximum bet | $5.00 |
| Bet increment | $0.25 |
| Target RTP | 90% |
| Bonus unit value | $1.00 |
| Maximum deal-generation attempts | 1,000 |

The exact Regular and Big prize-bag values must match the authoritative Mathematics specification supplied by the client. Verify these values before final submission.

## 4. RTP and Deal-Size Calculations

Return to Player (RTP) represents the percentage of total wagered money returned as prizes over a completed deal.

**Formula:**

`RTP (%) = (Total Payout / Total Wager) × 100`

For example, a deal with $10.00 total wagers and $9.00 total payouts has 90% RTP.

The engine uses integer cents for monetary calculations to avoid floating-point currency rounding errors.

The assignment requires a deal-size table covering all 40 combinations: two game modes multiplied by 20 bet levels.

## 5. Ticket Placement Rules

The placement validator is intended to check maximum consecutive Loss tickets and gaps between Regular and Big prize slots, including the gap across the end and start of a deal.

- Maximum consecutive Loss tickets: `2 × ceil(L / (P + 1))`
- Maximum Regular gap: `2 × ceil(N / (R + 1))`
- Maximum Big gap: `2 × ceil(N / (B + 1))`

Where:

- `L` = number of Loss tickets
- `P` = total number of prize slots
- `N` = total number of spins in the deal
- `R` = number of Regular slots
- `B` = number of Big slots

The deal-generation process is bounded to a maximum of 1,000 attempts. A mathematically impossible configuration should be rejected instead of silently relaxing the rules.

## 6. Gameplay — Step by Step

### Step 1: Select a Player

Choose one of the available player profiles. Each player maintains their own balance.

### Step 2: Select Game Mode

Choose either Inner or Outer. The selected mode determines the applicable configuration and deal.

### Step 3: Select Bet Amount

Choose one of the 20 configured bet levels, from $0.25 to $5.00.

### Step 4: Press Spin

Press the Spin button to play the next ticket in the active deal.

The service checks whether the player has sufficient balance before consuming a ticket. If the balance is insufficient, the spin is rejected and the ticket must remain unused.

### Step 5: Process the Result

For an accepted spin, the service processes the wager and award, updates the player's balance and deal progress, and records the spin.

The UI displays the result, award amount, updated balance, and game status.

### Step 6: Continue the Deal

Each accepted spin consumes the next ticket in the active deal. The engine does not generate a new random outcome for every spin.

### Step 7: Complete the Deal

When the final ticket is consumed, the deal should transition to Completed and its Ready successor should become active according to the lifecycle logic.

### Step 8: Verify Persistence

Stop and restart the game to verify that saved player balances and deal progress are restored. Pending-result recovery must be tested separately by closing the game after a spin is saved but before its result is shown.

## 7. What Happens When Spin Is Clicked?

The intended spin-processing flow is:

1. Validate the player, active deal, and available balance.
2. Read the next ticket from the active deal.
3. Calculate the wager and award in integer cents.
4. Update the player balance and deal totals/progress.
5. Create a spin record with a unique spin ID and relevant player, deal, and ticket information.
6. Save the updated state and spin record before the result display is completed.
7. Return the spin result to the UI.
8. Mark the result as shown after the UI displays it.

This ordering is intended to support recovery if the application closes after the spin is recorded but before the result is shown. The mid-spin interruption scenario still needs explicit end-to-end verification.

## 8. Spin History and Record Fields

The current persistence implementation records these spin fields:

| Field | Description |
|---|---|
| `SpinId` | Unique identifier for the spin |
| `DealId` | Deal used for the spin |
| `PlayerId` | Player who made the spin |
| `TicketPosition` | Ticket position consumed |
| `WagerCents` | Wager amount in cents |
| `AwardCents` | Award amount in cents |
| `ResultShown` | Whether the result has been marked as displayed |

The saved game state also includes player and deal data, such as player balances and deal progress.

Unity Console debug messages are diagnostic logs. The JSON save file is the persistent data file.

## 9. JSON Persistence and Save Location

The default JSON filename is:

`finite-prize-pool-save.json`

The file path is built from Unity's `Application.persistentDataPath`.

On macOS, this typically resolves below:

`~/Library/Application Support/<CompanyName>/<ProductName>/`

The actual Company Name and Product Name depend on Unity Player Settings. Use the full path printed in the Unity Console to locate the actual file.

The storage implementation also uses:

- `finite-prize-pool-save.json.bak` — backup file
- `finite-prize-pool-save.json.tmp` — temporary file used during saving

The persistence layer saves and loads player records, deals, spin history, and retirement history as represented by the save data model. It tries the main save file first and then attempts to load the backup if necessary.

### How to Inspect Spin History

1. Run the game and perform a spin.
2. Open the Unity Console.
3. Find the `[SAVE SUCCESS]` message.
4. Copy the JSON file location shown in the Console.
5. Open the JSON file in a text editor such as VS Code.
6. Search for `SpinHistory`.
7. Check the latest spin record's player, deal, ticket position, wager, award, and result-shown status.
8. Restart the game and verify the restored balance and ticket progress.

A save-success message confirms that the save method completed. It does not independently prove that every field is correct or that crash recovery works.

## 10. Deal Lifecycle

The deal lifecycle uses these states:

- **Ready:** Generated and waiting to be activated.
- **Active:** Currently used for gameplay.
- **Completed:** All tickets have been consumed.
- **Retired:** Removed from active use, with retirement information intended to be retained for audit.

The service manages active and Ready deals, spin processing, ticket consumption, deal completion, successor activation, and retirement history.

Configuration changes should affect newly generated deals rather than silently changing an already active deal. Verify this behaviour against the client acceptance requirements.

## 11. Code Architecture

### GameConfig

A Unity ScriptableObject that stores configuration such as version, target RTP, bonus unit value, bet levels, prize-bag payouts, and placement settings.

### DealBuilder

Responsible for generating and preparing deals. The implementation discussed during development includes:

- Secure Fisher–Yates shuffling.
- Independent shuffling of Regular and Big prize cycles.
- Preservation of prize-cycle order.
- Ticket sequence construction.
- Exact RTP verification.
- A maximum of 1,000 generation attempts.

### TicketPlacementValidator

Calculates and validates placement limits for Loss, Regular, and Big tickets. It checks loss runs and gaps between prize categories, including the circular end-to-start gap.

### DealService

Manages:

- Player registration and balance tracking.
- Active and Ready deal management.
- Spin processing and ticket consumption.
- Wager deductions and prize awards.
- Deal totals and spin history.
- Deal completion and successor activation.
- Deal retirement and retirement history.
- Pending spin result tracking.

### JsonDealPersistence

Maps application state and spin records to and from the JSON save data model.

### JsonSaveStorage

Writes and loads the JSON save file, maintains a backup, and attempts backup recovery when the main save cannot be loaded.

### UI and GameBootstrap

The gameplay UI provides player, mode, and bet selection, Spin interaction, balance/result/status display, and deal-audit information. GameBootstrap initializes and connects the game systems.

## 12. How to Open and Run the Project

1. Install a compatible Unity Editor version. The project was reported as using Unity 6000.3.5f2.
2. Open Unity Hub.
3. Select **Add project from disk**.
4. Choose the root folder of the Finite Prize Pool repository.
5. Wait for asset import and C# compilation.
6. Open the gameplay scene used by the project.
7. Press **Play**.
8. Select a player, game mode, and bet amount.
9. Press Spin to test gameplay.
10. Check the Unity Console for compilation errors and save/debug messages.

If the project does not run correctly, resolve compilation errors first and verify scene references and configuration assets.

## 13. Automated Testing

The latest reported test result is **68/68 EditMode tests passed**.

The tests discussed during development cover areas including:

- Deal creation and activation.
- Ticket generation and prize-cycle ordering.
- RTP validation for the test configuration.
- Ticket placement validation.
- Invalid configuration and ticket handling.
- Player balance and spin processing.
- Deal completion and successor activation.
- Persistence and retirement-history scenarios.

Passing tests demonstrates the behaviour covered by those tests. It does not automatically confirm every client acceptance scenario.

## 14. Required Reports and Submission Deliverables

The assignment calls for:

- A deal-size table covering all 40 mode/bet combinations.
- A simulation report covering at least 10 complete Inner $1.00 deals and 1 complete Outer $1.00 deal, with actual RTP recorded for each.
- A screen recording of the required gameplay and recovery scenarios, within the client's time limit.
- A 1–2 page design note documenting architecture, decisions, assumptions, and skipped requirements.
- A Git repository containing the project, README, tests, and required deliverables.

Include each report and recording in the submission location requested by the client. Verify that the required files exist before marking the submission complete.

## 15. Outstanding Verification Checklist

Update this checklist only after the relevant requirement has actually been verified.

- [ ] Confirm the exact Regular and Big prize-bag values against the authoritative client Mathematics specification.
- [ ] Implement and test the rule that prize slots must not follow a fixed repeating interval.
- [ ] Add and run the test rejecting mathematically impossible placement settings, including the specified Inner $1.00 case.
- [ ] Close and reopen the application after a spin is saved but before its result is shown; verify pending-result recovery.
- [ ] Generate and include the deal-size table for all 40 mode/bet combinations.
- [ ] Complete the simulation report for 10 full Inner $1.00 deals and 1 full Outer $1.00 deal.
- [ ] Complete the required screen recording.
- [ ] Complete the 1–2 page design note.
- [ ] Verify configuration changes apply to the next deal only.
- [ ] Verify lifecycle handoff and retirement audit behaviour.
- [ ] Test the project from a clean checkout.
- [ ] Confirm all required assets, scenes, tests, and generated reports are committed.

Do not mark an item complete based only on a related unit test if the required end-to-end scenario has not been tested.

## 16. Design Principles

- **Finite outcomes:** Each accepted spin consumes the next ticket in a prepared deal.
- **Exact monetary arithmetic:** Currency calculations use integer cents.
- **Secure shuffling:** Cryptographic random-number generation is used for shuffling.
- **Validation before use:** Deals are validated before activation.
- **Bounded generation:** Generation stops after the configured attempt limit.
- **Persistent state:** Player and deal progress are saved locally.
- **Auditability:** Spin records and retirement history support review.
- **Separation of responsibilities:** Configuration, deal generation, validation, gameplay, persistence, and UI are handled by separate components.

---

**Finite Prize Pool — Unity / C#**

This README describes the project structure and reported verification status. Update the project status and outstanding checklist after completing the remaining client acceptance checks.
