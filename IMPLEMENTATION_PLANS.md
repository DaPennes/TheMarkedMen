# The Marked Men: Detailed Implementation Plans

## Design Decisions Locked
1. **Raid Waves**: Reinforcement waves (arrive if 1st wave losing or after time expires)
2. **AI Behavior**: All of the above (cover + terrain tactics + planned ambushes)
3. **Vehicle Integration**: Both (spawn with vehicles + spawn to target vehicles)
4. **Infection Rebalance**: 3–4 core stages for clearer progression
5. **CA_MarkedTurned**: Both (fixed base + optional settings multiplier)
6. **Caravan Ambush**: Spawn and immediately attack

---

# GOAL 6: Rework Infected AI into Smart Hunter

## Overview
Replace blind rushing with intelligent behavior: tracking, hunting, cover use, pack coordination, target prioritization, and retreat/regroup when outgunned. Use cached, throttled checks to maintain performance.

## Proposed Architecture

### 1. New ThinkTree for Infected
**File**: `Source/TheMarkedMen/ThinkTree_CrossedHunter.cs` (new)

Create a dedicated ThinkTree that infected use instead of vanilla:
```
Root: DutyThink
  ├─ If(HasLord && LordJob is valid)
  │   └─ Use LordJob thinking
  ├─ Else (rogue/dev-spawned)
  │   └─ HunterThinkRoot
  │       ├─ JobGiver_FindNearestTarget (cached, 250-tick interval)
  │       ├─ JobGiver_HuntWounded (prioritize wounded)
  │       ├─ JobGiver_FlankTarget (if in pack)
  │       ├─ JobGiver_TakeCover (if under fire)
  │       └─ JobGiver_RetreatIfOutgunned (fallback)
```

**Key Classes to Add**:
- `ThinkTree_CrossedHunter` — Entry point (DefName: `CA_HunterThinkTree`)
- `JobGiver_FindNearestTarget` — Tracks last-seen position, uses noise
- `JobGiver_HuntWounded` — Prioritize wounded/downed colonists
- `JobGiver_FlankTarget` — Coordinate multi-infected attacks
- `JobGiver_TakeCover` — Move to cover when under fire
- `JobGiver_RetreatIfOutgunned` — Regroup or withdraw if outnumbered

### 2. Cached Target Finding
**Enhancement to** `CrossedTacticalAI.cs`:

Replace basic target finding with:
- **Last-Seen Tracking**: Record position of last target before LOS breaks; hunt that position
- **Noise-Based Detection**: Detect loud weapons, explosions; investigate source
- **Threat Assessment**: Calculate "outgunned factor" (enemy firepower vs infected firepower in radius)
- **Cache Duration**: 250 ticks for normal target, 500 ticks for last-seen position

**New Methods**:
```csharp
private static Dictionary<int, (Pawn target, IntVec3 lastSeenPos, int lastSeenTick)> targetMemory;

public static Pawn FindBestTargetWithMemory(Pawn pawn)
{
    // Check memory first; hunt last-seen position if target lost
    // If no target, search for new one (every 250 ticks)
}

public static bool CanReachTarget(Pawn pawn, Pawn target) 
{
    // Quick pathfinding check; cache result
}

public static float CalculateOutgunnedFactor(Pawn pawn) 
{
    // Count nearby enemies with ranged weapons; assess threat
    // Return ratio: >1.0 = outnumbered, <1.0 = superior
}
```

### 3. Cover Usage
**New File**: `Source/TheMarkedMen/CoverUtility.cs`

Infected should move to cover when under fire:
```csharp
public static IntVec3 FindNearestCover(Pawn pawn, Pawn threatFrom)
{
    // Search for buildings, walls, furniture that block LOS to threatFrom
    // Cache results; reuse every 500 ticks
    // Prefer cover that allows counter-fire
}

public static bool IsCovered(Pawn pawn, Pawn threat)
{
    // Check if current cell blocks LOS to threat
}
```

### 4. Pack Coordination
**Enhancement to** `HediffComp_PackCoordination.cs`:

Currently incomplete; finish it:
- **Detect nearby pack members** (infected within 20 cells)
- **Coordinate targets**: All attack the same target (no split fire)
- **Flank bonus**: If >2 infected attacking same target from different angles, grant melee accuracy bonus

**New Methods**:
```csharp
public void TryCoordinatePackAttack(Pawn pawn)
{
    // Find nearby infected allies
    // If targeting same enemy, grant bonuses
    // If targeting different enemies, attempt to consolidate
}

private float GetFlankBonusForTarget(Pawn pawn, Pawn target)
{
    // Check how many other infected can see/attack target
    // +5% melee accuracy per flank angle (max +15%)
}
```

### 5. Retreat & Regroup Logic
**New File**: `Source/TheMarkedMen/RetreatUtility.cs`

When outnumbered (outgunned factor > 1.5):
- **Retreat**: Move away from threats, seek cover
- **Regroup**: Wait for reinforcements (in raids with waves)
- **Fallback Position**: Move to last safe area or map edge

```csharp
public static bool ShouldRetreat(Pawn pawn)
{
    if (outgunnedFactor > 1.5 && healthPercent < 0.6)
        return true;
    return false;
}

public static IntVec3 FindRetreatPosition(Pawn pawn)
{
    // Move away from threats; prefer cover or high ground
}
```

### 6. Dev-Spawned Infected Fix
**Enhancement to** `CrossedUtility.cs`:

Auto-assign Lord to dev-spawned infected:
```csharp
public static void EnsureInfectedHasValidLord(Pawn pawn)
{
    if (pawn.Faction != CADefOf.CrossedFaction)
        pawn.SetFaction(Component?.EnsureCrossedFaction());
    
    if (pawn.GetLord() == null)
    {
        // Create a temporary Lord with custom LordJob
        LordMaker.MakeNewLord(pawn.Faction, new LordJob_AssaultColony(pawn.Map, false), pawn.Map, new List<Pawn> { pawn });
    }
}
```

## Files to Modify/Create
| File | Type | Changes |
|------|------|---------|
| `ThinkTree_CrossedHunter.cs` | NEW | Complete ThinkTree architecture |
| `JobGiver_FindNearestTarget.cs` | NEW | Target discovery with memory |
| `JobGiver_HuntWounded.cs` | NEW | Prioritize wounded colonists |
| `JobGiver_FlankTarget.cs` | NEW | Pack coordination |
| `JobGiver_TakeCover.cs` | NEW | Cover usage |
| `JobGiver_RetreatIfOutgunned.cs` | NEW | Retreat logic |
| `CoverUtility.cs` | NEW | Cover finding and LOS checks |
| `RetreatUtility.cs` | NEW | Retreat positioning |
| `CrossedTacticalAI.cs` | MODIFY | Add target memory, noise detection, threat assessment |
| `HediffComp_PackCoordination.cs` | MODIFY | Complete pack coordination logic |
| `CrossedUtility.cs` | MODIFY | Add `EnsureInfectedHasValidLord()` |
| `TheMarkedMen.cs` | MODIFY | Register new JobGivers in harmony patches |
| `TheMarkedMenPawnKinds.xml` | MODIFY | Update all infected kinds to use `CA_HunterThinkTree` |

## Testing Checklist (Goal 6)
- [ ] Dev-spawned infected no longer walk off map
- [ ] Infected track last-seen position when target LOS breaks
- [ ] Infected take cover when under fire from 3+ weapons
- [ ] Packs of infected (3+) focus fire on single target
- [ ] Infected retreat when outnumbered 2:1 and low health
- [ ] Infected pursue downed/wounded colonists preferentially
- [ ] AI performance remains acceptable (no tick lag spike)
- [ ] Existing saves load without errors

---

# GOAL 1: Rework Infection HediffDefs and Stats

## Overview
Rebalance infection into 3–4 core stages with distinct, escalating effects. Clarify immunity gain. Add CA_MarkedTurned strength settings. Ensure counterplay (treatment, medicine) is effective.

## Proposed Changes

### 1. New Infection Stages (TheMarkedMenHediffs.xml)

**Stage 1** (0.0–0.35): "Early Infection"
- **Label**: "Early Infection"
- **Duration**: 1–2 in-game days typical
- **Effects**: 
  - `painFactor: 0.3`
  - `MoveSpeed: 1.0` (normal)
  - `RestRateMultiplier: 0.9` (sleep interrupted)
  - `statOffset MentalBreakThreshold: -0.30`
- **Signal**: Visible symptom (rash), patient is mobile but uncomfortable
- **Counterplay**: Tendable; antibiotics reduce severity by 0.05/day

**Stage 2** (0.35–0.70): "Active Infection"
- **Label**: "Active Infection"
- **Duration**: 2–4 days typical
- **Effects**:
  - `painFactor: 0.15`
  - `MoveSpeed: 1.05` (accelerated, fever-driven)
  - `MeleeCooldownFactor: 0.88` (agitated, faster strikes)
  - `ImmunityGainSpeed: 0.75` (harder to fight off)
  - `RestRateMultiplier: 0.7` (insomnia)
  - `statOffset MentalBreakThreshold: -0.45`
  - `capMods Consciousness: -0.10` (disorientation)
- **Signal**: Fever, aggression, visible decline
- **Counterplay**: Medicine reduces severity by 0.08/day; rest helps

**Stage 3** (0.70–0.95): "Terminal Infection"
- **Label**: "Terminal Infection"
- **Duration**: 1–2 days typical
- **Effects**:
  - `painFactor: 0` (too far gone to feel pain)
  - `blocksMentalBreaks: true` (mind breaking)
  - `blocksInspirations: true`
  - `MoveSpeed: 1.12` (frenzy)
  - `MeleeCooldownFactor: 0.65` (violent strikes)
  - `IncomingDamageFactor: 0.75` (deadened to injury)
  - `pctAllThoughtNullification: 0.8`
  - `statOffset MentalBreakThreshold: -0.60`
  - `statOffset PainShockThreshold: 0.50`
- **Signal**: Violent behavior, rash spreading, imminent transformation or death
- **Counterplay**: Almost none; focus on containment

**Stage 4** (0.95–1.0): "Transformation Point"
- **Label**: "Transformation Point" (lasts only seconds/minutes before terminal outcome resolves)
- **Effects**: Same as Stage 3 + `pctAllThoughtNullification: 1.0`
- **Outcome Rolls**: 
  - 75% → Transformation (apply CA_MarkedTurned or legacy Crossed kind)
  - 20% → Death
  - 5% → Immunity (rare survivor)

### 2. CA_MarkedTurned Strength Settings
**File**: `TheMarkedMen.cs` (TheMarkedMenSettings class)

Add new fields (version bump to 15):
```csharp
public float markedTurnedMoveSpeedMultiplier = 1.0f;      // Range: 0.8–1.5
public float markedTurnedMeleeAccuracyBonus = 0f;         // Range: 0–10
public float markedTurnedRangedAccuracyBonus = 0f;        // Range: 0–5
public float markedTurnedIncomingDamageFactor = 0.85f;    // Range: 0.5–1.0
public float markedTurnedMeleeCooldownFactor = 0.9f;      // Range: 0.7–1.0
```

**Serialization** (ExposeData):
```csharp
Scribe_Values.Look(ref markedTurnedMoveSpeedMultiplier, "markedTurnedMoveSpeedMultiplier", 1f);
Scribe_Values.Look(ref markedTurnedMeleeAccuracyBonus, "markedTurnedMeleeAccuracyBonus", 0f);
// ... etc
```

**Clamping** (ClampSettings):
```csharp
markedTurnedMoveSpeedMultiplier = Mathf.Clamp(markedTurnedMoveSpeedMultiplier, 0.8f, 1.5f);
markedTurnedMeleeAccuracyBonus = Mathf.Clamp(markedTurnedMeleeAccuracyBonus, 0f, 10f);
// ... etc
```

### 3. Immunity Gain Clarity
**File**: `HediffComp_CrossVirus.cs`

Add comment explaining immunity roll:
```csharp
// Immunity survival chance rolls at terminal outcome (Stage 4 → 0.95 severity)
// Base: 5% default, modified by settings
// If survival rolls: Apply CA_CrossVirusImmunity hediff (permanent protection)
// Severity floor: 0.08 (initial exposure); never goes below
```

Ensure calculation is explicit:
```csharp
private void ResolveTerminalOutcome(Pawn pawn)
{
    // Terminal outcome resolution at 0.95 severity
    float immunityChance = settings.immunitySurvivalChance; // Default 5%
    
    if (Rand.Chance(immunityChance))
    {
        GrantCrossVirusImmunity(pawn);
        return;
    }
    
    // Remaining: transformation or death (75:20 ratio by default)
    // ...
}
```

### 4. Treatment Effectiveness
**File**: `TheMarkedMen.cs` (Settings)

Add clarity to medicine effectiveness:
- Medicine/Antibiotics reduce severity by **0.08 per day** (at Stage 1–2)
- Bed rest reduces severity by **0.05 per day**
- Both stack (together: 0.13 per day reduction)
- At Stage 3+, medicine effect reduced to 0.02 per day (containment only)

(Document in code; actual math is RimWorld built-in)

## Files to Modify
| File | Changes |
|------|---------|
| `TheMarkedMenHediffs.xml` | Rebalance CA_CrossVirus stages (4 → 3–4); rebalance CA_MarkedTurned stages |
| `TheMarkedMen.cs` | Add CA_MarkedTurned strength settings; bump version to 15 |
| `HediffComp_CrossVirus.cs` | Clarify immunity/terminal outcome logic with comments |
| `Languages/English/Keyed/Strings.xml` | Add translation keys for new settings + stage descriptions |

## Testing Checklist (Goal 1)
- [ ] Stage 1 lasts ~1–2 days typically (adjust if too fast/slow)
- [ ] Stage 2 lasts ~2–4 days typically
- [ ] Stage 3 lasts ~1–2 days before terminal outcome
- [ ] Terminal outcome rolls correctly (75% transform, 20% death, 5% immunity)
- [ ] Treatment (medicine + rest) is effective in Stages 1–2, weak in Stage 3
- [ ] CA_MarkedTurned settings apply correctly (load existing saves, no errors)
- [ ] Immunity survivors are marked permanently (cannot be re-infected)
- [ ] Starter lineage resistance shows in pawn history

---

# GOAL 5: Rework Settlement Raid Event to Be Dynamic and Engaging

## Overview
Replace flat "spawn N infected and charge" with varied, escalating raids: multiple waves, different entry points, flanking, diversions, escalation tied to threat/wealth/time, and optional siege phase. Add telegraphing and tension.

## Proposed Architecture

### 1. New LordJob for Waves
**File**: `Source/TheMarkedMen/LordJob_RaidWaves.cs` (new)

```csharp
public class LordJob_RaidWaves : LordJob
{
    private List<RaidWave> waves;
    private int currentWaveIndex;
    private int nextWaveSpawnTick;
    private float difficultyEscalation;
    
    public override void ExposeData() { /* save state */ }
    public override void Tick() { /* spawn waves, check victory */ }
    public override void LordJobTick() { /* main logic */ }
}

private class RaidWave
{
    public int delayTicksFromPrevious;
    public List<PawnGroupMaker> pawnGroups;
    public IntVec3 spawnPosition;
    public int maxWavePoints;
}
```

**Behavior**:
- **Wave 1**: Immediate spawn (scouts or vanguard)
- **Wave 2**: Spawn after 3–5 minutes if Wave 1 is losing (outgunned factor > 1.5) or automatically after 5 minutes
- **Wave 3** (optional): Spawn if Waves 1–2 are heavily losses (>50% casualties)
- **Entry Points**: Vary (not always same edge); flanking attacks from multiple directions
- **Escalation**: Later waves have better equipment, higher-tier infected (Alphas, Warlords)

### 2. Dynamic Raid Points Calculation
**File**: `TheMarkedMen.cs` (IncidentWorker_CrossedRaid modification)

```csharp
private float CalculateRaidStrength(Map map, Storyteller storyteller)
{
    float basePoints = storyteller.difficulty.threatPoints * settings.raidPointsMultiplier;
    
    // Escalation: each raid increases next raid difficulty
    float escalation = settings.raidEscalationPerRaid * raidCount;
    escalation = Mathf.Min(escalation, settings.raidEscalationMaxBonus);
    basePoints *= (1f + escalation);
    
    // Colony wealth factor
    float wealthFactor = 1f + (map.wealthWatcher.WealthTotal / 100000f) * 0.1f;
    basePoints *= wealthFactor;
    
    // Infection spread factor (if many infected on map, harder raid)
    int infectedCount = map.mapPawns.AllPawnsSpawned.Count(p => CrossedUtility.IsInfectedPawn(p));
    float infectionFactor = 1f + (infectedCount * 0.02f);
    basePoints *= infectionFactor;
    
    return Mathf.Clamp(basePoints, settings.minimumRaidPoints, settings.maximumRaidPoints);
}
```

### 3. Telegraphing & Tension
**File**: `Source/TheMarkedMen/CrossedRaidAlert.cs` (enhanced)

Add staged letters:
- **T-5min**: "Marked Men scouts detected approaching the colony..."
- **T-2min**: "The Marked Men are moving to attack positions. Prepare defenses!"
- **T-0min**: "The Marked Men are attacking!"
- **Mid-raid**: "Reinforcements arriving!" (Wave 2 spawn)

Letters use `ChoiceLetter` if possible to let player see threat assessment.

### 4. Varied Entry Points & Tactics
**File**: `Source/TheMarkedMen/RaidEntryPointSelector.cs` (new)

```csharp
public static List<IntVec3> SelectDiverseEntryPoints(Map map, int count)
{
    // Instead of all entering from same edge, use multiple entry vectors
    // Possible: North edge, South edge, flanking from East, West
    // Avoid clustering; spread them out to force player to defend multiple areas
}

public static RaidTactic SelectRaidTactic(int waveNumber, float threat)
{
    // Wave 1: Aggressive frontal assault (fast, testing defenses)
    // Wave 2: Flanking or siege (if frontal failed; different strategy)
    // Wave 3: All-in assault (desperate push)
}

public enum RaidTactic { FrontalAssault, Flanking, Siege, Ambush }
```

### 5. Raid Settings Enhancements
**File**: `TheMarkedMen.cs` (TheMarkedMenSettings)

Add new fields:
```csharp
public bool raidWavesEnabled = true;
public int waveDelayMinTicks = 180; // 3 minutes
public int waveDelayMaxTicks = 300; // 5 minutes
public bool raidTelepgrapher Enabled = true;
public bool raidFlankingEnabled = true;
```

### 6. Siege Phase (Optional, Later)
**File**: `Source/TheMarkedMen/LordJob_RaidSiege.cs` (new, future work)

Infected can camp outside colony, send waves, try to starve player out.
Not in initial implementation; noted for future enhancement.

## Files to Modify/Create
| File | Changes |
|------|---------|
| `LordJob_RaidWaves.cs` | NEW: Multi-wave raid orchestration |
| `RaidEntryPointSelector.cs` | NEW: Diverse entry tactics |
| `CrossedRaidAlert.cs` | MODIFY: Add staged letters, tension building |
| `TheMarkedMen.cs` | MODIFY: Add wave/escalation settings; enhance IncidentWorker_CrossedRaid |
| `TheMarkedMenIncidents.xml` | MODIFY: Update CA_CrossedRaid def with new points calculation |
| `Languages/English/Keyed/Strings.xml` | NEW: Staged raid alert strings |

## Testing Checklist (Goal 5)
- [ ] Raid spawns with 2–3 waves (configurable)
- [ ] Wave 1 spawns immediately; Wave 2 spawns 3–5 min later (or if Wave 1 losing)
- [ ] Waves enter from different map edges (not all same side)
- [ ] Later waves have stronger infected (more Alphas, Warlords)
- [ ] Raid difficulty escalates with colony wealth and infection spread
- [ ] Letters appear at T-5min, T-2min, T-0min, and Wave 2 spawn
- [ ] Player can read raid strength estimate before combat
- [ ] Existing saves load without raid errors
- [ ] Mod settings allow disabling waves, flanking, telegraphing

---

# GOAL 7: Fix "Walks to Map Edge and Exits" Bug

## Overview
Ensure infected never leave the map unless explicitly instructed. Give dev-spawned infected a valid Lord and behavior automatically. Prevent vanilla "exit map" defaults from triggering.

## Proposed Fix

### 1. Enhanced Dev-Spawn Handler
**File**: `CrossedUtility.cs` (modify `EnsureInfectedHasValidLord()`)

```csharp
public static void EnsureInfectedHasValidLord(Pawn pawn)
{
    if (pawn == null || pawn.Dead) return;
    
    // Step 1: Ensure faction is set
    if (pawn.Faction != CADefOf.CrossedFaction)
    {
        Faction crossed = Component?.EnsureCrossedFaction();
        if (crossed != null)
            pawn.SetFaction(crossed);
    }
    
    // Step 2: Check if pawn has a valid Lord
    if (pawn.GetLord() != null && pawn.GetLord().faction == pawn.Faction)
        return; // Already has valid Lord
    
    // Step 3: Assign to map's current raid Lord if one exists
    Map map = pawn.Map;
    if (map != null)
    {
        foreach (Lord lord in map.lordManager.lords)
        {
            if (lord.faction == CADefOf.CrossedFaction && 
                (lord.LordJob is LordJob_Assault || 
                 lord.LordJob is LordJob_RaidWaves))
            {
                lord.AddPawn(pawn);
                return;
            }
        }
    }
    
    // Step 4: Create new Lord if none found
    if (map != null)
    {
        LordMaker.MakeNewLord(
            pawn.Faction, 
            new LordJob_AssaultColony(map, false, false), 
            map, 
            new List<Pawn> { pawn }
        );
    }
}
```

### 2. Patch Vanilla ThinkTree Exit Logic
**File**: `Source/TheMarkedMen/Patches/TheMarkedMenHarmonyPatches.cs` (add new patch)

```csharp
[HarmonyPatch(typeof(Pawn_JobTracker), nameof(Pawn_JobTracker.EndCurrentJob))]
public static class Patch_PreventInfectedMapExit
{
    public static bool Prefix(Pawn_JobTracker __instance, JobCondition condition)
    {
        Pawn pawn = __instance.pawn;
        
        // If pawn is infected and trying to exit map, prevent it
        if (CrossedUtility.IsInfectedPawn(pawn) && condition == JobCondition.Left)
        {
            // Cancel exit; reassign to stay on map
            return false; // Prevent vanilla exit behavior
        }
        
        return true;
    }
}
```

### 3. Ensure Infected Use Correct ThinkTree
**File**: `TheMarkedMen.cs` (modify pawn setup in raid logic)

When spawning infected in raid:
```csharp
private void SetupInfectedPawn(Pawn pawn)
{
    // Ensure infected use CA_HunterThinkTree (from Goal 6)
    pawn.mindState.thinkTree = DefDatabase<ThinkTreeDef>.GetNamed("CA_HunterThinkTree");
    
    // Ensure valid Lord
    CrossedUtility.EnsureInfectedHasValidLord(pawn);
    
    // Mark to skip vanilla "wander off map" behavior
    pawn.mindState.exitMapAfterTick = -1;
}
```

### 4. Debug Action Fix
**File**: `Source/TheMarkedMen/CrossedDebugActions.cs` (enhance existing)

Update existing `DebugSpawn` action:
```csharp
[DebugAction("Pawns", "Spawn Marked Man (Hunter)")]
private static void SpawnMarkedHunter()
{
    Pawn pawn = PawnGenerator.GeneratePawn(CADefOf.CrossedCivilian);
    GenSpawn.Spawn(pawn, UI.MouseCell(), Find.CurrentMap);
    
    // CRITICAL: Initialize properly
    CrossedUtility.EnsureInfectedHasValidLord(pawn);
    CrossedTacticalAI.TryIssueTacticalJob(pawn);
    
    Messages.Message($"Spawned {pawn.NameFullColored}. Lord assigned: {pawn.GetLord()?.faction.Name ?? "NONE"}", MessageTypeDefOf.NeutralEvent);
}
```

## Files to Modify
| File | Changes |
|------|---------|
| `CrossedUtility.cs` | Enhance `EnsureInfectedHasValidLord()` with Lord assignment logic |
| `Patches/TheMarkedMenHarmonyPatches.cs` | Add `Patch_PreventInfectedMapExit` |
| `TheMarkedMen.cs` | Add `SetupInfectedPawn()` call in spawn logic |
| `CrossedDebugActions.cs` | Ensure debug spawn calls `EnsureInfectedHasValidLord()` |

## Testing Checklist (Goal 7)
- [ ] Dev-spawned infected assigned to valid Lord immediately
- [ ] Dev-spawned infected use CA_HunterThinkTree
- [ ] Dev-spawned infected do NOT walk toward map edge
- [ ] Dev-spawned infected engage nearest enemy instead of exiting
- [ ] Rogue infected (no Lord) reassign when new raid starts
- [ ] Existing saves load without error
- [ ] Debug message shows Lord assignment

---

## Implementation Order (Final)

| # | Goal | Estimated Effort | Dependencies |
|---|------|------------------|--------------|
| 1 | Goal 6 (Hunter AI) | HIGH | None |
| 2 | Goal 1 (Infection Rebalance) | MEDIUM | None (parallel ok) |
| 3 | Goal 7 (Exit Bug) | LOW | Goal 6 (needs ThinkTree) |
| 4 | Goal 5 (Raid Dynamics) | HIGH | Goal 6 (AI), Goal 1 (clarity) |
| 5 | Goal 2 (Incident Fixes) | MEDIUM | Goal 5 (raids) |
| 6 | Goal 3 (Vehicle Compat) | MEDIUM | Research Vehicle Framework API |
| 7 | Goal 4 (Timer UI) | LOW | None |

---

## Sign-Off Checklist

Before implementation begins, user must confirm:
- [ ] Design decisions locked (Goals 1,5,6 approved)
- [ ] Implementation order agreed
- [ ] File list reviewed (no conflicts)
- [ ] Ready to begin with Goal 6 (Hunter AI)
