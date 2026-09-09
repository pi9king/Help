using UnityEngine;

namespace Help.Core
{
    // 프로젝트 공용 물리 머티리얼.
    //
    // D-1(2026-09-07 인터뷰): **벽은 마찰이 없다.**
    // 이 프로젝트엔 PhysicsMaterial2D가 하나도 없어서 Unity 내장 기본값(마찰 0.4)이
    // 적용되고 있었다 — 공중에서 벽 쪽으로 밀면 걸려서 미끄러지는 어정쩡한 끊김이 생긴다.
    // 아무도 정하지 않은 값이 조작감을 지배하던 상태였다.
    //
    // 마찰을 0으로 두면 `ReachabilityAnalyzer`가 주장하는
    // "실제 물리를 그대로 시뮬레이션한다"가 비로소 사실이 된다 — 시뮬과 게임이 같은 물리를 쓴다.
    // (벽점프는 기본 이동이 아니라 나중에 제작으로 얻는 능력으로 연다 — D-1)
    //
    // 에셋이 아니라 런타임 생성인 이유: 방 셸 콜라이더가 런타임에 붙기 때문에
    // 씬 에셋에 걸어두면 새로 만들어지는 콜라이더가 빠진다.
    public static class PhysicsMaterials
    {
        private static PhysicsMaterial2D _frictionless;

        public static PhysicsMaterial2D Frictionless
        {
            get
            {
                if (_frictionless == null)
                {
                    _frictionless = new PhysicsMaterial2D("Frictionless")
                    {
                        friction = 0f,
                        bounciness = 0f,
                        hideFlags = HideFlags.HideAndDontSave,
                    };
                }
                return _frictionless;
            }
        }

        // 대상과 그 자식의 모든 2D 콜라이더에 적용.
        public static void ApplyFrictionless(GameObject go)
        {
            if (go == null) return;
            foreach (var col in go.GetComponentsInChildren<Collider2D>(true))
                col.sharedMaterial = Frictionless;
        }
    }
}
