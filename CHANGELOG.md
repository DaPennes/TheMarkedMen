# Changelog

## Unreleased

### Added
- All infected enemy types can now appear in raids: civilians, scouts, hunters, shooters, raiders, soldiers, brutes, pyromaniacs, alphas, warlords, and Marked Men.
- Alphas, warlords, and Marked Men now keep their elite toughness and combat bonuses.
- A countdown showing how long until the next Marked Men raid.
- A warning letter about a day before a raid, with the threat level and estimated arrival time.
- Marked Men can now break through walls and hunt colonists during raids. They prefer the wounded, the isolated, and anyone trying to run.
- Large raids now begin with a coordinated breach and switch to a full hunt once they are inside.

### Removed
- Infection from infected corpses and contaminated food.
- Duplicate enemy mix sliders in the settings menu.
- An old unused raid countdown display.
- Old background work that was no longer needed.

### Changed
- Raids now become more varied as they grow stronger: early attacks are mostly civilians, while later attacks bring more specialists and elites.
- The difficulty presets now keep the full enemy mix available instead of switching most types off.
- Raids now grow into large assaults based on colony wealth and difficulty, instead of stopping at a handful of attackers.
- Elite enemies are now limited per raid: the alpha limit works, and warlord and Marked Man counts scale with raid size.
- Raid letters now list the full enemy mix instead of only counting civilians.
- The countdown turns red when a raid is close.
- Raid warnings now appear once per raid instead of repeating.
- Scheduled raids and hordes keep their own timing and continue without interruptions.
- Infected colonists are checked less often, so large colonies run more smoothly.

### Fixed
- Fixed older saves keeping most raid types switched off after updating.
- Fixed the maximum alpha setting not actually limiting alphas.
- Fixed several presets accidentally switching off most infected types.
- Fixed scheduled raids sometimes spawning no attackers when the countdown ended.
- Fixed a scheduled raid being skipped for good when it failed to start; it now tries again the next day.
- Fixed caravan ambushes sometimes happening twice.
- Fixed raid warnings always appearing in English instead of the selected language.
- Fixed the countdown and warning letter sometimes showing different times.
- Fixed raids sometimes splitting into small groups instead of attacking together.
- Fixed caravan ambushes losing their leaders and coordination.
- Fixed large raids sometimes waiting outside after loading a save instead of continuing the attack.
