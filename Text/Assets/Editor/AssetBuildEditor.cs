/******************************************************************************
 * 【本类功能概述】                                 					      *
 *  版权所有（C）2015-20XX，大拇哥                                            *
 *  保留所有权利。                                                            *
 ******************************************************************************
 *  作者 : <LIJIJIAN>
 *  版本 : 
 *  创建时间: 
 *  文件描述: 
 *****************************************************************************/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;

public static class AssetBuildEditor
{
    [MenuItem("Editor/Bundle/SetBundle")]
    private static void SetBundle()
    {
        string[] files = Directory.GetFiles("Assets/Asset", "*.*", SearchOption.AllDirectories);
        for (int i = 0; i < files.Length; ++i)
        {
            string file = files[i];
            EditorUtility.DisplayProgressBar("SetBundle", file, (float)i / files.Length);
            if (file.EndsWith(".cs") || file.EndsWith(".meta"))
                continue;

            string bundle = file.Substring(0, file.LastIndexOf('.')).Replace("\\", "/").Replace("Assets/Asset/", "") + ".bundle";

            AssetImporter importer = AssetImporter.GetAtPath(file);
            if (importer.assetBundleName != bundle)
            {
                importer.SetAssetBundleNameAndVariant(bundle, "");
                importer.SaveAndReimport();
            }
        }
        EditorUtility.ClearProgressBar();
    }
    [MenuItem("Editor/Bundle/BuildBundle")]
    private static void BuildBundle()
    {
        string sourcePath = "Bundle";
        if (Directory.Exists(sourcePath))
        {
            Directory.Delete(sourcePath, true);
        }
        Directory.CreateDirectory(sourcePath);

        BuildPipeline.BuildAssetBundles(sourcePath, BuildAssetBundleOptions.DeterministicAssetBundle | BuildAssetBundleOptions.ChunkBasedCompression, EditorUserBuildSettings.activeBuildTarget);
    }
}
