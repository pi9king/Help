using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;
using Help.Crafting;
using Help.Dungeon;
using Help.Item;

namespace Help.Editor.Playtest
{
    // 정적 재미 리포트 — FUN_VALIDATION_CHECKLIST.md 중 **숫자로 잴 수 있는 항목**만 자동 측정한다.
    //
    // ★ 이 리포트는 판정하지 않는다. "재미있다/없다", "기준 미달" 같은 결론을 내지 않고 측정값만 적는다.
    //   기준은 사용자가 정하고, 채택된 결정만 DESIGN.md로 간다(CLAUDE.md 문서 작성 규율).
    //
    // 실행: Help ▸ Playtest ▸ Static Fun Report
    //   배치: Unity.exe -batchmode -nographics -projectPath . -executeMethod Help.Editor.Playtest.StaticFunReport.RunBatch -quit
    // 출력: <프로젝트>/Reports/static-fun-report.md (Assets 밖 — 임포트되지 않는다)
    public static class StaticFunReport
    {
        private const int SamplesPerFloor = 200;
        private const string OutputPath = "Reports/static-fun-report.md";

        private const string RecipeDbPath = "Assets/ScriptableObjects/RecipeDatabase.asset";
        private const string ContentLibPath = "Assets/ScriptableObjects/RoomContentLibrary.asset";
        private const string TemplateLibPath = "Assets/ScriptableObjects/RoomTemplateLibrary.asset";

        // 체크리스트 P0-1이 이름으로 지목한 1차 후보(사용자 작성 문서에서 가져온 것).
        private static readonly string[] ChecklistTrio = { "AXE", "ROPE", "FLARE" };

        [MenuItem("Help/Playtest/Static Fun Report")]
        public static void RunFromMenu()
        {
            string path = Generate();
            Debug.Log($"[StaticFunReport] 작성 완료: {path}");
            EditorUtility.RevealInFinder(path);
        }

        public static void RunBatch()
        {
            try
            {
                string path = Generate();
                Debug.Log($"[StaticFunReport] 작성 완료: {path}");
                EditorApplication.Exit(0);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                EditorApplication.Exit(1);
            }
        }

        public static string Generate()
        {
            var db = AssetDatabase.LoadAssetAtPath<RecipeDatabase>(RecipeDbPath);
            var content = AssetDatabase.LoadAssetAtPath<RoomContentLibrary>(ContentLibPath);
            var templates = AssetDatabase.LoadAssetAtPath<RoomTemplateLibrary>(TemplateLibPath);
            if (db == null) throw new InvalidOperationException($"RecipeDatabase 없음: {RecipeDbPath}");

            var md = new StringBuilder();
            WriteHeader(md);
            WriteItemDataReach(md, db);
            WriteContentVariety(md, content, templates);
            WriteFloorSamples(md, db, content);
            WriteNotMeasured(md);

            string full = Path.GetFullPath(OutputPath);
            Directory.CreateDirectory(Path.GetDirectoryName(full));
            File.WriteAllText(full, md.ToString(), new UTF8Encoding(false));
            return full;
        }

        // ── 머리말 ─────────────────────────────────────────────────────────

        private static void WriteHeader(StringBuilder md)
        {
            md.AppendLine("# 정적 재미 리포트 (자동 측정)");
            md.AppendLine();
            md.AppendLine("> **이 문서는 측정값이다. 결정도, 판정도 아니다.**");
            md.AppendLine("> `Docs/FUN_VALIDATION_CHECKLIST.md` 중 코드·데이터만으로 셀 수 있는 항목을 자동으로 잰 것이다.");
            md.AppendLine("> 기준(몇이면 괜찮은가)은 여기서 정하지 않는다. 채택된 결정만 `DESIGN.md`로 옮긴다.");
            md.AppendLine("> 재생성: `Help ▸ Playtest ▸ Static Fun Report` — 손으로 고치지 말 것(덮어써진다).");
            md.AppendLine();
            md.AppendLine($"- 생성: {DateTime.Now:yyyy-MM-dd HH:mm}");
            md.AppendLine($"- 빌드/커밋: {GitDescribe()}");
            md.AppendLine($"- 표본: 층마다 생성기 시드 0~{SamplesPerFloor - 1} ({SamplesPerFloor}장 × {GameManagerMaxFloors()}층)");
            md.AppendLine();
        }

        // ── P0-1: 아이템 데이터가 플레이에 닿는가 ────────────────────────────

        private static void WriteItemDataReach(StringBuilder md, RecipeDatabase db)
        {
            md.AppendLine("## 1. 아이템 데이터가 게임 코드에 닿는가 — 체크리스트 P0-1");
            md.AppendLine();
            md.AppendLine("`ItemDefinition`의 각 필드를 **게임 코드(`Assets/Scripts`, 정의 파일 제외)가 읽는 곳**을 셌다.");
            md.AppendLine("값이 다른 **멤버**로 옮겨 담기면 그 멤버의 읽기까지 한 번 더 따라간다(1홉). 지역 변수는 따라가지 않는다.");
            md.AppendLine("주석·문자열은 제외. 정규식 근사라 리플렉션·간접 접근은 놓칠 수 있다.");
            md.AppendLine();

            var sources = LoadGameSources();
            var fields = typeof(ItemDefinition)
                .GetFields(BindingFlags.Public | BindingFlags.Instance)
                .Select(f => f.Name).ToList();
            var items = db.AllItems.Where(i => i != null).ToList();

            md.AppendLine("| 필드 | 게임 코드 읽기 | 읽는 폴더 | 값이 옮겨간 곳 → 그 이름의 읽기 | 실제 아이템 값 종류 |");
            md.AppendLine("|---|---:|---|---|---:|");

            var unread = new List<string>();
            foreach (var field in fields)
            {
                var reads = SymbolUsageScanner.FindMemberUsages(field, sources).Where(s => !s.IsWrite).ToList();
                var folders = reads.Select(s => TopFolder(s.Path)).Distinct().OrderBy(x => x).ToList();

                var hops = new List<string>();
                foreach (var derived in SymbolUsageScanner.FindDerivedSymbols(field, sources))
                {
                    // 멤버(PascalCase / _camelCase)만 따라간다. 지역 변수(camelCase)는 이름이 흔해서
                    // (label, stack…) 전 파일의 같은 이름을 다 세게 되어 소음만 늘어난다.
                    if (!IsMemberName(derived)) continue;
                    int n = SymbolUsageScanner.FindIdentifierReads(derived, sources).Count;
                    hops.Add($"`{derived}` → {n}");
                }

                int distinct = DistinctValues(items, field);
                if (reads.Count == 0) unread.Add(field);

                md.AppendLine($"| `{field}` | {reads.Count} | {(folders.Count == 0 ? "—" : string.Join(", ", folders))} | " +
                              $"{(hops.Count == 0 ? "—" : string.Join("<br>", hops))} | {distinct} |");
            }
            md.AppendLine();

            if (unread.Count > 0)
            {
                md.AppendLine($"**게임 코드가 한 번도 읽지 않는 필드**: {string.Join(", ", unread.Select(f => $"`{f}`"))}");
                md.AppendLine("— 아이템마다 값이 달라도 플레이에서는 차이가 나지 않는다.");
                md.AppendLine();
            }

            // 체크리스트 1차 후보 세 단어 비교
            md.AppendLine("### 1-1. 체크리스트 1차 후보 세 단어의 데이터");
            md.AppendLine();
            md.AppendLine("체크리스트 P0-1이 지목한 `AXE` / `ROPE` / `FLARE`. 위 표에서 읽기 0인 필드는 ⚪로 표시 — 값이 달라도 플레이에 안 닿는다.");
            md.AppendLine();
            var shown = new[] { "Type", "WeaponCategory", "Element", "Capabilities", "AttackBonus", "DefenseBonus", "AttackSpeedMult", "HealAmount" }
                .Where(fields.Contains).ToList();
            md.AppendLine("| 단어 | " + string.Join(" | ", shown.Select(f => unread.Contains(f) ? $"⚪ `{f}`" : $"`{f}`")) + " |");
            md.AppendLine("|---|" + string.Concat(shown.Select(_ => "---|")));
            foreach (var word in ChecklistTrio)
            {
                var item = items.FirstOrDefault(i => string.Equals(i.Word, word, StringComparison.OrdinalIgnoreCase));
                if (item == null) { md.AppendLine($"| {word} | " + string.Join(" | ", shown.Select(_ => "(데이터 없음)")) + " |"); continue; }
                md.AppendLine($"| {word} | " + string.Join(" | ", shown.Select(f => FieldText(item, f))) + " |");
            }
            md.AppendLine();
        }

        // ── P1-6: 콘텐츠 후보 수 ─────────────────────────────────────────

        private static void WriteContentVariety(StringBuilder md, RoomContentLibrary content, RoomTemplateLibrary templates)
        {
            md.AppendLine("## 2. 방 유형마다 후보가 몇 개인가 — 체크리스트 P1-6");
            md.AppendLine();
            md.AppendLine("후보가 1개면 그 유형의 방은 **매 런 같은 모양·같은 콘텐츠**다. 층에 항목이 없으면 1층 풀로 폴백한다.");
            md.AppendLine();

            var tmpl = ReadEntries(templates, "templates");
            var cont = ReadEntries(content, "contentPrefabs");
            int floors = GameManagerMaxFloors();

            md.AppendLine("| 방 유형 | " + string.Join(" | ", Enumerable.Range(1, floors).Select(f => $"{f}층 지형")) +
                          " | " + string.Join(" | ", Enumerable.Range(1, floors).Select(f => $"{f}층 콘텐츠")) + " | 콘텐츠 프리팹 |");
            md.AppendLine("|---|" + string.Concat(Enumerable.Repeat("---:|", floors * 2)) + "---|");
            foreach (RoomType type in Enum.GetValues(typeof(RoomType)))
            {
                var t = Enumerable.Range(1, floors).Select(f => CountCell(tmpl, type, f));
                var c = Enumerable.Range(1, floors).Select(f => CountCell(cont, type, f));
                var names = cont.Where(e => e.type == type).SelectMany(e => e.names).Distinct();
                md.AppendLine($"| {type} | {string.Join(" | ", t)} | {string.Join(" | ", c)} | {string.Join(", ", names.DefaultIfEmpty("—"))} |");
            }
            md.AppendLine();
            md.AppendLine("표기: `N` = 그 층 전용 후보 N개, `(1층)` = 전용이 없어 1층 풀을 씀, `0` = 1층에도 없음.");
            md.AppendLine();

            // 콘텐츠 프리팹 공유
            var shared = cont.SelectMany(e => e.names.Select(n => (n, e.type)))
                             .GroupBy(x => x.n)
                             .Where(g => g.Select(x => x.type).Distinct().Count() > 1)
                             .ToList();
            if (shared.Count > 0)
            {
                md.AppendLine("**여러 방 유형이 같은 콘텐츠 프리팹을 공유**:");
                foreach (var g in shared)
                    md.AppendLine($"- `{g.Key}` ← {string.Join(", ", g.Select(x => x.type).Distinct())}");
                md.AppendLine();
            }

        }

        // ── P1-6·P1-7: 생성된 층 표본 ──────────────────────────────────────

        private static void WriteFloorSamples(StringBuilder md, RecipeDatabase db, RoomContentLibrary content)
        {
            md.AppendLine("## 3. 생성된 층 표본 — 체크리스트 P1-6·P1-7");
            md.AppendLine();
            md.AppendLine("실제 `RecipeDatabase`·`RoomContentLibrary`로 게임과 같은 경로(`DungeonGenerator.Generate`)를 돌렸다.");
            md.AppendLine("적 수는 방 유형별 콘텐츠 프리팹의 `EnemyBase` 개수(후보가 여럿이면 첫 번째 기준).");
            md.AppendLine();

            var basic = db.AllItems.Where(i => i != null && AlphabetWordRule.IsBasicCraftable(i)).ToList();
            int floors = GameManagerMaxFloors();

            md.AppendLine($"기본 제작 가능 아이템은 DB 전체에 **{basic.Count}개**, 층마다 그중 일부가 층 레시피로 뽑힌다.");
            md.AppendLine();

            var rows = new List<(int floor, string row)>();
            var typeTotals = new Dictionary<RoomType, (int rooms, int noLetters)>();
            for (int floor = 1; floor <= floors; floor++)
            {
                int f = floor;
                Func<RoomType, List<Capability>> caps = content != null ? (t => content.RequiredCapabilities(t, f)) : null;
                var maps = new List<DungeonMap>();
                for (int seed = 0; seed < SamplesPerFloor; seed++)
                    maps.Add(new DungeonGenerator().Generate(new DungeonConfig { FloorNumber = f, Seed = seed }, db, caps));

                var s = FloorSampler.Measure(maps, t => EnemyCount(content, t, f));
                var econ = maps.Select(m => LetterEconomy.Analyze(m.FloorRecipes, basic)).ToList();

                rows.Add((f, $"{(double)s.Rooms / s.Floors:0.0} | {(double)s.ConditionalRooms / s.Floors:0.00} | " +
                             $"{Pct(s.FloorsWithout(RoomType.EnvironmentPuzzle), s.Floors)} | {Pct(s.FloorsWithout(RoomType.Secret), s.Floors)} | " +
                             $"{Pct(s.EmptyHandedEnemies, s.Enemies)} | {econ.Average(e => e.BudgetSize):0.0} | " +
                             $"{Pct(econ.Count(e => e.ExtraCraftable.Count == 0), econ.Count)} |"));

                foreach (RoomType t in Enum.GetValues(typeof(RoomType)))
                {
                    typeTotals.TryGetValue(t, out var acc);
                    typeTotals[t] = (acc.rooms + s.RoomsOf(t), acc.noLetters + s.RoomsWithoutLetters(t));
                }
            }

            md.AppendLine("| 층 | 평균 방 수 | 조건부 방/층 | 환경퍼즐 **없는** 층 | 비밀방 없는 층 | 빈손 적 | 평균 예산(글자) | 예산으로 다른 것을 못 만드는 층 |");
            md.AppendLine("|---|---:|---:|---:|---:|---:|---:|---:|");
            bool allSame = rows.Select(r => r.row).Distinct().Count() == 1;
            if (allSame)
                md.AppendLine($"| 1~{floors}층 (전부 동일) | {rows[0].row}");
            else
                foreach (var r in rows) md.AppendLine($"| {r.floor} | {r.row}");
            md.AppendLine();

            if (allSame)
            {
                md.AppendLine("**층 간 차이 없음**: 같은 시드로 돌리면 1·2·3층이 **완전히 같은 층**이 나온다. " +
                              "생성기가 `DungeonConfig.FloorNumber`를 읽지 않고, 2·3층 콘텐츠·지형이 전부 1층 풀로 폴백하기 때문이다(위 2절). " +
                              "실제 게임은 층마다 랜덤 시드라 배치는 달라지지만, **분포(방 수·유형·예산·적 구성)는 층과 무관하게 같다.**");
                md.AppendLine();
            }

            md.AppendLine("- **빈손 적**: 잡아도 글자를 안 떨구는 적의 비율(`LootDistribution` 배분 기준).");
            md.AppendLine("- **예산으로 다른 것을 못 만드는 층**: 층 예산 글자만으로는 층 레시피 *외의* 기본 아이템을 하나도 만들 수 없는 층. " +
                          "이런 층에서는 예산이 곧 정답 목록이라 \"무엇을 만들까\"의 선택이 없다. " +
                          "(두 레시피가 같은 글자를 쓰는 것은 기회비용이 아니다 — 예산에 그 글자가 두 장 들어 있다.)");
            md.AppendLine();

            md.AppendLine("### 3-1. 방 유형별 등장 수와 글자 없는 방 (전 층 합계)");
            md.AppendLine();
            md.AppendLine("| 방 유형 | 등장 | 글자 0개인 방 |");
            md.AppendLine("|---|---:|---:|");
            foreach (var kv in typeTotals.Where(kv => kv.Value.rooms > 0))
                md.AppendLine($"| {kv.Key} | {kv.Value.rooms} | {Pct(kv.Value.noLetters, kv.Value.rooms)} |");
            md.AppendLine();
        }

        // ── 이 리포트가 답하지 않는 것 ──────────────────────────────────────

        private static void WriteNotMeasured(StringBuilder md)
        {
            md.AppendLine("## 4. 이 리포트가 재지 않는 것");
            md.AppendLine();
            md.AppendLine("정적 분석으로는 답할 수 없다. 자동 플레이(체크리스트 ③층) 또는 사람 플레이테스트가 필요하다.");
            md.AppendLine();
            md.AppendLine("- 제작→장착→사용 입력 수·시간 (P0-5) — 자동 플레이 필요");
            md.AppendLine("- 안전지대로 전투 무력화 (P0-3) — 적 도달성 시뮬 필요(D-16 미구현)");
            md.AppendLine("- 보스전 딜 불가 구간 (P0-4) — 자동 플레이 필요");
            md.AppendLine("- 세 단어를 **행동만으로** 구분하는가, 영리하다고 느낀 순간, 다시 하고 싶은가 — **사람만**");
            md.AppendLine();
        }

        // ── 보조 ──────────────────────────────────────────────────────────

        private static List<SourceFile> LoadGameSources()
        {
            var root = Path.GetFullPath("Assets/Scripts");
            return Directory.GetFiles(root, "*.cs", SearchOption.AllDirectories)
                .Where(p => !p.EndsWith("ItemDefinition.cs"))
                .Select(p => new SourceFile(Path.GetRelativePath(root, p).Replace('\\', '/'), File.ReadAllText(p)))
                .ToList();
        }

        // CLAUDE.md 네이밍: 멤버 = PascalCase 또는 _camelCase, 지역 = camelCase.
        private static bool IsMemberName(string name) =>
            name.Length > 0 && (char.IsUpper(name[0]) || name[0] == '_');

        private static string TopFolder(string relPath)
        {
            int i = relPath.IndexOf('/');
            return i < 0 ? "(root)" : relPath.Substring(0, i);
        }

        private static int DistinctValues(List<ItemDefinition> items, string field)
        {
            return items.Select(i => FieldText(i, field)).Distinct().Count();
        }

        private static string FieldText(ItemDefinition item, string field)
        {
            var v = typeof(ItemDefinition).GetField(field)?.GetValue(item);
            return v switch
            {
                null => "—",
                System.Collections.IList list => list.Count == 0 ? "∅" : string.Join(" ", list.Cast<object>().Select(ElementText)),
                _ => v.ToString()
            };
        }

        private static string ElementText(object o) =>
            o is MaterialRequirement m ? (m.Count > 1 ? $"{m.Material}×{m.Count}" : m.Material.ToString()) : o?.ToString();

        private sealed class LibEntry { public int floor; public RoomType type; public List<string> names = new(); }

        // 라이브러리의 private 목록을 런타임 코드 수정 없이 읽는다.
        private static List<LibEntry> ReadEntries(ScriptableObject lib, string listField)
        {
            var result = new List<LibEntry>();
            if (lib == null) return result;
            var entries = new SerializedObject(lib).FindProperty("_entries");
            for (int i = 0; i < entries.arraySize; i++)
            {
                var e = entries.GetArrayElementAtIndex(i);
                var entry = new LibEntry
                {
                    floor = e.FindPropertyRelative("floor").intValue,
                    type = (RoomType)e.FindPropertyRelative("type").intValue,
                };
                var list = e.FindPropertyRelative(listField);
                for (int k = 0; k < list.arraySize; k++)
                {
                    var obj = list.GetArrayElementAtIndex(k).objectReferenceValue;
                    if (obj != null) entry.names.Add(obj.name);
                }
                result.Add(entry);
            }
            return result;
        }

        private static string CountCell(List<LibEntry> entries, RoomType type, int floor)
        {
            int own = entries.Where(e => e.type == type && e.floor == floor).Sum(e => e.names.Count);
            if (own > 0) return own.ToString();
            if (floor == RoomTemplateNaming.DefaultFloor) return "0";
            int fallback = entries.Where(e => e.type == type && e.floor == RoomTemplateNaming.DefaultFloor).Sum(e => e.names.Count);
            return fallback > 0 ? $"(1층)" : "0";
        }

        private static readonly Dictionary<(RoomType, int), int> EnemyCache = new();

        private static int EnemyCount(RoomContentLibrary content, RoomType type, int floor)
        {
            if (content == null) return 0;
            if (EnemyCache.TryGetValue((type, floor), out var n)) return n;
            var prefab = content.Pick(type, floor, 0);
            n = prefab != null ? prefab.GetComponentsInChildren<Help.Enemy.EnemyBase>(true).Length : 0;
            EnemyCache[(type, floor)] = n;
            return n;
        }

        private static int GameManagerMaxFloors() => Help.Core.GameManager.MaxFloors;

        private static string Pct(int part, int whole) =>
            whole <= 0 ? "—" : $"{100.0 * part / whole:0}% ({part}/{whole})";

        private static string GitDescribe()
        {
            try
            {
                string head = RunGit("rev-parse --short HEAD").Trim();
                int dirty = RunGit("status --porcelain").Split('\n').Count(l => l.Trim().Length > 0);
                return dirty > 0 ? $"`{head}` + 미커밋 변경 {dirty}개" : $"`{head}`";
            }
            catch { return "알 수 없음"; }
        }

        private static string RunGit(string args)
        {
            var psi = new System.Diagnostics.ProcessStartInfo("git", args)
            {
                RedirectStandardOutput = true, UseShellExecute = false, CreateNoWindow = true,
                WorkingDirectory = Path.GetFullPath(".")
            };
            using var p = System.Diagnostics.Process.Start(psi);
            string o = p.StandardOutput.ReadToEnd();
            p.WaitForExit(5000);
            return o;
        }
    }
}
