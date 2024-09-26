using Studio23.SS2.AuthSystem.XboxCorePC.Core;
using UnityEditor;
using UnityEngine;

namespace Studio23.SS2.AuthSystem.XboxCorePC.Editor
{
    public class DependencyInstaller : EditorWindow
    {
        
        private string scid = "";

        [MenuItem("Studio-23/AuthSystem/DependencyInstaller/XBoxCorePC Runtime", false, 10)]
        public static void ShowWindow()
        {
            GetWindow<DependencyInstaller>("Dependency Installer");
        }

        private void OnGUI()
        {
            GUILayout.Label("Service Component ID", EditorStyles.boldLabel);
            scid = EditorGUILayout.TextField("SCID", scid);

            if (GUILayout.Button("Install Runtime"))
            {
                InstallRuntime();
            }
        }

        private void InstallRuntime()
        {
            GameObject prefab = new GameObject("GamingRuntimeManager");

            prefab.AddComponent<GamingRuntimeManager>().SCID=scid;
            prefab.AddComponent<DontDestroyUtility>();
        }

    }

}
