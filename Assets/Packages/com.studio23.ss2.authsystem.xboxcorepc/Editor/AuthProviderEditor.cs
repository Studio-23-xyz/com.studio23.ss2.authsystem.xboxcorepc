using Studio23.SS2.AuthSystem.XboxCorePC.Data;
using System.IO;
using UnityEditor;
using UnityEngine;


namespace Studio23.SS2.AuthSystem.XboxCorePC.Editor
{
    public class AuthProviderEditor : UnityEditor.Editor
    {
        [MenuItem("Studio-23/AuthSystem/Providers/XBoxCorePC", false, 10)]
        static void CreateDefaultProvider()
        {
            XboxPcAuthProvider providerSettings = ScriptableObject.CreateInstance<XboxPcAuthProvider>();

            // Create the resource folder path
            string resourceFolderPath = "Assets/Resources/AuthSystem/Providers";

            if (!Directory.Exists(resourceFolderPath))
            {
                Directory.CreateDirectory(resourceFolderPath);
            }

            // Create the ScriptableObject asset in the resource folder
            string assetPath = resourceFolderPath + "/AuthProvider.asset";
            AssetDatabase.CreateAsset(providerSettings, assetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("Default Cloud Provider created at: " + assetPath);
        }
    }
}