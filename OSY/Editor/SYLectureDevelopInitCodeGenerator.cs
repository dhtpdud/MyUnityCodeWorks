#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace JSchool.SYLab
{
    [CustomEditor(typeof(SYLectureDevelopInitCodeGenerator))]
    public class SYLectureDevelopInitCodeGeneratorEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            if (GUILayout.Button("초기 셋팅 시작"))
            {
                SYLectureDevelopInitCodeGenerator generator = (SYLectureDevelopInitCodeGenerator)target;
                var currentScene = SceneManager.GetActiveScene();
                string sceneName = currentScene.name;
                bool isNotDesignScene = sceneName.Contains("_Scene");
                string lectureCode = isNotDesignScene ? sceneName.Replace("_Scene", "") : sceneName;
                string lectureType = "";
                switch (lectureCode[0])
                {
                    case 'L':
                        lectureType = "Logic";
                        break;
                    case 'M':
                        lectureType = "Math";
                        break;

                    case 'C':
                        lectureType = "Coding";
                        break;
                    case 'E':
                        lectureType = "EXP";
                        break;
                }

                string lecturePath = $"{Application.dataPath}\\JSchool\\Contents\\{lectureType}\\";
                string scenePath = $"{lecturePath}{lectureCode}\\";
                string scriptPath = $"{scenePath}Script\\";
                if (!Directory.Exists(scriptPath))
                    Directory.CreateDirectory(scriptPath);
                
                string atlasPath = scriptPath.Replace("Script", "Atlas");
                if (!Directory.Exists(atlasPath))
                    Directory.CreateDirectory(atlasPath);

                using (StreamWriter sw = new StreamWriter(scriptPath + $"{lectureCode}_Manager.cs"))
                    sw.WriteLine("using System;using JSchool.SYLab;namespace JSchool.Contents." + lectureType + "." +
                                 lectureCode + ".Script{[Serializable]public class " + lectureCode +
                                 "_Sound : SYSoundList{}public class " + lectureCode +
                                 "_Manager : SYTouchAndDragManager{public " + lectureCode +
                                 "_Sound sounds;public override SYSoundList Sounds() => sounds;protected override string LectureCode() => \"" +
                                 lectureCode + "\";public " + lectureCode +
                                 "_Stage[] stages;public override SYTouchAndDragStage[] Stages() => stages;}}");
                using (StreamWriter sw = new StreamWriter(scriptPath + $"{lectureCode}_Stage.cs"))
                    sw.WriteLine(
                        "using System.Collections;using JSchool.SYLab;using UnityEngine;namespace JSchool.Contents." +
                        lectureType + "." + lectureCode + ".Script{public class " + lectureCode +
                        "_Stage : SYTouchAndDragStage{[HideInInspector]public " + lectureCode +
                        "_Manager manager;protected override SYTouchAndDragManager Manager() => manager;private void Awake(){manager = FindObjectOfType<" +
                        lectureCode +
                        "_Manager>();}protected override IEnumerator StartInit(){manager.StartTimeOut();yield break;}}}");

                
                var cam = Camera.main;
                cam.farClipPlane = 1000;
                DestroyImmediate(cam.GetComponent<AudioListener>());
                
                string sceneFilePath = $"{scenePath}{lectureCode}_Scene.unity";
                if(!File.Exists(sceneFilePath))
                    EditorSceneManager.SaveScene(currentScene, sceneFilePath, true);
                EditorSceneManager.OpenScene(sceneFilePath);
            }
        }
    }

    public class SYLectureDevelopInitCodeGenerator : MonoBehaviour
    {
    }
}
#endif