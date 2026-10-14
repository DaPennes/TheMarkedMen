# The Marked Men: Architecture Analysis & Root Cause Assessment

## Overview
- **Mod Name**: The Marked Men
- **RimWorld Versions**: 1.6, 1.7
- **Harmony**: Required dependency, v2.x
- **Source Files**: 53 C# classes
- **Def Files**: 26 XML definition files
- **Primary Author**: Dapennes
- **Latest Commit**: CA_MarkedTurned implementation (shambler-style transformation preserving original identity and gear)

---

## 1. INFECTION & HEDIFF SYSTEM

### Current Architecture
**Primary Def**: `CA_CrossVirus` (TheMarkedMenHediffs.xml)
- **Classes**: 5 progression stages (0 → 1.0 severity)
  - **Stage 1** (0.0): "Rapid incubation" — painFactor 0.5, MoveSpeed 1.02, ImmunityGainSpeed unaffected
  - **Stage 2** (0.2): "Active symptoms" — painFactor 0.3, ImmunityGainSpeed 0.8, Consciousness -0.05
  - **Stage 3** (0.45): "Neurological fever" — painFactor 0.1, MoveSpeed 1.04, SocialImpact 0.35
  - **Stage 4** (0.72): "Violent psychosis" — painFactor 0, MeleeCooldownFactor 0.76, IncomingDamageFactor 0.9
  - **Stage 5** (0.95): "Irreversible mark" — painFactor 0, MeleeCooldownFactor 0.52, pctAllThoughtNullification 1.0

**HediffComps**:
- `HediffCompProperties_CrossVirus` — Terminal outcome resolution, incubation, immunity generation
  - Tracks `transformed`, `incubationResolved`, `terminalOutcome` (enum: Immunity, Death, Transformation)
  - Severity progresses via `HediffComp_CrossVirus.CompPostTick()`
  - Terminal outcome resolved at 0.95 severity

**Immunity**:
- `CA_CrossVirusImmunity` hediff applied on survival
- Starter lineage can grant breakthrough resistance (rare)

**Recent Addition**: 
- `CA_MarkedTurned` hediff (newly implemented) — permanent marker for virus-transformed original-kind pawns
  - Retains original pawnkind, name, traits, gear
  - Applies mindless properties: painFactor 0, blocksMentalBreaks, pctAllThoughtNullification 1.0
  - Configured via `HediffComp_MarkedTurned` with per-pawn strength modifiers

### Root Causes for Goal 1 (Infection Rebalance)
1. **Progression is unclear**: Stages don't feel distinct; stat modifiers are incremental, not escalating.
2. **Immunity gain is opaque**: `ImmunityGainSpeed 0.8` at stage 2 but no clear indication of how fast immunity develops.
3. **Terminal outcome distribution is fixed in XML**: `terminalTransformationChance` and `terminalDeathWeight` hardcoded; no dynamic scaling with threat or player power.
4. **CA_MarkedTurned integration incomplete**: New hediff exists but settings/UI for configurable strength are not yet added.
5. **Counterplay is limited**: Treatment benefits unclear; medicine effectiveness not well tuned against progression speed.

---

## 2. INCIDENTS & EVENTS SYSTEM

### Current Architecture

**IncidentDefs** (TheMarkedMenIncidents.xml):
| Def Name | Worker | Base Chance | Category | Target |
|----------|--------|------------|----------|--------|
| CA_CrossedRaid | IncidentWorker_CrossedRaid | 0 (storyteller) | ThreatBig | Map_PlayerHome |
| CA_CrossedProbe | IncidentWorker_CrossedProbe | 0.45 | ThreatSmall | Map_PlayerHome |
| CA_CrossedDownedSurvivor | IncidentWorker_CrossedDownedSurvivor | 0.30 | ThreatSmall | Map_PlayerHome |
| CA_CrossedCaravanAmbush | IncidentWorker_CrossedCaravanAmbush | 0 (storyteller) | ThreatSmall | Caravan |
| CA_CrossedHorde | IncidentWorker_CrossedHorde | 0 (storyteller) | ThreatBig | Map_PlayerHome |

**IncidentWorker Implementations** (TheMarkedMen.cs, ~2000 LOC):
- `IncidentWorker_CrossedRaid` (extends IncidentWorker_RaidEnemy)
  - Uses standard RaidStrategyDef ("ImmediateAssault", "Siege", etc.)
  - Spawns flat number of infected from a single entry point
  - No wave mechanics, no escalation during raid
  
- `IncidentWorker_CrossedCaravanAmbush` (extends IncidentWorker_Ambush)
  - **BUG**: Does not check if caravan is vehicle-based
  - Spawns ambush pawns but does not handle vehicle caravans differently
  - No vehicle spawn logic

- `IncidentWorker_CrossedHorde`
  - Spawns large pack (configurable min/max size)
  - Single-phase assault, no waves

- `IncidentWorker_CrossedProbe`
  - Small scouting attack, mostly working as intended

- `IncidentWorker_CrossedDownedSurvivor`
  - Spawns downed infected on map; player must rescue or execute
  - Mostly working as intended

**Raid Timer UI**:
- Stored in `TheMarkedMenGameComponent` (WorldComponent subclass)
- Updated via `Update()` method
- **BUG**: Only visible on hover over raid timer label; not always-visible as promised

**Settings Integration**:
- `scheduledWarbandsEnabled`, `scheduledHordesEnabled`, `scoutingProbesEnabled` (bool toggles)
- `warbandFrequencyMultiplier`, `hordeFrequencyMultiplier`, `probeFrequencyMultiplier` (float multipliers)
- `firstMarkedRaidDay` (int days)
- Storyteller integration via `StorytellerDef` (TheMarkedManStoryteller.xml)

### Root Causes for Goal 2 (Incident Fixes)
1. **Caravan ambush is incomplete**: Does not detect vehicle caravans; treats all caravans as foot caravans.
2. **Raid timer is hidden**: Only visible on tooltip hover, not persistent HUD element.
3. **Raids are flat**: No wave mechanics, escalation, or dynamic difficulty.
4. **No telegraphing**: Ambush triggers instantly with no warning or buildup.
5. **Spawn logic is basic**: Single entry point, single spawn phase, no flanking or surprise tactics.

---

## 3. VEHICLE FRAMEWORK COMPATIBILITY

### Current State
- **No existing Vehicle Framework compatibility** in the mod.
- Vehicle Framework is a popular mod that adds vehicles (cars, tanks, etc.) to caravans and raids.
- Vehicles are driven across the world map and arrive as units in raids/ambushes.

### API Exposure (from decompiled Vehicles source, as reference):
- **CaravanPawn / VehiclePawn**: Vehicles can be added to caravans like pawns.
- **Vehicles spawn in raids** via custom IncidentWorker integrations; they arrive as "vehicles" not "pawns".
- **Vehicles have drivers** and can contain passengers.
- **Vehicles have health, fuel, armor**.

### Root Causes for Goal 3 (Vehicle Compatibility)
1. **No Vehicle Framework detection**: Mod has no conditional patches for Vehicle Framework.
2. **Caravan ambush ignores vehicles**: Does not spawn infected to target vehicles.
3. **No infected vehicle spawning**: Infected do not commandeer or spawn with vehicles.
4. **Unclear API**: Vehicle Framework API is not well-documented in mod; would need to reverse-engineer from decompiled source.

---

## 4. RAID TIMER UI

### Current Implementation
- **Component**: `TheMarkedMenGameComponent` (WorldComponent subclass)
- **Storage**: `nextRaidTick` (int field)
- **Update Method**: `Update()` called each tick
- **Display Method**: Tooltip-only (hover over raid countdown label)
- **Persistence**: Saved/loaded via `ExposeData()`

### Root Causes for Goal 4 (Always-Visible Timer)
1. **No persistent HUD element**: Timer is only drawn when hovering, not as a continuous on-screen indicator.
2. **Not integrated into alerts system**: Could use RimWorld's letter/alert system for better visibility.
3. **No scaling for different resolutions**: HUD element would need to be tested at different UI scales.
4. **Unclear how to make it always visible**: May require hooking into `MapInterface.Update()` or creating a custom UI layer.

---

## 5. SETTLEMENT RAID DYNAMICS

### Current Behavior
- **Single wave**: All infected spawn at once from one entry point.
- **Flat strategy**: Uses standard RaidStrategyDef ("ImmediateAssault"); no custom raid behavior.
- **No escalation during raid**: Infected do not call reinforcements or adapt strategy mid-raid.
- **No siege phase**: No hold-out or multi-phase raid mechanics.
- **No telegraphing**: Letter is sent, but no countdown or escalating threat indicators.

### Root Causes for Goal 5 (Dynamic Raids)
1. **IncidentWorker_CrossedRaid is too simple**: Delegates to vanilla RaidEnemy logic; no custom multi-phase spawning.
2. **No LordJob customization**: Could use custom LordJob to orchestrate wave spawning, retreat, flanking.
3. **No raid escalation tied to colony state**: Raids don't scale with wealth, population, or infection spread.
4. **Settings are raid-frequency based only**: No in-raid difficulty settings (wave size, special types, escalation speed).

---

## 6. INFECTED AI SYSTEM

### Current Architecture

**Core AI Classes**:
- `CrossedTacticalAI` (static utility)
  - `TryIssueTacticalJob(Pawn pawn)` — Main entry point for issuing infected jobs
  - `FindBestTarget(Pawn pawn)` — Caches target finding; updates every 250 ticks
  - Infrastructure/medical/food/defensive target prioritization
  - Infighting detection and prevention
  - Rampage state handling
  
- `MarkedMenAIManager` (MapComponent)
  - Manages infected state per map
  - Throttles per-tick AI checks
  - Tracks infected pawns and their state
  
- **No dedicated ThinkTree**: Infected use vanilla pawn behavior trees with tactical job injection.
- **No dedicated LordJob**: Infected use vanilla LordJobs (e.g., "Assault", "Siege").

**JobGiver Integration**:
- Infected use vanilla `JobGiver_AIDefendPoint`, `JobGiver_Manhunter`, etc.
- Tactical AI injects jobs via `CrossedTacticalAI.TryIssueJob()` in periodic updates.

**Behavior Components**:
- `HediffComp_MarkedHunt` — Triggers hunt behavior on certain hediff application (partial implementation)
- `HediffComp_PackCoordination` — Pack coordination via staged hediff (partial implementation)
- `Hediff_MarkedAnticipation` — Anticipation mechanic for kills (tracking system incomplete)

### Root Causes for Goal 6 (Smart Hunter AI)
1. **No dedicated ThinkTree**: Infected are just vanilla pawns with injected jobs; not a coherent behavior system.
2. **Target finding is basic**: Finds wounded/downed first, but no tracking of last-seen position or noise-based hunting.
3. **No cover/ambush logic**: Infected do not use cover or plan flank attacks; they just rush.
4. **No retreat/regroup**: When outnumbered, infected don't fall back or regroup; they fight to the death.
5. **No pack tactics**: Multiple infected don't coordinate; they just attack independently.
6. **Rampage is disconnected**: Rampage state exists but is not integrated with normal hunting behavior.
7. **MarkedHunt and PackCoordination are incomplete**: Exist as hediff comps but are not fully wired into behavior tree.

---

## 7. MAP EXIT BUG

### Current Symptoms
- Dev-mode spawned infected sometimes wander off the map and despawn.
- Infected without a valid Lord or faction assignment may exit the map.
- Infected spawned via debug actions may not have a valid target or behavior.

### Root Causes for Goal 7 (Fix Exit Bug)
1. **No default Lord assignment**: Dev-mode spawned infected may not be assigned to a Lord; without one, they default to vanilla pawn behavior (which includes exit-map).
2. **ThinkTree allows exit**: Vanilla pawn think trees include "ExitMap" nodes; nothing overrides them for infected.
3. **No fallback behavior**: If infected have no target and no valid job, they wander and can exit.
4. **Faction assignment is optional**: Infected can exist without Crossed faction; vanilla behavior then dominates.
5. **Debug spawning is uncontrolled**: Dev-mode `DebugSpawn` action doesn't wire infected into the mod's AI system.

---

## 8. INTEGRATION POINT: CA_MarkedTurned

The recently implemented `CA_MarkedTurned` hediff affects:
- **Infection progression**: Marked victims skip class/role hediff assignment and keep original pawnkind.
- **AI behavior**: Marked victims should not use predatory systems (bloodlust, anticipation).
- **Raid composition**: Generated Crossed pawn kinds (CA_CrossedCivilian, etc.) remain separate; marked victims are NPCs who joined the faction.
- **Apparel handling**: Marked victims retain original gear; no generated equipment replacement.

**Integration for Goals 1–7**:
- Goal 1 (Infection): CA_MarkedTurned is a permanent marker; rebalanced infection should keep it intact.
- Goal 2 (Incidents): Marked victims may participate in raids/ambushes as infected units.
- Goal 3 (Vehicle compat): Marked victims could ride vehicles if vehicle compatibility is built.
- Goal 6 (AI): Marked victims should use hunter AI (no predatory behavior, shambler-like).

---

## Summary Table: Root Causes by Goal

| Goal | Root Causes | Priority |
|------|------------|----------|
| 1. Infection Rebalance | Unclear stage progression, opaque immunity gain, fixed terminal outcomes, incomplete CA_MarkedTurned integration, limited counterplay | HIGH |
| 2. Incident Fixes | Caravan ambush ignores vehicles, timer UI hidden, raids are flat/single-phase, no telegraphing | HIGH |
| 3. Vehicle Compat | No Vehicle Framework detection, caravan ambush incomplete, no infected vehicle spawning, unclear API | MEDIUM |
| 4. Timer UI | No persistent HUD, not integrated into alerts, no resolution scaling | MEDIUM |
| 5. Raid Dynamics | IncidentWorker too simple, no LordJob customization, no escalation, frequency-only settings | HIGH |
| 6. Hunter AI | No dedicated ThinkTree, basic target finding, no cover/ambush, no retreat, no pack tactics, incomplete hediff comps | HIGH |
| 7. Map Exit Bug | No default Lord assignment, vanilla ThinkTree allows exit, no fallback behavior, uncontrolled debug spawning | MEDIUM |

---

## Recommended Implementation Order

1. **Goal 6 (Hunter AI)** first — Build solid AI foundation before raids use it (Goal 5).
2. **Goal 1 (Infection)** in parallel — Rebalance hediff stages; integrate CA_MarkedTurned.
3. **Goal 5 (Raid Dynamics)** — Use new AI in custom LordJob; add wave mechanics.
4. **Goal 7 (Exit Bug)** — Fix by ensuring Lord assignment in AI manager.
5. **Goal 2 (Incident Fixes)** — Fix caravan ambush and timer UI.
6. **Goal 3 (Vehicle Compat)** — Build soft-dependent patch; test with Vehicle Framework.
7. **Goal 4 (Timer UI)** — Add persistent HUD element (last, lowest priority).

This order ensures dependencies are resolved and allows testing of each layer in isolation.
