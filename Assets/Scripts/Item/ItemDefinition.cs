using System.Collections.Generic;
using Help.Combat;
using UnityEngine;

namespace Help.Item
{
    [CreateAssetMenu(fileName = "Item", menuName = "Help/Item Definition")]
    public class ItemDefinition : ScriptableObject
    {
        public string Id;
        public string Word;        // 영단어 (예: "BLADE")
        public ItemType Type;
        public ElementType Element;
        public WeaponCategory WeaponCategory;
        public List<Capability> Capabilities = new(); // 이 아이템이 제공하는 능력(퍼즐/장애물 요구 판정용)
        public List<MaterialRequirement> Recipe = new();

        // 장비 스탯
        public int AttackBonus;
        public int DefenseBonus;
        public float AttackSpeedMult = 1f;

        // 소모품(Consumable) 사용 시 회복량. 0이면 회복 효과 없음.
        public int HealAmount;

        // 특수방 제작대에서 만들 때 드는 골드(기본 제작에는 적용되지 않음).
        public int GoldCost;
    }
}
