namespace Microsoft.Azure.ProcessSimple.Data.Components.AdaptiveCards
{
    /// <summary>
    /// Adaptive action data representing a user's choice among options.
    /// </summary>
    public class AdaptiveOptionsResponseData : AdaptiveActionData
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AdaptiveOptionsResponseData" /> class.
        /// </summary>
        public AdaptiveOptionsResponseData()
        {
            this.ActionType = AdaptiveActionType.OptionsResponse;
        }

        /// <summary>
        /// Gets the option chosen by the user.
        /// </summary>
        public string SelectedOption { get; set; }

        /// <summary>
        /// Gets the set of options among which the user chose
        /// </summary>
        public string[] Options { get; set; }

        /// <summary>
        /// Gets comments made by the user on the choice.
        /// </summary>
        public string Comments { get; set; }
    }
}