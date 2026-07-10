namespace TaskTown.KDH
{
    using TaskTown.Gacha;

    [System.Serializable]

    public class AnimalInventorySlot
    {
        public GachaEntryData gachaData;
        public int currentCount;

        public AnimalInventorySlot(GachaEntryData data, int count)
        {
            gachaData = data;
            currentCount = count;
        }
    }
}
