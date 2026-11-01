# Cure-to-Refugee Feature Implementation Guide

## Overview
This feature adds a refugee/wanderer system for cured infected pawns. When a pawn is cured of the Marked Virus, they become a refugee seeking acceptance into the colony with a choice letter offering accept/refuse options.

## Files Created

### 1. Core Classes

#### `ChoiceLetter_CuredRefugee.cs`
- **Location:** `Source/TheMarkedMen/ChoiceLetter_CuredRefugee.cs`
- **Purpose:** Custom choice letter for cured refugees
- **Key Features:**
  - Inherits from `ChoiceLetter` (RimWorld pattern)
  - Two dialogue options: Accept and Reject
  - Accept: Recruits pawn to player faction, grants gratitude thought
  - Reject: Sends pawn to wanderer faction or despawns them
  - Proper save/load via `ExposeData()`
  - Translation string integration

#### `CuredRefugeeManager.cs`
- **Location:** `Source/TheMarkedMen/CuredRefugeeManager.cs`
- **Purpose:** MapComponent to track and manage refugees
- **Key Features:**
  - Tracks pending refugees by pawn ID
  - Automatic cleanup of dead/despawned pawns
  - Letter generation with narrative text
  - Health status display in letter
  - Proper save/load persistence
  - Static factory method `GetForMap()` for access

#### `Patch_CureUtility_CuredRefugee.cs`
- **Location:** `Source/TheMarkedMen/Patches/Patch_CureUtility_CuredRefugee.cs`
- **Purpose:** Harmony patch to integrate refugee system
- **Key Features:**
  - Postfix patch on `CureUtility.ApplyCureDirectly()`
  - Registers cured pawn as refugee after cure completes
  - Only triggers for non-colony members (colonists stay colonists)
  - Automatically calls `CuredRefugeeManager.RegisterRefugee()`

### 2. XML Definitions

#### `TheMarkedMenCuredRefugee.xml` (HediffDefs)
- **Location:** `Defs/HediffDefs/TheMarkedMenCuredRefugee.xml`
- **Contains:**
  - `TheMarkedMen_CuredRefugee` hediff (optional tracking)
  - `TheMarkedMen_RefugeeAccepted` thought (+15 mood, 30 days)
  - `TheMarkedMen_RefugeeRejected` thought (-20 mood, 60 days)
  - `TheMarkedMen_SecondChance` trait (work +15% speed bonus)

#### `TheMarkedMenCuredRefugeeDefs.xml` (Misc)
- **Location:** `Defs/Misc/TheMarkedMenCuredRefugeeDefs.xml`
- **Contains:**
  - `TheMarkedMen_CuredRefugeeLetter` letter def (green color)
  - `TheMarkedMen_AcceptRefugee` interaction def

#### `TheMarkedMenCuredRefugee.xml` (Strings)
- **Location:** `Languages/English/DefInjected/TheMarkedMenCuredRefugee.xml`
- **Contains:** All translation strings for UI and messages

## Integration Points

### How It Works - Step by Step

1. **Cure Application**
   ```
   Player administers cure syringe to infected pawn
   ↓
   CureUtility.ApplyCureDirectly() executes
   ↓
   Harmony patch postfix fires
   ↓
   CuredRefugeeManager.RegisterRefugee() called
   ```

2. **Refugee Registration**
   ```
   RegisterRefugee() called
   ↓
   Pawn ID added to tracking set
   ↓
   GenerateRefugeeLetter() creates choice letter
   ↓
   Letter shows pawn name, health status, request to join
   ```

3. **Player Decision**
   ```
   Accept Button:
   - pawn.SetFaction(Faction.OfPlayer)
   - Add gratitude thought
   - Remove from refugee tracking
   - Notification sent
   
   Reject Button:
   - pawn.SetFaction(wanderer faction)
   - Send away message
   - Remove from refugee tracking
   - Pawn leaves as wanderer
   ```

## Code Integration Required

### In TheMarkedMenHarmonyPatches.cs

Add this harmony patch registration (if not already present):

```csharp
// Inside PatchAll or manual harmony setup:
Harmony harmony = new Harmony("edria.themarkedmen");
harmony.PatchAll(Assembly.GetExecutingAssembly());
// This will auto-discover Patch_CureUtility_CuredRefugee
```

**Note:** The patch file is in the `Patches/` folder and should be auto-discovered by `harmony.PatchAll()`.

### No Changes Needed to CureUtility.cs

The Harmony postfix handles everything. CureUtility.cs remains unchanged - the integration is non-invasive.

## Translation Strings Needed

Add these to your language file (or use the provided XML):

```
TheMarkedMen_CuredRefugee_LetterTitle
TheMarkedMen_CuredRefugee_LetterBody
TheMarkedMen_CuredRefugee_Backstory
TheMarkedMen_CuredRefugee_HealthStatus
TheMarkedMen_CuredRefugee_RequestToJoin
TheMarkedMen_CuredRefugee_Accept
TheMarkedMen_CuredRefugee_Reject
TheMarkedMen_MessageRefugeeAccepted
TheMarkedMen_MessageRefugeeRejected
```

## Behavioral Details

### Letter Appearance
- **Color:** Green (NeutralEvent letter def)
- **Title:** "Cured Survivor: [PawnName]"
- **Body:** Narrative text about infection, cure, request to join
- **Health Status:** Shows pawn's current health percentage
- **Buttons:** Accept / Reject / Close

### Accept Logic
- Pawn becomes colony member (player faction)
- Gains "accepted as refugee" thought (+15 mood for 30 days)
- Removes refugee state
- Optional: Could apply "Second Chance" trait for work bonus

### Reject Logic
- Pawn becomes wanderer (Wastelander faction preferred)
- Leaves the map
- Message confirms rejection
- Player loses potential colonist

### Edge Cases Handled
- Dead/despawned refugees auto-removed from tracking
- Colonists who were infected and cured stay colonists
- Non-spawned pawns don't trigger refugee system
- Null safety checks throughout

## Performance Considerations

- **Memory:** Minimal - only tracks pawn IDs (integers)
- **CPU:** One MapComponent tick per map
- **GC:** Cleanup every 600 ticks removes stale entries
- **Letters:** One per cure completion (normal RimWorld load)

## Optional Enhancements (Future)

1. **Timeline Variations:** Can refuse immediately or after X days
2. **Skills Retention:** Track pre-infection skills
3. **Relationship Tracking:** Remember who cured them
4. **Reputation System:** Accept/reject affects faction opinion
5. **Psychological Hediff:** Add permanent infection trauma thought
6. **Storyteller Integration:** Frequency of refugees can scale with difficulty

## Testing Checklist

- [ ] Cure syringe applied to infected pawn
- [ ] Letter appears with correct name/health
- [ ] Accept button recruits pawn successfully
- [ ] Reject button makes pawn leave
- [ ] Messages appear for both outcomes
- [ ] Pawn appears in colony roster after accept
- [ ] Refugees cleaned up after save/load
- [ ] Non-colonists trigger refugee system
- [ ] Colonists stay colonists after cure

## Compilation Notes

1. **Required References:**
   - RimWorld.dll
   - Assembly-CSharp.dll
   - UnityEngine.dll
   - 0Harmony.dll

2. **Namespace:** `TheMarkedMen` (consistent with existing code)

3. **No New Dependencies:** Uses only existing RimWorld APIs

4. **Harmony Requirement:** Already a dependency (required mod)

## File Locations Summary

```
Source/TheMarkedMen/
├── ChoiceLetter_CuredRefugee.cs          [NEW]
├── CuredRefugeeManager.cs                 [NEW]
└── Patches/
    └── Patch_CureUtility_CuredRefugee.cs  [NEW]

Defs/
├── HediffDefs/
│   └── TheMarkedMenCuredRefugee.xml       [NEW]
└── Misc/
    └── TheMarkedMenCuredRefugeeDefs.xml   [NEW]

Languages/English/DefInjected/
└── TheMarkedMenCuredRefugee.xml           [NEW]
```

## Harmony Patch Details

```csharp
[HarmonyPatch(typeof(CureUtility), nameof(CureUtility.ApplyCureDirectly))]
public static class Patch_CureUtility_CuredRefugee
{
    // Postfix runs AFTER ApplyCureDirectly completes
    // At this point:
    // - Pawn is cured
    // - Faction is set (via ResolveCureFaction)
    // - Hediffs are cleaned
    // - Non-player pawns are ready for refugee system
}
```

The postfix checks:
1. Pawn is alive and spawned
2. Pawn is NOT in player faction (colonists skip refugee system)
3. Gets/creates CuredRefugeeManager for the map
4. Registers pawn as refugee

## Next Steps After Implementation

1. Rebuild the DLL
2. Load a save with infected pawns
3. Apply cure via syringe
4. Verify letter appears
5. Test both accept/reject paths
6. Check save/load persistence
7. Monitor performance in large colonies

---

**Status:** Ready for compilation and testing
**Estimated Dev Time:** 10-15 minutes to rebuild DLL
**Risk Level:** Low - Non-invasive integration via Harmony postfix

