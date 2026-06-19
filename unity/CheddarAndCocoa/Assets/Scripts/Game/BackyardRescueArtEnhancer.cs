using System.Collections.Generic;
using UnityEngine;

namespace CheddarAndCocoa.Game
{
    /// <summary>Applies extracted Backyard Rescue sprites as safe cosmetic overlays.</summary>
    public sealed class BackyardRescueArtEnhancer : MonoBehaviour
    {
        public const string SquirrelOverlayName = "FinalSquirrelOverlay";
        public const string PredatorOverlayName = "FinalPredatorOverlay";
        public const string RopeOverlayName = "FinalRopeOverlay";
        public const string BunnyOverlayName = "FinalBunnyOverlay";

        private GameManager _game;
        private SpriteRenderer _squirrel;
        private SpriteRenderer _predator;
        private SpriteRenderer _rope;

        public void Init(GameManager game)
        {
            _game = game;
            _squirrel = RuntimeArtSpriteFactory.AddOverlay(game.SquirrelObject.transform, SquirrelOverlayName,
                FinalGameplayArt.SquirrelIdle, Vector3.zero, Vector3.one * 1.15f, 18);
            _predator = RuntimeArtSpriteFactory.AddOverlay(game.PredatorObject.transform, PredatorOverlayName,
                FinalGameplayArt.EagleThreat, Vector3.zero, Vector3.one * 1.45f, 18);
            _rope = RuntimeArtSpriteFactory.AddOverlay(game.RopeObject.transform, RopeOverlayName,
                FinalGameplayArt.RopeTug, Vector3.zero, Vector3.one * 1.05f, 18);

            var bunny = GameObject.Find(ArenaDraftArt.BunnyCameoName);
            if (bunny != null)
                RuntimeArtSpriteFactory.AddOverlay(bunny.transform, BunnyOverlayName, FinalGameplayArt.BunnyIdle,
                    Vector3.zero, Vector3.one * 1.55f, 8);
            EnhanceEnvironment();
            Refresh();
        }

        private void Update() => Refresh();

        private void Refresh()
        {
            if (_game == null) return;
            if (_squirrel != null)
            {
                string path = _game.LastFeedback switch
                {
                    GameManager.FeedbackKind.SquirrelScared => FinalGameplayArt.SquirrelScared,
                    GameManager.FeedbackKind.SquirrelStealing => FinalGameplayArt.SquirrelSteal,
                    _ => FinalGameplayArt.SquirrelIdle
                };
                _squirrel.sprite = FinalGameplayArt.Load(path) ?? _squirrel.sprite;
            }
            if (_predator != null)
            {
                string path = _game.ActiveMissionVariant == GameManager.MissionVariant.CoyotesFence
                    ? FinalGameplayArt.CoyoteThreat
                    : _game.Phase == GameManager.State.PredatorAttack ? FinalGameplayArt.EagleAction : FinalGameplayArt.EagleThreat;
                _predator.sprite = FinalGameplayArt.Load(path) ?? _predator.sprite;
            }
            if (_rope != null)
                _rope.sprite = FinalGameplayArt.Load(_game.TugComplete ? FinalGameplayArt.RopeComplete : FinalGameplayArt.RopeTug) ?? _rope.sprite;
        }

        private static void EnhanceEnvironment()
        {
            var environment = GameObject.Find(ArenaArtCatalog.BackyardEnvironmentObjectName);
            if (environment == null) return;
            int bush = 0, rock = 0, grass = 0;
            var originalChildren = new List<Transform>();
            foreach (Transform child in environment.transform)
            {
                originalChildren.Add(child);
            }

            foreach (var child in originalChildren)
            {
                if (child.name.StartsWith("CoverBush") || child.name.StartsWith("Bush_"))
                {
                    var overlay = RuntimeArtSpriteFactory.AddWorldOverlay(environment.transform, $"FinalBush_{bush++}",
                        FinalGameplayArt.Bush, child.position, child.name.StartsWith("CoverBush") ? 5.2f : 3.5f, -4);
                    HideFallbackRenderer(child, overlay);
                }
                else if (child.name.StartsWith("SteppingStone"))
                {
                    rock++;
                    var overlay = RuntimeArtSpriteFactory.AddWorldOverlay(environment.transform, $"FinalRock_{rock}",
                        FinalGameplayArt.Rock, child.position, 1.7f + (rock % 3) * 0.25f, -5);
                    HideFallbackRenderer(child, overlay);
                }
                else if ((child.name == "GardenBed" || child.name == "PicnicBlanket") && grass++ < 2)
                    RuntimeArtSpriteFactory.AddWorldOverlay(environment.transform, $"FinalGrass_{grass}",
                        FinalGameplayArt.Grass, child.position, child.name == "GardenBed" ? 4.5f : 7f, -5);
            }
        }

        private static void HideFallbackRenderer(Transform fallback, SpriteRenderer overlay)
        {
            if (overlay == null || fallback == null) return;
            var renderer = fallback.GetComponent<SpriteRenderer>();
            if (renderer != null) renderer.enabled = false;
        }
    }
}
