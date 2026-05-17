using System;
using System.Reflection;
using UnityEngine;

namespace Stratum.Editor
{
    internal static class FieldLayoutUtil
    {
        private const float FallbackMinWidth = 80f;

        internal static bool IsSerializedField(FieldInfo field) =>
            !field.IsStatic &&
            !field.IsDefined(typeof(NonSerializedAttribute), false) &&
            !field.IsDefined(typeof(HideInInspector), false) &&
            (field.IsPublic || field.IsDefined(typeof(SerializeField), false));

        internal static float GetMinWidth(FieldInfo field)
        {
            var type = field.FieldType;
            if (type == typeof(bool)) return 40f;
            if (type == typeof(int) || type == typeof(float) || type.IsEnum) return 120f;
            if (type == typeof(Color)) return 120f;
            if (type == typeof(string)) return 140f;
            if (type == typeof(LayerMask)) return 120f;
            if (type == typeof(AnimationCurve) || type == typeof(Gradient)) return 120f;
            if (type == typeof(Vector2) || type == typeof(Vector2Int)) return 140f;
            if (type == typeof(Vector3) || type == typeof(Vector3Int) || type == typeof(Quaternion)) return 210f;
            if (type == typeof(Vector4)) return 280f;
            if (typeof(UnityEngine.Object).IsAssignableFrom(type)) return 140f;
            return FallbackMinWidth;
        }
    }
}
