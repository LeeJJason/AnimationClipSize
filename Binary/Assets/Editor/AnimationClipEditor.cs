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

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.Profiling;

public static class AnimationClipEditor
{
    [MenuItem("Assets/Animation/ShowClipSize")]
    private static void ShowClipSize()
    {
        foreach (var guid in Selection.assetGUIDs)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (Directory.Exists(path))
            {
                string[] files = Directory.GetFiles(path, "*.anim", SearchOption.AllDirectories);
                foreach (var file in files)
                {
                    ShowClipSize(file);
                }
            }
            else if (File.Exists(path))
            {
                ShowClipSize(path);
            }
        }
    }


    public static void ShowClipSize(string clipPath)
    {
        if (clipPath.EndsWith(".anim"))
        {

            AnimationClip aniClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
            

            Assembly asm = Assembly.GetAssembly(typeof(Editor));
            MethodInfo getAnimationClipStats = typeof(AnimationUtility).GetMethod("GetAnimationClipStats", BindingFlags.Static | BindingFlags.NonPublic);
            Type aniclipstats = asm.GetType("UnityEditor.AnimationClipStats");
            FieldInfo sizeInfo = aniclipstats.GetField("size", BindingFlags.Public | BindingFlags.Instance);

            var fileInfo = new System.IO.FileInfo(clipPath);
            var stats = getAnimationClipStats.Invoke(null, new object[] { aniClip });

            Debug.LogErrorFormat("{0} => FileSize : {1}, MemorySize : {2}, BlobSize : {3}", aniClip.name, EditorUtility.FormatBytes(fileInfo.Length), EditorUtility.FormatBytes(Profiler.GetRuntimeMemorySizeLong(aniClip)), EditorUtility.FormatBytes((int)sizeInfo.GetValue(stats)));
        }
    }
}
