namespace Help.Dungeon
{
    public class DungeonConfig
    {
        public int FloorNumber { get; set; } = 1;
        public int MinRooms { get; set; } = 8;
        public int MaxRooms { get; set; } = 12;
        public int Seed { get; set; } = -1;  // -1 = 랜덤
    }
}
