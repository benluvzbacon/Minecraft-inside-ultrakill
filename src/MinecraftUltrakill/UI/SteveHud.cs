using MinecraftUltrakill.Core;
using UnityEngine;

namespace MinecraftUltrakill.UI
{
    public sealed class SteveHud
    {
        readonly Plugin plugin;
        readonly Hotbar bar;
        public bool Palette { get; private set; }
        public SteveHud(Plugin p, Hotbar inventory) { plugin = p; bar = inventory; }
        public void Reset() { Palette = false; }
        public void InputTick()
        {
            if (plugin.InputKeys.Down(plugin.Settings.PaletteKey.Value)) { Palette = !Palette; plugin.Mining.Reset(); }
            if (Palette)
            {
                if (plugin.InputKeys.Down(KeyCode.Escape)) Palette = false;
                for (int i = 0; i < 8; i++) if (plugin.InputKeys.Down((KeyCode)((int)KeyCode.Alpha1 + i)))
                { if (bar.Item != ItemKind.Block) bar.Select(0); bar.Assign((BlockKind)i); Palette = false; }
                return;
            }
            for (int i = 0; i < 9; i++) if (plugin.InputKeys.Down((KeyCode)((int)KeyCode.Alpha1 + i))) bar.Select(i);
            float scroll = plugin.InputKeys.Scroll();
            if (scroll != 0) { bar.Scroll(scroll > 0 ? -1 : 1); plugin.Mining.Reset(); }
        }
        public void Draw()
        {
            if (!plugin.Characters.IsSteve || !plugin.Characters.Player.Playable) return;
            var old = GUI.color; var oldMatrix = GUI.matrix;
            float scale = Mathf.Clamp(Screen.height / 900f, .65f, 2f);
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1));
            float width = Screen.width / scale, height = Screen.height / scale;
            float left = width * .5f - 9 * 54 * .5f;
            for (int i = 0; i < 9; i++)
            {
                var rect = new Rect(left + i * 54, height - 68, 52, 52);
                GUI.color = i == bar.Selected ? new Color(.3f, 1f, .6f) : Color.gray; GUI.Box(rect, ""); GUI.color = Color.white;
                if (i < 7) GUI.DrawTexture(new Rect(rect.x + 11, rect.y + 5, 30, 30), plugin.Art.Icon(bar.Blocks[i]), ScaleMode.ScaleToFit);
                else DrawToolIcon(new Rect(rect.x + 9, rect.y + 4, 32, 32), i == 7);
                GUI.Label(new Rect(rect.x + 5, rect.y + 33, 48, 20), (i + 1).ToString());
            }
            string selected = bar.Item == ItemKind.Block ? Name(bar.Block) : "Diamond " + bar.Item;
            GUI.Label(new Rect(width * .5f - 100, height - 94, 260, 24), selected + "  (creative)");
            GUI.Label(new Rect(width * .5f - 5, height * .5f - 12, 30, 30), "+");
            if (plugin.Melee.FeedbackUntil > Time.time)
            { GUI.color = Color.red; GUI.Label(new Rect(width * .5f - 7, height * .5f - 12, 30, 30), "×"); GUI.color = Color.white; }
            if (bar.Item == ItemKind.Block && plugin.World != null && !Palette)
            {
                GUI.color = plugin.World.PreviewValid ? new Color(.3f, 1f, .6f) : new Color(1f, .65f, .3f);
                GUI.Label(new Rect(width * .5f - 230, height - 120, 520, 24), GridPlacement.Explain(plugin.World.Failure));
                GUI.color = Color.white;
                GUI.Label(new Rect(18, height - 58, 260, 24), "Blocks: " + plugin.World.Blocks.Count + "/" + plugin.Settings.MaxBlocks.Value + "  |  E: palette");
            }
            if (plugin.Mining.Active)
            {
                var r = new Rect(width * .5f - 90, height * .5f + 40, 180, 22); GUI.Box(r, "");
                GUI.color = Color.green; GUI.DrawTexture(new Rect(r.x + 2, r.y + 2, (r.width - 4) * plugin.Mining.Progress, r.height - 4), Texture2D.whiteTexture);
                GUI.color = Color.white; GUI.Label(new Rect(r.x, r.y + 25, 200, 24), plugin.Mining.Bedrock ? "Bedrock is indestructible" : "Mining...");
            }
            if (Palette)
            {
                var rect = new Rect(width * .5f - 190, height * .5f - 185, 380, 290);
                GUI.Box(rect, "Block palette — press 1–8 to assign selected slot");
                for (int i = 0; i < 8; i++)
                {
                    float x = rect.x + 16 + (i % 2) * 182, y = rect.y + 38 + (i / 2) * 58;
                    GUI.DrawTexture(new Rect(x, y, 34, 34), plugin.Art.Icon((BlockKind)i));
                    GUI.Label(new Rect(x + 40, y + 6, 140, 30), (i + 1) + ": " + Name((BlockKind)i));
                }
            }
            if (plugin.NoticeUntil > Time.unscaledTime) GUI.Label(new Rect(18, 18, width - 36, 60), plugin.NoticeText);
            GUI.matrix = oldMatrix; GUI.color = old;
        }
        static string Name(BlockKind kind) { return kind == BlockKind.OakPlanks ? "Oak Planks" : kind == BlockKind.Grass ? "Grass Block" : kind.ToString(); }
        static void DrawToolIcon(Rect r, bool sword)
        {
            Color old = GUI.color; float pixel = r.width / 14;
            for (int y = 0; y < 14; y++) for (int x = 0; x < 9; x++)
            {
                bool handle = x == 4 && y < (sword ? 6 : 11);
                bool head = sword ? y >= 6 && x >= 3 && x <= 5 : y >= 9 && y <= 12 && x <= 5;
                bool guard = sword && y == 5 && x >= 1 && x <= 7;
                if (!(handle || head || guard)) continue;
                GUI.color = head ? Color.cyan : guard ? new Color(.03f, .35f, .4f) : new Color(.43f, .26f, .11f);
                GUI.DrawTexture(new Rect(r.x + x * pixel, r.y + (13 - y) * pixel, pixel, pixel), Texture2D.whiteTexture);
            }
            GUI.color = old;
        }
    }
}
