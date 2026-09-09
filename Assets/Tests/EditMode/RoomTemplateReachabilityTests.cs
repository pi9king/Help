using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using Help.Dungeon;
using Help.Player;

namespace Tests.EditMode
{
    // 이 프로젝트의 두 번째 불변식: **모든 방 템플릿은 돌 수 있어야 한다.**
    // (첫 번째는 FloorValidator의 재료 보장 불변식)
    //
    // Assets/Rooms/*.txt를 전량 스캔하므로, 새 템플릿을 추가하면 자동으로 검사 대상이 된다.
    // 확률 칸이 어떻게 굴러도 막히지 않아야 하므로 양극단을 모두 본다.
    public class RoomTemplateReachabilityTests
    {
        private static string RoomsDir => Path.Combine(Application.dataPath, "Rooms");

        // 층 폴더(Floor1/Floor2/…)까지 훑는다. 작업 중인 방(_draft)은 건너뛴다 —
        // 에디터 셋업(RoomTemplateSetup)과 **같은 규칙**을 써야
        // "테스트는 통과하는데 라이브러리엔 들어간" 방이 생기지 않는다.
        private static IEnumerable<string> TemplateFiles()
        {
            if (!Directory.Exists(RoomsDir)) yield break;
            var files = Directory.GetFiles(RoomsDir, "*.txt", SearchOption.AllDirectories);
            System.Array.Sort(files);
            foreach (var f in files)
            {
                if (RoomTemplateNaming.IsDraft(Path.GetFileNameWithoutExtension(f))) continue;
                yield return f;
            }
        }

        [Test]
        public void ShouldHaveTemplates()
        {
            var count = 0;
            foreach (var _ in TemplateFiles()) count++;
            Assert.Greater(count, 0, $"{RoomsDir}에 방 템플릿이 없습니다.");
        }

        [Test]
        public void EveryTemplateShouldParse()
        {
            var failures = new List<string>();
            foreach (var file in TemplateFiles())
            {
                var name = Path.GetFileNameWithoutExtension(file);
                var parsed = RoomTemplateParser.Parse(File.ReadAllText(file), name);
                if (!parsed.Success) failures.AddRange(parsed.Errors);
            }
            Assert.IsEmpty(failures, "\n" + string.Join("\n", failures));
        }

        [Test]
        public void EveryTemplateShouldBeFullyTraversable()
        {
            var metrics = PlatformerMetrics.PlayerDefault;
            var failures = new List<string>();

            foreach (var file in TemplateFiles())
            {
                var name = Path.GetFileNameWithoutExtension(file);
                var parsed = RoomTemplateParser.Parse(File.ReadAllText(file), name);
                if (!parsed.Success) continue;  // 파싱 실패는 위 테스트가 잡는다

                var validation = RoomTemplateValidator.ValidateAllChanceExtremes(parsed.Template, metrics);
                failures.AddRange(validation.Errors);
            }

            Assert.IsEmpty(failures,
                "\n템플릿에서 갈 수 없는 곳이 있습니다:\n" + string.Join("\n", failures));
        }

        [Test]
        public void EveryRoomTypeShouldHaveAtLeastOneTemplate()
        {
            // 파일명 규약: {RoomType}_{SizeClass}_{NN}.txt
            var covered = new HashSet<string>();
            foreach (var file in TemplateFiles())
            {
                var name = Path.GetFileNameWithoutExtension(file);
                int i = name.IndexOf('_');
                if (i > 0) covered.Add(name.Substring(0, i));
            }

            var missing = new List<string>();
            foreach (RoomType t in System.Enum.GetValues(typeof(RoomType)))
            {
                // Shop은 아직 Treasure와 같은 콘텐츠라 전용 템플릿을 두지 않는다(OPEN_QUESTIONS #2).
                if (t == RoomType.Shop) continue;
                if (!covered.Contains(t.ToString())) missing.Add(t.ToString());
            }

            Assert.IsEmpty(missing, "템플릿이 없는 방 유형: " + string.Join(", ", missing));
        }
    }
}
