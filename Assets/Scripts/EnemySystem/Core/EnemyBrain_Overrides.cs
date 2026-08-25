using System;
using System.Collections.Generic;
using System.Reflection;
using Sirenix.OdinInspector;
using UnityEngine;

namespace EnemySystem
{
    public enum OverrideType { None, Float, Int, Bool, String, LayerMask, Enum }

    [Serializable]
    [InlineProperty]
    [HideReferenceObjectPicker]
    public class StatOverride
    {
        [HideInInspector] 
        public OverrideType currentType = OverrideType.None;
        
        [HideInInspector] 
        public string enumAssemblyQualifiedName;
        
        [HideInInspector]
        public string lastLoadedDefaultPath = "";

        [HorizontalGroup("Row")] 
        [HideLabel]
        [ValueDropdown("GetAllStatPaths")]
        [OnValueChanged("@$root.UpdateOverrideType($value)")]
        public string variablePath;

        [HorizontalGroup("Row")] [HideLabel] [ShowIf("currentType", OverrideType.Float)]
        public float floatValue;

        [HorizontalGroup("Row")] [HideLabel] [ShowIf("currentType", OverrideType.Int)]
        public int intValue;

        [HorizontalGroup("Row")] [HideLabel] [ShowIf("currentType", OverrideType.Bool)]
        public bool boolValue;

        [HorizontalGroup("Row")] [HideLabel] [ShowIf("currentType", OverrideType.String)]
        public string stringValue;

        [HorizontalGroup("Row")] [HideLabel] [ShowIf("currentType", OverrideType.LayerMask)]
        public LayerMask layerMaskValue;
        
        [HorizontalGroup("Row")] [HideLabel] [ShowIf("currentType", OverrideType.Enum)]
        [ValueDropdown("GetEnumDropdownChoices")]
        public string enumStringValue;

#if UNITY_EDITOR
        static IEnumerable<string> GetAllStatPaths()
        {
            List<string> paths = new List<string>();

            void BuildPaths(Type type, string currentPath)
            {
                FieldInfo[] fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance);

                foreach (FieldInfo field in fields)
                {
                    if (field.FieldType == typeof(float) || field.FieldType == typeof(int) || 
                        field.FieldType == typeof(bool) || field.FieldType == typeof(string) || 
                        field.FieldType == typeof(LayerMask) || field.FieldType.IsEnum)
                    {
                        paths.Add(string.IsNullOrEmpty(currentPath) ? field.Name : $"{currentPath}.{field.Name}");
                    }
                    else if (field.FieldType.IsClass || (field.FieldType.IsValueType && !field.FieldType.IsPrimitive && !field.FieldType.IsEnum))
                    {
                        if (field.FieldType.Namespace != null && field.FieldType.Namespace.StartsWith("UnityEngine")) continue;
                        BuildPaths(field.FieldType, string.IsNullOrEmpty(currentPath) ? field.Name : $"{currentPath}.{field.Name}");
                    }
                }
            }

            BuildPaths(typeof(EnemyConfig), ""); 
            return paths;
        }

        IEnumerable<string> GetEnumDropdownChoices()
        {
            if (string.IsNullOrEmpty(enumAssemblyQualifiedName)) return new List<string>();

            Type enumType = Type.GetType(enumAssemblyQualifiedName);
            if (enumType != null && enumType.IsEnum)
            {
                return Enum.GetNames(enumType); 
            }
            return new List<string>();
        }
#endif
    }
    
    public partial class EnemyBrain
    {
        [SerializeField, HideInInspector]
        string gitReadableOverrides;
        
        void OnValidate()
        {
            if (statOverrides == null || statOverrides.Count == 0)
            {
                gitReadableOverrides = "No Overrides";
                return;
            }
            
            string summary = "";
            foreach (var stat in statOverrides)
            {
                if (string.IsNullOrEmpty(stat.variablePath)) continue;

                string val = stat.currentType switch
                {
                    OverrideType.Float => stat.floatValue.ToString(),
                    OverrideType.Int => stat.intValue.ToString(),
                    OverrideType.Bool => stat.boolValue.ToString(),
                    OverrideType.String => stat.stringValue,
                    OverrideType.Enum => stat.enumStringValue,
                    _ => "..."
                };

                summary += $"[{stat.variablePath}: {val}] ";
            }

            gitReadableOverrides = summary;
        }
        
        void ApplyOverrides()
        {
            foreach (var statOverride in statOverrides)
            {
                string fullPath = statOverride.variablePath;
                if (string.IsNullOrEmpty(fullPath)) continue;

                string[] pathParts = fullPath.Split('.');
                object currentObject = Config;
                FieldInfo currentField = null;

                for (int i = 0; i < pathParts.Length; i++)
                {
                    string currentPart = pathParts[i];
                    Type currentType = currentObject.GetType();

                    currentField = currentType.GetField(currentPart, BindingFlags.Public | BindingFlags.Instance);
                    if (currentField == null) break; 

                    if (i < pathParts.Length - 1)
                    {
                        currentObject = currentField.GetValue(currentObject);
                    }
                    else
                    {
                        Type type = currentField.FieldType;
                        if (type == typeof(float)) currentField.SetValue(currentObject, statOverride.floatValue);
                        else if (type == typeof(int)) currentField.SetValue(currentObject, statOverride.intValue);
                        else if (type == typeof(bool)) currentField.SetValue(currentObject, statOverride.boolValue);
                        else if (type == typeof(string)) currentField.SetValue(currentObject, statOverride.stringValue);
                        else if (type == typeof(LayerMask)) currentField.SetValue(currentObject, statOverride.layerMaskValue);
                        else if (type.IsEnum) 
                            currentField.SetValue(currentObject, Enum.Parse(type, statOverride.enumStringValue));
                    }
                }
            }
        }
        
        public void UpdateOverrideType(string fullPath)
        {
            if (baseConfig == null || string.IsNullOrEmpty(fullPath)) return;

            StatOverride target = statOverrides.Find(s => s.variablePath == fullPath && s.lastLoadedDefaultPath != fullPath);
            if (target == null) return;

#if UNITY_EDITOR
            UnityEditor.Undo.RecordObject(this, "Set Stat Override Default");
#endif
            
            target.lastLoadedDefaultPath = fullPath;
            
            string[] pathParts = fullPath.Split('.');
            object currentObject = baseConfig; 
            FieldInfo currentField = null;

            for (int i = 0; i < pathParts.Length; i++)
            {
                currentField = currentObject.GetType().GetField(pathParts[i], BindingFlags.Public | BindingFlags.Instance);
                if (currentField == null) return;

                if (i < pathParts.Length - 1)
                {
                    currentObject = currentField.GetValue(currentObject);
                    if (currentObject == null) return;
                }
                else
                {
                    object val = currentField.GetValue(currentObject);
                    Type type = currentField.FieldType;

                    if (type == typeof(float)) { target.currentType = OverrideType.Float; target.floatValue = (float)val; }
                    else if (type == typeof(int)) { target.currentType = OverrideType.Int; target.intValue = (int)val; }
                    else if (type == typeof(bool)) { target.currentType = OverrideType.Bool; target.boolValue = (bool)val; }
                    else if (type == typeof(string)) { target.currentType = OverrideType.String; target.stringValue = (string)val; }
                    else if (type == typeof(LayerMask)) { target.currentType = OverrideType.LayerMask; target.layerMaskValue = (LayerMask)val; }
                    else if (type.IsEnum)
                    {
                        target.currentType = OverrideType.Enum;
                        target.enumStringValue = val.ToString();
                        
                        target.enumAssemblyQualifiedName = $"{type.FullName}, {type.Assembly.GetName().Name}";
                    }
                }
            }

#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
#endif
        }
    }
}