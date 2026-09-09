using System;

namespace Help.Dungeon
{
    // 템플릿 파일 규약 (순수). 인터뷰 D-13(층)·D-17(작업중 격리)에서 확정.
    //
    //   Assets/Rooms/Floor{N}/{RoomType}_{SizeClass}_{NN}[_draft].txt
    //
    // 층은 폴더명, 작업중 표시는 파일명 접미사다.
    // 파일명의 크기(Small 등)는 사람이 읽기 위한 것 — 실제 크기는 격자를 파싱해서 나온다.
    //
    // 이 규칙은 에디터 셋업(RoomTemplateSetup)과 검증 테스트가 **함께** 써야 한다.
    // 한쪽만 draft를 걸러내면 "테스트는 통과하는데 라이브러리엔 들어간" 방이 생긴다.
    public static class RoomTemplateNaming
    {
        public const string DraftSuffix = "_draft";
        public const string FloorFolderPrefix = "Floor";
        public const int DefaultFloor = 1;

        // 작업 중인 템플릿인가. 접미사만 본다 — 이름 가운데 "draft"가 든 방을 오탐하지 않도록.
        public static bool IsDraft(string fileNameWithoutExtension) =>
            !string.IsNullOrEmpty(fileNameWithoutExtension) &&
            fileNameWithoutExtension.EndsWith(DraftSuffix, StringComparison.OrdinalIgnoreCase);

        // 경로에서 층 번호를 읽는다. 층 폴더가 없거나 해석되지 않으면 1층으로 본다
        // (규약 도입 전 파일이 남아 있어도 게임이 멈추지 않게).
        public static int FloorOf(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath)) return DefaultFloor;

            var parts = assetPath.Replace('\\', '/').Split('/');
            // 마지막은 파일명이므로 그 앞의 폴더들만 본다.
            for (int i = parts.Length - 2; i >= 0; i--)
            {
                var seg = parts[i];
                if (!seg.StartsWith(FloorFolderPrefix, StringComparison.OrdinalIgnoreCase)) continue;
                if (int.TryParse(seg.Substring(FloorFolderPrefix.Length), out var floor)) return floor;
            }
            return DefaultFloor;
        }
    }
}
