# PlatformUtil

Namespace: `MegaCrit.Sts2.Core.Platform`

## Feature: override Steam in-run ascension text for Heaven runs

Relevant vanilla flow:

- `RunManager.UpdateRichPresence()`
- `PlatformUtil.SetRichPresence("IN_RUN", ...)`
- `PlatformUtil.SetRichPresenceValue("Ascension", value)`
- `SteamPlatformUtilStrategy.SetRichPresence(...)`

## Implementation note in this repo

Steam rich presence for in-run state is updated from `RunManager.UpdateRichPresence()`.

- Vanilla writes:
  - `steam_display = #IN_RUN`
  - `Character = ...`
  - `Act = ...`
  - `Ascension = this.State.AscensionLevel.ToString()`
- Repo behavior adds a postfix on `RunManager.UpdateRichPresence()`.
- If `HeavenState.SelectedOption >= 1`, the mod overwrites rich presence field:
  - `Ascension = "{level} - 天堂"`
  - because the Steam `IN_RUN` template still prepends the vanilla `进阶`, the final display becomes `进阶X - 天堂`

This is the only client-side field override needed for the in-run Steam status text.
