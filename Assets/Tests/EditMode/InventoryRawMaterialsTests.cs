using System.Collections.Generic;
using NUnit.Framework;
using Help.Inventory;
using Help.Item;
using UnityEngine;

namespace Tests.EditMode
{
    public class InventoryRawMaterialsTests
    {
        private ItemDefinition MakeMaterial(AlphabetMaterial mat, int recipeCount)
        {
            var def = ScriptableObject.CreateInstance<ItemDefinition>();
            def.Id = $"mat_{mat}";
            def.Word = mat.ToString();
            def.Type = ItemType.Material;
            def.Recipe = new List<MaterialRequirement> { new MaterialRequirement(mat, recipeCount) };
            return def;
        }

        [Test]
        public void GetRawMaterials_RespectsRecipeCount()
        {
            var inv = new Inventory();
            // 자기참조 Recipe Count=2 인 재료를 3개 보유 → 6개로 계산되어야 함
            inv.Add(MakeMaterial(AlphabetMaterial.A, 2), 3);

            var raw = inv.GetRawMaterials();

            Assert.AreEqual(6, raw[AlphabetMaterial.A]);
        }

        [Test]
        public void GetRawMaterials_MatchesTotalPoolForMaterialsOnly()
        {
            var inv = new Inventory();
            inv.Add(MakeMaterial(AlphabetMaterial.B, 1), 4);

            var raw = inv.GetRawMaterials();
            var total = MaterialPoolCalculator.GetTotalPool(inv);

            Assert.AreEqual(total[AlphabetMaterial.B], raw[AlphabetMaterial.B]);
        }
    }
}
