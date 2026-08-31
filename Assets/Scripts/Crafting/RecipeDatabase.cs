using System.Collections.Generic;
using Help.Item;
using UnityEngine;

namespace Help.Crafting
{
    [CreateAssetMenu(fileName = "RecipeDatabase", menuName = "Help/Recipe Database")]
    public class RecipeDatabase : ScriptableObject
    {
        [SerializeField] private List<ItemDefinition> _items = new();

        // 단일 알파벳 재료 아이템 (Id = "mat_A" 형식)
        [SerializeField] private List<ItemDefinition> _materials = new();

        public ItemDefinition Find(string itemId) =>
            _items.Find(i => i.Id == itemId);

        public ItemDefinition FindMaterial(AlphabetMaterial mat) =>
            _materials.Find(m => m.Id == $"mat_{mat}");

        public IReadOnlyList<ItemDefinition> AllItems => _items;

        public void AddItem(ItemDefinition item) => _items.Add(item);

        public void AddMaterial(ItemDefinition item) => _materials.Add(item);
    }
}
