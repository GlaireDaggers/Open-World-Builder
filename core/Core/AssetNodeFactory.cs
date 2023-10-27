namespace OpenWorldBuilder
{
    /// <summary>
    /// Base class for a processor which can convert certain file types into scene nodes
    /// </summary>
    public class AssetNodeFactory
    {
        /// <summary>
        /// Check if the given asset path is supported by this node factory
        /// </summary>
        /// <param name="assetPath">The path to the asset, relative to the content folder</param>
        /// <returns>True if the node factory can handle this asset, false otherwise</returns>
        public virtual bool CanHandle(string assetPath) => false;

        /// <summary>
        /// Process the given asset, converting it into a scene node
        /// </summary>
        /// <param name="assetPath">The path to the asset, relative to the content folder</param>
        /// <returns>A new scene node</returns>
        public virtual Node Process(string assetPath) => throw new NotImplementedException();
    }
}