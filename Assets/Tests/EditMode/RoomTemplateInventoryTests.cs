using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using Help.Dungeon;

namespace Tests.EditMode
{
    // D-11: 방 크기는 생성기가 굴리지 않는다 — **고른 템플릿에서 파생된다.**
    //
    // 그래서 "시작 방은 한 화면", "보스전은 넓게" 같은 규칙은 이제 생성기 로직이 아니라
    // **템플릿 데이터**가 지킨다. 예전 DungeonGeneratorSizeTests가 하던 일을 여기로 옮겼다.
    // (24% 확률 규칙을 검사하던 MostRoomsShouldStaySmall은 규칙 자체가 사라져 삭제)
    public class RoomTemplateInventoryTests
    {
        private static string RoomsDir => Path.Combine(Application.dataPath, "Rooms");

        // 층 폴더까지 훑고, 작업 중(_draft)인 방은 건너뛴다(D-17).
        private static IEnumerable<(string name, RoomTemplate template)> Templates()
        {
            if (!Directory.Exists(RoomsDir)) yield break;
            var files = Directory.GetFiles(RoomsDir, "*.txt", SearchOption.AllDirectories);
            System.Array.Sort(files);

            foreach (var f in files)
            {
                var name = Path.GetFileNameWithoutExtension(f);
                if (RoomTemplateNaming.IsDraft(name)) continue;

                var parsed = RoomTemplateParser.Parse(File.ReadAllText(f), name);
                if (parsed.Success) yield return (name, parsed.Template);
            }
        }

        private static IEnumerable<(string name, RoomTemplate template)> OfType(RoomType type) =>
            Templates().Where(t => t.name.StartsWith(type.ToString() + "_"));

        [Test]
        public void StartRoomTemplatesShouldBeSmall()
        {
            var tutorials = OfType(RoomType.Tutorial).ToList();
            Assert.IsNotEmpty(tutorials, "시작 방 템플릿이 하나도 없다");

            foreach (var (name, t) in tutorials)
                Assert.AreEqual(RoomSizeClass.Small, t.SizeClass,
                    $"{name}: 시작 방은 한 화면에 들어와야 한다(튜토리얼)");
        }

        [Test]
        public void BossRoomTemplatesShouldBeWide()
        {
            var bosses = OfType(RoomType.Boss).ToList();
            Assert.IsNotEmpty(bosses, "보스 방 템플릿이 하나도 없다");

            foreach (var (name, t) in bosses)
                Assert.AreEqual(RoomSizeClass.Wide, t.SizeClass,
                    $"{name}: 보스전은 회피할 가로 공간이 필요하다");
        }

        // 파일명 첫 토큰이 방 유형과 맞아야 라이브러리에 등록된다(RoomTemplateSetup.TryResolveType).
        // 오타 하나면 그 방은 조용히 안 나오고 절차적 빈 상자로 떨어진다 — 실제로 겪은 증상이다.
        [Test]
        public void EveryTemplateNameShouldResolveToARoomType()
        {
            var bad = new List<string>();
            foreach (var (name, _) in Templates())
            {
                int i = name.IndexOf('_');
                if (i <= 0 || !System.Enum.TryParse<RoomType>(name.Substring(0, i), out _))
                    bad.Add(name);
            }
            Assert.IsEmpty(bad,
                "방 유형으로 해석되지 않는 템플릿 이름: " + string.Join(", ", bad) +
                "\n규약: Assets/Rooms/Floor{N}/{RoomType}_{SizeClass}_{NN}[_draft].txt");
        }

        // ── 위험 지형은 보류 중이다 (2026-09-08) ──
        //
        // 가시/구덩이는 사용자가 요청한 기능이 아니라 2026-09-02 레벨 디자인 세션에서
        // 모델이 넣은 것이다. 그리고 지금 **구현이 깨져 있다**:
        //   HazardTile은 ColliderType.None(=콜라이더 없는 구멍)인데
        //   D-12에서 도달성 시뮬만 "밟고 설 수 있다"로 바꿨다 → 시뮬과 게임이 정반대다.
        //   실제로 적이 빠져 맵 밖으로 사라져 방 클리어가 불가능해졌고, 플레이어도 통과했다.
        //
        // 되살리려면 먼저 **타일에 콜라이더를 주고** 시뮬과 런타임을 일치시켜야 한다.
        // 그때까지 템플릿에서 못 쓰게 막는다.
        [Test]
        public void HazardTerrainShouldStayShelved()
        {
            var used = Templates().Where(e => HasHazard(e.template)).Select(e => e.name).ToList();

            Assert.IsEmpty(used,
                "위험 지형(^ ~)은 보류 중이다 — 사용한 템플릿: " + string.Join(", ", used) +
                " / 되살리려면 HazardTile에 콜라이더를 주고 도달성 시뮬과 런타임을 먼저 맞춰야 한다.");
        }

        private static bool HasHazard(RoomTemplate t)
        {
            for (int x = 0; x < t.Width; x++)
                for (int y = 0; y < t.Height; y++)
                    if (t.TileAt(x, y) == TileKind.Hazard) return true;
            return false;
        }
    }
}
