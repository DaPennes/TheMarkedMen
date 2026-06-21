using System;
using System.Collections.Generic;
using LudeonTK;
using UnityEngine;
using Verse;

namespace TheMarkedMen
{
    [StaticConstructorOnStartup]
    public static class MarkedMenDebugOverlay
    {
        private static bool active;

        public static bool Active
        {
            get => active;
            set
            {
                active = value;
                Log.Message($"[The Marked Men] Predator AI overlay {(value ? "enabled" : "disabled")}.");
            }
        }

        [DebugAction("The Marked Men", "Toggle AI overlay (scent/noise/memory)", allowedGameStates = AllowedGameStates.PlayingOnMap, actionType = DebugActionType.Action, displayPriority = 800)]
        public static void ToggleOverlay()
        {
            if (!TheMarkedMenSettings.DebugActionsEnabled)
            {
                Log.Message("[The Marked Men] Debug actions are disabled in mod settings.");
                return;
            }

            Active = !Active;
        }

        public static void Draw()
        {
            if (!active)
            {
                return;
            }

            Map map = Find.CurrentMap;
            if (map == null)
            {
                return;
            }

            Camera camera = Camera.current;
            if (camera == null)
            {
                return;
            }

            CellRect viewRect = Find.CameraDriver?.CurrentViewRect ?? CellRect.Empty;
            if (viewRect.IsEmpty)
            {
                return;
            }

            DrawScentMarkers(map, camera, viewRect);
            DrawNoiseEvents(map, camera, viewRect);
            DrawMemoryEvents(map, camera, viewRect);
            DrawInfectedPawnInfo(map, camera);
        }

        private static void DrawScentMarkers(Map map, Camera camera, CellRect viewRect)
        {
            MarkedMenMemoryGrid memory = map.GetComponent<MarkedMenMemoryGrid>();
            if (memory == null)
            {
                return;
            }

            int tick = Find.TickManager.TicksGame;

            for (int i = memory.ScentCount - 1; i >= 0; i--)
            {
                ScentMarker marker = memory.GetScent(i);
                if (!marker.position.InBounds(map) || !viewRect.Contains(marker.position))
                {
                    continue;
                }

                float age = Mathf.Clamp01((tick - marker.createdTick) / 7500f);
                float alpha = Mathf.Lerp(0.7f, 0.1f, age);
                if (alpha < 0.05f)
                {
                    continue;
                }

                Vector3 screenPos = marker.position.ToVector3Shifted();
                Vector3 worldPos = camera.WorldToScreenPoint(screenPos);
                worldPos.y = Screen.height - worldPos.y;

                float size = Mathf.Lerp(8f, 3f, age);
                Rect rect = new Rect(worldPos.x - size / 2f, worldPos.y - size / 2f, size, size);

                GUI.color = new Color(1f, 0.2f, 0.2f, alpha);
                GUI.DrawTexture(rect, BaseContent.WhiteTex);
                GUI.color = Color.white;
            }
        }

        private static void DrawNoiseEvents(Map map, Camera camera, CellRect viewRect)
        {
            MarkedMenMemoryGrid memory = map.GetComponent<MarkedMenMemoryGrid>();
            if (memory == null)
            {
                return;
            }
            int tick = Find.TickManager.TicksGame;

            for (int i = memory.NoiseCount - 1; i >= 0; i--)
            {
                NoiseEvent noise = memory.GetNoise(i);
                if (!noise.position.InBounds(map) || !viewRect.Contains(noise.position))
                {
                    continue;
                }

                float age = noise.decayTicks > 0
                    ? (tick - noise.createdTick) / (float)noise.decayTicks
                    : 1f;
                if (age >= 1f)
                {
                    continue;
                }

                float alpha = Mathf.Lerp(0.6f, 0.1f, age);
                Vector3 screenPos = noise.position.ToVector3Shifted();
                Vector3 worldPos = camera.WorldToScreenPoint(screenPos);
                worldPos.y = Screen.height - worldPos.y;

                float radius = Mathf.Lerp(12f, 4f, age);
                Rect rect = new Rect(worldPos.x - radius / 2f, worldPos.y - radius / 2f, radius, radius);

                GUI.color = new Color(0.2f, 0.8f, 0.2f, alpha);
                GUI.DrawTexture(rect, BaseContent.WhiteTex);
                GUI.color = Color.white;
            }
        }

        private static void DrawMemoryEvents(Map map, Camera camera, CellRect viewRect)
        {
            MarkedMenMemoryGrid memory = map.GetComponent<MarkedMenMemoryGrid>();
            if (memory == null)
            {
                return;
            }
            int tick = Find.TickManager.TicksGame;

            for (int i = memory.MemoryCount - 1; i >= 0; i--)
            {
                MemoryEvent mem = memory.GetMemory(i);
                if (!mem.position.InBounds(map) || !viewRect.Contains(mem.position))
                {
                    continue;
                }

                int age = tick - mem.lastSeenTick;
                if (age > 30000)
                {
                    continue;
                }

                float alpha = Mathf.Lerp(0.5f, 0.05f, age / 30000f);
                Vector3 screenPos = mem.position.ToVector3Shifted();
                Vector3 worldPos = camera.WorldToScreenPoint(screenPos);
                worldPos.y = Screen.height - worldPos.y;

                float size = 6f;
                Rect rect = new Rect(worldPos.x - size / 2f, worldPos.y - size / 2f, size, size);

                GUI.color = new Color(0.3f, 0.4f, 1f, alpha);
                GUI.DrawTexture(rect, BaseContent.WhiteTex);
                GUI.color = Color.white;
            }
        }

        private static void DrawInfectedPawnInfo(Map map, Camera camera)
        {
            MarkedMenAIManager ai = MarkedMenAIManager.GetForMap(map);
            if (ai == null)
            {
                return;
            }

            foreach (Pawn pawn in map.mapPawns.AllPawnsSpawned)
            {
                if (!CrossedUtility.IsInfectedPawn(pawn))
                {
                    continue;
                }

                Vector3 screenPos = pawn.DrawPos;
                Vector3 worldPos = camera.WorldToScreenPoint(screenPos);
                worldPos.y = Screen.height - worldPos.y;

                MarkedPursuitState state = ai.GetPursuitState(pawn);

                string label = state.ToString();
                Color textColor = state switch
                {
                    MarkedPursuitState.Frenzy => Color.red,
                    MarkedPursuitState.Hunting => new Color(1f, 0.5f, 0f),
                    MarkedPursuitState.Searching => Color.yellow,
                    _ => Color.gray
                };

                Vector2 textSize = GUI.skin.label.CalcSize(new GUIContent(label));
                Rect labelRect = new Rect(worldPos.x - textSize.x / 2f, worldPos.y - 20f, textSize.x, textSize.y);

                GUI.color = new Color(0f, 0f, 0f, 0.6f);
                GUI.DrawTexture(new Rect(labelRect.x - 2f, labelRect.y - 1f, labelRect.width + 4f, labelRect.height + 2f), BaseContent.WhiteTex);
                GUI.color = textColor;
                GUI.Label(labelRect, label);
                GUI.color = Color.white;
            }
        }

        [DebugAction("The Marked Men", "Add debug scent at cursor", allowedGameStates = AllowedGameStates.PlayingOnMap, actionType = DebugActionType.ToolMap, displayPriority = 799)]
        public static void AddScentAtCursor()
        {
            if (!TheMarkedMenSettings.DebugActionsEnabled)
            {
                return;
            }

            IntVec3 cell = UI.MouseCell();
            if (!cell.InBounds(Find.CurrentMap))
            {
                return;
            }

            MarkedMenMemoryGrid memory = MarkedMenMemoryGrid.GetForMap(Find.CurrentMap);
            memory.AddScent(cell, 1f, null);
            Log.Message($"[The Marked Men] Added debug scent marker at {cell}.");
        }

        [DebugAction("The Marked Men", "Add debug noise at cursor", allowedGameStates = AllowedGameStates.PlayingOnMap, actionType = DebugActionType.ToolMap, displayPriority = 798)]
        public static void AddNoiseAtCursor()
        {
            if (!TheMarkedMenSettings.DebugActionsEnabled)
            {
                return;
            }

            IntVec3 cell = UI.MouseCell();
            if (!cell.InBounds(Find.CurrentMap))
            {
                return;
            }

            MarkedMenMemoryGrid memory = MarkedMenMemoryGrid.GetForMap(Find.CurrentMap);
            memory.AddNoise(cell, 1f, 3000);
            Log.Message($"[The Marked Men] Added debug noise event at {cell}.");
        }
    }
}
