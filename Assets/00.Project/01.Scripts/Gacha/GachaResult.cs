namespace TaskTown.Gacha
{
    public readonly struct GachaResult
    {
        public readonly GachaEntryData Entry;
        public readonly ItemGrade Grade;

        public GachaResult(GachaEntryData entry, ItemGrade grade)
        {
            Entry = entry;
            Grade = grade;
        }
    }
}
