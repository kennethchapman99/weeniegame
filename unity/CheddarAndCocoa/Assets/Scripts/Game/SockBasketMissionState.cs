namespace CheddarAndCocoa.Game
{
    /// <summary>
    /// Pure state for Sock Panic's "Tip and Dive" co-op beat. One dog opens the basket and the
    /// other dog must collect the exposed sock before the short opening closes.
    /// </summary>
    public sealed class SockBasketMissionState
    {
        public enum CollectResult { BasketClosed, PartnerDive, SameDogDecoy }

        public bool BasketOpen { get; private set; }
        public int OpenerDogIndex { get; private set; } = -1;
        public int SuccessfulDives { get; private set; }
        public int Fumbles { get; private set; }

        public bool TryOpen(int dogIndex)
        {
            if (BasketOpen || dogIndex < 0) return false;
            BasketOpen = true;
            OpenerDogIndex = dogIndex;
            return true;
        }

        public CollectResult TryCollect(int dogIndex)
        {
            if (!BasketOpen) return CollectResult.BasketClosed;

            if (dogIndex == OpenerDogIndex)
            {
                CloseAsFumble();
                return CollectResult.SameDogDecoy;
            }

            SuccessfulDives++;
            Close();
            return CollectResult.PartnerDive;
        }

        public bool ExpireOpening()
        {
            if (!BasketOpen) return false;
            CloseAsFumble();
            return true;
        }

        public void Reset()
        {
            BasketOpen = false;
            OpenerDogIndex = -1;
            SuccessfulDives = 0;
            Fumbles = 0;
        }

        private void CloseAsFumble()
        {
            Fumbles++;
            Close();
        }

        private void Close()
        {
            BasketOpen = false;
            OpenerDogIndex = -1;
        }
    }
}
