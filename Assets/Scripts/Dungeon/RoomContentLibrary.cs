using System.Collections.Generic;
using UnityEngine;

namespace Help.Dungeon
{
    // 방 유형 → 콘텐츠 프리팹 매핑. 방을 로드할 때 RoomManager가 유형에 맞는 콘텐츠 프리팹을
    // 결정적으로 골라 스폰한다(적/퍼즐/루팅을 씬에 손배치하는 대신 데이터로 저작).
    // 콘텐츠 프리팹 = 방 중심(원점) 기준으로 자식(적/장애물/RoomPuzzle 등)을 배치한 GameObject.
    [CreateAssetMenu(fileName = "RoomContentLibrary", menuName = "Help/Room Content Library")]
    public class RoomContentLibrary : ScriptableObject
    {
        [System.Serializable]
        public class Entry
        {
            public RoomType type;
            public List<GameObject> contentPrefabs = new();
        }

        [SerializeField] private List<Entry> _entries = new();

        // 방 유형에 맞는 콘텐츠 프리팹을 결정적으로 선택(같은 seed=같은 콘텐츠 → 방마다 안정적). 없으면 null.
        public GameObject Pick(RoomType type, int seed)
        {
            foreach (var e in _entries)
            {
                if (e.type != type || e.contentPrefabs == null || e.contentPrefabs.Count == 0) continue;
                return e.contentPrefabs[SelectIndex(seed, e.contentPrefabs.Count)];
            }
            return null;
        }

        // 결정적 인덱스 선택(순수 — EditMode 테스트 가능). 음수 seed도 안전.
        public static int SelectIndex(int seed, int count)
        {
            if (count <= 0) return 0;
            return (int)((uint)seed % (uint)count);
        }

        // 에디터/테스트용 구성 API
        public void AddEntry(RoomType type, GameObject prefab)
        {
            var e = _entries.Find(x => x.type == type);
            if (e == null) { e = new Entry { type = type }; _entries.Add(e); }
            e.contentPrefabs.Add(prefab);
        }
    }
}
