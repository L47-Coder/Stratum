namespace Stratum.Editor
{
    internal static class WorkbenchPaths
    {
        public const string PackageId = "com.l47coder.stratum";
        public const string PackageRoot = "Packages/" + PackageId;
        public const string TemplatesRoot = PackageRoot + "/Templates~";
        public const string GameSkeletonTemplateFolder = TemplatesRoot + "/Game";
        public const string ManagerTemplatesFolder = TemplatesRoot + "/Managers";
        public const string ComponentTemplatesFolder = TemplatesRoot + "/Components";

        public const string AssetsRoot = "Assets";
        public const string GameRoot = AssetsRoot + "/Game";
        public const string FrameRoot = GameRoot + "/Frame";
        public const string ManagerRoot = GameRoot + "/Manager";
        public const string ComponentRoot = GameRoot + "/Component";
        public const string PrefabRoot = GameRoot + "/Prefab";

        public const string ManagerOrder = FrameRoot + "/ManagerOrder.asset";
        public const string ComponentOrder = FrameRoot + "/ComponentOrder.asset";
        public const string PageOrder = FrameRoot + "/PageOrder.asset";
        public const string AddressableGroupOrder = FrameRoot + "/AddressableGroupOrder.asset";
    }
}
