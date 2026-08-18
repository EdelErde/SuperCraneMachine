# Metaball Liquid 2D (URP)

A 2D "liquid" effect for Unity: any number of spherical sprites that visually
merge into one connected blob when they get close, and pinch apart again when
they separate. Built for the **Universal Render Pipeline (URP)**.

## How it works (short version)

Individual sprites can't "see" each other's positions in a normal per-object
shader, so this uses the standard real-time metaball technique instead:

1. An offscreen **Metaball Camera** renders only your blob sprites (on a
   dedicated `Liquid` layer) into a private RenderTexture, using an
   **additive**, soft-radial-gradient shader (`MetaballBlob.shader`). Where
   blobs overlap, their brightness adds up.
2. A visible **Liquid Composite** quad reads that texture with
   `MetaballComposite.shader` and thresholds it: bright enough → solid liquid
   color (anti-aliased edge via `smoothstep`), too dim → transparent. Because
   the gap between two nearby blobs is bright enough on its own, it gets
   filled in too — that's the merge.
3. Your Main Camera never renders the raw blob sprites directly (its culling
   mask excludes `Liquid`) — only the composite quad, which is what the
   player actually sees.

## Folder contents

```
Shaders/
  MetaballBlob.shader        - additive per-blob gradient (offscreen only)
  MetaballComposite.shader   - threshold + tint + rim highlight (visible)
Scripts/
  MetaballFieldCamera.cs     - put on the Metaball Camera; manages the RenderTexture
  MetaballComposite.cs       - put on the visible quad; binds the texture, tracks the camera
  MetaballBlob.cs            - optional, put on each blob sprite for per-instance tuning
  Demo/
    MetaballBlobSpawner.cs   - spawns wandering test blobs, not required for the effect
Editor/
  MetaballSetupEditor.cs     - Tools > Metaball Liquid 2D > Create Metaball Setup
```

## Quick start (recommended)

1. Copy the `MetaballLiquid2D` folder anywhere under your project's `Assets/`.
2. In the Unity menu bar: **Tools > Metaball Liquid 2D > Create Metaball
   Setup**. This will:
   - Create a `Liquid` layer (if one doesn't already exist).
   - Create `Assets/MetaballLiquid2D/Generated/` with a `MetaballField`
     RenderTexture and two materials (`M_MetaballBlob`, `M_MetaballComposite`).
   - Create a `Metaball Camera` GameObject (offscreen, orthographic,
     culling mask = `Liquid`) with `MetaballFieldCamera` attached.
   - Create a `Liquid Composite` quad with `MetaballComposite` attached,
     already wired to the camera above.
   - Exclude the `Liquid` layer from your scene's Main Camera, if one exists.
3. Make a blob prefab: a `GameObject` with a `SpriteRenderer` using a filled
   circle sprite, material = `M_MetaballBlob`, layer = `Liquid`. Add the
   `MetaballBlob` component if you want per-instance intensity/color control.

   **Important sprite import setting**: the circle sprite's **Mesh Type**
   must be **Full Rect**, not "Tight". The blob shader assumes UV (0,0)-(1,1)
   maps to the sprite's full square bounding box so its radial-distance math
   is correct; a tight/trimmed mesh will distort the gradient.
4. Drop a few of that prefab into the scene (or use
   `Demo/MetaballBlobSpawner.cs` on an empty GameObject to spawn a wandering
   test batch), press Play, and move them near each other.
5. Select `Liquid Composite` and tune `M_MetaballComposite`:
   - **Threshold** — how close blobs need to be before they visibly merge.
     Lower = merges sooner / blobs look bigger on their own. Higher = blobs
     stay more separate until very close.
   - **Edge Softness** — anti-aliasing width of the outline. Too low can
     look jagged at low RenderTexture resolutions; too high looks blurry.
   - **Liquid Color / Rim Color / Rim Width / Rim Intensity** — visual
     styling; the rim gives a soft highlight near the edge to read as
     "liquid" rather than a flat blob.
   - On `M_MetaballBlob`: **Intensity** (how strongly a blob contributes to
     the field — raise this if blobs aren't merging until they nearly
     touch) and **Falloff** (how sharply a single blob's own brightness
     drops off toward its edge).

## Manual setup (if you'd rather not use the menu command, or need to adapt it)

1. Add a `Liquid` layer under **Project Settings > Tags and Layers**.
2. Create a `RenderTexture` asset (Create > Render Texture). A single-channel
   float format (`R Half`) avoids visible banding on the threshold edge, but
   `ARGB32` also works if your target platform doesn't support it.
3. Create an empty GameObject, add a `Camera` component:
   - Culling Mask: `Liquid` only
   - Clear Flags: Solid Color, background = black
   - Projection: Orthographic, sized/positioned to cover the area your
     blobs move around in
   - Output Texture: the RenderTexture from step 2
   - Add `MetaballFieldCamera` (it will manage the texture for you instead
     of you assigning it by hand, if you leave Output Texture unset and
     just set Texture Width/Height on the component)
4. Create a Quad (`GameObject > 3D Object > Quad`), remove its Collider,
   give it a material using `MetaballComposite.shader`, add the
   `MetaballComposite` component and drag the camera from step 3 into its
   `Field Camera` slot.
5. On your Main Camera, remove `Liquid` from its Culling Mask.
6. Build a blob prefab as described in step 3 of Quick Start above.

## Tuning tips

- **Blobs never merge / stay as separate circles**: lower `Threshold` on the
  composite material, or raise `Intensity` on the blob material — the field
  values in the gap between two blobs need to add up past the threshold.
- **Everything is one big blob even when apart**: raise `Threshold`, or
  lower `Intensity`/raise `Falloff` on the blob material so a single blob's
  field drops off faster.
- **Jagged/pixelated edges**: increase the RenderTexture resolution
  (`Texture Width/Height` on `MetaballFieldCamera`), and/or increase
  `Edge Softness` slightly.
- **Blurry, soft-focus edges**: decrease `Edge Softness`, and/or increase
  RenderTexture resolution.
- **Blobs need to render behind/in front of other (non-liquid) sprites
  individually**: this setup draws the whole liquid as one flat composite
  layer, so it doesn't natively support per-blob depth-sorting against other
  objects. That needs a different technique (per-sprite shader with a
  neighbor-position array) — ask if you need that variant instead.
- **Performance with many blobs**: the field RenderTexture cost scales with
  its resolution, not directly with blob count, so this scales well to a lot
  of blobs. If you're using `MetaballBlob.cs`'s per-instance
  MaterialPropertyBlock overrides on hundreds of blobs and see it show up in
  the profiler, consider dropping per-instance intensity variation and just
  using material defaults for better batching.

## Requirements

- Unity with the Universal Render Pipeline (URP) package installed.
- Tested against modern URP HLSL includes (`Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl`).
  If your URP package version has moved/renamed this include path, update
  the `#include` line at the top of both shaders' fragment programs.
