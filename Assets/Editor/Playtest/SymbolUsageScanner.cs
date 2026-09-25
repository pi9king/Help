using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Help.Editor.Playtest
{
    public readonly struct SourceFile
    {
        public readonly string Path;
        public readonly string Text;
        public SourceFile(string path, string text) { Path = path; Text = text; }
    }

    public readonly struct UsageSite
    {
        public readonly string Path;
        public readonly int Line;       // 1부터
        public readonly bool IsWrite;
        public UsageSite(string path, int line, bool isWrite) { Path = path; Line = line; IsWrite = isWrite; }
    }

    // 소스 텍스트에서 "이 데이터를 게임 코드가 실제로 읽는가"를 센다 — 정적 재미 리포트의 죽은 데이터 측정.
    //
    // 컴파일러 수준 분석이 아니라 정규식 근사다. 목적은 "정의만 있고 아무도 안 읽는 값"을
    // 사람에게 보여주는 것이라, 과소집계(놓침)보다 **주석·문자열 오염으로 인한 과대집계**를 더 경계한다 —
    // 이 코드베이스는 주석에 필드명을 자주 쓴다.
    public static class SymbolUsageScanner
    {
        // 대입 연산자: =, +=, -= 등. ==, => 는 제외.
        private const string AssignOp = @"\s*[+\-*/|&]?=(?![=>])";

        private static readonly Regex StringLiteral = new(@"@?""(?:[^""\\]|\\.)*""", RegexOptions.Compiled);

        // `.member` 형태의 접근. 읽기/쓰기를 가른다.
        public static List<UsageSite> FindMemberUsages(string member, IEnumerable<SourceFile> files)
        {
            var access = new Regex(@"\." + Regex.Escape(member) + @"\b");
            var write = new Regex(@"^" + AssignOp);
            var result = new List<UsageSite>();

            foreach (var (file, lineNo, code) in CodeLines(files))
                foreach (Match m in access.Matches(code))
                {
                    bool isWrite = write.IsMatch(code.Substring(m.Index + m.Length));
                    result.Add(new UsageSite(file.Path, lineNo, isWrite));
                }
            return result;
        }

        // `lhs = ...member...` 처럼 member 값을 받아 담는 이름들(한 홉 추적용).
        public static List<string> FindDerivedSymbols(string member, IEnumerable<SourceFile> files)
        {
            var assign = new Regex(@"(?<lhs>[A-Za-z_]\w*)" + AssignOp + @"(?<rhs>[^;]*)");
            var access = new Regex(@"\." + Regex.Escape(member) + @"\b");
            var result = new List<string>();

            foreach (var (_, _, code) in CodeLines(files))
                foreach (Match m in assign.Matches(code))
                {
                    string lhs = m.Groups["lhs"].Value;
                    if (lhs == member || result.Contains(lhs)) continue;
                    if (access.IsMatch(m.Groups["rhs"].Value)) result.Add(lhs);
                }
            return result;
        }

        // 식별자의 진짜 읽기(선언·대입 제외). 멤버 접근(p.X)과 맨 이름(X) 모두 센다.
        public static List<UsageSite> FindIdentifierReads(string ident, IEnumerable<SourceFile> files)
        {
            string id = Regex.Escape(ident);
            var occurrence = new Regex(@"\b" + id + @"\b");
            var write = new Regex(@"^" + AssignOp);
            var declaration = new Regex(
                @"\b(public|private|protected|internal)\b[^=;(]*\b" + id + @"\b\s*(\{|=|;)" +
                @"|\b(var|int|float|bool|string|double|long|[A-Z]\w*(<[^>]*>)?(\[\])?)\s+" + id + @"\s*(=|;)");
            var result = new List<UsageSite>();

            foreach (var (file, lineNo, code) in CodeLines(files))
            {
                if (declaration.IsMatch(code)) continue;
                foreach (Match m in occurrence.Matches(code))
                {
                    if (write.IsMatch(code.Substring(m.Index + m.Length))) continue;
                    result.Add(new UsageSite(file.Path, lineNo, false));
                }
            }
            return result;
        }

        // 주석과 문자열 리터럴을 걷어낸 코드 줄. 줄 번호는 원본 기준(1부터).
        private static IEnumerable<(SourceFile file, int line, string code)> CodeLines(IEnumerable<SourceFile> files)
        {
            foreach (var file in files)
            {
                if (string.IsNullOrEmpty(file.Text)) continue;
                var lines = file.Text.Split('\n');
                bool inBlock = false;
                for (int i = 0; i < lines.Length; i++)
                {
                    string code = StringLiteral.Replace(lines[i], "\"\"");
                    code = StripComments(code, ref inBlock);
                    if (code.Trim().Length > 0) yield return (file, i + 1, code);
                }
            }
        }

        private static string StripComments(string line, ref bool inBlock)
        {
            var sb = new System.Text.StringBuilder(line.Length);
            for (int i = 0; i < line.Length; i++)
            {
                if (inBlock)
                {
                    if (line[i] == '*' && i + 1 < line.Length && line[i + 1] == '/') { inBlock = false; i++; }
                    continue;
                }
                if (line[i] == '/' && i + 1 < line.Length)
                {
                    if (line[i + 1] == '/') break;
                    if (line[i + 1] == '*') { inBlock = true; i++; continue; }
                }
                sb.Append(line[i]);
            }
            return sb.ToString();
        }
    }
}
