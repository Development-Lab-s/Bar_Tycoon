using System;
using System.Diagnostics;
using System.Reflection;
using UnityEngine;
using Object = UnityEngine.Object;

namespace BBJ
{
    public static class UtilDebugger
    {
        private static readonly BindingFlags FieldFlags =
            BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.DeclaredOnly;

        private static readonly BindingFlags PropertyFlags =
            BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.DeclaredOnly;

        [Conditional("UNITY_EDITOR")]
        public static void AssertAllAssigned(object target)
        {
            Object unityObj  = target as Object;
            string name      = unityObj != null ? unityObj.name : target.GetType().Name;
            Type   stopType  = unityObj != null ? typeof(Object) : typeof(object);
            bool   unityMode = unityObj != null;

            Type type = target.GetType();
            while (type != null && type != stopType)
            {
                foreach (FieldInfo field in type.GetFields(FieldFlags))
                {
                    // UnityObject: [SerializeField] 한정 / 일반 C#: 모든 인스턴스 필드
                    if (unityMode && !field.IsDefined(typeof(SerializeField), inherit: false))
                        continue;

                    object value = field.GetValue(target);
                    bool   isNull = value == null || (value is Object uObj && uObj == null);
                    UnityEngine.Debug.Assert(!isNull, $"[{name}] {PrettyFieldName(field.Name)} 이 할당되지 않았습니다.", unityObj);
                }
                type = type.BaseType;
            }
        }

        [Conditional("UNITY_EDITOR")]
        public static void LogNullMembers(object target)
        {
            Object unityObj = target as Object;
            string name     = unityObj != null ? unityObj.name : target.GetType().Name;
            Type   stopType = unityObj != null ? typeof(Object) : typeof(object);

            Type type = target.GetType();
            while (type != null && type != stopType)
            {
                foreach (FieldInfo field in type.GetFields(FieldFlags))
                {
                    object value = field.GetValue(target);
                    bool   isNull = value == null || (value is Object uObj && uObj == null);
                    if (isNull)
                        UnityEngine.Debug.Log($"[{name}] Field '{PrettyFieldName(field.Name)}' is null.", unityObj);
                }

                foreach (PropertyInfo prop in type.GetProperties(PropertyFlags))
                {
                    if (!prop.CanRead || prop.GetIndexParameters().Length > 0) continue;
                    try
                    {
                        object value = prop.GetValue(target);
                        bool   isNull = value == null || (value is Object uObj && uObj == null);
                        if (isNull)
                            UnityEngine.Debug.Log($"[{name}] Property '{prop.Name}' is null.", unityObj);
                    }
                    catch { }
                }

                type = type.BaseType;
            }
        }

        const string fieldPrefix = "<";
        const string fieldSuffix = ">k__BackingField";
        static readonly Range fieldRange = fieldPrefix.Length..^fieldSuffix.Length;
        private static string PrettyFieldName(string fieldName)
        {
            if (fieldName.StartsWith(fieldPrefix) && fieldName.EndsWith(fieldSuffix))
                return fieldName[fieldRange];
            return fieldName;
        }
    }
}
