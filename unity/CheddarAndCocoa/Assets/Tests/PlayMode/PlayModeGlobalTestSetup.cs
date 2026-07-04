using NUnit.Framework;
using CheddarAndCocoa.Game;

// Assembly-wide (no namespace) so it covers every test namespace in this assembly.
// The sniff-around lead-in freezes the mission clock for several real-time seconds after
// StartMission; headless batchmode deltaTime is tiny, so the existing deterministic tests would
// stall inside it. They exercise the post-GO game, so the lead-in defaults to disabled here and
// LeadInPlayModeTests re-enables it explicitly to pin the lead-in contract itself.
[SetUpFixture]
public sealed class PlayModeGlobalTestSetup
{
    [OneTimeSetUp]
    public void DisableLeadInForLegacySuite() => GameManager.LeadInSecondsOverride = 0f;

    [OneTimeTearDown]
    public void RestoreLeadInDefault() => GameManager.LeadInSecondsOverride = null;
}
