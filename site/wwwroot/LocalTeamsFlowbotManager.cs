extern alias FlowWeb;
extern alias FlowData;
extern alias FlowCommon;
using System.Web.Http;
using FlowWeb::Microsoft.Azure.ProcessSimple.Web.Components;
using FlowData::Microsoft.Azure.ProcessSimple.Data.Configuration;
using FlowData::Microsoft.Azure.ProcessSimple.Data.DataProviders;
using FlowCommon::Microsoft.Azure.ProcessSimple.Common.Logging;

namespace SimpleEchoBot
{
    public class LocalTeamsFlowbotManager : TeamsFlowbotManager
    {
        public LocalTeamsFlowbotManager(
            ProcessSimpleConfiguration processSimpleConfiguration,
            HttpConfiguration httpConfiguration,
            CreateBotActivityDelegate createActivity,
            SendBotActivityAsyncDelegate postActivityAsync,
            SendBotActivityAsyncDelegate updateActivityAsync)
            : base(
                  processSimpleConfiguration: processSimpleConfiguration,
                  httpConfiguration: httpConfiguration,
                  createActivity: createActivity,
                  postActivityAsync: postActivityAsync,
                  updateActivityAsync: updateActivityAsync)
        { }

        protected override LogicAppsRuntimeDataProvider GetLogicAppsRuntimeDataProvider()
        {
            return new LogicAppsRuntimeDataProvider(eventSource: ProcessSimpleLog.Current, serviceClientDataProvider: new ServiceClientDataProvider());
        }
    }
}