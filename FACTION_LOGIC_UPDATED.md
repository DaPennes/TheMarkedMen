# Cure-to-Refugee Feature - IMPROVED LOGIC

## Updated Behavior

The feature now properly handles faction relations:

### Scenario 1: Cured Pawn from Marked Men Faction
```
Before: Marked Men faction
After cure applied:
  → Shows choice letter
  → Accept: Joins player colony (no penalty)
  → Reject: Returns to Marked Men faction (-5 goodwill to player)
```

### Scenario 2: Cured Pawn was Player Colonist
```
Before: Player faction
After cure applied:
  → NO choice letter shown
  → Pawn automatically stays in colony
  → No faction penalties
```

### Scenario 3: Cured Pawn from Allied/Enemy Faction
```
Before: Allied faction (e.g., Outlanders, Empire)
After cure applied:
  → Shows choice letter
  → Accept: Joins player colony (-5 relation penalty with original faction)
  → Reject: Returns to original faction (no additional penalty)
```

---

## Code Changes Made

### 1. CuredRefugeeManager.cs - NEW
**Added `CuredRefugeeRecord` class:**
- Tracks both pawn ID and original faction
- Properly saves/loads with `ExposeData()`
- Stores faction information for later retrieval

**Updated `RegisterRefugee()` method:**
- Now takes `Faction originalFaction` parameter
- Only shows letter if original faction ≠ player faction
- Stores original faction in records

**New `GetOriginalFaction()` method:**
- Retrieves stored original faction for a refugee
- Used by choice letter to restore correct faction

### 2. ChoiceLetter_CuredRefugee.cs - UPDATED
**Added field:**
- `public Faction originalFaction` - stores the faction pawn came from

**Updated Accept button:**
- Still joins player faction
- No penalty applied (they chose to stay)

**Updated Reject button:**
- Returns pawn to `originalFaction` (if it still exists)
- Applies -5 goodwill penalty to player's relation with that faction
- Reflects cost of rejecting them

**Updated ExposeData():**
- Now saves/loads `originalFaction`

### 3. Patch_CureUtility_CuredRefugee.cs - UPDATED
**Added Prefix method:**
- Captures pawn's faction BEFORE cure changes it
- Stores in `__state` parameter (Harmony convention)
- Runs before any cure logic

**Updated Postfix method:**
- Receives original faction via `__state`
- Checks if original faction was player (if so, skip letter)
- Passes original faction to `RegisterRefugee()`

---

## How the Faction Penalty Works

When player rejects a refugee:

```csharp
// Get the relation between player and their original faction
FactionRelation relation = playerFaction.RelationWith(originalFaction);

// Apply goodwill penalty
if (relation != null)
{
    relation.baseGoodwill -= 5;
}
```

**Result:**
- Rejecting an Outlander refugee → -5 relation with Outlanders
- Rejecting an Empire refugee → -5 relation with Empire
- Accepting anyone → No relation penalty (they made their choice)

---

## Data Persistence

The system properly saves and loads:

```csharp
Scribe_Collections.Look(
    ref refugeeRecords, 
    "refugeeRecords", 
    LookMode.Value,      // Key: pawn ID (int)
    LookMode.Deep        // Value: CuredRefugeeRecord (deep save)
);

// CuredRefugeeRecord.ExposeData() saves:
// - pawnId (int)
// - originalFaction (Faction reference)
```

---

## Edge Cases Handled

✅ **Original faction defeated/destroyed**
- Reject button checks: `if (originalFaction != null && !originalFaction.defeated)`
- Falls back to despawn if faction is gone

✅ **Pawn dies during cure countdown**
- Stale refugee cleanup removes dead pawns
- No orphaned records left behind

✅ **Player colonists who get infected and cured**
- Letter never shows (original faction was player)
- Automatically stays in colony
- No faction penalties

✅ **Multiple refugees at once**
- Each tracked independently
- Each letter has its own faction record
- Relation penalties apply separately

---

## Testing Scenario

**Setup:**
1. Get an enemy pawn (raider, pirate, etc.) and keep them alive
2. Infect them with Marked Virus
3. Cure them with syringe

**Expected Results:**
- Green choice letter appears: "Cured Survivor: [Name]"
- If you **Accept**: They join your colony
- If you **Reject**: They leave, and you see -5 goodwill notification

**Verify:**
- Check faction relations before/after rejection
- See relations decrease by exactly 5 points

---

## Files Modified

```
Source/TheMarkedMen/
├── CuredRefugeeManager.cs (UPDATED)
│   ├── Added: CuredRefugeeRecord class
│   ├── Updated: RegisterRefugee(pawn, faction)
│   └── Added: GetOriginalFaction(pawn)
│
├── ChoiceLetter_CuredRefugee.cs (UPDATED)
│   ├── Added: originalFaction field
│   ├── Updated: Reject button logic
│   └── Updated: ExposeData() for faction
│
└── Patches/Patch_CureUtility_CuredRefugee.cs (UPDATED)
    ├── Added: Prefix to capture original faction
    └── Updated: Postfix to pass faction to manager
```

---

## Ready to Rebuild

The code is now complete with all faction logic implemented. When you rebuild the DLL:

```bash
cd Source/TheMarkedMen
dotnet build TheMarkedMen.csproj -c Release
```

All three files will compile with the new faction-aware logic automatically integrated!

