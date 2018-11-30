namespace Microsoft.Azure.ProcessSimple.Data.Components.AdaptiveCards
{
    using System.Diagnostics.CodeAnalysis;
    using Newtonsoft.Json;

    /// <summary>
    /// Data about an action initiated by a user while interacting with an adaptive card.
    /// </summary>
    [SuppressMessage("Microsoft.StyleCop.CSharp.MaintainabilityRules", "SA1402:FileMayOnlyContainASingleClass", Justification = "Small closely related classes")]
    public class AdaptiveActionData
    {
        /// <summary>
        /// Deserialize a string into the appropriate adaptive action data subclass.
        /// </summary>
        /// <param name="serializedAdaptiveActionData">String containing serialized adaptive action data.</param>
        /// <returns>The resulting deserialized adaptive action data instance.</returns>
        public static AdaptiveActionData Deserialize(string serializedAdaptiveActionData)
        {
            var adaptiveACtionData = JsonConvert.DeserializeObject<AdaptiveActionData>(serializedAdaptiveActionData);

            switch (adaptiveACtionData.ActionType)
            {
                case AdaptiveActionType.ApprovalResponse:
                    return JsonConvert.DeserializeObject<AdaptiveApprovalResponseData>(serializedAdaptiveActionData);
                case AdaptiveActionType.OptionsResponse:
                    return JsonConvert.DeserializeObject<AdaptiveOptionsResponseData>(serializedAdaptiveActionData);
                default:
                    return null;
            }
        }

        /// <summary>
        /// Serialize the current adaptive card action instance to string
        /// </summary>
        /// <returns>The resulting string.</returns>
        public string Serialize()
        {
            return JsonConvert.SerializeObject(
                this,
                new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
        }

        /// <summary>
        /// Enumeration indicating the type of action represented by this adaptive action data instance.
        /// </summary>
        public AdaptiveActionType ActionType { get; set; }
    }

    /// <summary>
    /// Adaptive action data representing a user's choice among options.
    /// </summary>
    [SuppressMessage("Microsoft.StyleCop.CSharp.MaintainabilityRules", "SA1402:FileMayOnlyContainASingleClass", Justification = "Small closely related classes")]
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
        public string ResponseOption { get; set; }

        /// <summary>
        /// Gets the set of options among which the user chose
        /// </summary>
        public string[] ResponseOptions { get; set; }

        /// <summary>
        /// Gets comments made by the user on the choice.
        /// </summary>
        public string Comments { get; set; }
    }

    /// <summary>
    /// Adaptive action data representing a user's response to an approval.
    /// </summary>
    [SuppressMessage("Microsoft.StyleCop.CSharp.MaintainabilityRules", "SA1402:FileMayOnlyContainASingleClass", Justification = "Small closely related classes")]
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