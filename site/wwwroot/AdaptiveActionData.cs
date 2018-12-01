namespace Microsoft.Azure.ProcessSimple.Data.Components.AdaptiveCards
{
    using Newtonsoft.Json;

    /// <summary>
    /// Data about an action initiated by a user while interacting with an adaptive card.
    /// </summary>
    public class AdaptiveActionData
    {
        /// <summary>
        /// Enumeration indicating the type of action represented by this adaptive action data instance.
        /// </summary>
        public AdaptiveActionType ActionType { get; set; }

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
    }
}