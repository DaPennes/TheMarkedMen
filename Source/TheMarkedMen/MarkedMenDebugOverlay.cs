using System;
using LudeonTK;
using UnityEngine;
using Verse;

namespace TheMarkedMen
{
    [StaticConstructorOnStartup]
    public static class MarkedMenDebugOverlay
    {
        // ─── State ─────────────────────────────────────────────────
        private static bool active;
        private static bool showScent = true;
        private static bool showNoise = true;
        private static bool showMemory = true;
        private static bool showLabels = true;
        private static float overlayOpacity = 0.85f;
        private static bool collapsed;
        private static Vector2 panelPos = new Vector2(12f, -1f);

        // ─── Animation ─────────────────────────────────────────────
        private static float fadeAlpha;
        private static float lastToggleTime;
        private static float[] flashAlpha = new float[5];
        private static int hoveredIdx = -1;

        // ─── Drag ──────────────────────────────────────────────────
        private static bool dragging;
        private static Vector2 dragOffset;

        // ─── Textures ──────────────────────────────────────────────
        private static Texture2D glowTex;
        private static bool texReady;

        // ─── Colors ────────────────────────────────────────────────
        private static readonly Color colScent = new Color(1f, 0.25f, 0.15f);
        private static readonly Color colNoise = new Color(0.15f, 0.9f, 0.25f);
        private static readonly Color colMemory = new Color(0.2f, 0.45f, 1f);
        private static readonly Color colLabel = new Color(0.85f, 0.85f, 0.9f);
        private static readonly Color bgPanel = new Color(0.06f, 0.06f, 0.08f, 0.78f);
        private static readonly Color bgHeader = new Color(0.1f, 0.1f, 0.13f, 0.85f);
        private static readonly Color borderDim = new Color(0.3f, 0.3f, 0.35f, 0.45f);
        private static readonly Color borderHi = new Color(0.5f, 0.5f, 0.6f, 0.3f);

        private static readonly string[] stateLabels = { "IDLE", "SEARCH", "HUNT", "FRENZY" };

        public static bool Active
        {
            get => active;
            set
            {
                if (active == value) return;
                active = value;
                lastToggleTime = Time.realtimeSinceStartup;
                Log.Message($"[TMM] Predator AI overlay {(value ? "enabled" : "disabled")}.");
            }
        }

        // ─── Texture init ─────────────────────────────────────────
        private static void EnsureTex()
        {
            if (texReady) return;
            texReady = true;
            int r = 64;
            glowTex = new Texture2D(r, r, TextureFormat.ARGB32, false);
            glowTex.name = "TMMGlow";
            float h = r * 0.5f;
            for (int y = 0; y < r; y++)
                for (int x = 0; x < r; x++)
                {
                    float d = Mathf.Sqrt(Mathf.Pow((x - h) / h, 2f) + Mathf.Pow((y - h) / h, 2f));
                    float a = Mathf.Clamp01(1f - d);
                    a = a * a * (3f - 2f * a);
                    glowTex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
                }
            glowTex.Apply();
        }

        // ─── Debug action ──────────────────────────────────────────
        [DebugAction("The Marked Men", "Toggle AI overlay (scent/noise/memory)",
            allowedGameStates = AllowedGameStates.PlayingOnMap,
            actionType = DebugActionType.Action, displayPriority = 800)]
        public static void ToggleOverlay()
        {
            if (!TheMarkedMenSettings.DebugActionsEnabled)
            {
                Log.Message("[TMM] Debug actions disabled in settings.");
                return;
            }
            Active = !Active;
        }

        // ─── Main draw ─────────────────────────────────────────────
        public static void Draw()
        {
            float now = Time.realtimeSinceStartup;
            float dt = Time.deltaTime;

            // fade animation
            if (active) fadeAlpha = Mathf.Min(1f, fadeAlpha + dt * 3f);
            else { fadeAlpha = Mathf.Max(0f, fadeAlpha - dt * 3f); if (fadeAlpha < 0.001f) return; }

            Map map = Find.CurrentMap;
            if (map == null) return;

            Camera cam = Find.CameraDriver?.GetComponent<Camera>();
            if (cam == null) { cam = Camera.current; if (cam == null) return; }

            CellRect vr = Find.CameraDriver?.CurrentViewRect ?? CellRect.Empty;
            if (vr.IsEmpty) return;

            EnsureTex();
            float time = now;
            float fa = fadeAlpha;

            try
            {
                if (showScent) DrawScent(map, cam, vr, time, fa);
                if (showNoise) DrawNoise(map, cam, vr, time, fa);
                if (showMemory) DrawMemory(map, cam, vr, time, fa);
                if (showLabels) DrawLabels(map, cam, time, fa);
                DrawUI(map, time, fa, dt);
            }
            catch (Exception ex) { Log.Warning("[TMM] Overlay error: " + ex.Message); }
        }

        // ─── Glow helper ───────────────────────────────────────────
        private static void Glow(IntVec3 cell, Camera cam, CellRect vr, Color c,
            float baseSize, float alpha, float pulseHz, float time, float fadeMul)
        {
            if (!vr.Contains(cell)) return;
            Vector3 sp = cam.WorldToScreenPoint(cell.ToVector3Shifted());
            sp.y = Screen.height - sp.y;

            float pm = 0.85f + 0.15f * Mathf.Sin(time * pulseHz);
            float fa = fadeMul * alpha * pm;
            if (fa < 0.005f) return;

            float sz = baseSize * overlayOpacity;

            // outer
            float o = sz * 2.2f;
            GUI.color = new Color(c.r, c.g, c.b, fa * 0.18f);
            GUI.DrawTexture(new Rect(sp.x - o * 0.5f, sp.y - o * 0.5f, o, o), glowTex);

            // mid
            float m = sz * 1.2f;
            GUI.color = new Color(c.r, c.g, c.b, fa * 0.4f);
            GUI.DrawTexture(new Rect(sp.x - m * 0.5f, sp.y - m * 0.5f, m, m), glowTex);

            // core
            float cr = sz * 0.5f;
            GUI.color = new Color(1f, 1f, 1f, fa * 0.75f);
            GUI.DrawTexture(new Rect(sp.x - cr * 0.5f, sp.y - cr * 0.5f, cr, cr), glowTex);

            GUI.color = Color.white;
        }

        private static void DrawScent(Map map, Camera cam, CellRect vr, float time, float fa)
        {
            var mem = map.GetComponent<MarkedMenMemoryGrid>();
            if (mem == null) return;
            int tick = Find.TickManager.TicksGame;
            for (int i = mem.ScentCount - 1; i >= 0; i--)
            {
                var m = mem.GetScent(i);
                if (!m.position.InBounds(map)) continue;
                float age = Mathf.Clamp01((tick - m.createdTick) / 7500f);
                float a = Mathf.Lerp(0.85f, 0.06f, age);
                if (a < 0.02f) continue;
                float sf = 0.5f + 0.5f * m.strength;
                float sz = Mathf.Lerp(18f, 5f, age) * sf;
                Glow(m.position, cam, vr, colScent, sz, a, 1f, time, fa);
            }
        }

        private static void DrawNoise(Map map, Camera cam, CellRect vr, float time, float fa)
        {
            var mem = map.GetComponent<MarkedMenMemoryGrid>();
            if (mem == null) return;
            int tick = Find.TickManager.TicksGame;
            for (int i = mem.NoiseCount - 1; i >= 0; i--)
            {
                var n = mem.GetNoise(i);
                if (!n.position.InBounds(map)) continue;
                float age = n.decayTicks > 0 ? (tick - n.createdTick) / (float)n.decayTicks : 1f;
                if (age >= 1f) continue;
                float a = Mathf.Lerp(0.9f, 0.08f, age);
                float sf = 0.5f + 0.5f * n.strength;
                float sz = Mathf.Lerp(26f, 6f, age) * sf;
                Glow(n.position, cam, vr, colNoise, sz, a, 2.2f, time, fa);

                Vector3 sp = cam.WorldToScreenPoint(n.position.ToVector3Shifted());
                sp.y = Screen.height - sp.y;
                float ra = fa * a * (0.5f + 0.5f * Mathf.Sin(time * 3.5f));
                float rs = sz * 1.6f;
                GUI.color = new Color(colNoise.r, colNoise.g, colNoise.b, ra * 0.1f);
                GUI.DrawTexture(new Rect(sp.x - rs * 0.5f, sp.y - rs * 0.5f, rs, rs), glowTex);
                GUI.color = Color.white;
            }
        }

        private static void DrawMemory(Map map, Camera cam, CellRect vr, float time, float fa)
        {
            var mem = map.GetComponent<MarkedMenMemoryGrid>();
            if (mem == null) return;
            int tick = Find.TickManager.TicksGame;
            for (int i = mem.MemoryCount - 1; i >= 0; i--)
            {
                var m = mem.GetMemory(i);
                if (!m.position.InBounds(map)) continue;
                int age = tick - m.lastSeenTick;
                if (age > 30000) continue;
                float a = Mathf.Lerp(0.6f, 0.03f, age / 30000f);
                if (a < 0.01f) continue;
                Glow(m.position, cam, vr, colMemory, 8f, a, 0.6f, time, fa);
            }
        }

        private static void DrawLabels(Map map, Camera cam, float time, float fa)
        {
            var ai = MarkedMenAIManager.GetForMap(map);
            if (ai == null) return;

            foreach (Pawn p in map.mapPawns.AllPawnsSpawned)
            {
                if (!CrossedUtility.IsInfectedPawn(p)) continue;

                Vector3 sp = cam.WorldToScreenPoint(p.DrawPos);
                sp.y = Screen.height - sp.y;

                var state = ai.GetPursuitState(p);
                string lbl = stateLabels[(int)state];
                Color tc = state switch
                {
                    MarkedPursuitState.Frenzy => new Color(1f, 0.12f, 0.08f),
                    MarkedPursuitState.Hunting => new Color(1f, 0.55f, 0f),
                    MarkedPursuitState.Searching => new Color(1f, 0.85f, 0.1f),
                    _ => new Color(0.5f, 0.5f, 0.55f)
                };

                Vector2 ts = GUI.skin.label.CalcSize(new GUIContent(lbl));
                float lw = ts.x + 14f;
                float lh = ts.y + 6f;
                float lx = sp.x - lw * 0.5f;
                float ly = sp.y - 24f;

                // shadow
                GUI.color = new Color(0f, 0f, 0f, 0.45f * fa);
                GUI.DrawTexture(new Rect(lx + 1f, ly + 1f, lw, lh), BaseContent.WhiteTex);

                // bg
                GUI.color = new Color(0.08f, 0.08f, 0.12f, 0.7f * fa);
                GUI.DrawTexture(new Rect(lx, ly, lw, lh), BaseContent.WhiteTex);

                // border
                GUI.color = new Color(tc.r, tc.g, tc.b, 0.4f * fa);
                GUI.DrawTexture(new Rect(lx, ly, lw, 1f), BaseContent.WhiteTex);
                GUI.DrawTexture(new Rect(lx, ly + lh - 1f, lw, 1f), BaseContent.WhiteTex);

                GUI.color = new Color(tc.r, tc.g, tc.b, 0.85f * fa);
                GUI.Label(new Rect(lx + 7f, ly + 3f, ts.x, ts.y), lbl);
                GUI.color = Color.white;
            }
        }

        // ─── UI Panel ──────────────────────────────────────────────
        private static void DrawUI(Map map, float time, float fa, float dt)
        {
            float pad = 10f;
            float ih = 24f;
            float tw = 22f;
            float lw = 70f;
            float pw = lw + tw + pad * 3f + 6f;

            // header
            float hh = 28f;
            int rows = 5;
            float ph = hh + pad + rows * ih + pad;

            // collapse tab size
            float tabW = 32f;
            float tabH = 40f;

            if (collapsed)
            {
                DrawCollapsedTab(tabW, tabH, fa);
                return;
            }

            // position
            if (panelPos.y < 0) panelPos.y = Screen.height - ph - pad;
            float px = panelPos.x;
            float py = panelPos.y;

            // clamp
            px = Mathf.Clamp(px, 2f - pw + tabW, Screen.width - pw - 2f);
            py = Mathf.Clamp(py, 2f, Screen.height - ph - 2f);

            Rect bodyR = new Rect(px, py + hh, pw, ph - hh);
            Rect fullR = new Rect(px, py, pw, ph);
            Rect headR = new Rect(px, py, pw, hh);

            // body bg
            GUI.color = bgPanel;
            GUI.DrawTexture(bodyR, BaseContent.WhiteTex);

            // body border
            GUI.color = borderDim;
            GUI.DrawTexture(new Rect(px, py + hh, pw, 1f), BaseContent.WhiteTex);
            GUI.DrawTexture(new Rect(px, py + ph - 1f, pw, 1f), BaseContent.WhiteTex);
            GUI.DrawTexture(new Rect(px, py + hh, 1f, ph - hh), BaseContent.WhiteTex);
            GUI.DrawTexture(new Rect(px + pw - 1f, py + hh, 1f, ph - hh), BaseContent.WhiteTex);
            GUI.color = Color.white;

            // ── header ──
            GUI.color = bgHeader;
            GUI.DrawTexture(headR, BaseContent.WhiteTex);
            GUI.color = borderDim;
            GUI.DrawTexture(new Rect(px, py, pw, 1f), BaseContent.WhiteTex);
            GUI.DrawTexture(new Rect(px, py + hh - 1f, pw, 1f), BaseContent.WhiteTex);
            GUI.DrawTexture(new Rect(px, py, 1f, hh), BaseContent.WhiteTex);
            GUI.DrawTexture(new Rect(px + pw - 1f, py, 1f, hh), BaseContent.WhiteTex);
            GUI.color = Color.white;

            // title
            GUI.color = new Color(0.7f, 0.7f, 0.8f, 0.85f * fa);
            GUI.Label(new Rect(px + 10f, py + 5f, 120f, 18f), "PREDATOR AI");
            GUI.color = Color.white;

            // collapse button
            float btnS = 18f;
            float btnX = px + pw - btnS - 6f;
            float btnY = py + (hh - btnS) * 0.5f;
            Rect collapseR = new Rect(btnX, btnY, btnS, btnS);
            if (collapseR.Contains(Event.current.mousePosition))
                GUI.color = new Color(1f, 1f, 1f, 0.6f);
            else
                GUI.color = new Color(0.5f, 0.5f, 0.6f, 0.7f * fa);
            // "−" icon for collapse
            GUI.Label(new Rect(btnX + 2f, btnY + 1f, btnS, btnS), "−");
            GUI.color = Color.white;
            if (Event.current.type == EventType.MouseDown && collapseR.Contains(Event.current.mousePosition))
            { collapsed = true; Event.current.Use(); }

            // ── body items ──
            float cy = py + hh + pad;
            float ix = px + pad + 6f;

            DrawItem(ix, ref cy, "SCENT", colScent, ref showScent, 0, lw, tw, ih, fa, time);
            DrawItem(ix, ref cy, "NOISE", colNoise, ref showNoise, 1, lw, tw, ih, fa, time);
            DrawItem(ix, ref cy, "MEMORY", colMemory, ref showMemory, 2, lw, tw, ih, fa, time);
            DrawItem(ix, ref cy, "LABELS", colLabel, ref showLabels, 3, lw, tw, ih, fa, time);

            // opacity
            float opY = cy + (ih - 18f) * 0.5f;
            GUI.color = new Color(0.55f, 0.55f, 0.6f, 0.75f * fa);
            GUI.Label(new Rect(ix, opY, lw, 18f), "OPACITY");
            float slX = ix + lw + pad;
            float slW = tw;
            float slY = cy + (ih - 14f) * 0.5f;
            Rect slR = new Rect(slX, slY, slW, 14f);
            overlayOpacity = GUI.HorizontalSlider(slR, overlayOpacity, 0.1f, 1f);
            GUI.color = Color.white;

            // mouse scroll on slider area
            if (slR.Contains(Event.current.mousePosition) && Event.current.type == EventType.ScrollWheel)
            {
                overlayOpacity = Mathf.Clamp01(overlayOpacity - Event.current.delta.y * 0.05f);
                Event.current.Use();
            }

            // ── drag logic ──
            if (Event.current.type == EventType.MouseDown && headR.Contains(Event.current.mousePosition))
            { dragging = true; dragOffset = new Vector2(Event.current.mousePosition.x - px, Event.current.mousePosition.y - py); Event.current.Use(); }
            if (dragging && Event.current.type == EventType.MouseDrag)
            {
                panelPos = new Vector2(Event.current.mousePosition.x - dragOffset.x, Event.current.mousePosition.y - dragOffset.y);
                Event.current.Use();
            }
            if (dragging && (Event.current.type == EventType.MouseUp || Event.current.rawType == EventType.MouseUp))
            { dragging = false; SnapPanel(ph); Event.current.Use(); }

            // hover detection
            float hy = py + hh + pad;
            hoveredIdx = -1;
            for (int i = 0; i < 4; i++)
            {
                Rect r = new Rect(ix, hy, lw + tw + pad, ih);
                if (r.Contains(Event.current.mousePosition)) hoveredIdx = i;
                hy += ih;
            }

            // flash decay
            for (int i = 0; i < 5; i++) flashAlpha[i] = Mathf.Max(0f, flashAlpha[i] - dt * 2f);
        }

        private static void DrawCollapsedTab(float tabW, float tabH, float fa)
        {
            float cx = panelPos.x;
            float cy = panelPos.y;

            Rect tabR = new Rect(cx, cy, tabW, tabH);
            GUI.color = new Color(0.06f, 0.06f, 0.08f, 0.7f * fa);
            GUI.DrawTexture(tabR, BaseContent.WhiteTex);
            GUI.color = borderDim;
            GUI.DrawTexture(new Rect(cx, cy, tabW, 1f), BaseContent.WhiteTex);
            GUI.DrawTexture(new Rect(cx, cy + tabH - 1f, tabW, 1f), BaseContent.WhiteTex);
            GUI.DrawTexture(new Rect(cx, cy, 1f, tabH), BaseContent.WhiteTex);
            GUI.DrawTexture(new Rect(cx + tabW - 1f, cy, 1f, tabH), BaseContent.WhiteTex);
            GUI.color = Color.white;

            GUI.color = new Color(0.5f, 0.5f, 0.6f, 0.7f * fa);
            GUI.Label(new Rect(cx + 7f, cy + 8f, tabW, tabH), "▶");
            GUI.color = Color.white;

            if (Event.current.type == EventType.MouseDown && tabR.Contains(Event.current.mousePosition))
            { collapsed = false; Event.current.Use(); }

            if (Event.current.type == EventType.MouseDown && tabR.Contains(Event.current.mousePosition))
            { dragging = true; dragOffset = new Vector2(Event.current.mousePosition.x - cx, Event.current.mousePosition.y - cy); Event.current.Use(); }
            if (dragging && Event.current.type == EventType.MouseDrag)
            {
                panelPos = new Vector2(Event.current.mousePosition.x - dragOffset.x, Event.current.mousePosition.y - dragOffset.y);
                Event.current.Use();
            }
            if (dragging && (Event.current.type == EventType.MouseUp || Event.current.rawType == EventType.MouseUp))
            { dragging = false; Event.current.Use(); }
        }

        private static void DrawItem(float x, ref float y, string label, Color color,
            ref bool enabled, int idx, float lw, float tw, float ih, float fa, float time)
        {
            bool hovered = idx == hoveredIdx;
            float ds = 10f;
            float dy = y + (ih - ds) * 0.5f;

            // hover bg
            if (hovered)
            {
                GUI.color = new Color(1f, 1f, 1f, 0.04f);
                GUI.DrawTexture(new Rect(x - 4f, y, lw + tw + 12f, ih), BaseContent.WhiteTex);
                GUI.color = Color.white;
            }

            // dot
            float dotA = enabled ? (0.9f + 0.1f * Mathf.Sin(time * 1.5f + idx)) : 0.25f;
            GUI.color = new Color(color.r, color.g, color.b, dotA * fa);
            GUI.DrawTexture(new Rect(x, dy, ds, ds), glowTex);
            GUI.color = Color.white;

            // label
            float lx = x + ds + 6f;
            float ly2 = y + (ih - 16f) * 0.5f;
            Color lc = hovered ? new Color(1f, 1f, 1f, 0.95f * fa) : new Color(0.85f, 0.85f, 0.9f, (enabled ? 0.9f : 0.3f) * fa);
            GUI.color = lc;
            GUI.Label(new Rect(lx, ly2, lw, 16f), label);
            GUI.color = Color.white;

            // toggle
            float tx = x + lw + ds + 8f;
            float ty = y + (ih - tw) * 0.5f;
            Rect tr = new Rect(tx, ty, tw, tw);

            // toggle bg
            Color tbg = enabled ? new Color(color.r * 0.3f, color.g * 0.3f, color.b * 0.3f, 0.8f * fa)
                                : new Color(0.2f, 0.2f, 0.25f, 0.6f * fa);
            GUI.color = tbg;
            GUI.DrawTexture(tr, BaseContent.WhiteTex);
            GUI.color = borderDim;
            GUI.DrawTexture(new Rect(tx, ty, tw, 1f), BaseContent.WhiteTex);
            GUI.DrawTexture(new Rect(tx, ty + tw - 1f, tw, 1f), BaseContent.WhiteTex);
            GUI.DrawTexture(new Rect(tx, ty, 1f, tw), BaseContent.WhiteTex);
            GUI.DrawTexture(new Rect(tx + tw - 1f, ty, 1f, tw), BaseContent.WhiteTex);
            GUI.color = Color.white;

            // checkmark
            if (enabled)
            {
                float flash = flashAlpha[idx];
                float cm = tw * 0.45f;
                GUI.color = new Color(color.r, color.g, color.b, (0.75f + flash * 0.25f) * fa);
                GUI.DrawTexture(new Rect(tx + (tw - cm) * 0.5f, ty + (tw - cm) * 0.5f, cm, cm), BaseContent.WhiteTex);
                GUI.color = Color.white;
            }

            // click
            if (Event.current.type == EventType.MouseDown && tr.Contains(Event.current.mousePosition) && idx < 4)
            {
                enabled = !enabled;
                flashAlpha[idx] = 1f;
                Event.current.Use();
            }

            y += ih;
        }

        private static void SnapPanel(float ph)
        {
            float edgeDist = 40f;
            float sx = panelPos.x;
            float sy = panelPos.y;
            if (sx < edgeDist) sx = 2f;
            else if (sx > Screen.width - edgeDist) sx = Screen.width - (panelPos.x > Screen.width * 0.5f ? 240f : 200f);
            if (sy < edgeDist) sy = 2f;
            else if (sy > Screen.height - edgeDist) sy = Screen.height - ph - 2f;
            panelPos = new Vector2(sx, sy);
        }

        [DebugAction("The Marked Men", "Add debug scent at cursor",
            allowedGameStates = AllowedGameStates.PlayingOnMap,
            actionType = DebugActionType.ToolMap, displayPriority = 799)]
        public static void AddScentAtCursor()
        {
            if (!TheMarkedMenSettings.DebugActionsEnabled) return;
            IntVec3 c = UI.MouseCell();
            if (!c.InBounds(Find.CurrentMap)) return;
            MarkedMenMemoryGrid.GetForMap(Find.CurrentMap).AddScent(c, 1f, null);
            Log.Message($"[TMM] Scent at {c}.");
        }

        [DebugAction("The Marked Men", "Add debug noise at cursor",
            allowedGameStates = AllowedGameStates.PlayingOnMap,
            actionType = DebugActionType.ToolMap, displayPriority = 798)]
        public static void AddNoiseAtCursor()
        {
            if (!TheMarkedMenSettings.DebugActionsEnabled) return;
            IntVec3 c = UI.MouseCell();
            if (!c.InBounds(Find.CurrentMap)) return;
            MarkedMenMemoryGrid.GetForMap(Find.CurrentMap).AddNoise(c, 1f, 3000);
            Log.Message($"[TMM] Noise at {c}.");
        }
    }
}
