namespace OpenWorldBuilder
{
    /// <summary>
    /// Represents a level in a project
    /// </summary>
    public class Level
    {
        /// <summary>
        /// Root node of the scene hierarchy
        /// </summary>
        public SceneRootNode root = new() {
            name = "Level"
        };
    }
}