# Boss battle brief

## Assets

- Boss name:
- Main StateMachineSO:
- Boss Prefab:
- Output folder:
- Existing attacks/prefabs to reuse:
- Explicitly deferred or excluded mechanics:

## Encounter goal

- Player-facing goal:
- Battle completion condition:
- Global rules or constraints:

## Phase overview

| Phase | Entry condition | First Pattern | Exit conditions |
|---|---|---|---|
| Phase 1 | Immediate | Pattern 1 | |

## Pattern

Duplicate this section for each Pattern.

- Phase / Pattern name:
- Design ID:
- Entry: `PhaseStart`, `AfterPattern`, or `Conditional`
- Entry source and conditions:
- Can repeat within this Phase:
- Summary:

Special Actions:

| Logical ID | Trigger | Shared by Patterns | Required count | Required for Pattern Clear | Failure or unanswered reason |
|---|---|---|---:|---|---|
| | | | 1 | No | Ignored |

Responses and punishments in priority order:

| Priority | Kind | Condition | Consequence or existing Action | Progress effect |
|---:|---|---|---|---|
| 1 | Forbidden action | | | None |
| 2 | Special Action success | | | Progress or resolve |
| 3 | Wrong response, evade, or unanswered | | | Failure reason only |

Pattern Clear:

- Rule: `TimelineOnly`, `RequiredSpecialActions`, or `TimelineAndRequiredSpecialActions`
- Loop timeline until cleared:
- Loop start Beat index:
- Loop delay:
- Recovery duration:

Count dependent behavior:

- Scope: `Battle Started`, `Battle Cleared`, or current run `Attempt Count`
- Pattern used as count source:
- Value mapping or comparison:

Timeline:

| Order | Delay from previous Beat | Action or existing asset | Important Inspector values |
|---:|---:|---|---|
| 1 | 0 | | |

Outgoing branches in priority order:

| Priority | Condition | Destination Pattern or Phase | Notes |
|---:|---|---|---|
| 1 | | | |

## Playtest

- Fast route to encounter:
- Expected successful route:
- Expected failure/retry route:
