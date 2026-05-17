namespace Stratum.Editor
{
    internal static class WorkbenchPaths
    {
        public const string PackageId = "com.l47coder.stratum";
        public const string PackageRoot = "Packages/" + PackageId;
        public const string TemplatesRoot = PackageRoot + "/Templates~";
        public const string GameSkeletonTemplateFolder = TemplatesRoot + "/Game";

        public const string AssetsRoot = "Assets";
        public const string GameRoot = AssetsRoot + "/Game";
        public const string FrameRoot = GameRoot + "/Frame";
        public const string ManagerRoot = GameRoot + "/Manager";
        public const string SoRoot = GameRoot + "/ScriptableObject";
        public const string MonoBehaviourRoot = GameRoot + "/MonoBehaviour";
        public const string ContainerRoot = GameRoot + "/Container";

        public const string ManagerOrder = FrameRoot + "/ManagerOrder.asset";
        public const string PageOrder = FrameRoot + "/PageOrder.asset";
    }
}
