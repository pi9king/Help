using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.Tilemaps;
using Help.Dungeon;
using Help.Player;

namespace Tests.PlayMode
{
    public class QuarterViewRuntimeTests
    {
        [UnityTest]
        public IEnumerator PrototypeSceneShouldBootWithPlanarPhysicsAndWalkableFloor()
        {
            AsyncOperation load = SceneManager.LoadSceneAsync("QuarterViewPrototype", LoadSceneMode.Single);
            while (!load.isDone) yield return null;
            yield return null;
            yield return new WaitForFixedUpdate();

            PlayerController player = Object.FindFirstObjectByType<PlayerController>();
            RoomManager rooms = Object.FindFirstObjectByType<RoomManager>();
            Assert.IsNotNull(player);
            Assert.IsNotNull(rooms);
            Assert.IsNotNull(rooms.CurrentRoom, "던전 시작 방이 로드되지 않았습니다.");

            Rigidbody2D body = player.GetComponent<Rigidbody2D>();
            Assert.AreEqual(0f, body.gravityScale);
            Assert.IsTrue(body.freezeRotation);
            Assert.IsNotNull(player.GetComponent<CircleCollider2D>());

            Tilemap floor = GetPrivate<Tilemap>(rooms, "_floorTilemap");
            Assert.IsNotNull(floor, "쿼터뷰 바닥 Tilemap이 만들어지지 않았습니다.");
            TilemapCollider2D floorCollider = floor.GetComponent<TilemapCollider2D>();
            Assert.IsTrue(floorCollider == null || !floorCollider.enabled,
                "이동 바닥의 콜라이더가 플레이어를 막고 있습니다.");
        }

        private static T GetPrivate<T>(object target, string field) where T : class
        {
            FieldInfo info = target.GetType().GetField(field, BindingFlags.NonPublic | BindingFlags.Instance);
            return info?.GetValue(target) as T;
        }
    }
}
