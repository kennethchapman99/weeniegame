using System.Collections.Generic;
using CheddarAndCocoa.Dogs;
using UnityEngine;

namespace CheddarAndCocoa.Game
{
    /// <summary>
    /// Code-side visual slot catalog for the generated ArenaScene placeholders. These slots are the
    /// contract future authored sprites should replace first; gameplay code should not depend on
    /// ad hoc child names, colors, or scales outside this catalog.
    /// </summary>
    public static class ArenaArtCatalog
    {
        public enum ActorKind { Squirrel, Predator, Rope }
        public enum ColorRole { Fixed, MissionPrimary, MissionAccent, MissionSecondary }

        public const string ArenaHudObjectName = "ArenaHud";
        public const string DebugHudObjectName = "DebugHud";
        public const string GameManagerObjectName = "GameManager";
        public const string DogReadabilityLabelName = "DogReadabilityLabel";
        public const string ObjectiveArrowLabelName = "ObjectiveArrowLabel";
        public const string BackyardEnvironmentObjectName = "BackyardEnvironment";

        public static readonly Color FloorColor = Hex("#3c6b2f");
        public static readonly Color CameraBackgroundColor = Hex("#243a1c");
        public static readonly Vector3 ArenaDogBodyScale = new Vector3(1.6f, 0.62f, 1f);

        public static readonly LabelSlot DogLabel = new LabelSlot(
            DogReadabilityLabelName, new Vector3(0f, 0.95f, -0.1f), Vector3.one * 0.085f, 22, Color.white);

        public static readonly LabelSlot ObjectiveArrowLabel = new LabelSlot(
            ObjectiveArrowLabelName, Vector3.zero, Vector3.one * 0.085f, 22, Color.white);

        public static readonly WorldPopSlot WorldPop = new WorldPopSlot(
            "MissionPop", Vector3.up * 0.95f, 28, 1.05f, 0.35f, 0.28f);

        public static readonly BarkFeedbackSlot BarkFeedback = new BarkFeedbackSlot(
            "BarkRing", "BarkBurst", "BARK!", 32, 0.45f, 0.65f, 0.4f, 2.4f);

        public static DogVisualSlot Dog(DogId id)
        {
            bool cheddar = id == DogId.Cheddar;
            Color body = cheddar ? new Color(1f, 0.67f, 0.22f) : new Color(0.28f, 0.13f, 0.06f);
            Color muzzle = cheddar ? new Color(1f, 0.82f, 0.42f) : new Color(0.7f, 0.43f, 0.24f);
            Color ear = cheddar ? new Color(0.91f, 0.44f, 0.08f) : new Color(0.13f, 0.06f, 0.03f);
            Color collar = cheddar ? new Color(0.95f, 0.18f, 0.1f) : new Color(0.08f, 0.78f, 0.84f);
            Color foot = cheddar ? new Color(0.87f, 0.41f, 0.08f) : new Color(0.12f, 0.06f, 0.03f);
            Color tail = cheddar ? new Color(1f, 0.82f, 0.28f) : new Color(0.16f, 0.07f, 0.03f);
            Color marker = cheddar ? new Color(1f, 0.95f, 0.25f) : new Color(0.95f, 0.82f, 0.55f);
            Color intent = cheddar ? new Color(1f, 0.15f, 0.08f, 0.78f) : new Color(0.08f, 0.8f, 0.9f, 0.78f);

            var parts = new List<PartSlot>
            {
                new PartSlot("DachshundHead", body, new Vector3(0.52f, 0.1f, -0.03f), new Vector3(0.38f, 0.52f, 1f), 14),
                new PartSlot("LongDogSnout", muzzle, new Vector3(0.78f, 0.03f, -0.05f), new Vector3(0.34f, 0.22f, 1f), 15),
                new PartSlot(cheddar ? "CheddarFloppyEar" : "CocoaVelvetEar", ear, new Vector3(0.38f, 0.3f, -0.06f), new Vector3(0.2f, 0.46f, 1f), 16),
                new PartSlot(cheddar ? "CheddarRedCollar" : "CocoaTealCollar", collar, new Vector3(0.2f, 0.02f, -0.07f), new Vector3(0.12f, 0.7f, 1f), 17),
                new PartSlot("ExpressionEye", Color.black, new Vector3(0.62f, 0.18f, -0.08f), new Vector3(0.1f, 0.1f, 1f), 18),
                new PartSlot("ChestPatch", cheddar ? new Color(1f, 0.9f, 0.52f) : new Color(0.96f, 0.84f, 0.64f), new Vector3(0.08f, -0.08f, -0.02f), new Vector3(0.28f, 0.52f, 1f), 12),
                new PartSlot("TinyFrontFeet", foot, new Vector3(0.38f, -0.53f, 0f), new Vector3(0.18f, 0.22f, 1f), 8),
                new PartSlot("TinyBackFeet", foot, new Vector3(-0.42f, -0.53f, 0f), new Vector3(0.2f, 0.22f, 1f), 8),
                new PartSlot("TailFlag", tail, new Vector3(-0.68f, 0.16f, 0f), new Vector3(0.16f, 0.58f, 1f), 9),
                new PartSlot("MoodSpark", marker, new Vector3(0.24f, 0.54f, -0.08f), new Vector3(0.18f, 0.18f, 1f), 19),
                new PartSlot(cheddar ? "CheddarIntentArrow" : "CocoaIntentArrow", intent, new Vector3(0.9f, 0f, -0.09f), new Vector3(0.24f, 0.1f, 1f), 21)
            };

            if (cheddar)
            {
                parts.Add(new PartSlot("CheddarChaosTuft", new Color(1f, 0.88f, 0.22f), new Vector3(0.52f, 0.54f, -0.08f), new Vector3(0.14f, 0.24f, 1f), 20));
                parts.Add(new PartSlot("CheddarMischiefFlash", new Color(1f, 0.32f, 0.08f), new Vector3(-0.18f, 0.25f, -0.04f), new Vector3(0.34f, 0.12f, 1f), 13));
                parts.Add(new PartSlot("CheddarChaosBolt", new Color(1f, 0.98f, 0.12f), new Vector3(-0.38f, 0.36f, -0.07f), new Vector3(0.16f, 0.34f, 1f), 20));
            }
            else
            {
                parts.Add(new PartSlot("CocoaQueenSpotA", new Color(0.08f, 0.035f, 0.02f), new Vector3(-0.2f, 0.2f, -0.04f), new Vector3(0.26f, 0.22f, 1f), 13));
                parts.Add(new PartSlot("CocoaQueenSpotB", new Color(0.74f, 0.47f, 0.28f), new Vector3(-0.48f, -0.04f, -0.04f), new Vector3(0.22f, 0.18f, 1f), 13));
                parts.Add(new PartSlot("CocoaQueenSpotC", new Color(0.96f, 0.84f, 0.64f), new Vector3(0.26f, -0.28f, -0.05f), new Vector3(0.18f, 0.16f, 1f), 14));
                parts.Add(new PartSlot("CocoaQueenCrown", new Color(1f, 0.86f, 0.18f), new Vector3(0.46f, 0.6f, -0.08f), new Vector3(0.42f, 0.16f, 1f), 20));
            }

            return new DogVisualSlot(
                id,
                body,
                cheddar ? Hex("#e3ab63") : Hex("#5e3a20"),
                cheddar ? new Color(1f, 0.92f, 0.25f) : new Color(0.72f, 0.9f, 1f),
                cheddar ? new Color(1f, 0.95f, 0.5f, 0.8f) : new Color(0.7f, 0.85f, 1f, 0.8f),
                cheddar ? "CHEDDAR CHAOS PUP" : "COCOA SPOT QUEEN",
                cheddar ? "WIGGLE READY" : "QUEEN READY",
                cheddar ? "CHAOS ZOOM" : "SPOT PATROL",
                cheddar ? "long-low-golden-chaos-puppy-red-collar" : "long-low-chocolate-spot-queen-teal-collar",
                parts.ToArray());
        }

        public static CollectibleVisualSlot Collectible(GameManager.MissionVariant variant)
        {
            return variant switch
            {
                GameManager.MissionVariant.SnackHeist => new CollectibleVisualSlot(
                    new Vector3(0.54f, 0.54f, 1f),
                    new[]
                    {
                        new PartSlot("SnackPlate", ColorRole.MissionSecondary, new Vector3(0f, -0.08f, 0.04f), new Vector3(1.38f, 0.32f, 1f), 4),
                        new PartSlot("SnackCheeseCorner", ColorRole.MissionAccent, new Vector3(0.24f, 0.24f, -0.03f), new Vector3(0.38f, 0.25f, 1f), 7),
                        new PartSlot("SnackCrumbA", new Color(1f, 0.92f, 0.38f), new Vector3(-0.28f, 0.28f, -0.04f), new Vector3(0.14f, 0.14f, 1f), 8),
                        new PartSlot("SnackCrumbB", new Color(1f, 0.92f, 0.38f), new Vector3(0.32f, -0.22f, -0.04f), new Vector3(0.12f, 0.12f, 1f), 8)
                    }),
                GameManager.MissionVariant.SockPanic => new CollectibleVisualSlot(
                    new Vector3(0.46f, 0.86f, 1f),
                    new[]
                    {
                        new PartSlot("SockToe", ColorRole.MissionAccent, new Vector3(0f, -0.48f, -0.03f), new Vector3(1.18f, 0.24f, 1f), 7),
                        new PartSlot("SockCuff", ColorRole.MissionSecondary, new Vector3(0f, 0.48f, -0.03f), new Vector3(1.18f, 0.2f, 1f), 7),
                        new PartSlot("SockStripeA", ColorRole.MissionAccent, new Vector3(0f, 0.18f, -0.04f), new Vector3(1.12f, 0.12f, 1f), 8),
                        new PartSlot("SockStripeB", ColorRole.MissionSecondary, new Vector3(0f, -0.12f, -0.04f), new Vector3(1.12f, 0.12f, 1f), 8)
                    }),
                _ => new CollectibleVisualSlot(
                    new Vector3(0.72f, 0.32f, 1f),
                    new[]
                    {
                        new PartSlot("WeenieBunLeft", new Color(0.98f, 0.76f, 0.4f), new Vector3(-0.5f, 0f, -0.03f), new Vector3(0.2f, 0.9f, 1f), 6),
                        new PartSlot("WeenieBunRight", new Color(0.98f, 0.76f, 0.4f), new Vector3(0.5f, 0f, -0.03f), new Vector3(0.2f, 0.9f, 1f), 6),
                        new PartSlot("WeenieMustard", new Color(1f, 0.9f, 0.12f), new Vector3(0f, 0.18f, -0.04f), new Vector3(0.8f, 0.18f, 1f), 7)
                    })
            };
        }

        public static ActorVisualSlot Actor(ActorKind kind)
        {
            return kind switch
            {
                ActorKind.Predator => new ActorVisualSlot(
                    "Predator Warning", new Color(0.7f, 0.05f, 0.08f), 1.1f, "Predator Warning",
                    Vector3.up * 1.8f, 0.25f, Vector3.zero,
                    new Vector3(1.25f, 0.48f, 1f),
                    new[]
                    {
                        new PartSlot("EagleWingSweep", new Color(0.08f, 0.02f, 0.025f), new Vector3(0f, -0.12f, -0.03f), new Vector3(1.55f, 0.2f, 1f), 6),
                        new PartSlot("PredatorWingLeft", new Color(0.18f, 0.03f, 0.04f), new Vector3(-0.62f, 0.04f, -0.02f), new Vector3(0.55f, 0.28f, 1f), 7),
                        new PartSlot("PredatorWingRight", new Color(0.18f, 0.03f, 0.04f), new Vector3(0.62f, 0.04f, -0.02f), new Vector3(0.55f, 0.28f, 1f), 7),
                        new PartSlot("CoyoteFenceEars", new Color(0.45f, 0.26f, 0.11f), new Vector3(0f, 0.34f, -0.05f), new Vector3(0.42f, 0.18f, 1f), 8),
                        new PartSlot("PredatorWarningEyeA", new Color(1f, 0.1f, 0.06f), new Vector3(-0.16f, 0.16f, -0.04f), new Vector3(0.12f, 0.12f, 1f), 9),
                        new PartSlot("PredatorWarningEyeB", new Color(1f, 0.1f, 0.06f), new Vector3(0.16f, 0.16f, -0.04f), new Vector3(0.12f, 0.12f, 1f), 9),
                        new PartSlot("CoyoteFenceEyes", new Color(1f, 0.64f, 0.18f), new Vector3(0f, 0.02f, -0.06f), new Vector3(0.52f, 0.07f, 1f), 10),
                        new PartSlot("PredatorShadowClaws", new Color(0.04f, 0f, 0f), new Vector3(0f, -0.33f, -0.07f), new Vector3(0.85f, 0.1f, 1f), 10)
                    },
                    new[] { ArenaDraftArt.SpriteId.EagleReference, ArenaDraftArt.SpriteId.CoyoteReference }),
                ActorKind.Rope => new ActorVisualSlot(
                    "Rope/Tug", new Color(0.95f, 0.7f, 0.15f), 0.9f, "Rope/Tug",
                    Vector3.up * 1.8f, 0.1f, new Vector3(0f, 0f, 45f),
                    new Vector3(1.45f, 0.24f, 1f),
                    new[]
                    {
                        new PartSlot("RopeCenterKnot", new Color(0.78f, 0.42f, 0.12f), new Vector3(0f, 0f, -0.04f), new Vector3(0.22f, 1.45f, 1f), 9),
                        new PartSlot("RopeStripeA", new Color(0.55f, 0.27f, 0.08f), new Vector3(-0.36f, 0f, -0.02f), new Vector3(0.16f, 1.1f, 1f), 7),
                        new PartSlot("RopeStripeB", new Color(0.55f, 0.27f, 0.08f), new Vector3(0.36f, 0f, -0.02f), new Vector3(0.16f, 1.1f, 1f), 7),
                        new PartSlot("RopeEndLeft", new Color(1f, 0.82f, 0.3f), new Vector3(-0.76f, 0f, -0.03f), new Vector3(0.16f, 1.5f, 1f), 8),
                        new PartSlot("RopeEndRight", new Color(1f, 0.82f, 0.3f), new Vector3(0.76f, 0f, -0.03f), new Vector3(0.16f, 1.5f, 1f), 8),
                        new PartSlot("RopeBiteMarks", new Color(0.18f, 0.08f, 0.02f), new Vector3(0f, 0.17f, -0.05f), new Vector3(1.05f, 0.06f, 1f), 10)
                    },
                    new[] { ArenaDraftArt.SpriteId.BackyardProps }),
                _ => new ActorVisualSlot(
                    "Squirrel", new Color(0.55f, 0.32f, 0.12f), 0.7f, "Squirrel",
                    Vector3.up * 1.8f, 0.18f, new Vector3(0f, 0f, 80f),
                    new Vector3(0.78f, 0.48f, 1f),
                    new[]
                    {
                        new PartSlot("SquirrelFlagTail", new Color(0.72f, 0.42f, 0.12f), new Vector3(-0.55f, 0.22f, -0.02f), new Vector3(0.32f, 0.9f, 1f), 7),
                        new PartSlot("SquirrelPointNose", new Color(0.36f, 0.18f, 0.07f), new Vector3(0.52f, 0.04f, -0.03f), new Vector3(0.28f, 0.22f, 1f), 8),
                        new PartSlot("SquirrelBeadyEye", Color.black, new Vector3(0.28f, 0.2f, -0.04f), new Vector3(0.09f, 0.09f, 1f), 9),
                        new PartSlot("SquirrelGrabPaws", new Color(0.95f, 0.65f, 0.22f), new Vector3(0.18f, -0.26f, -0.04f), new Vector3(0.34f, 0.11f, 1f), 9),
                        new PartSlot("SquirrelLootAcorn", new Color(0.38f, 0.2f, 0.08f), new Vector3(-0.1f, -0.32f, -0.05f), new Vector3(0.18f, 0.18f, 1f), 10)
                    },
                    new[] { ArenaDraftArt.SpriteId.SquirrelCharacter })
            };
        }

        public static string[] DogPartNames(DogId id) => Names(Dog(id).Parts);
        public static string[] CollectiblePartNames(GameManager.MissionVariant variant) => Names(Collectible(variant).Parts);
        public static string[] ActorPartNames(ActorKind kind) => Names(Actor(kind).Parts);

        private static string[] Names(PartSlot[] parts)
        {
            var names = new string[parts.Length];
            for (int i = 0; i < parts.Length; i++) names[i] = parts[i].Name;
            return names;
        }

        private static Color Hex(string hex)
        {
            return ColorUtility.TryParseHtmlString(hex, out var c) ? c : Color.magenta;
        }
    }

    public readonly struct DogVisualSlot
    {
        public readonly DogId Id;
        public readonly Color BodyColor;
        public readonly Color BootstrapColor;
        public readonly Color ObjectiveArrowColor;
        public readonly Color BarkTint;
        public readonly string Title;
        public readonly string IdlePoseLabel;
        public readonly string RunPoseLabel;
        public readonly string ArtDirectionSignature;
        public readonly PartSlot[] Parts;

        public DogVisualSlot(DogId id, Color bodyColor, Color bootstrapColor, Color objectiveArrowColor,
            Color barkTint, string title, string idlePoseLabel, string runPoseLabel,
            string artDirectionSignature, PartSlot[] parts)
        {
            Id = id;
            BodyColor = bodyColor;
            BootstrapColor = bootstrapColor;
            ObjectiveArrowColor = objectiveArrowColor;
            BarkTint = barkTint;
            Title = title;
            IdlePoseLabel = idlePoseLabel;
            RunPoseLabel = runPoseLabel;
            ArtDirectionSignature = artDirectionSignature;
            Parts = parts;
        }
    }

    public readonly struct CollectibleVisualSlot
    {
        public readonly Vector3 RootScale;
        public readonly PartSlot[] Parts;

        public CollectibleVisualSlot(Vector3 rootScale, PartSlot[] parts)
        {
            RootScale = rootScale;
            Parts = parts;
        }
    }

    public readonly struct ActorVisualSlot
    {
        public readonly string ObjectName;
        public readonly Color RootColor;
        public readonly float RootScale;
        public readonly string Label;
        public readonly Vector3 LabelOffset;
        public readonly float PulseAmount;
        public readonly Vector3 RotationPerSecond;
        public readonly Vector3 BodyScale;
        public readonly PartSlot[] Parts;
        public readonly ArenaDraftArt.SpriteId[] DraftSprites;

        public ActorVisualSlot(string objectName, Color rootColor, float rootScale, string label,
            Vector3 labelOffset, float pulseAmount, Vector3 rotationPerSecond, Vector3 bodyScale,
            PartSlot[] parts, ArenaDraftArt.SpriteId[] draftSprites = null)
        {
            ObjectName = objectName;
            RootColor = rootColor;
            RootScale = rootScale;
            Label = label;
            LabelOffset = labelOffset;
            PulseAmount = pulseAmount;
            RotationPerSecond = rotationPerSecond;
            BodyScale = bodyScale;
            Parts = parts;
            DraftSprites = draftSprites ?? System.Array.Empty<ArenaDraftArt.SpriteId>();
        }
    }

    public readonly struct PartSlot
    {
        public readonly string Name;
        public readonly Color Color;
        public readonly ArenaArtCatalog.ColorRole ColorRole;
        public readonly Vector3 LocalPosition;
        public readonly Vector3 LocalScale;
        public readonly int SortingOrder;

        public PartSlot(string name, Color color, Vector3 localPosition, Vector3 localScale, int sortingOrder)
            : this(name, color, ArenaArtCatalog.ColorRole.Fixed, localPosition, localScale, sortingOrder)
        {
        }

        public PartSlot(string name, ArenaArtCatalog.ColorRole colorRole, Vector3 localPosition,
            Vector3 localScale, int sortingOrder)
            : this(name, Color.white, colorRole, localPosition, localScale, sortingOrder)
        {
        }

        private PartSlot(string name, Color color, ArenaArtCatalog.ColorRole colorRole,
            Vector3 localPosition, Vector3 localScale, int sortingOrder)
        {
            Name = name;
            Color = color;
            ColorRole = colorRole;
            LocalPosition = localPosition;
            LocalScale = localScale;
            SortingOrder = sortingOrder;
        }

        public Color ResolveColor(Color missionPrimary, Color missionAccent, Color missionSecondary)
        {
            return ColorRole switch
            {
                ArenaArtCatalog.ColorRole.MissionPrimary => missionPrimary,
                ArenaArtCatalog.ColorRole.MissionAccent => missionAccent,
                ArenaArtCatalog.ColorRole.MissionSecondary => missionSecondary,
                _ => Color
            };
        }
    }

    public readonly struct LabelSlot
    {
        public readonly string Name;
        public readonly Vector3 LocalPosition;
        public readonly Vector3 LocalScale;
        public readonly int FontSize;
        public readonly Color Color;

        public LabelSlot(string name, Vector3 localPosition, Vector3 localScale, int fontSize, Color color)
        {
            Name = name;
            LocalPosition = localPosition;
            LocalScale = localScale;
            FontSize = fontSize;
            Color = color;
        }
    }

    public readonly struct WorldPopSlot
    {
        public readonly string NamePrefix;
        public readonly Vector3 SpawnOffset;
        public readonly int FontSize;
        public readonly float LifeSeconds;
        public readonly float RiseSpeed;
        public readonly float PopScaleAmount;

        public WorldPopSlot(string namePrefix, Vector3 spawnOffset, int fontSize, float lifeSeconds,
            float riseSpeed, float popScaleAmount)
        {
            NamePrefix = namePrefix;
            SpawnOffset = spawnOffset;
            FontSize = fontSize;
            LifeSeconds = lifeSeconds;
            RiseSpeed = riseSpeed;
            PopScaleAmount = popScaleAmount;
        }
    }

    public readonly struct BarkFeedbackSlot
    {
        public readonly string RingName;
        public readonly string BurstName;
        public readonly string BurstText;
        public readonly int BurstFontSize;
        public readonly float RingLifeSeconds;
        public readonly float BurstLifeSeconds;
        public readonly float RingStartScale;
        public readonly float RingEndScale;

        public BarkFeedbackSlot(string ringName, string burstName, string burstText, int burstFontSize,
            float ringLifeSeconds, float burstLifeSeconds, float ringStartScale, float ringEndScale)
        {
            RingName = ringName;
            BurstName = burstName;
            BurstText = burstText;
            BurstFontSize = burstFontSize;
            RingLifeSeconds = ringLifeSeconds;
            BurstLifeSeconds = burstLifeSeconds;
            RingStartScale = ringStartScale;
            RingEndScale = ringEndScale;
        }
    }
}
