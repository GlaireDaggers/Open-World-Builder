namespace OpenWorldBuilder
{
    /// <summary>
    /// Represents a level in a project
    /// </summary>
    public class Level : IDisposable
    {
        /// <summary>
        /// Root node of the scene hierarchy
        /// </summary>
        public SceneRootNode root = new() {
            name = "Level"
        };

        public void Dispose()
        {
            root.Dispose();
        }
    }
}