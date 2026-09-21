using System.IO;
using NUnit.Framework;
using UnityEngine;

namespace Tests.EditMode
{
    public class TopDownInputConfigurationTests
    {
        [Test]
        public void PlayerMapShouldUsePointerPositionAndHaveNoJumpAction()
        {
            string path = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Assets", "InputSystem_Actions.inputactions"));
            string json = File.ReadAllText(path);

            StringAssert.DoesNotContain("\"name\": \"Jump\"", json);
            StringAssert.DoesNotContain("\"action\": \"Jump\"", json);
            StringAssert.Contains("\"path\": \"<Pointer>/position\"", json);
            StringAssert.DoesNotContain("\"path\": \"<Pointer>/delta\"", json);
        }
    }
}
