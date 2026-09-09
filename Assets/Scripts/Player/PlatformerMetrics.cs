using System;

namespace Help.Player
{
    // 플레이어의 이동 능력을 "타일 단위 거리"로 환산한 순수 값 객체.
    //
    // 레벨 디자인의 단일 진실이다. 방 템플릿 도달성 검증(ReachabilityAnalyzer)과
    // 문법 문서(Docs/LEVEL_DESIGN.md)가 모두 이 값을 참조하므로,
    // 물리를 튜닝하면 "넘을 수 있는 단차/갭"의 판정도 함께 따라온다.
    //
    // MonoBehaviour/UnityEngine 비의존 — EditMode에서 그대로 테스트된다.
    public readonly struct PlatformerMetrics
    {
        // Unity 2D 기본 중력(ProjectSettings/Physics2DSettings: -9.81)
        public const float Gravity = 9.81f;

        // 튜닝된 플레이어 물리 — PlayerController/씬의 Rigidbody2D와 반드시 일치해야 한다.
        public const float DefaultGravityScale = 2.85f;
        public const float DefaultFallMultiplier = 1.7f;

        // 이론 최대치를 그대로 설계에 쓰면 "프레임 단위로 완벽히 눌러야 겨우 닿는" 지형이 나온다.
        // 설계 권장치는 이만큼 깎아서 쓴다.
        public const float SafetyFactor = 0.75f;

        // 단차 판정 여유 — 정점에서 간신히 스치는 높이는 오르지 못한 것으로 본다.
        private const float LedgeMargin = 0.25f;

        // 통로 높이 여유 — 플레이어 키와 정확히 같은 통로는 끼일 수 있으므로 한 칸 더 요구한다.
        private const float HeadroomMargin = 0.2f;

        public float MaxJumpHeight { get; }   // 타일(=월드 유닛)
        public float RiseTime { get; }        // 초
        public float FallTime { get; }        // 초 (정점 → 출발 높이)
        public float MaxJumpRun { get; }      // 점프 한 번의 이론 최대 수평 이동
        public float DashDistance { get; }
        public float PlayerHeightTiles { get; }

        // 원본 물리값 — 도달성 분석기가 궤적을 그대로 시뮬레이션할 때 쓴다.
        public float JumpForce { get; }
        public float MoveSpeed { get; }
        public float DashForce { get; }
        public float DashDuration { get; }
        public float RiseGravity { get; }     // 상승 중 가속도 (양수)
        public float FallGravity { get; }     // 낙하 중 가속도 (양수, RiseGravity보다 크다)

        public float AirTime => RiseTime + FallTime;

        // 설계에 실제로 쓰는 보수적 거리
        public float SafeJumpRun => MaxJumpRun * SafetyFactor;
        public float SafeDashDistance => DashDistance * SafetyFactor;

        // 통로/천장이 확보해야 할 빈 타일 수
        public int RequiredHeadroomTiles => (int)Math.Ceiling(PlayerHeightTiles + HeadroomMargin);

        private PlatformerMetrics(float maxJumpHeight, float riseTime, float fallTime,
                                  float maxJumpRun, float dashDistance, float playerHeightTiles,
                                  float jumpForce, float moveSpeed, float dashForce, float dashDuration,
                                  float riseGravity, float fallGravity)
        {
            MaxJumpHeight = maxJumpHeight;
            RiseTime = riseTime;
            FallTime = fallTime;
            MaxJumpRun = maxJumpRun;
            DashDistance = dashDistance;
            PlayerHeightTiles = playerHeightTiles;
            JumpForce = jumpForce;
            MoveSpeed = moveSpeed;
            DashForce = dashForce;
            DashDuration = dashDuration;
            RiseGravity = riseGravity;
            FallGravity = fallGravity;
        }

        public static PlatformerMetrics FromPhysics(float jumpForce, float moveSpeed, float dashForce,
                                                    float dashDuration, float gravityScale,
                                                    float fallMultiplier, float playerHeightTiles = 1f,
                                                    float gravity = Gravity)
        {
            float aRise = gravity * gravityScale;
            float aFall = aRise * fallMultiplier;

            float height = (jumpForce * jumpForce) / (2f * aRise);
            float rise = jumpForce / aRise;
            float fall = (float)Math.Sqrt(2f * height / aFall);
            float run = moveSpeed * (rise + fall);

            return new PlatformerMetrics(height, rise, fall, run, dashForce * dashDuration, playerHeightTiles,
                                         jumpForce, moveSpeed, dashForce, dashDuration, aRise, aFall);
        }

        // 현재 튜닝된 플레이어. PlayerStats 기본값 + 위 상수 = 씬의 실제 물리.
        public static PlatformerMetrics PlayerDefault
        {
            get
            {
                var s = new PlayerStats();
                return FromPhysics(s.JumpForce, s.MoveSpeed, s.DashForce,
                                   dashDuration: PlayerController.DefaultDashDuration,
                                   gravityScale: DefaultGravityScale,
                                   fallMultiplier: DefaultFallMultiplier);
            }
        }

        // riseTiles 높이의 단차 위로 올라설 수 있는가.
        public bool CanClearLedge(int riseTiles) => MaxJumpHeight >= riseTiles + LedgeMargin;

        // 빈 칸 gapTiles개짜리 구덩이를 건널 수 있는가.
        // 출발 타일 중심 → 도착 타일 중심이므로 실제 이동 거리는 gapTiles + 1이다.
        public bool CanClearGap(int gapTiles, bool withDash)
        {
            float reach = SafeJumpRun + (withDash ? SafeDashDistance : 0f);
            return gapTiles + 1 <= reach;
        }
    }
}
