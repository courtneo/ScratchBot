namespace Microsoft.Azure.ProcessSimple.Data.Components.AdaptiveCards
{
    /// <summary>
    /// Adaptive action data representing a user's response to an approval.
    /// </summary>
    public class AdaptiveApprovalResponseData : AdaptiveOptionsResponseData
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AdaptiveApprovalResponseData" /> class.
        /// </summary>
        public AdaptiveApprovalResponseData()
        {
            this.ActionType = AdaptiveActionType.ApprovalResponse;
        }

        /// <summary>
        /// Gets the environment in which the approval resides.
        /// </summary>
        public string Environment { get; set; }

        /// <summary>
        /// Gets the title of the approval.
        /// </summary>
        public string ApprovalTitle { get; set; }

        /// <summary>
        /// Gets the link to the approval in the portal.
        /// </summary>
        public string ApprovalLink { get; set; }

        /// <summary>
        /// Gets the name of the approval.
        /// </summary>
        public string ApprovalName { get; set; }
    }
}