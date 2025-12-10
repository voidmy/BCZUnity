using System;

namespace QFramework
{
    /// <summary>
    /// 标记在 MonoBehaviour 的无参实例方法上，使其在 Inspector 中显示为按钮。
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, Inherited = true, AllowMultiple = false)]
    public class MethodButtonAttribute : Attribute
    {
        /// <summary>
        /// 在 Inspector 上显示的按钮名称，不填则使用方法名。
        /// </summary>
        public string DisplayName { get; }

        /// <summary>
        /// 是否仅在运行模式下可点击。
        /// </summary>
        public bool OnlyInPlayMode { get; }

        /// <summary>
        /// 是否仅在编辑模式（非运行）下可点击。
        /// </summary>
        public bool OnlyInEditMode { get; }

        public MethodButtonAttribute(string displayName = null, bool onlyInPlayMode = false, bool onlyInEditMode = false)
        {
            DisplayName = displayName;
            OnlyInPlayMode = onlyInPlayMode;
            OnlyInEditMode = onlyInEditMode;
        }
    }
}