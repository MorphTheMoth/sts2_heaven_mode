# NTopBar

Namespace: `MegaCrit.Sts2.Core.Nodes.CommonUi`

## Feature: sync Heaven fire visuals to the in-run top-left ascension icon

Relevant vanilla method:

- `NTopBar.Initialize(IRunState runState)`

## Vanilla behavior

During in-run top bar initialization, vanilla configures the left-top ascension icon through:

- private field `%AscensionIcon`
- private field `_ascensionHsv`

Vanilla then applies:

- red fire for singleplayer ascension
- blue fire for multiplayer ascension

## Implementation note in this repo

Use a Harmony postfix on `NTopBar.Initialize(...)`.

- If Heaven is active, reuse the same black-purple animated fire direction as the character-select Heaven icon.
- If Heaven is active, overwrite the top-bar ascension number so it shows the Heaven level instead of the effective vanilla ascension `10`.
- Animate the top-bar `%AscensionIcon` shader by driving `_ascensionHsv` parameters `h` and `v`.
- Apply the same dark outline treatment to the ascension label.
- Spawn small purple ember particles as child nodes under the ascension icon so the in-run icon visually matches the Heaven selection screen.
- If Heaven is not active, leave the vanilla red/blue fire behavior intact.
