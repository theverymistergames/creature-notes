namespace _Project.Scripts.Runtime.Levels {
    
    public static class LevelLoadingOptions {
        
        /// <summary>
        /// Disabled during build to avoid opening scenes in <see cref="LevelRoot"/>.
        /// </summary>
        public static bool AllowLevelLoadingInEditor { get; set; }
        
    }

}