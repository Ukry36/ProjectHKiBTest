---
name: projecthkib-boss-authoring
description: Author or revise ProjectHKiB boss battles through Unity Boss Authoring, ScriptableObjects, prefab components, and Inspector wiring. Use for boss Phase/Pattern/Special Action implementation; do not use for requests that explicitly require new runtime C# systems.
---

# ProjectHKiB Boss Authoring

Implement the requested encounter as Unity assets and Inspector wiring. Treat runtime C# changes as outside scope unless the user explicitly requests code or an existing authoring capability is demonstrably insufficient.

## Required context

1. Read `ProjectHKiB_Re/Assets/Scripts/Combat/Boss/README.md` completely.
2. Read `ProjectHKiB_Re/Assets/Scripts/Combat/Boss/GAMEPLAY_EVENT_SPECIAL_ACTIONS.md` when a Special Action depends on parry, hit, knockback, interaction, airborne, or another gameplay event.
3. Inspect the supplied boss brief and existing assets under that boss folder. Reuse existing attacks, prefabs, Actions, Decisions, and modules before proposing a new one.
4. Check `git status --short` and preserve unrelated or pre-existing changes.

When the user supplies a Boss Authoring Snapshot, treat it as a navigation aid. Verify the actual Unity assets before changing them because the snapshot may be stale.

## Authoring boundary

- Use `Tools > Boss Authoring` for hierarchy creation, PatternSO editing, entry rewiring, fallback refresh, and validation.
- Use the normal Inspector or StateMachine graph only for details the authoring window does not expose.
- Do not directly edit serialized `.asset`, `.prefab`, or `.unity` YAML.
- Do not add C# merely to avoid Inspector work. If a required general Action or Decision is absent, identify the missing capability and ask before adding or integrating a module.
- Keep Special Action results separate from Pattern Clear. A Special Action failure does not mean the Pattern failed.
- Treat success consequences, wrong-response consequences, unanswered or evasion consequences, and forbidden-action punishments as separate authoring concerns. A forbidden-action punishment such as an instant-kill branch is not a Special Action failure or Pattern failure.
- Treat a Special Action ID as a battle-wide logical progress key. Reuse the same ID and `Required Count` across Patterns that intentionally contribute to one goal, such as P7 and P8 both contributing to `AttackWing`.
- Keep IDs unique only inside one Pattern's `Special Actions` array. Independent goals must use different IDs, and every definition sharing an ID must use the same `Required Count`.
- For a looping Pattern with a one-time entrance beat, set `Loop Start Beat` to the first retry beat so the entrance is not replayed.
- Use `BossPatternCountDecision` or `Boss Pattern Battle Count` when behavior depends on battle-wide Pattern starts or clears. Use `BossPatternAttemptCountDecision` only for retry cycles inside the current Pattern run.
- Keep conditional and phase-exit transitions above default completion transitions. Keep `Boss Phase Restart Fallback` last.
- Preserve explicit planning status. Do not implement a mechanic marked deferred, postponed, or out of scope unless the user includes it in the requested scope.
- Minimize new knobs. Use existing presets and defaults unless the design requires a distinct value.

If no Unity Editor control surface is available, do not substitute raw YAML or C# edits. Produce the exact phase/pattern authoring matrix, report that asset mutation is blocked, and ask for a Unity control surface or for the user to perform the listed Inspector operations.

## Workflow

1. Translate the design into a compact authoring matrix: Phase, Pattern, entry trigger, Special Actions and any shared progress ID, success/wrong/unanswered/forbidden consequences, Pattern Clear rule, initial/retry timeline beats, count-dependent behavior, and outgoing branches.
2. Resolve ambiguity only when it changes the graph. Safe presentation details may use conservative defaults and should be reported.
3. Create or select the Main StateMachine and Boss Prefab in `Boss Authoring`. Set the output root to the boss's asset folder.
4. Build one Phase skeleton at a time, then its Patterns. Configure PatternSO timelines before adding exceptional branches.
5. Wire Special Action reporting where the event occurs and result Decisions where branching occurs. Prefer `ReportBossGameplayEventSpecialAction` for supported gameplay events. Define a shared logical ID in every Pattern that can report it.
6. Review transition order explicitly. Put immediate forbidden-action punishments and encounter-ending conditions before ordinary Pattern progression, then keep the phase fallback last. Run Validation after each Phase rather than after every field change.
7. At completion, save a fresh Agent Snapshot. Report changed assets, validation error/warning counts, assumptions, unresolved items, and the shortest useful playtest route.

For a reusable request template and recommended task sizing, read [references/prompt-and-strategy.md](references/prompt-and-strategy.md) when preparing or interpreting an authoring request.

When the user has no structured brief, copy and fill [assets/boss-battle-brief-template.md](assets/boss-battle-brief-template.md) before asset mutation.
