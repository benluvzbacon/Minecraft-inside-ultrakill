using System;
using MinecraftUltrakill.Core;
using MinecraftUltrakill.Integration;
using UnityEngine;
using UnityEngine.Rendering;

namespace MinecraftUltrakill.Presentation
{
    public sealed class SteveModel : IDisposable
    {
        readonly PlayerContext player;
        readonly GameObject body, view, viewSword, viewAxe, worldSword, worldAxe, heldBlock;
        readonly Transform leftArm, rightArm, leftLeg, rightLeg, viewPivot;
        readonly ProceduralArt art;
        SteveCameraRig rig;
        float swingStart = -10f, swingLength = .35f;
        ItemKind lastItem = (ItemKind)(-1); BlockKind lastBlock = (BlockKind)(-1);
        public SteveModel(PlayerContext p, ProceduralArt graphics)
        {
            player = p; art = graphics;
            try
            {
            var skin = art.ColorMaterial(new Color(.65f, .44f, .3f));
            var shirt = art.ColorMaterial(new Color(.04f, .63f, .68f));
            var pants = art.ColorMaterial(new Color(.22f, .24f, .67f));
            var hair = art.ColorMaterial(new Color(.19f, .12f, .07f));
            var shoes = art.ColorMaterial(new Color(.24f, .23f, .23f));
            body = new GameObject("MCUK_SteveWorld"); body.transform.SetParent(p.Transform, false);
            float scale = Mathf.Clamp(p.Collider.bounds.size.y / 2f, .5f, 2f); body.transform.localScale = Vector3.one * scale;
            art.Box("Torso", body.transform, new Vector3(0, 1.125f, 0), new Vector3(.5f, .75f, .25f), shirt);
            var head = art.Box("CubicHead", body.transform, new Vector3(0, 1.75f, 0), Vector3.one * .5f, skin);
            art.Box("Hair", head.transform, new Vector3(0, .3f, -.01f), new Vector3(1.02f, .42f, 1.02f), hair);
            art.Box("LeftEye", head.transform, new Vector3(-.22f, .06f, .51f), new Vector3(.16f, .12f, .025f), art.ColorMaterial(new Color(.35f, .35f, .8f)));
            art.Box("RightEye", head.transform, new Vector3(.22f, .06f, .51f), new Vector3(.16f, .12f, .025f), art.ColorMaterial(new Color(.35f, .35f, .8f)));
            art.Box("Nose", head.transform, new Vector3(0, -.13f, .52f), new Vector3(.18f, .13f, .06f), skin);
            art.Box("Beard", head.transform, new Vector3(0, -.31f, .51f), new Vector3(.42f, .17f, .025f), hair);
            leftArm = Limb("LeftArm", new Vector3(-.375f, 1.5f, 0), shirt, skin, false);
            rightArm = Limb("RightArm", new Vector3(.375f, 1.5f, 0), shirt, skin, false);
            leftLeg = Limb("LeftLeg", new Vector3(-.125f, .75f, 0), pants, shoes, true);
            rightLeg = Limb("RightLeg", new Vector3(.125f, .75f, 0), pants, shoes, true);
            worldSword = art.Tool(rightArm, ItemKind.Sword, .06f); worldAxe = art.Tool(rightArm, ItemKind.Axe, .06f);
            foreach (var w in new[] { worldSword, worldAxe }) { w.transform.localPosition = new Vector3(0, -.67f, .1f); w.transform.localRotation = Quaternion.Euler(80, 0, 0); }
            foreach (var renderer in body.GetComponentsInChildren<Renderer>()) renderer.shadowCastingMode = ShadowCastingMode.ShadowsOnly;
            // Existing game camera remains authoritative. Viewmodel is relative to its real eye position.
            view = new GameObject("MCUK_SteveView"); view.transform.SetParent(p.Camera.transform, false);
            viewPivot = new GameObject("AttackPivot").transform; viewPivot.SetParent(view.transform, false); viewPivot.localPosition = new Vector3(.34f, -.32f, .65f);
            art.Box("SteveForearm", viewPivot, new Vector3(0, -.12f, -.12f), new Vector3(.18f, .36f, .18f), skin);
            viewSword = art.Tool(viewPivot, ItemKind.Sword, .045f); viewAxe = art.Tool(viewPivot, ItemKind.Axe, .045f);
            heldBlock = art.Block("HeldBlock", viewPivot, BlockKind.Grass, new Vector3(0, .1f, .1f), .28f, false);
            foreach (var renderer in view.GetComponentsInChildren<Renderer>()) { renderer.shadowCastingMode = ShadowCastingMode.Off; renderer.receiveShadows = false; }
            rig = p.Camera.gameObject.AddComponent<SteveCameraRig>(); rig.Initialize(p, this);
            }
            catch { Dispose(); throw; }
        }
        Transform Limb(string name, Vector3 position, Material upper, Material lower, bool leg)
        {
            var pivot = new GameObject(name).transform; pivot.SetParent(body.transform, false); pivot.localPosition = position;
            art.Box("Upper", pivot, new Vector3(0, -.19f, 0), new Vector3(.25f, .38f, .25f), upper);
            art.Box("Lower", pivot, new Vector3(0, -.565f, 0), new Vector3(.25f, .37f, .25f), leg ? upper : lower);
            if (leg) art.Box("Boot", pivot, new Vector3(0, -.7f, .01f), new Vector3(.25f, .1f, .27f), lower);
            return pivot;
        }
        public bool Owns(Transform t) { return t.IsChildOf(body.transform) || t.IsChildOf(view.transform); }
        public void Swing(float duration) { swingStart = Time.time; swingLength = Mathf.Max(.18f, Mathf.Min(duration, .65f)); }
        public void Tick(ItemKind item, BlockKind block, bool mining)
        {
            float speed = new Vector2(player.Body.velocity.x, player.Body.velocity.z).magnitude;
            float walk = Mathf.Sin(Time.time * 8f) * Mathf.Min(speed * 5f, 32f);
            body.transform.position = new Vector3(player.Transform.position.x, player.Collider.bounds.min.y, player.Transform.position.z);
            body.transform.rotation = Quaternion.Euler(0, player.Camera.transform.eulerAngles.y, 0);
            leftLeg.localRotation = Quaternion.Euler(walk, 0, 0); rightLeg.localRotation = Quaternion.Euler(-walk, 0, 0);
            leftArm.localRotation = Quaternion.Euler(-walk, 0, 0);
            float phase = Mathf.Clamp01((Time.time - swingStart) / swingLength);
            float swing = phase < 1 ? Mathf.Sin(phase * Mathf.PI) : mining ? Mathf.Abs(Mathf.Sin(Time.time * 7f)) * .5f : 0;
            rightArm.localRotation = Quaternion.Euler(walk - swing * 95f, 0, 0);
            viewPivot.localRotation = Quaternion.Euler(-15 + swing * 70, -10 - swing * 25, -15 + swing * 35);
            viewPivot.localPosition = new Vector3(.34f - swing * .15f, -.32f + walk * .0004f, .65f - swing * .08f);
            if (item != lastItem)
            {
                viewSword.SetActive(item == ItemKind.Sword); worldSword.SetActive(item == ItemKind.Sword);
                viewAxe.SetActive(item == ItemKind.Axe); worldAxe.SetActive(item == ItemKind.Axe);
                heldBlock.SetActive(item == ItemKind.Block); lastItem = item;
            }
            if (block != lastBlock) { heldBlock.GetComponent<Renderer>().sharedMaterials = art.BlockMaterials(block); lastBlock = block; }
        }
        // Other game cameras can show a real body; default first-person renderer is shadows-only.
        public void ToggleView() { if (rig) rig.Toggle(); }
        public void SetViewVisible(bool visible) { if (view) view.SetActive(visible); }
        public void SetWorldVisible(bool visible)
        { foreach (var r in body.GetComponentsInChildren<Renderer>()) r.shadowCastingMode = visible ? ShadowCastingMode.On : ShadowCastingMode.ShadowsOnly; }
        public void Dispose() { if (rig) { rig.Release(); UnityEngine.Object.Destroy(rig); } if (body) UnityEngine.Object.Destroy(body); if (view) UnityEngine.Object.Destroy(view); }
    }
}
