using UnityEditor;
using UnityEngine;

namespace JSchool.Modules.Common.OSY
{
    [CustomEditor(typeof(SYComponentConverter))]
    public class SYComponentConverterButton : Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            SYComponentConverter converter = (SYComponentConverter)target;
            GUILayout.Label("\r\n※주의! 이 과정은 되돌릴 수 없습니다.※", EditorStyles.boldLabel);
            if (GUILayout.Button("변환!"))
                converter.ConvertAll();
        }
    }
}