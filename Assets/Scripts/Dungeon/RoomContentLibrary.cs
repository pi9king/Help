using System.Collections.Generic;
using UnityEngine;

namespace Help.Dungeon
{
    // (층, 방 유형) → 콘텐츠 프리팹 매핑. 방을 로드할 때 RoomManager가 맞는 콘텐츠 프리팹을
    // 결정적으로 골라 스폰한다(적/퍼즐/루팅을 씬에 손배치하는 대신 데이터로 저작).
    //
    // D-13: 층마다 적 풀이 다르다 — 1층은 약한 적, 3층은 강한 적.
    // 지형(RoomTemplateLibrary)과 같은 축을 쓴다.
    // 콘텐츠 프리팹 = 방 중심(원점) 기준으로 자식(적/장애물/RoomPuzzle 등)을 배치한 GameObject.
    [CreateAssetMenu(fileName = "RoomContentLibrary", menuName = "Help/Room Content Library")]
    public class RoomContentLibrary : ScriptableObject
    {
        [System.Serializable]
        public class Entry
        {
            // 기존 에셋에는 이 필드가 없다 — Unity가 Entry를 생성한 뒤 YAML 값으로 덮으므로
            // 없는 필드는 이 초기값(1층)이 그대로 남는다. 즉 예전 데이터는 1층 풀이 된다.
            public int floor = RoomTemplateNaming.DefaultFloor;
            public RoomType type;
            public List<GameObject> contentPrefabs = new();

            // 켜면 통짜 콘텐츠 프리팹 대신 **방 템플릿의 마커 위치**에 개별 프리팹을 스폰한다.
            // 기본은 꺼짐 — 기존에 검증된 손배치 경로를 그대로 두고, 방 유형별로 하나씩 옮긴다.
            public bool useTemplateMarkers;
        }

        // 템플릿 마커 문자 → 스폰할 프리팹. 여러 개면 방마다 결정적으로 하나 고른다.
        [System.Serializable]
        public class MarkerEntry
        {
            public string symbol = "e";   // 한 글자. char는 인스펙터에서 다루기 어려워 문자열로 둔다.
            public List<GameObject> prefabs = new();
        }

        [SerializeField] private List<Entry> _entries = new();
        [SerializeField] private List<MarkerEntry> _markerPrefabs = new();

        // 층·유형에 맞는 콘텐츠 프리팹을 결정적으로 선택(같은 seed=같은 콘텐츠 → 방마다 안정적).
        // 그 층에 없으면 1층 풀로 폴백한다 — 2·3층 콘텐츠를 아직 안 만들었을 때 방이 비지 않도록.
        public GameObject Pick(RoomType type, int floor, int seed)
        {
            var e = Find(type, floor);
            if (e == null && floor != RoomTemplateNaming.DefaultFloor)
                e = Find(type, RoomTemplateNaming.DefaultFloor);

            if (e == null) return null;
            return e.contentPrefabs[SelectIndex(seed, e.contentPrefabs.Count)];
        }

        private Entry Find(RoomType type, int floor)
        {
            foreach (var e in _entries)
            {
                if (e.type != type || e.floor != floor) continue;
                if (e.contentPrefabs == null || e.contentPrefabs.Count == 0) continue;
                return e;
            }
            return null;
        }

        // 이 층·유형의 콘텐츠가 요구하는 능력들. 던전 생성기가 **진입 조건을 여기서 역산**한다 —
        // 레이어1(입장 판정)과 레이어2(클리어 조건)가 어긋나지 않게 하는 단일 출처다.
        //
        // 후보가 여럿이면 **합집합**을 돌려준다(보수적). 어떤 프리팹이 뽑히든 플레이어가
        // 대응할 수 있어야 하기 때문이다.
        public List<Help.Item.Capability> RequiredCapabilities(RoomType type, int floor)
        {
            var result = new List<Help.Item.Capability>();
            var e = Find(type, floor);
            if (e == null && floor != RoomTemplateNaming.DefaultFloor)
                e = Find(type, RoomTemplateNaming.DefaultFloor);
            if (e == null) return result;

            foreach (var prefab in e.contentPrefabs)
            {
                if (prefab == null) continue;
                foreach (var target in prefab.GetComponentsInChildren<Help.Puzzle.CapabilityTarget>(true))
                {
                    var cap = target.RequiredCapability;
                    if (cap != Help.Item.Capability.None && !result.Contains(cap)) result.Add(cap);
                }
            }
            return result;
        }

        // 결정적 인덱스 선택(순수 — EditMode 테스트 가능). 음수 seed도 안전.
        public static int SelectIndex(int seed, int count)
        {
            if (count <= 0) return 0;
            return (int)((uint)seed % (uint)count);
        }

        // 이 방 유형이 템플릿 마커로 콘텐츠를 스폰하는가.
        public bool UsesTemplateMarkers(RoomType type, int floor = RoomTemplateNaming.DefaultFloor)
        {
            var e = _entries.Find(x => x.type == type && x.floor == floor)
                 ?? _entries.Find(x => x.type == type && x.floor == RoomTemplateNaming.DefaultFloor);
            return e != null && e.useTemplateMarkers;
        }

        // 마커 문자에 해당하는 프리팹. 등록이 없으면 null(그 마커는 스폰하지 않는다).
        public GameObject PickForMarker(char symbol, int seed)
        {
            foreach (var m in _markerPrefabs)
            {
                if (string.IsNullOrEmpty(m.symbol) || m.symbol[0] != symbol) continue;
                if (m.prefabs == null || m.prefabs.Count == 0) return null;
                return m.prefabs[SelectIndex(seed, m.prefabs.Count)];
            }
            return null;
        }

        // 에디터/테스트용 구성 API
        public void AddMarkerPrefab(char symbol, GameObject prefab)
        {
            var m = _markerPrefabs.Find(x => !string.IsNullOrEmpty(x.symbol) && x.symbol[0] == symbol);
            if (m == null) { m = new MarkerEntry { symbol = symbol.ToString() }; _markerPrefabs.Add(m); }
            m.prefabs.Add(prefab);
        }

        // floor 기본값 1: 기존 호출부(에디터 셋업·테스트)를 그대로 두고 층을 점진 도입한다.
        public void SetUsesTemplateMarkers(RoomType type, bool value,
                                           int floor = RoomTemplateNaming.DefaultFloor)
        {
            Ensure(type, floor).useTemplateMarkers = value;
        }

        public void AddEntry(RoomType type, GameObject prefab,
                             int floor = RoomTemplateNaming.DefaultFloor)
        {
            Ensure(type, floor).contentPrefabs.Add(prefab);
        }

        private Entry Ensure(RoomType type, int floor)
        {
            var e = _entries.Find(x => x.type == type && x.floor == floor);
            if (e == null) { e = new Entry { floor = floor, type = type }; _entries.Add(e); }
            return e;
        }
    }
}
