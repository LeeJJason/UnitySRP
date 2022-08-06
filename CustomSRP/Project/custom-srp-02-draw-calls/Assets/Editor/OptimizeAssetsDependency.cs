using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

public class OptimizeAssetsDependency
{
    [MenuItem("Assets/OptimizeAssetsDependency/CheckPrefabDependency")]
    public static void CheckPrefabDependency()
    {
        string[] guids = AssetDatabase.FindAssets("t:prefab");
        for (int i = 0; i < guids.Length; ++i)
        {
            string guid = guids[i];
            string file = AssetDatabase.GUIDToAssetPath(guid);
            int count = i + 1;
            EditorUtility.DisplayProgressBar($"CheckPrefabDependency({count}/{guids.Length})", file, (float)count / guids.Length);
            GameObject go = AssetDatabase.LoadAssetAtPath<GameObject>(file);
            if (go)
            {
                EditorUtility.SetDirty(go);
            }
        }
        EditorUtility.ClearProgressBar();
        Debug.Log("<color=green> CheckPrefabDependency Success </color>");
        AssetDatabase.SaveAssets();
    }

    [MenuItem("Assets/OptimizeAssetsDependency/CheckParticleDependency")]
    public static void CheckParticleDependency()
    {
        string[] guids = AssetDatabase.FindAssets("t:prefab");
        for (int i = 0; i < guids.Length; ++i)
        {
            string guid = guids[i];
            string file = AssetDatabase.GUIDToAssetPath(guid);
            int count = i + 1;
            EditorUtility.DisplayProgressBar($"CheckParticleDependency({count}/{guids.Length})", file, (float)count / guids.Length);
            GameObject go = AssetDatabase.LoadAssetAtPath<GameObject>(file);
            if (go)
            {
                ParticleSystemRenderer[] renders = go.GetComponentsInChildren<ParticleSystemRenderer>(true);
                foreach (var renderItem in renders)
                {
                    if (renderItem.renderMode != ParticleSystemRenderMode.Mesh)
                    {
                        renderItem.mesh = null;
                        EditorUtility.SetDirty(go);
                    }
                }
            }
        }
        EditorUtility.ClearProgressBar();

        Debug.Log("<color=green> CheckParticleDependency Success </color>");
        AssetDatabase.SaveAssets();
    }


    [MenuItem("Assets/OptimizeAssetsDependency/CheckMaterialProperty")]
    private static void CheckMaterialProperty()
    {
        HashSet<string> keywords = new HashSet<string>();
        string[] guids = AssetDatabase.FindAssets("t:material");
        for (int i = 0; i < guids.Length; ++i)
        {
            string guid = guids[i];
            string file = AssetDatabase.GUIDToAssetPath(guid);
            int count = i + 1;
            EditorUtility.DisplayProgressBar($"CheckMaterialProperty({count}/{guids.Length})", file, (float)count / guids.Length);
            Material material = AssetDatabase.LoadAssetAtPath<Material>(file);
            if (material != null)
            {
                bool flag = false;
                if (material.shader != null)
                {
                    if (GetShaderKeywords(material.shader, out var global, out var local))
                    {
                        keywords.Clear();
                        foreach (var g in global)
                        {
                            keywords.Add(g);
                        }
                        foreach (var l in local)
                        {
                            keywords.Add(l);
                        }
                        //重置keywords
                        List<string> resetKeywords = new List<string>(material.shaderKeywords);
                        foreach (var item in material.shaderKeywords)
                        {
                            if (!keywords.Contains(item))
                            {
                                flag = true;
                                resetKeywords.Remove(item);
                            }
                        }
                        material.shaderKeywords = resetKeywords.ToArray();
                    }
                }

                
                SerializedObject serializedObject = new SerializedObject(material);
                SerializedProperty disabledShaderPasses = serializedObject.FindProperty("disabledShaderPasses");
                for (int j = disabledShaderPasses.arraySize - 1; j >= 0; j--)
                {
                    if (!material.HasProperty(disabledShaderPasses.GetArrayElementAtIndex(j).displayName))
                    {
                        disabledShaderPasses.DeleteArrayElementAtIndex(i);
                    }
                }

                SerializedProperty serializedProperty = serializedObject.FindProperty("m_SavedProperties");
                serializedProperty.Next(true);
                do
                {
                    if (serializedProperty.isArray)
                    {
                        for (int j = serializedProperty.arraySize - 1; j >= 0; --j)
                        {
                            SerializedProperty property = serializedProperty.GetArrayElementAtIndex(j);
                            if (!material.HasProperty(property.displayName))
                            {
                                flag = true;
                                serializedProperty.DeleteArrayElementAtIndex(j);
                            }
                        }
                    }
                } while (serializedProperty.Next(false));

                if (flag)
                {
                    serializedObject.ApplyModifiedProperties();
                    Debug.LogError($"Check Material : {AssetDatabase.GetAssetPath(material)}");
                }
            }
        }
        EditorUtility.ClearProgressBar();

        Debug.Log("<color=green> CheckMaterialProperty Success </color>");
        AssetDatabase.SaveAssets();
    }
    //获取shader中所有的宏
    public static bool GetShaderKeywords(Shader target, out string[] global, out string[] local)
    {
        try
        {
            MethodInfo globalKeywords = typeof(ShaderUtil).GetMethod("GetShaderGlobalKeywords", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            global = (string[])globalKeywords.Invoke(null, new object[] { target });
            MethodInfo localKeywords = typeof(ShaderUtil).GetMethod("GetShaderLocalKeywords", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            local = (string[])localKeywords.Invoke(null, new object[] { target });
            return true;
        }
        catch
        {
            global = local = null;
            return false;
        }
    }
}
