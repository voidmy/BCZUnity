using System;
using System.Reflection;
using QFramework;
using UnityEditor;
using UnityEngine;


/// <summary>
/// 通用的 MonoBehaviour 自定义 Inspector：
/// - 先绘制默认 Inspector
/// - 再为所有标记了 [MethodButton] 的无参实例方法绘制按钮
/// </summary>
[CanEditMultipleObjects]
[CustomEditor(typeof(MonoBehaviour), true)]
public class MethodButtonEditor : Editor
{
    public override void OnInspectorGUI()
    {
        // 先画默认 Inspector
        DrawDefaultInspector();

        // 只对 MonoBehaviour 目标处理
        var targetBehaviour = target as MonoBehaviour;
        if (targetBehaviour == null)
        {
            return;
        }

        var type = targetBehaviour.GetType();
        var methods = type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        bool anyButtonDrawn = false;

        foreach (var method in methods)
        {
            var attrs = method.GetCustomAttributes(typeof(MethodButtonAttribute), true);
            if (attrs == null || attrs.Length == 0)
            {
                continue;
            }

            var buttonAttr = (MethodButtonAttribute)attrs[0];

            // 只支持无参数实例方法
            if (method.GetParameters().Length != 0)
            {
                continue;
            }

            // 确定按钮文本
            string buttonLabel = string.IsNullOrEmpty(buttonAttr.DisplayName)
                ? method.Name
                : buttonAttr.DisplayName;

            // 根据模式控制是否可点击
            bool isPlayMode = Application.isPlaying;
            bool enabledByMode = true;

            if (buttonAttr.OnlyInPlayMode && !isPlayMode)
            {
                enabledByMode = false;
            }

            if (buttonAttr.OnlyInEditMode && isPlayMode)
            {
                enabledByMode = false;
            }

            using (new EditorGUI.DisabledScope(!enabledByMode))
            {
                if (GUILayout.Button(buttonLabel))
                {
                    InvokeOnTargets(method);
                }
            }

            anyButtonDrawn = true;
        }

        if (anyButtonDrawn)
        {
            EditorGUILayout.Space();
        }
    }

    private void InvokeOnTargets(MethodInfo method)
    {
        // 对所有选中的对象执行
        foreach (var t in targets)
        {
            var mb = t as MonoBehaviour;
            if (mb == null)
            {
                continue;
            }

            try
            {
                method.Invoke(mb, null);
            }
            catch (Exception e)
            {
                Debug.LogError($"[MethodButton] 调用 {method.DeclaringType?.Name}.{method.Name} 时发生异常: {e}", mb);
            }
        }
    }
}