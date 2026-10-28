# Changelog

## Unreleased

### Added
- A countdown in the corner showing how long until the next Marked Men raid.
- A warning letter about a day before a raid, with the threat level and estimated arrival time.
- A setting for how likely colonists are to become infected after lingering near infected corpses.

### Changed
- The countdown turns red when a raid is close.
- Raid warnings now appear once per raid instead of repeating.
- Lingering near infected corpses can now infect colonists. The longer a colonist stays close, the higher the risk.
- The Outbreak preset now makes infected corpses more dangerous.
- Scheduled raids and hordes continue even when there are no infected corpses on the map.
- The game now checks infected colonists less often, reducing lag in large colonies.

### Removed
- Removed an old unused raid countdown display.
- Removed leftover background work that was no longer needed.

### Fixed
- Fixed infection from infected corpses never happening.
- Fixed a scheduled raid being skipped for good when it failed to start; it now tries again the next day.
- Fixed caravan ambushes being handled twice.
- Fixed raid warnings always appearing in English instead of the selected language.
- Fixed the countdown and warning letter sometimes showing different times.
