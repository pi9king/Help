using NUnit.Framework;
using Help.Inventory;
using Help.Item;

namespace Tests.EditMode
{
    public class EquipmentSlotResolverTests
    {
        [Test]
        public void Weapon_ResolvesToWeaponSlot()
        {
            Assert.IsTrue(EquipmentSlotResolver.TryResolve(ItemType.Weapon, out var slot));
            Assert.AreEqual(EquipmentSlotType.Weapon, slot);
        }

        [Test]
        public void SubWeapon_ResolvesToSubWeaponSlot()
        {
            Assert.IsTrue(EquipmentSlotResolver.TryResolve(ItemType.SubWeapon, out var slot));
            Assert.AreEqual(EquipmentSlotType.SubWeapon, slot);
        }

        [Test]
        public void HeadArmor_ResolvesToHeadSlot()
        {
            Assert.IsTrue(EquipmentSlotResolver.TryResolve(ItemType.HeadArmor, out var slot));
            Assert.AreEqual(EquipmentSlotType.Head, slot);
        }

        [Test]
        public void BodyArmor_ResolvesToBodySlot()
        {
            Assert.IsTrue(EquipmentSlotResolver.TryResolve(ItemType.BodyArmor, out var slot));
            Assert.AreEqual(EquipmentSlotType.Body, slot);
        }

        [Test]
        public void LegArmor_ResolvesToLegsSlot()
        {
            Assert.IsTrue(EquipmentSlotResolver.TryResolve(ItemType.LegArmor, out var slot));
            Assert.AreEqual(EquipmentSlotType.Legs, slot);
        }

        [Test]
        public void Accessory_ResolvesToAccessorySlot()
        {
            Assert.IsTrue(EquipmentSlotResolver.TryResolve(ItemType.Accessory, out var slot));
            Assert.AreEqual(EquipmentSlotType.Accessory, slot);
        }

        [Test]
        public void Material_IsNotEquippable()
        {
            Assert.IsFalse(EquipmentSlotResolver.TryResolve(ItemType.Material, out _));
        }

        [Test]
        public void Consumable_IsNotEquippable()
        {
            Assert.IsFalse(EquipmentSlotResolver.TryResolve(ItemType.Consumable, out _));
        }
    }
}
