namespace Stratum.Editor
{
    internal static class StratumPaths
    {
        public const string PackageId = "com.l47coder.stratum";
        public const string PackageRoot = "Packages/" + PackageId;
        public const string TemplatesRoot = PackageRoot + "/Templates~";
        public const string GameSkeletonTemplateFolder = TemplatesRoot + "/Game";

        public const string AssetsRoot = "Assets";
        public const string GameRoot = AssetsRoot + "/Game";
        public const string AppRoot = GameRoot + "/App";
        public const string CoreRoot = GameRoot + "/Core";
        public const string ManagerRoot = GameRoot + "/Manager";
        public const string ScriptableObjectRoot = GameRoot + "/ScriptableObject";
        public const string ComponentRoot = GameRoot + "/Component";

        public const string ManagerOrder = AppRoot + "/ManagerOrder.asset";
    }
}
