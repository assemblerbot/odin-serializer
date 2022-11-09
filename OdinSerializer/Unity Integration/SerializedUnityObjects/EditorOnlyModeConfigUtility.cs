﻿//-----------------------------------------------------------------------
// <copyright file="EditorOnlyModeConfigUtility.cs" company="Sirenix IVS">
// Copyright (c) 2018 Sirenix IVS
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>
//-----------------------------------------------------------------------

#if UNITY_EDITOR

namespace Sirenix.OdinInspector
{
    using Sirenix.Serialization;
    using Sirenix.Utilities;
    using System;
    using System.Reflection;
    using UnityEngine;

    public static class EditorOnlyModeConfigUtility
    {
        private static bool initialized;

        private static object instance;
        private static WeakValueGetter<bool> isInEditorOnlyModeGetter;

        public const string SERIALIZATION_DISABLED_ERROR_TEXT =
            "ERROR: EDITOR ONLY MODE ENABLED\n\n" +
            "Odin is currently in editor only mode, meaning the serialization system is disabled in builds. " +
            "This class is specially serialized by Odin - if you try to compile with this class in your project, you *will* get compiler errors. " +
            "Either disable editor only mode in Tools -> Odin Inspector -> Preferences -> Editor Only Mode, or make sure that this type does not " +
            "inherit from a type that is serialized by Odin.";

        public static bool IsSerializationEnabled
        {
            get
            {
                if (!initialized)
                {
                    initialized = true;

                    Type editorOnlyModeConfigType = null;

                    try
                    {
                        Assembly editorAssembly = null;

                        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                        {
                            if (assembly.GetName().Name == "Sirenix.OdinInspector.Editor")
                            {
                                editorAssembly = assembly;
                                break;
                            }
                        }

                        if (editorAssembly != null)
                        {
                            editorOnlyModeConfigType = editorAssembly.GetType("Sirenix.OdinInspector.Editor.EditorOnlyModeConfig");
                        }
                    }
                    catch
                    {
                    }
                    
                    var instanceProperty = editorOnlyModeConfigType.GetProperty("Instance", Flags.StaticAnyVisibility | BindingFlags.FlattenHierarchy);
                    var isInEditorOnlyModeField = editorOnlyModeConfigType.GetField("isInEditorOnlyMode", Flags.InstanceAnyVisibility);

                    if (isInEditorOnlyModeField != null)
                    {
                        instance = instanceProperty.GetValue(null, null);
                        isInEditorOnlyModeGetter = EmitUtilities.CreateWeakInstanceFieldGetter<bool>(editorOnlyModeConfigType, isInEditorOnlyModeField);
                    }
                    else
                    {
                        // If the field doesn't exist, we are fully in editor only mode
                        instance = instanceProperty.GetValue(null, null);
                        isInEditorOnlyModeGetter = (ref object inst) => true;
                    }
                }

                return !isInEditorOnlyModeGetter(ref instance);
            }
        }

        public static void InternalOnInspectorGUI(UnityEngine.Object obj)
        {
            if (!EditorOnlyModeConfigUtility.IsSerializationEnabled)
            {
                GUILayout.Space(10);

                var color = GUI.color;
                GUI.color = new Color(0.8f, 0.1f, 0.4f, 1f);
                UnityEditor.EditorGUILayout.HelpBox(
                    EditorOnlyModeConfigUtility.SERIALIZATION_DISABLED_ERROR_TEXT,
                    UnityEditor.MessageType.Error);
                GUI.color = color;
            }

            if (!GlobalSerializationConfig.Instance.HideSerializationCautionaryMessage && !obj.GetType().FullName.StartsWith("Sirenix.OdinInspector."))
            {
                GUILayout.Space(10);
                UnityEditor.EditorGUILayout.HelpBox(
                    GlobalSerializationConfig.ODIN_SERIALIZATION_CAUTIONARY_WARNING_TEXT + "\n\n\n\n",
                    UnityEditor.MessageType.Warning);

                var rect = GUILayoutUtility.GetLastRect();
                rect.xMin += 34;
                rect.yMax -= 10;
                rect.xMax -= 10;
                rect.yMin = rect.yMax - 25;

                if (GUI.Button(rect, GlobalSerializationConfig.ODIN_SERIALIZATION_CAUTIONARY_WARNING_BUTTON_TEXT))
                {
                    GlobalSerializationConfig.Instance.HideSerializationCautionaryMessage = true;
                    UnityEditor.EditorUtility.SetDirty(GlobalSerializationConfig.Instance);
                    UnityEditor.HandleUtility.Repaint();
                }
                GUILayout.Space(10);
            }

            if (!GlobalSerializationConfig.Instance.HidePrefabCautionaryMessage 
                && obj is Component 
                && OdinPrefabSerializationEditorUtility.HasNewPrefabWorkflow 
                && (OdinPrefabSerializationEditorUtility.ObjectIsPrefabInstance(obj) || UnityEditor.AssetDatabase.Contains(obj)) 
                && !obj.GetType().FullName.StartsWith("Sirenix.OdinInspector."))
            {
                GUILayout.Space(10);
                UnityEditor.EditorGUILayout.HelpBox(
                    GlobalSerializationConfig.ODIN_PREFAB_CAUTIONARY_WARNING_TEXT + "\n\n\n\n",
                    UnityEditor.MessageType.Error);

                var rect = GUILayoutUtility.GetLastRect();
                rect.xMin += 34;
                rect.yMax -= 10;
                rect.xMax -= 10;
                rect.yMin = rect.yMax - 25;

                if (GUI.Button(rect, GlobalSerializationConfig.ODIN_PREFAB_CAUTIONARY_WARNING_BUTTON_TEXT))
                {
                    GlobalSerializationConfig.Instance.HidePrefabCautionaryMessage = true;
                    UnityEditor.EditorUtility.SetDirty(GlobalSerializationConfig.Instance);
                    UnityEditor.HandleUtility.Repaint();
                }
                GUILayout.Space(10);

            }
        }
    }
}

#endif