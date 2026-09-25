using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using Help.Dungeon;
using Help.Player;

namespace Tests.EditMode
{
    // 두 번째 불변식의 빠진 절반을 메운다.
    //
    // 기존 검증기는 문 안쪽 칸에서 **아래로 떨어뜨려**(TrySettle) 설 자리를 찾았다.
    // 천장에 있는 북문은 그 낙하가 13칸 아래 바닥까지 이어져, "바닥에서 바닥으로 갈 수 있다"를
    // 검사하는 꼴이 됐다 — 그래서 아무도 올라갈 수 없는 북문이 전 템플릿에서 통과했다.
    //
    // 들어오는 것(떨어지면 된다)과 나가는 것(문까지 올라가야 한다)은 다른 조건이다.
    public class DoorAccessValidationTests
    {
        // ★ new PlatformerMetrics()는 struct 기본값이라 이동속도·점프력이 전부 0이다 —
        //   그러면 플레이어가 한 칸도 못 움직여 모든 문 쌍이 불통으로 나오고,
        //   "실패를 기대하는" 테스트만 우연히 통과한다(2026-09-19 발견).
        //   검증은 반드시 실제 튜닝값으로 한다.
        private static readonly PlatformerMetrics Metrics = PlatformerMetrics.PlayerDefault;

        // 25×15 Small 방 한 장을 문자열로 짓는다. 천장/바닥/좌우 벽 + 4방향 문.
        private static List<string> BuildRoom(params (int row, string content)[] overrides)
        {
            const int W = 25, H = 15;
            var rows = new List<string>();
            for (int r = 0; r < H; r++)
            {
                string line;
                if (r == 0) line = new string('#', W / 2) + "D" + new string('#', W - W / 2 - 1);
                else if (r == H - 1) line = new string('=', W / 2) + "D" + new string('=', W - W / 2 - 1);
                else if (r == H - 2) line = "D" + new string('.', W - 2) + "D";
                else line = "#" + new string('.', W - 2) + "#";
                rows.Add(line);
            }
            foreach (var (row, content) in overrides) rows[row] = content;
            return rows;
        }

        private static TemplateValidation Validate(List<string> rows)
        {
            var parsed = RoomTemplateParser.Parse(rows, "test");
            Assert.IsTrue(parsed.Success, "테스트 방이 파싱되지 않음: " + string.Join("\n", parsed.Errors));
            return RoomTemplateValidator.ValidateAllChanceExtremes(parsed.Template, Metrics);
        }

        // 천장 문 아래가 통째로 빈 공기면 나갈 수 없는 문이다 — 잡아내야 한다.
        [Test]
        public void CeilingDoorWithNoWayUpShouldFail()
        {
            var result = Validate(BuildRoom());
            Assert.IsFalse(result.Ok, "천장 문에 올라갈 길이 없는데 통과함");
            Assert.IsTrue(result.Errors.Any(e => e.Contains("North")),
                "북문 관련 오류가 아님: " + string.Join(" / ", result.Errors));
        }

        // 단차 3 이하 간격으로 발판을 놓아 천장까지 이으면 통과해야 한다.
        //
        // 최상단이 row 2가 아니라 row 3인 이유: 문 칸은 Wall로 저장되므로(셸에 구멍을 내지 않는다)
        // 문 바로 아래 칸에 서면 머리가 문에 박힌다. 한 칸 낮은 자리가 실제 퇴장 지점이다.
        [Test]
        public void CeilingDoorWithPlatformLadderShouldPass()
        {
            var rows = BuildRoom();
            foreach (int r in new[] { 11, 8, 5, 3 })
                rows[r] = ReplaceAt(rows[r], 11, "---");

            var result = Validate(rows);
            Assert.IsTrue(result.Ok, "사다리가 있는데 실패함:\n" + string.Join("\n", result.Errors));
        }

        // 바닥에 있는 남문은 그냥 걸어가면 되므로 사다리 없이도 통과해야 한다
        // (북문만 막아 두고 남문 판정이 도매금으로 실패하지 않는지 확인).
        [Test]
        public void FloorLevelDoorsShouldNotRequireLadder()
        {
            var result = Validate(BuildRoom());
            Assert.IsFalse(result.Errors.Any(e => e.Contains("South")),
                "바닥 높이 남문이 실패함: " + string.Join(" / ", result.Errors));
        }

        private static string ReplaceAt(string line, int start, string patch) =>
            line.Substring(0, start) + patch + line.Substring(start + patch.Length);
    }
}
