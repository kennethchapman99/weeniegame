using UnityEngine;

namespace CheddarAndCocoa.Game
{
    /// <summary>
    /// Runtime handles for the first DRAFT art import. These are intentionally light-touch: the
    /// generated arena still carries gameplay readability, while imported sheets/portraits appear as
    /// replaceable visual badges until final transparent gameplay sprites are authored.
    /// </summary>
    public static class ArenaDraftArt
    {
        public enum SpriteId
        {
            CheddarPortrait,
            CheddarPoses,
            CocoaPortrait,
            CocoaPoses,
            SquirrelCharacter,
            EagleReference,
            CoyoteReference,
            BunnyReference,
            BackyardProps,
            UiKit,
            Vfx
        }

        public const string CheddarPortraitBadgeName = "DraftCheddarPortraitBadge";
        public const string CocoaPortraitBadgeName = "DraftCocoaPortraitBadge";
        public const string SquirrelBadgeName = "DraftSquirrelCharacterBadge";
        public const string EagleBadgeName = "DraftEagleThreatBadge";
        public const string CoyoteBadgeName = "DraftCoyoteThreatBadge";
        public const string BunnyCameoName = "DraftBunnyYardCameo";
        public const string BackyardPropsBadgeName = "DraftBackyardPropsBadge";
        public const string VfxBarkBadgeName = "DraftVfxBarkBadge";

        public static string PathFor(SpriteId id)
        {
            return id switch
            {
                SpriteId.CheddarPortrait => "ArenaDraft/Characters/Dogs/cheddar_portrait",
                SpriteId.CheddarPoses => "ArenaDraft/Characters/Dogs/cheddar_poses",
                SpriteId.CocoaPortrait => "ArenaDraft/Characters/Dogs/cocoa_portrait",
                SpriteId.CocoaPoses => "ArenaDraft/Characters/Dogs/cocoa_poses",
                SpriteId.SquirrelCharacter => "ArenaDraft/Characters/Squirrel/squirrel_character",
                SpriteId.EagleReference => "ArenaDraft/Characters/Eagle/eagle_reference",
                SpriteId.CoyoteReference => "ArenaDraft/Characters/Coyote/coyote_reference",
                SpriteId.BunnyReference => "ArenaDraft/Characters/Bunny/bunny_reference",
                SpriteId.BackyardProps => "ArenaDraft/Props/Backyard/backyard_props_sheet",
                SpriteId.UiKit => "ArenaDraft/UI/ui_kit_assets",
                SpriteId.Vfx => "ArenaDraft/VFX/vfx_assets",
                _ => string.Empty
            };
        }

        public static Sprite LoadSprite(SpriteId id)
        {
            string path = PathFor(id);
            return string.IsNullOrEmpty(path) ? null : Resources.Load<Sprite>(path);
        }

        public static Texture2D LoadTexture(SpriteId id)
        {
            var sprite = LoadSprite(id);
            if (sprite != null) return sprite.texture;

            string path = PathFor(id);
            return string.IsNullOrEmpty(path) ? null : Resources.Load<Texture2D>(path);
        }

        public static bool HasSprite(SpriteId id) => LoadSprite(id) != null;

        public static SpriteRenderer AddSpriteBadge(Transform parent, string name, SpriteId id,
            Vector3 localPosition, Vector3 localScale, int sortingOrder, Color tint)
        {
            var sprite = LoadSprite(id);
            if (sprite == null || parent == null) return null;

            var go = new GameObject(name);
            go.transform.SetParent(parent);
            go.transform.localPosition = localPosition;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = localScale;

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = sortingOrder;
            sr.color = tint;
            return sr;
        }
    }
}
