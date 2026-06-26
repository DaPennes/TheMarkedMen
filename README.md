# The Marked Men

**Author:** Dapennes  
**Workshop ID:** 3739467787  
**Supported Version:** RimWorld 1.6  
**License:** All Rights Reserved

---

## Overview

The Marked Men adds a hostile infected faction to RimWorld, built around fast outbreaks, escalating raids, random pressure events, corpse reanimation, and colony survival pressure. The Marked Virus spreads through close contact, infected blood exposure, contaminated food, and combat. Most victims progress quickly into violent infection; rare survivors can develop immunity. The faction uses weapons, coordinates through scouts, warbands, hordes, and ambushes, and punishes wounded or isolated colonists. Includes a custom storyteller, "The Marked Man," tuned for outbreak survival.

---

## Features

### The Marked Virus

A highly contagious pathogen that spreads through multiple vectors:

- **Close contact** with infected individuals
- **Blood exposure** from combat or butchering infected corpses
- **Contaminated food** from infected sources
- **Corpse contamination** from decaying infected remains
- **Assault transmission** from melee attacks by the infected
- **RJW integration** (optional, auto-detected)

Infection progression:

1. **Exposure** -- pawns are exposed through any of the transmission vectors
2. **Progression** -- the virus progresses through stages, visible as a facial rash tattoo
3. **Resolution** -- most victims transform into hostile infected; rare survivors develop immunity
4. **Immunity** -- immune pawns are permanently protected and cannot be reinfected

Protection factors:

- Warcaskets, sealed armor, vacsuits, and gas masks reduce or block exposure
- The Marked Virus Resistance stat provides additional protection
- Immunity can be acquired through surviving infection or the Marked One xenotype

### Infected Faction

The infected are organized into a functional faction with hierarchy and tactics:

- **Scouts** -- fast, light units that probe defenses
- **Hunters** -- track and pursue fleeing colonists
- **Shooters** -- ranged attackers
- **Raiders** -- standard melee infantry
- **Soldiers** -- armored and coordinated
- **Brutes** -- heavy melee units
- **Pyromaniacs** -- incendiary attackers
- **Alphas** -- command units that coordinate nearby infected
- **Warlords** -- high-threat leaders
- **Marked Men** -- unique powerful infected with special abilities

### Faction Behavior

- Infected retain weapon use, speech, door operation, cover-seeking, and group tactics
- Alphas coordinate nearby infected through sound, gesture, and remembered command structures
- Infected pursue fleeing colonists aggressively
- Wounded or isolated colonists are prioritized targets
- Corpses of infected can reanimate if not properly disposed of
- Infected raids escalate in size and composition over time

### Raid System

| Raid Type | Description |
|-----------|-------------|
| **Probe** | Small scouting force, tests defenses |
| **Caravan Ambush** | Attacks caravans on the world map |
| **Raid** | Standard assault force |
| **Horde** | Large overwhelming force |
| **Infected Survivor** | An infected survivor wanders in |

Raids escalate based on:

- Number of past raids (escalation rate scales per raid)
- Colony wealth and population
- Days elapsed
- Storyteller difficulty

### Custom Storyteller: The Marked Man

"The outbreak given intent" -- a hostile storyteller that replaces Randy Random for outbreak survival scenarios.

Characteristics:

- Very aggressive threat scaling
- Population intent starts at 15.0 (0 colonists), drops to 0.0 at 15 colonists
- Points factor ramps from 1.0 (day 2) to 7.0 (day 150)
- Adaptation range of -180 to +180 days
- 8-day grace period before threats begin
- Frequent diseases and quests
- Rare traders, visitors, and travelers
- Very rare orbital traders
- Custom threat cycle comps with configurable MTB and points factors

### Predator Intelligence Framework

The infected use an AI system for tracking and pursuing prey:

- **Memory Grid** -- tracks scent trails, noise sources, and memory locations on the map
- **Scent tracking** -- infected follow scent trails left by moving pawns
- **Noise detection** -- gunfire, explosions, and construction attract infected
- **Pursuit state machine** -- infected switch between wandering, investigating, pursuing, and flanking states
- **Interception and flanking** -- infected coordinate to cut off fleeing colonists
- **Debug overlay** -- visualize scent (red dots), noise (green dots), and memory (blue dots) on the map

### Dormant Infection System (Lost Survivor)

A colonist with a hidden dormant infection arrives at the colony:

1. A lost survivor appears at the map edge
2. A choice letter appears asking to accept or reject them
3. If accepted, the survivor joins the colony with a hidden dormant infection
4. The dormant infection is invisible in the health tab (no symptoms)
5. After a configurable period (8-30 days by default), or under stress, the infection activates
6. The colonist transforms into a hostile infected, triggering a betrayal event

Transformation triggers:

- **Timer expiry** -- automatic activation after the dormant period
- **Combat damage** -- taking significant damage (>15% max health) can trigger early activation
- **Near-death state** -- critical blood loss or severe illness can trigger activation
- **Witnessing transformations** -- seeing another dormant carrier transform can trigger sympathetic activation
- **Proximity to active Crossed** -- signals from active infected can trigger dormant carriers

On activation:

- The pawn transforms into a hostile infected (preserving name and records)
- Nearby colonists witness the transformation, receiving mood penalties
- A betrayal letter is sent documenting the event
- Social relationships are affected (betrayal thoughts apply)
- Alpha variant chance: 10% (doubled for prisoners); alphas spawn with 1-3 escorts
- Proximity aura: transforming pawn emits panic to nearby colonists (12-cell radius)
- Optional group activation: nearby dormant carriers may activate simultaneously

### Predatory Instincts System

Colonists (especially those with the Marked One xenotype or bloodlust gene) develop predatory instincts:

- **Kill Anticipation** -- senses sharpen when enemies are near; improves combat stats
- **Fresh Kill Satisfaction** -- mood boost from killing enemies
- **Bloodthirsty Craving** -- mood penalty if too much time passes without combat
- **Overwhelming Bloodlust** -- combat high during intense battles
- **Predator Patience** -- waiting and stalking behavior
- **Predator AI** -- infected may stalk, wait at chokepoints, and ambush colonists

---

## Settings

All settings are configurable in the mod settings menu, organized into collapsible categories:

### Infection Settings
- Enable/disable infection
- Exposure chances for each transmission vector
- Armor protection settings (warcaskets, vacsuits, gas masks, sealed armor)

### Raid Settings
- Frequency multiplier (0-5x)
- First raid day
- Points multiplier
- Minimum and maximum raid points
- Escalation rate per raid

### Pawn Composition
- Individual weight sliders for each infected type
- Horde and probe min/max size settings

### Dormant Mark Settings
- Enable/disable lost survivor incidents
- Trigger multiplier
- Alpha chance
- Group variant chance
- Minimum and maximum dormant days

### Performance
- Tick interval (default 120)
- Maximum AI ticks per frame

### Debug
- Debug overlay toggle
- Verbose compatibility logging

### Presets
- Save and load setting presets
- Custom preset naming

---

## Compatibility

### Required
- **Harmony** (brrainz.harmony) -- core dependency

### Optional
- **RJW** -- auto-detected with optional integration
- **RocketMan** (krkr.rocketman) -- performance optimization
- **Performance Fish** (taranchuk.performancefish) -- performance optimization
- **Vanilla Factions Expanded** series -- load order compatibility
- **Ancient Urban Ruins** (XMB.AncientUrbanRuins.MO) -- map compatibility
- **World Builder** (ferny.worldbuilder) -- compatibility patch

### Load Order

The mod should load after:
- Harmony and Prepatcher
- All Vanilla Expanded modules
- All official DLCs (Royalty, Ideology, Biotech, Anomaly)
- Combat Update
- RocketMan and Performance Fish

### Known Interactions
- Warcaskets and sealed armor provide infection protection
- Gas masks filter airborne exposure
- Vacsuits block all exposure while sealed
- The Marked One xenotype grants immunity to the virus

---

## Installation

### Steam Workshop
1. Subscribe to the mod on Steam Workshop (ID: 3739467787)
2. Enable the mod in the RimWorld mod menu
3. Ensure Harmony is installed and loads before The Marked Men

### Manual Installation
1. Download the latest release from the GitHub repository
2. Extract to `RimWorld/Mods/The Marked Men/`
3. Enable the mod in the RimWorld mod menu

### Recommended Starting Scenario
The mod includes a custom scenario (The Marked Men) that sets up a balanced outbreak survival game with the Marked Man storyteller. Select it when starting a new colony for the intended experience.

---

## Technical Details

### Def Structure

| Def Type | Count |
|----------|-------|
| HediffDef | 21 |
| ThoughtDef | 12 |
| PawnKindDef | 11 |
| IncidentDef | 8 |
| TattooDef | 5 |
| InteractionDef | 4 |
| GeneDef | 2 |
| ThingSetMakerDef | 2 |
| ScenPartDef | 2 |
| RulePackDef | 2 |
| StorytellerDef | 1 |
| FactionDef | 1 |
| XenotypeDef | 1 |
| NeedDef | 1 |
| StatDef | 1 |
| LetterDef | 1 |
| HediffGiverSetDef | 1 |

### Performance

- AI updates run on a configurable tick interval (default: every 120 ticks)
- Memory grid updates are staggered across frames
- Infection checks are optimized to avoid per-tick scanning
- Max AI ticks per frame can be limited in settings

### Save Compatibility

- All hediffs and comps implement proper ExposeData serialization
- Settings are versioned (current version: 11) with upgrade paths
- World pawn tracking for infected and dormant carriers
- Quest and signal-based event handling for deferred actions

---

## Credits

- **Author:** Dapennes
- **Inspiration:** The Crossed comic series by Garth Ennis and Jacen Burrows
- **Dependencies:** Harmony (brrainz.harmony)

---

## Links

- **Steam Workshop:** steam://url/CommunityFilePage/3739467787
- **GitHub:** https://github.com/DaPennes/TheMarkedMen
