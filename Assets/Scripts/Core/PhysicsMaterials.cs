using UnityEngine;

namespace Help.Core
{
    // 프로젝트 공용 물리 머티리얼.
    //
    // 쿼터뷰에서 벽을 따라 이동할 때 마찰로 속도가 끊기지 않도록 마찰을 0으로 둔다.
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
