namespace Help.Dungeon
{
    // 방의 재료를 그 방 적들에게 나눠주는 규칙 — 순수 로직.
    //
    // "몬스터를 잡아도 알파벳이 안 나올 수 있다"를 확률 롤이 아니라 **배분의 희소성**으로 만든다.
    // 확률로 굴리면 운 나쁜 런에서 재료를 영영 못 얻어 "모든 몬스터를 잡으면 방 몫을 전부 얻는다"가
    // 깨지고, 던전 생성이 보장한 재료 보장 불변식도 함께 무너지기 때문이다.
    public static class LootDistribution
    {
        // 적별로 받을 재료 개수. 총합은 항상 lootCount(전멸 시 전량 획득).
        // 재료가 적보다 적으면 일부 적은 0개가 된다.
        public static int[] Assign(int lootCount, int enemyCount, int seed)
        {
            if (enemyCount <= 0) return new int[0];

            var shares = new int[enemyCount];
            if (lootCount <= 0) return shares;

            // 어떤 적이 무엇을 떨구는지는 결정적으로 섞는다(같은 방=같은 결과, 재방문 시 흔들림 없음)
            var order = new int[enemyCount];
            for (int i = 0; i < enemyCount; i++) order[i] = i;

            var rng = new System.Random(seed);
            for (int i = enemyCount - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                (order[i], order[j]) = (order[j], order[i]);
            }

            // 섞인 순서로 라운드로빈 → 몫 차이는 최대 1, 재료가 모자라면 뒤쪽 적은 빈손
            for (int i = 0; i < lootCount; i++)
                shares[order[i % enemyCount]]++;

            return shares;
        }
    }
}
