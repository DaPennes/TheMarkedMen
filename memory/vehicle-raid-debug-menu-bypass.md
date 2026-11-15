# Vehicle raid debug menu point bypass

## Behavior

The developer vehicle-assault actions use a deliberately high point budget so
vehicle eligibility checks cannot fail on point requirements. `debugPoints` is
`Mathf.Max(incident.minThreatPoints, 20000f)` and is used for:

- `IncidentParms.points`
- `HasEligibleRaidVehicle(...)`
- `HasEligibleAirborneRaidVehicle(...)`

Normal raids are unaffected: `IncidentWorker_CrossedVehicleRaid` keeps using
its own escalated `parms.points` for eligibility and execution.

## Implementation

`CrossedDebugActions.FireVehicleAssault` computes `debugPoints` once and reuses
it for both land and air checks. The previous code assigned 20,000 directly to
the eligibility helper while giving `parms.points` a different value; the
refactor keeps the two paths consistent.

## Verification

- Debug-only path: `DebugRaidPoints` / `debugPoints` appear only in
  `CrossedDebugActions.cs`.
- Normal raid path: `IncidentWorker_CrossedVehicleRaid` and the airborne
  variant do not reference 20,000 points.
- `dotnet build TheMarkedMen.csproj` succeeds.
