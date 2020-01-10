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

using System.Collections.Generic;
using UnityEngine;

//****************************************************************************
//
//  File:      OptimizeAnimationClipTool.cs
//
//  Copyright (c) SuiJiaBin
//
// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF
// ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO
// THE IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A
// PARTICULAR PURPOSE.
//
//****************************************************************************
using System;
using System.Reflection;
using UnityEditor;
using System.IO;
using UnityEngine.Profiling;

namespace EditorTool
{
    class AnimationOpt
    {
        static Dictionary<uint, string> FLOAT_FORMAT;
        static MethodInfo getAnimationClipStats;
        static FieldInfo sizeInfo;
        static object[] param = new object[1];

        static AnimationOpt()
        {
            FLOAT_FORMAT = new Dictionary<uint, string>();
            for (uint i = 1; i < 6; i++)
            {
                FLOAT_FORMAT.Add(i, "f" + i.ToString());
            }
            Assembly asm = Assembly.GetAssembly(typeof(Editor));
            getAnimationClipStats = typeof(AnimationUtility).GetMethod("GetAnimationClipStats", BindingFlags.Static | BindingFlags.NonPublic);
            Type aniclipstats = asm.GetType("UnityEditor.AnimationClipStats");
            sizeInfo = aniclipstats.GetField("size", BindingFlags.Public | BindingFlags.Instance);
        }

        AnimationClip clip;
        public string Path { get; private set; }

        public long OriginFileSize { get; private set; }

        public int OriginMemorySize { get; private set; }

        public int OriginInspectorSize { get; private set; }

        public long OptFileSize { get; private set; }

        public int OptMemorySize { get; private set; }

        public int OptInspectorSize { get; private set; }

        public AnimationOpt(string path, AnimationClip clip)
        {
            Path = path;
            this.clip = clip;
            GetOriginSize();
        }

        void GetOriginSize()
        {
            OriginFileSize = GetFileZie();
            OriginMemorySize = GetMemSize();
            OriginInspectorSize = GetInspectorSize();
        }

        void GetOptSize()
        {
            OptFileSize = GetFileZie();
            OptMemorySize = GetMemSize();
            OptInspectorSize = GetInspectorSize();
        }

        long GetFileZie()
        {
            FileInfo fi = new FileInfo(Path);
            return fi.Length;
        }

        int GetMemSize()
        {
            return (int)Profiler.GetRuntimeMemorySizeLong(this.clip);
        }

        int GetInspectorSize()
        {
            param[0] = clip;
            var stats = getAnimationClipStats.Invoke(null, param);
            return (int)sizeInfo.GetValue(stats);
        }

        public void OptmizeAnimationScaleCurve()
        {
            if (clip != null)
            {
                //去除scale曲线
                foreach (EditorCurveBinding theCurveBinding in AnimationUtility.GetCurveBindings(clip))
                {
                    string name = theCurveBinding.propertyName.ToLower();
                    if (name.Contains("scale"))
                    {
                        AnimationUtility.SetEditorCurve(clip, theCurveBinding, null);
                    }
                }
            }
        }

        void OptmizeAnimationFloat_X(uint x, bool scalOpt)
        {
            if (clip != null && x > 0)
            {
                //浮点数精度压缩到f3
                EditorCurveBinding[] bindings = AnimationUtility.GetCurveBindings(this.clip);
                Keyframe key;
                Keyframe[] keyFrames;
                string floatFormat;
                if (FLOAT_FORMAT.TryGetValue(x, out floatFormat))
                {
                    if (bindings != null && bindings.Length > 0)
                    {
                        for (int ii = 0; ii < bindings.Length; ++ii)
                        {
                            EditorCurveBinding binding = bindings[ii];

                            if (scalOpt && binding.propertyName.ToLower().Contains("scale"))
                            {
                                AnimationUtility.SetEditorCurve(clip, binding, null);
                            }
                            else
                            {
                                AnimationCurve curveData = AnimationUtility.GetEditorCurve(this.clip, binding);
                                if (curveData == null || curveData.keys == null)
                                {
                                    //Debug.LogWarning(string.Format("AnimationClipCurveData {0} don't have curve; Animation name {1} ", curveDate, animationPath));
                                    continue;
                                }
                                keyFrames = curveData.keys;
                                for (int i = 0; i < keyFrames.Length; i++)
                                {
                                    key = keyFrames[i];
                                    key.value = float.Parse(key.value.ToString(floatFormat));
                                    key.inTangent = float.Parse(key.inTangent.ToString(floatFormat));
                                    key.outTangent = float.Parse(key.outTangent.ToString(floatFormat));
                                    keyFrames[i] = key;
                                }
                                curveData.keys = keyFrames;
                                AnimationUtility.SetEditorCurve(this.clip, binding, curveData);
                            }
                        }
                    }
                }
                else
                {
                    Debug.LogErrorFormat("目前不支持{0}位浮点", x);
                }
            }
        }

        public void Optimize(bool scaleOpt, uint floatSize)
        {
            OptmizeAnimationFloat_X(floatSize, scaleOpt);
            GetOptSize();
        }

        public void Optimize_Scale_Float3(bool flag)
        {
            Optimize(flag, 3);
        }

        public void LogOrigin()
        {
            LogSize(OriginFileSize, OriginMemorySize, OriginInspectorSize);
        }

        public void LogOpt()
        {
            LogSize(OptFileSize, OptMemorySize, OptInspectorSize);
        }

        public void LogDelta()
        {

        }

        void LogSize(long fileSize, int memSize, int inspectorSize)
        {
            Debug.LogFormat("{0} \nSize=[ {1} ]", Path, string.Format("FSize={0} ; Mem->{1} ; inspector->{2}",
                EditorUtility.FormatBytes(fileSize), EditorUtility.FormatBytes(memSize), EditorUtility.FormatBytes(inspectorSize)));
        }
    }

    public enum OptimizeType
    {
        NONE,
        SACLE,
        FLOAT,
    }

    public class OptimizeAnimationClipTool
    {
        

        static List<AnimationOpt> AnimOptList = new List<AnimationOpt>();
        static List<string> Errors = new List<string>();

        [MenuItem("Assets/Animation/裁剪浮点数")]
        public static void OptimizeFloat()
        {
            Optimize(OptimizeType.FLOAT);
        }

        [MenuItem("Assets/Animation/去除Scale")]
        public static void OptimizeScale()
        {
            Optimize(OptimizeType.SACLE);
        }

        [MenuItem("Assets/Animation/裁剪浮点数去除Scale")]
        public static void Optimize()
        {
            Optimize(OptimizeType.NONE);
        }


        public static void Optimize(OptimizeType optimizeType)
        {
            AnimOptList = FindAnims();
            for (int i = 0; i < AnimOptList.Count; ++i)
            {
                AnimationOpt anim = AnimOptList[i];
                if (EditorUtility.DisplayCancelableProgressBar("优化AnimationClip", anim.Path, (float)i / (float)AnimOptList.Count))
                {
                    break;
                }
                switch (optimizeType)
                {
                    case OptimizeType.SACLE:
                        anim.OptmizeAnimationScaleCurve();
                        break;
                    case OptimizeType.FLOAT:
                        anim.Optimize_Scale_Float3(false);
                        break;
                    default:
                        anim.Optimize_Scale_Float3(true);
                        break;
                }
                
            }
            EditorUtility.ClearProgressBar();
            AssetDatabase.SaveAssets();
            AnimOptList.Clear();
            cachedOpts.Clear();
        }

        static Dictionary<string, AnimationOpt> cachedOpts = new Dictionary<string, AnimationOpt>();

        static AnimationOpt GetNewAOpt(string path)
        {
            AnimationOpt opt = null;
            if (!cachedOpts.TryGetValue(path, out opt))
            {
                AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
                if (clip != null)
                {
                    opt = new AnimationOpt(path, clip);
                    cachedOpts[path] = opt;
                }
            }
            return opt;
        }

        static List<AnimationOpt> assets = new List<AnimationOpt>();
        static List<AnimationOpt> FindAnims()
        {
            assets.Clear();
            foreach (var guid in Selection.assetGUIDs)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (Directory.Exists(path))
                {
                    string[] files = Directory.GetFiles(path, "*.anim", SearchOption.AllDirectories);
                    foreach (var file in files)
                    {
                        AnimationOpt animopt = GetNewAOpt(file);
                        if (animopt != null)
                            assets.Add(animopt);
                    }
                }
                else if (File.Exists(path))
                {
                    AnimationOpt animopt = GetNewAOpt(path);
                    if (animopt != null)
                        assets.Add(animopt);
                }
            }
            return assets;
        }
    }
}