using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>実シーンの段差端で左右の脚の到達位置を比較します。</summary>
public static class PlayerFootIKSceneCheck
{
    [MenuItem("Tools/Yagi/Validate Foot IK")]
    public static void Validate()
    {
        var scene = EditorSceneManager.OpenPreviewScene("Assets/PreDevelop/Yagi/IKTest/IKTestScene.unity");
        try
        {
            foreach (var root in scene.GetRootGameObjects())
            foreach (var ik in root.GetComponentsInChildren<PlayerFootIK>(true))
            {
                ik.SendMessage("Awake");
                var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
                var leg = typeof(PlayerFootIK).GetField("m_left", flags).GetValue(ik);
                var ankle = (Transform)leg.GetType().GetProperty("Ankle").GetValue(leg);
                var motor = ik.GetComponent<PlayerMotor>();
                var serialized = new SerializedObject(motor);
                if (!serialized.FindProperty("m_verticalOnlyStep").boolValue)
                    throw new System.Exception("PlayerMotor scene override is missing.");
                Vector3 start = ankle.position;
                leg.GetType().GetMethod("Solve").Invoke(leg, new object[] { .12f, Vector3.up, 1f, ik.transform.forward });
                float rise = ankle.position.y - start.y;
                Debug.Log("FootIK validation: " + ankle.name + " rise=" + rise);
                if (Mathf.Abs(rise - .12f) > .02f) throw new System.Exception("Foot target missed.");
                return;
            }
            throw new System.Exception("PlayerFootIK missing.");
        }
        finally { EditorSceneManager.ClosePreviewScene(scene); }
    }
}
