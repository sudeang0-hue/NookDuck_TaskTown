[System.Serializable]
public class AnimalInventorySlot
{
    public AnimalData animalData;
    public int currentCount;

    public AnimalInventorySlot(AnimalData data, int count)
    {
        animalData = data;
        currentCount = count;
    }
}
