#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace JSchool.Modules.Common.OSY
{
    //특정 오브젝트를 또다른 오브젝트들 자식으로 심을 수 있는 기능 제공
    [CustomEditor(typeof(SYObjectPlanter))]
    public class SYObjectPlanterButton : Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            SYObjectPlanter planter = (SYObjectPlanter)target;
            GUILayout.Label("\r\n※주의! 이 과정은 되돌릴 수 없습니다.※", EditorStyles.boldLabel);
            if (GUILayout.Button("배치!"))
                planter.PlantAll();
        }
    }

    public class SYObjectPlanter : MonoBehaviour
    {
        public Transform[] targets;
        public GameObject @object;
        public int siblingIndex;

        public void PlantAll()
        {
            for (var i = 0; i < targets.Length; i++)
                Plant(targets[i]);
        }

        public void Plant(Transform target)
        {
            var planted = Instantiate(@object).transform;

            planted.SetParent(target);
            if (siblingIndex >= 0)
                planted.SetSiblingIndex(siblingIndex);
        }
    }
}
#endif