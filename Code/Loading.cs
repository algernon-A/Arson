using ICities;


namespace Arson
{
    /// <summary>
    /// Main loading class: the mod runs from here.
    /// </summary>
    public class Loading : LoadingExtensionBase
    {
        // Internal flags.
        internal static bool isLoaded = false;
        private static bool isModEnabled = false;


        /// <summary>
        /// Called by the game when the mod is initialised at the start of the loading process.
        /// </summary>
        /// <param name="loading">Loading mode (e.g. game, editor, scenario, etc.)</param>
        public override void OnCreated(ILoading loading)
        {
            base.OnCreated(loading);

            // Don't do anything if not in game (e.g. if we're going into an editor).
            if (loading.currentMode != AppMode.Game)
            {
                isModEnabled = false;
                Logging.KeyMessage("not loading into game, skipping activation");
            }

            // Passed all checks - okay to load (if we haven't already for some reason).
            if (!isModEnabled)
            {
                isModEnabled = true;
                Logging.KeyMessage("v " + ArsonMod.Version + " loading");
            }
        }


        /// <summary>
        /// Called by the game when level loading is complete.
        /// </summary>
        /// <param name="mode">Loading mode (e.g. game, editor, scenario, etc.)</param>
        public override void OnLevelLoaded(LoadMode mode)
        {
            // Load mod if it's enabled.
            if (isModEnabled)
            {
                // Initialise tool.
                ToolsModifierControl.toolController.gameObject.AddComponent<ArsonTool>();

                // Set loaded status flag.
                isLoaded = true;

                Logging.Message("loading complete");
            }
        }
    }
}