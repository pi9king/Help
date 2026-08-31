using System.Collections.Generic;
using NUnit.Framework;
using Help.Inventory;
using Help.Item;
using UnityEngine;

namespace Tests.EditMode
{
    public class MaterialPoolCalculatorTests
    {
        private ItemDefinition MakeMaterial(AlphabetMaterial mat)
        {
            var def = ScriptableObject.CreateInstance<ItemDefinition>();
            def.Id = $"mat_{mat}";
            def.Word = mat.ToString();
            def.Type = ItemType.Material;
            def.Recipe = new List<MaterialRequirement>
            {
                new MaterialRequirement(mat, 1)
            };
            return def;
        }

        private ItemDefinition MakeWeapon(string id, string word, List<MaterialRequirement> recipe)
        {
            var def = ScriptableObject.CreateInstance<ItemDefinition>();
            def.Id = id;
            def.Word = word;
            def.Type = ItemType.Weapon;
            def.Recipe = recipe;
            return def;
        }

        [Test]
        public void ShouldCountRawMaterialsInInventory()
        {
            var inv = new Inventory();
            var matB = MakeMaterial(AlphabetMaterial.B);
            inv.Add(matB, 3);

            var pool = MaterialPoolCalculator.GetTotalPool(inv);
            Assert.AreEqual(3, pool[AlphabetMaterial.B]);
        }

        [Test]
        public void ShouldIncludeEquippedItemMaterialsInPool()
        {
            var inv = new Inventory();
            var blade = MakeWeapon("blade", "BLADE", new List<MaterialRequirement>
            {
                new MaterialRequirement(AlphabetMaterial.B, 1),
                new MaterialRequirement(AlphabetMaterial.L, 1),
                new MaterialRequirement(AlphabetMaterial.A, 1),
                new MaterialRequirement(AlphabetMaterial.D, 1),
            });

            inv.Add(blade);
            var stack = inv.Items[0];
            inv.Equip(stack, EquipmentSlotType.Weapon);

            var pool = MaterialPoolCalculator.GetTotalPool(inv);
            Assert.AreEqual(1, pool[AlphabetMaterial.B]);
            Assert.AreEqual(1, pool[AlphabetMaterial.L]);
        }

        [Test]
        public void ShouldCombineInventoryAndEquippedMaterials()
        {
            var inv = new Inventory();
            var matB = MakeMaterial(AlphabetMaterial.B);
            inv.Add(matB, 2);

            var blade = MakeWeapon("blade", "BLADE", new List<MaterialRequirement>
            {
                new MaterialRequirement(AlphabetMaterial.B, 1),
            });
            inv.Add(blade);
            inv.Equip(inv.Items[^1], EquipmentSlotType.Weapon);

            var pool = MaterialPoolCalculator.GetTotalPool(inv);
            Assert.AreEqual(3, pool[AlphabetMaterial.B]);  // 인벤토리 2 + 장착 분해 1
        }
    }
}
