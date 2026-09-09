using System.Collections.Generic;
using UnityEngine;

namespace Help.Dungeon
{
    // (층, 방 유형) → ASCII 지형 템플릿(.txt) 매핑. RoomContentLibrary가 "방에 무엇을 놓을지"를
    // 정한다면 이쪽은 "방이 어떻게 생겼는지"를 정한다.
    //
    // D-11: **크기는 고른 템플릿에서 파생된다.** 예전엔 생성기가 크기를 먼저 굴리고(24%)
    // 그 크기의 템플릿을 찾았는데, 맞는 게 없으면 절차적 빈 상자로 떨어졌다.
    // 이제 유형에 맞는 템플릿 중에서 고르고, 방 크기는 그 결과를 따른다.
    //
    // D-13: 층마다 템플릿 풀이 다르다 — 1층은 평탄하게, 3층은 험하게.
    //
    // 같은 방(좌표)은 항상 같은 템플릿을 받는다 — 나갔다 돌아왔는데 지형이 바뀌면 안 된다.
    [CreateAssetMenu(fileName = "RoomTemplateLibrary", menuName = "Help/Room Template Library")]
    public class RoomTemplateLibrary : ScriptableObject
    {
        [System.Serializable]
        public class Entry
        {
            public int floor = RoomTemplateNaming.DefaultFloor;
            public RoomType type;
            public List<TextAsset> templates = new();
        }

        [SerializeField] private List<Entry> _entries = new();

        // 파싱은 비싸지 않지만 방을 옮길 때마다 하긴 아까우므로 캐시한다.
        private readonly Dictionary<TextAsset, RoomTemplate> _cache = new();

        // 층·유형에 맞는 템플릿을 결정적으로 고른다. 맞는 게 없으면 null
        // (RoomManager가 절차적 셸로 폴백한다 — 템플릿이 없다고 게임이 멈추면 안 된다).
        //
        // 그 층에 템플릿이 없으면 1층 풀로 폴백한다. 2·3층을 아직 안 그렸을 때
        // 방이 통째로 빈 상자가 되는 걸 막는다(D-14의 "1층부터 채운다" 진행 방식 대응).
        public RoomTemplate Pick(RoomType type, int floor, int seed)
        {
            var candidates = Collect(type, floor);
            if (candidates.Count == 0 && floor != RoomTemplateNaming.DefaultFloor)
                candidates = Collect(type, RoomTemplateNaming.DefaultFloor);

            if (candidates.Count == 0) return null;
            return candidates[RoomContentLibrary.SelectIndex(seed, candidates.Count)];
        }

        private List<RoomTemplate> Collect(RoomType type, int floor)
        {
            var candidates = new List<RoomTemplate>();
            foreach (var e in _entries)
            {
                if (e.type != type || e.floor != floor || e.templates == null) continue;
                foreach (var asset in e.templates)
                {
                    var t = Load(asset);
                    if (t != null) candidates.Add(t);
                }
            }
            return candidates;
        }

        public RoomTemplate Load(TextAsset asset)
        {
            if (asset == null) return null;
            if (_cache.TryGetValue(asset, out var cached)) return cached;

            var parsed = RoomTemplateParser.Parse(asset.text, asset.name);
            if (!parsed.Success)
            {
                // 잘못된 템플릿은 조용히 무시하지 않는다 — 안 그러면 왜 폴백됐는지 알 수 없다.
                Debug.LogError($"[RoomTemplateLibrary] 템플릿 파싱 실패:\n{string.Join("\n", parsed.Errors)}");
                _cache[asset] = null;
                return null;
            }

            _cache[asset] = parsed.Template;
            return parsed.Template;
        }

        // 에디터/테스트용 구성 API
        public void AddEntry(int floor, RoomType type, TextAsset asset)
        {
            var e = _entries.Find(x => x.type == type && x.floor == floor);
            if (e == null) { e = new Entry { floor = floor, type = type }; _entries.Add(e); }
            e.templates.Add(asset);
        }

        public void Clear() { _entries.Clear(); _cache.Clear(); }

        public IReadOnlyList<Entry> Entries => _entries;
    }
}
