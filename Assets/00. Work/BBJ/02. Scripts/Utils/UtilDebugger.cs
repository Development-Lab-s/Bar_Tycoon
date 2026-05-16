using System;
using System.Diagnostics;
using System.Reflection;
using UnityEngine;
using Object = UnityEngine.Object;

namespace BBJ
{
    public static class UtilDebugger
    {
        // BindingFlage는 메모리 위치, 접근한정자, 상속 으로 이루어짐
        // 대략 0000 / 0000 / 0000이런식으로 비트 마스크를 정한다는 뜻
        private static readonly BindingFlags FieldFlags =
            BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.DeclaredOnly;

        private static readonly BindingFlags PropertyFlags =
            BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.DeclaredOnly;

        [Conditional("UNITY_EDITOR")]
        public static void AssertAllAssigned(Object target)
        {
            Type type = target.GetType();
            while (type != null && type != typeof(Object))
            {
                FieldInfo[] fields = type.GetFields(FieldFlags);
                foreach (FieldInfo field in fields)
                {
                    if (field.IsDefined(typeof(SerializeField), inherit: false))
                    {
                        object value = field.GetValue(target);

                        //  Unity는 UnityEngine.Object에 == 연산자를 오버라이드해서 Fake Null을 감지
                        //  Destroy()된 오브젝트는 C# 참조는 살아있지만
                        //  네이티브 쪽이 이미 해제된 상태인데, 이걸 Unity가 == null로 잡을 수 있음
                        bool isNull = value == null || (value is Object unityObj && unityObj == null);
                        UnityEngine.Debug.Assert(!isNull, $"[{target.name}] {PrettyFieldName(field.Name)} 이 할당되지 않았습니다.", target);
                    }
                }
                type = type.BaseType;
            }
        }

        [Conditional("UNITY_EDITOR")]
        public static void LogNullMembers(Object target)
        {
            Type type = target.GetType();
            while (type != null && type != typeof(Object))
            {
                foreach (FieldInfo field in type.GetFields(FieldFlags))
                {
                    object value = field.GetValue(target);
                    bool isNull = value == null || (value is Object unityObj && unityObj == null);
                    if (isNull)
                        UnityEngine.Debug.Log($"[{target.name}] Field '{PrettyFieldName(field.Name)}' is null.", target);
                }

                foreach (PropertyInfo prop in type.GetProperties(PropertyFlags))
                {
                    if (!prop.CanRead || prop.GetIndexParameters().Length > 0) continue;
                    try
                    {
                        object value = prop.GetValue(target);
                        bool isNull = value == null || (value is Object unityObj && unityObj == null);
                        if (isNull)
                            UnityEngine.Debug.Log($"[{target.name}] Property '{prop.Name}' is null.", target);
                    }
                    catch { }
                }

                type = type.BaseType;
            }
        }

        const string fieldPrefix = "<";
        const string fieldSuffix = ">k__BackingField";
        readonly static Range fieldRange = fieldPrefix.Length..^fieldSuffix.Length;
        private static string PrettyFieldName(string fieldName)
        {
            if (fieldName.StartsWith(fieldPrefix) && fieldName.EndsWith(fieldSuffix))
                return fieldName[fieldRange];
            return fieldName;
        }
    }
}
