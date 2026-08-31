using Help.Item;

namespace Help.Inventory
{
    public class ItemStack
    {
        public ItemDefinition Definition { get; }
        public int Count { get; private set; }

        public ItemStack(ItemDefinition definition, int count = 1)
        {
            Definition = definition;
            Count = count;
        }

        public void Add(int amount) => Count += amount;

        public bool Remove(int amount)
        {
            if (Count < amount) return false;
            Count -= amount;
            return true;
        }
    }
}
