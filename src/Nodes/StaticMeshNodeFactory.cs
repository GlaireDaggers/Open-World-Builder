namespace OpenWorldBuilder
{
    public class StaticMeshNodeFactory : AssetNodeFactory
    {
        public override bool CanHandle(string assetPath)
        {
            return assetPath.EndsWith(".obj");
        }

        public override Node Process(string assetPath)
        {
            StaticMeshNode node = new StaticMeshNode();
            node.name = Path.GetFileNameWithoutExtension(assetPath);
            node.LoadMesh(assetPath);

            return node;
        }
    }
}