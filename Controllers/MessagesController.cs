using System.Threading.Tasks;
using System.Web.Http;

using Microsoft.Bot.Connector;
using Microsoft.Bot.Builder.Dialogs;
using System.Web.Http.Description;
using System.Net.Http;
using System.Linq;
using System;
using SimpleEchoBot;
using Newtonsoft.Json;
using System.Collections.Generic;
using Microsoft.Bot.Connector.Teams.Models;
using BotAuth.AADv2;
using BotAuth;

namespace Microsoft.Bot.Sample.SimpleEchoBot
{
   
    [BotAuthentication]
    public class MessagesController : ApiController
    {
       
        /// <summary>
        /// POST: api/Messages
        /// receive a message from a user and send replies
        /// </summary>
        /// <param name="activity"></param>
        [ResponseType(typeof(void))]
        public virtual async Task<HttpResponseMessage> Post([FromBody] Activity activity)
        {
            // check if activity is of type message
            if (activity != null && activity.GetActivityType() == ActivityTypes.Message)
            {
                await Conversation.SendAsync(activity, () => new EchoDialog());
            }
            else
            {
                await HandleSystemMessage(activity);
            }
            return new HttpResponseMessage(System.Net.HttpStatusCode.Accepted);
        }

        private async Task<Activity> HandleSystemMessage(Activity message)
        {
            if(message.ServiceUrl == "https://directline.botframework.com/")
            {
                string input = "hello world, direct line";
                
                Activity userMessage = new Activity
                {
                    From = new ChannelAccount(),
                    Text = input,
                    Type = Connector.DirectLine.ActivityTypes.Message,
                    ChannelId = "8c8e13d4-fb54-4777-aea2-7b6ace72bd92"
                };

                //await Conversation.SendAsync(userMessage, () => new EchoDialog());
            }
            else if (message.Type == ActivityTypes.DeleteUserData)
            {
                // Implement user deletion here
                // If we handle user deletion, return a real message
            }
            else if (message.Type == ActivityTypes.ConversationUpdate && message.MembersAdded != null && message.MembersAdded.Any(x=> x.Name == null))
            {
                // Handle conversation state changes, like members being added and removed
                // Use Activity.MembersAdded and Activity.MembersRemoved and Activity.Action for info
                // Not available in all channels

                // Fetch the members in the current conversation
                //var connector = new ConnectorClient(new Uri("http://hack005.ngrok.io"));
                var connector = new ConnectorClient(new Uri(message.ServiceUrl));
                var members = await connector.Conversations.GetConversationMembersAsync(message.Conversation.Id);


                var teamID = message.Conversation.Id;


                var registerDto = new RegisterDto()
                {
                    TeamId = teamID,
                    Members = members.Select(x => new TeamMemberDto()
                    {
                        Id = x.Properties.GetValue("objectId").ToString(),
                        Name = x.Name
                    }).ToList()
                };

                using (var httpclient = new HttpClient())
                {
                    var response = await httpclient.PostAsJsonAsync("https://msopenhackeu.azurewebsites.net/api/trivia/register", registerDto);
                }

                
                Activity reply = message.CreateReply($"Welcome");
                var msgToUpdate = await connector.Conversations.ReplyToActivityAsync(reply);
            }
            else if (message.Type == ActivityTypes.ContactRelationUpdate)
            {
                // Handle add/remove from contact lists
                // Activity.From + Activity.Action represent what happened
            }
            else if (message.Type == ActivityTypes.Typing)
            {
                // Handle knowing tha the user is typing
            }
            else if (message.Type == ActivityTypes.Ping)
            {
            }

            return null;
        }

    }
}