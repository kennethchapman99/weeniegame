using CheddarAndCocoa.Game;
using NUnit.Framework;

namespace CheddarAndCocoa.Tests.PlayMode
{
    public sealed class SockBasketMissionStateTests
    {
        [Test]
        public void TipAndDive_RequiresDifferentDogs()
        {
            var state = new SockBasketMissionState();

            Assert.IsTrue(state.TryOpen(1));
            Assert.AreEqual(SockBasketMissionState.CollectResult.SameDogDecoy, state.TryCollect(1));
            Assert.AreEqual(1, state.Fumbles);
            Assert.IsFalse(state.BasketOpen);

            Assert.IsTrue(state.TryOpen(1));
            Assert.AreEqual(SockBasketMissionState.CollectResult.PartnerDive, state.TryCollect(0));
            Assert.AreEqual(1, state.SuccessfulDives);
            Assert.IsFalse(state.BasketOpen);
        }

        [Test]
        public void OpeningTimeout_FumblesAndResetClearsRoundState()
        {
            var state = new SockBasketMissionState();

            Assert.AreEqual(SockBasketMissionState.CollectResult.BasketClosed, state.TryCollect(0));
            Assert.IsTrue(state.TryOpen(0));
            Assert.IsFalse(state.TryOpen(1));
            Assert.IsTrue(state.ExpireOpening());
            Assert.AreEqual(1, state.Fumbles);

            state.Reset();
            Assert.AreEqual(0, state.Fumbles);
            Assert.AreEqual(0, state.SuccessfulDives);
            Assert.AreEqual(-1, state.OpenerDogIndex);
            Assert.IsFalse(state.BasketOpen);
        }
    }
}
