using System;
using System.Collections.Generic;
using MinecraftUltrakill.Core;
using UnityEngine;
using UnityEngine.Rendering;

namespace MinecraftUltrakill.Presentation
{
    // All pixels/geometry are authored here; no external game or Minecraft files.
    public sealed class ProceduralArt : IDisposable
    {
        readonly List<UnityEngine.Object> resources = new List<UnityEngine.Object>();
        readonly Dictionary<string, Material> colors = new Dictionary<string, Material>();
        readonly Material[][] blocks = new Material[8][];
        public readonly Mesh Cube;
        public readonly Material GhostValid, GhostInvalid;
        public ProceduralArt()
        {
            Cube = MakeCube(); resources.Add(Cube);
            foreach (BlockKind kind in Enum.GetValues(typeof(BlockKind)))
            {
                blocks[(int)kind] = new Material[6];
                for (int face = 0; face < 6; face++) blocks[(int)kind][face] = PixelMaterial(kind, face);
            }
            GhostValid = Transparent(new Color(.15f, 1f, .3f, .28f));
            GhostInvalid = Transparent(new Color(1f, .15f, .1f, .28f));
        }
        public Material[] BlockMaterials(BlockKind kind) { return blocks[(int)kind]; }
        public Material ColorMaterial(Color color)
        {
            string key = color.ToString(); Material m;
            if (colors.TryGetValue(key, out m)) return m;
            m = new Material(Shader.Find("Standard") ?? Shader.Find("Unlit/Color")); m.color = color;
            resources.Add(m); colors.Add(key, m); return m;
        }
        Material Transparent(Color c)
        {
            var m = new Material(Shader.Find("Standard") ?? Shader.Find("Unlit/Color")); m.color = c;
            m.SetFloat("_Mode", 3); m.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha); m.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
            m.SetInt("_ZWrite", 0); m.EnableKeyword("_ALPHABLEND_ON"); m.renderQueue = 3000; resources.Add(m); return m;
        }
        Material PixelMaterial(BlockKind kind, int face)
        {
            var texture = new Texture2D(16, 16, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
            var random = new System.Random(719 + (int)kind * 127 + face);
            for (int y = 0; y < 16; y++) for (int x = 0; x < 16; x++)
            {
                Color c;
                switch (kind)
                {
                    case BlockKind.Grass: c = face == 2 || (face != 3 && y >= 12 + (x % 3)) ? new Color(.24f, .55f, .13f) : new Color(.43f, .27f, .14f); break;
                    case BlockKind.Dirt: c = new Color(.43f, .27f, .14f); break;
                    case BlockKind.Stone: c = new Color(.49f, .5f, .51f); break;
                    case BlockKind.Cobblestone: c = x % 7 == 0 || y % 5 == 0 ? new Color(.22f, .23f, .24f) : new Color(.47f, .48f, .49f); break;
                    case BlockKind.OakPlanks: c = y % 4 == 0 || (x + (y / 4) * 5) % 15 == 0 ? new Color(.43f, .28f, .12f) : new Color(.67f, .48f, .25f); break;
                    case BlockKind.Glass: c = new Color(.6f, .9f, 1f, x == 0 || y == 0 || x == 15 || y == 15 || x == y ? .7f : .12f); break;
                    case BlockKind.TNT: c = face < 2 || face > 3 ? (y >= 5 && y <= 10 ? Color.white : new Color(.75f, .12f, .07f)) : new Color(.6f, .2f, .1f); break;
                    default: c = new Color(.15f, .15f, .17f); break;
                }
                float noise = ((float)random.NextDouble() - .5f) * (kind == BlockKind.Bedrock ? .6f : .16f);
                if (kind != BlockKind.Glass) c = new Color(Mathf.Clamp01(c.r + noise), Mathf.Clamp01(c.g + noise), Mathf.Clamp01(c.b + noise), 1);
                if (kind == BlockKind.TNT && face != 2 && face != 3 && y >= 6 && y <= 9)
                {
                    // Tiny original pixel lettering, T N T.
                    bool t = (x >= 2 && x <= 4 || x >= 11 && x <= 13) && (y == 9 || x == 3 || x == 12);
                    bool n = x >= 6 && x <= 9 && (x == 6 || x == 9 || x - 6 == 9 - y);
                    if (t || n) c = Color.black;
                }
                texture.SetPixel(x, y, c);
            }
            texture.Apply(); resources.Add(texture);
            Material m = kind == BlockKind.Glass ? Transparent(Color.white) : new Material(Shader.Find("Standard") ?? Shader.Find("Unlit/Texture"));
            if (kind != BlockKind.Glass) resources.Add(m);
            m.mainTexture = texture; return m;
        }
        public GameObject Box(string name, Transform parent, Vector3 pos, Vector3 size, Material material)
        {
            var go = new GameObject(name); go.transform.SetParent(parent, false); go.transform.localPosition = pos; go.transform.localScale = size;
            go.AddComponent<MeshFilter>().sharedMesh = Cube;
            go.AddComponent<MeshRenderer>().sharedMaterials = new[] { material, material, material, material, material, material };
            return go;
        }
        public GameObject Block(string name, Transform parent, BlockKind kind, Vector3 position, float size, bool collision)
        {
            var go = Box(name, parent, position, Vector3.one * size, blocks[(int)kind][0]);
            go.GetComponent<Renderer>().sharedMaterials = blocks[(int)kind];
            if (collision) go.AddComponent<BoxCollider>();
            return go;
        }
        public Texture Icon(BlockKind kind) { return blocks[(int)kind][0].mainTexture; }
        static Mesh MakeCube()
        {
            var verts = new List<Vector3>(); var uv = new List<Vector2>(); var indices = new List<int[]>();
            Vector3[] normals = { Vector3.forward, Vector3.back, Vector3.up, Vector3.down, Vector3.right, Vector3.left };
            foreach (var n in normals)
            {
                Vector3 u = Vector3.Cross(Mathf.Abs(n.y) > .5f ? Vector3.forward : Vector3.up, n).normalized;
                Vector3 v = Vector3.Cross(n, u); int i = verts.Count;
                verts.Add((n - u - v) * .5f); verts.Add((n + u - v) * .5f); verts.Add((n + u + v) * .5f); verts.Add((n - u + v) * .5f);
                uv.Add(new Vector2(0, 0)); uv.Add(new Vector2(1, 0)); uv.Add(new Vector2(1, 1)); uv.Add(new Vector2(0, 1));
                indices.Add(new[] { i, i + 1, i + 2, i, i + 2, i + 3 });
            }
            var mesh = new Mesh { name = "OriginalVoxelCube", vertices = verts.ToArray(), uv = uv.ToArray(), subMeshCount = 6 };
            for (int face = 0; face < 6; face++) mesh.SetTriangles(indices[face], face);
            mesh.RecalculateNormals(); mesh.RecalculateBounds(); return mesh;
        }
        public GameObject Tool(Transform parent, ItemKind kind, float pixel)
        {
            var root = new GameObject("Diamond " + kind); root.transform.SetParent(parent, false);
            var diamond = ColorMaterial(new Color(.14f, .85f, .8f)); var edge = ColorMaterial(new Color(.03f, .35f, .4f));
            var wood = ColorMaterial(new Color(.43f, .26f, .11f));
            // 3D pixel weapon, separate handle, guard and diamond head.
            for (int y = 0; y < 14; y++) for (int x = 0; x < 9; x++)
            {
                Material m = null;
                if (x == 4 && y < 6) m = wood;
                if (kind == ItemKind.Sword)
                {
                    if (y == 5 && x >= 1 && x <= 7) m = edge;
                    if (y >= 6 && y <= 12 && x >= 3 && x <= 5) m = x == 4 ? diamond : edge;
                    if (y == 13 && x == 4) m = diamond;
                }
                else
                {
                    if (x == 4 && y < 11) m = wood;
                    if (y >= 9 && y <= 12 && x >= 0 && x <= 5) m = y == 9 || y == 12 || x == 0 ? edge : diamond;
                }
                if (m != null) Box("pixel", root.transform, new Vector3((x - 4) * pixel, y * pixel, 0), new Vector3(pixel, pixel, pixel * .8f), m);
            }
            return root;
        }
        public void Dispose() { foreach (var r in resources) if (r) UnityEngine.Object.Destroy(r); resources.Clear(); }
    }
}
