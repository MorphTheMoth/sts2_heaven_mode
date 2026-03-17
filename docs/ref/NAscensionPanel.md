# NAscensionPanel

Namespace: `MegaCrit.Sts2.Core.Nodes.Screens.CharacterSelect`

## Feature: Heaven level piggybacks on the official ascension panel

Relevant vanilla methods:

- `NAscensionPanel.DecrementAscension()`
- `NAscensionPanel.IncrementAscension()`
- `NAscensionPanel.RefreshArrowVisibility()`
- `NAscensionPanel.RefreshAscensionText()`
- `NAscensionPanel.SetAscensionLevel(int ascension)`

## Implementation note in this repo

Use Harmony to hijack the official ascension panel instead of injecting a custom Heaven selector.

- When official ascension is `0`, patch `DecrementAscension()` so the left arrow enters Heaven levels.
- Heaven levels are stored in `HeavenState.SelectedOption`:
- `0` = Off
- `1..11` = Heaven 1..11
- Patch `IncrementAscension()` so Heaven `11 -> ... -> 1 -> 0` uses the official right arrow.
- Patch `RefreshArrowVisibility()` so the left arrow remains visible at official ascension `0`.
- Patch `RefreshAscensionText()` to overwrite the official description box with Heaven title + description.
- Patch `SetAscensionLevel(int)` to clear Heaven selection when the player moves back into official ascension `> 0`.

## Result

The official ascension panel becomes the only difficulty UI:

- Official ascension `0` + left arrow => Heaven `1`
- Heaven `1` + left arrow => Heaven `2` ... Heaven `11`
- Heaven `1..11` + right arrow => move back toward official ascension `0`
- At Heaven `11`, the left arrow is hidden because there is no further Heaven level to advance into
- The official description box shows the current Heaven title and description while Heaven is selected

## Preference persistence

Heaven preference is not part of vanilla `PreferredAscension`.

Current repo behavior:

- save the selected Heaven level per character when the player changes Heaven in `NAscensionPanel`
- clear the saved Heaven preference when the player moves back to official ascension `> 0`
- restore the saved Heaven level when `StartRunLobby.SetSingleplayerAscensionAfterCharacterChanged(ModelId)` reapplies that character's preferred difficulty
- Heaven descriptions communicate inheritance:
  - Heaven `1` enables the ancient option-count restriction
  - Heaven `6` additionally enables the Neow HP override
  - Heaven `11` makes `Save and Quit` destroy the current run save instead of preserving the run
  - higher Heaven levels inherit lower Heaven effects

## Unlock gating

Relevant vanilla surfaces:

- `NAscensionPanel.RefreshArrowVisibility()`
- `NAscensionPanel.DecrementAscension()`
- `MegaCrit.Sts2.Core.Nodes.GodotExtensions.NClickableControl.Disable()`
- `MegaCrit.Sts2.Core.Nodes.GodotExtensions.NClickableControl.Enable()`

Implementation note in this repo:

- Heaven unlock state is checked when the official ascension panel is at `0`.
- Official A10 remains the vanilla prerequisite:
  - if the selected character's `_maxAscension < 10`, Heaven `1` is still locked
- Heaven progression is stored separately by the mod:
  - clearing Heaven `n` unlocks Heaven `n+1`
- If the mod config `config.json` has `"unlock": true`, all Heaven levels are forced unlocked.
- The official left arrow remains visible at ascension `0`, but it is disabled via `NClickableControl.Disable()` when the next Heaven step is not unlocked.
