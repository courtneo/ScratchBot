namespace Microsoft.Azure.ProcessSimple.Data.Components.AdaptiveCards
{
    /// <summary>
    /// Enumeration of kinds of action that can be taken in Flow adaptive cards
    /// </summary>
    public enum AdaptiveActionType
    {
        /// <summary>
        /// Not speficied
        /// </summary>
        NotSpecified,

        /// <summary>
        /// Recipient's response to an approval request
        /// </summary>
        ApprovalResponse,

        /// <summary>
        /// Recipient's response to an options request
        /// </summary>
        OptionsResponse
    }
}