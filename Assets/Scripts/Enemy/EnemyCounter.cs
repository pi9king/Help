namespace Help.Enemy
{
    // 순수 로직: 방 안의 적 처치 진행을 집계한다(UnityEngine 비의존 → EditMode 테스트 가능).
    // 등록된 적이 1마리 이상이고 전부 죽으면 AllDead=true. MonoBehaviour(EnemyClearObjective)가
    // 실제 EnemyBase.OnDied를 이 카운터에 연결한다.
    public class EnemyCounter
    {
        public int Total { get; private set; }
        public int Dead { get; private set; }

        // 적이 1마리 이상 등록됐고 전부 죽었는가. (등록 0이면 false — 빈 방 오클리어 방지)
        public bool AllDead => Total > 0 && Dead >= Total;

        public void Register() => Total++;

        // 처치 1건 반영. 총계를 넘지 않도록 클램프(중복 호출 방어).
        public void MarkDead()
        {
            if (Dead < Total) Dead++;
        }

        public void Reset()
        {
            Total = 0;
            Dead = 0;
        }
    }
}
