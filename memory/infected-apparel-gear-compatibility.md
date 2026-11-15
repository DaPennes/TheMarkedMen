# Infected apparel and gear compatibility

## Settings

The mod options expose eight infected-apparel toggles. All default to `true`
and are serialized by `TheMarkedMenSettings.ExposeData()`:

- `allowInfectedWearTribalApparel`
- `allowInfectedWearNeolithicApparel`
- `allowInfectedWearMilitaryApparel`
- `allowInfectedWearScoutApparel`
- `allowInfectedWearPoweredArmor`
- `allowInfectedWearShields`
- `allowInfectedWearHelmets`
- `allowInfectedWearApparelWithTags`

`ApplyBaselinePreset()` resets all eight to `true`.

## Rule evaluation

`CrossedApparelCompatibility.CanWearApparel(Pawn, ThingDef)` is the single
source of truth. Both the `Pawn_ApparelTracker.Wear` Harmony prefix and
`CrossedEquipmentGenerator` call it, so generated raids and gear equipped in
play use the same rules.

The category toggles are independent. Disabling a category blocks every
apparel item that matches it. `allowInfectedWearApparelWithTags` only controls
tagged apparel that does not match the tribal, neolithic, military, or scout
categories; it is not a global bypass. `CA_MarkedTurned` victims keep their
pre-infection gear and are exempt from these restrictions.

## Gear preservation through transformation

`CrossedUtility.TransformPawn` snapshots worn apparel and equipped weapons
before any transformation side effects. After `ApplyClassHediffs`, it calls
`RestoreOriginalEquipment`, which reclaims originals that were dropped or
unequipped and re-wears/re-equips them. Restoration is non-destructive: gear
already worn or wielded is left alone, and conflicting replacement gear is
dropped rather than destroyed.

`CrossedUtility.SuppressInfectedApparelRestrictions` is set while originals
are restored so the Harmony prefix does not block the pawn's own gear.

## Verification

- `dotnet build TheMarkedMen.csproj` succeeds with zero errors and warnings.
- All keyed XML parses, has no duplicate keys, and every literal
  `MarkedMen_`/`CA_`/`TheMarkedMen_` translation key used in C# exists.
