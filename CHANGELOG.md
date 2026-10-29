# Changelog

## Unreleased

### Added
- All infected enemy types can now appear in raids: civilians, scouts, hunters, shooters, raiders, soldiers, brutes, pyromaniacs, alphas, warlords, and Marked Men.
- A countdown in the corner showing how long until the next Marked Men raid.
- A warning letter about a day before a raid, with the threat level and estimated arrival time.
- A setting for how likely colonists are to become infected after lingering near infected corpses.
- Marked Men now break through walls and hunt colonists during raids. They prefer the wounded, the isolated, and anyone trying to run.
- Large raids now begin with a coordinated breach and switch to a full hunt once they get inside.

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
- The game now checks infected colonists less often, reducing lag in large colonies.

### Removed
- Duplicate Enemy Mix sliders in the settings menu.
- An old unused raid countdown display.
- Leftover background work that was no longer needed.

### Fixed
- Fixed saved games keeping most raid types switched off after updating.
- Fixed the maximum alpha setting not actually limiting alphas.
- Fixed several presets accidentally switching off most infected types.
- Fixed infection from infected corpses never happening.
- Fixed a scheduled raid being skipped for good when it failed to start; it now tries again the next day.
- Fixed caravan ambushes being handled twice.
- Fixed raid warnings always appearing in English instead of the selected language.
- Fixed the countdown and warning letter sometimes showing different times.
- Fixed raids sometimes splitting into small groups instead of attacking together.
- Fixed caravan ambushes losing their leaders and coordination.
