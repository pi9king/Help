using UnityEngine;
using Help.Crafting;

namespace Help.UI
{
    // 씬에 배치하여 RecipeDatabase SO를 CraftingUI에 노출하는 브릿지
    public class RecipeDatabaseBridge : MonoBehaviour
    {
        [SerializeField] private RecipeDatabase _database;
        public RecipeDatabase Database => _database;
    }
}
