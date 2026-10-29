# Changelog

## Unreleased

### Added
- All infected enemy types can now appear in raids: civilians, scouts, hunters, shooters, raiders, soldiers, brutes, pyromaniacs, alphas, warlords, and Marked Men.
- A countdown showing how long until the next Marked Men raid.
- A warning letter about a day before a raid, with the threat level and estimated arrival time.
- A setting for how likely colonists are to become infected after lingering near infected corpses.
- Marked Men can now break through walls and hunt colonists during raids. They prefer the wounded, the isolated, and anyone trying to run.
- Large raids now begin with a coordinated breach and switch to a full hunt once they are inside.

### Removed
- Duplicate enemy mix sliders in the settings menu.
- An old unused raid countdown display.
- Old background work that was no longer needed.

### Changed
- Raids now become more varied as they grow stronger: early attacks are mostly civilians, while later attacks bring more specialists and elites.
- The difficulty presets now keep the full enemy mix available instead of switching most types off.
- Elite enemies are now limited per raid: the alpha limit works, and a raid can include at most one warlord and one extra Marked Man.
- Raid letters now list the full enemy mix instead of only counting civilians.
- The countdown turns red when a raid is close.
- Raid warnings now appear once per raid instead of repeating.
- Lingering near infected corpses can now infect colonists. The longer a colonist stays close, the higher the risk.
- The Outbreak preset now makes infected corpses more dangerous.
- Scheduled raids and hordes continue even when there are no infected corpses on the map.
- Infected colonists are checked less often, so large colonies run more smoothly.

### Fixed
- Fixed older saves keeping most raid types switched off after updating.
- Fixed the maximum alpha setting not actually limiting alphas.
- Fixed several presets accidentally switching off most infected types.
- Fixed infected corpses never spreading infection.
- Fixed a scheduled raid being skipped for good when it failed to start; it now tries again the next day.
- Fixed caravan ambushes sometimes happening twice.
- Fixed raid warnings always appearing in English instead of the selected language.
- Fixed the countdown and warning letter sometimes showing different times.
- Fixed raids sometimes splitting into small groups instead of attacking together.
- Fixed caravan ambushes losing their leaders and coordination.
- Fixed large raids sometimes waiting outside after loading a save instead of continuing the attack.
