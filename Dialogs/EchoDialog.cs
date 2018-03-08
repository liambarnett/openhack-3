using System;
using System.Threading.Tasks;

using Microsoft.Bot.Connector;
using Microsoft.Bot.Builder.Dialogs;
using System.Net.Http;
using SimpleEchoBot;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using BotAuth.AADv2;
using BotAuth;
using BotAuth.Dialogs;
using BotAuth.Models;
using System.Threading;

namespace Microsoft.Bot.Sample.SimpleEchoBot
{
    [Serializable]
    public class EchoDialog : IDialog<object>
    {
        protected int count = 1;

        public async Task StartAsync(IDialogContext context)
        {
            context.Wait(MessageReceivedAsync);
        }

        public async Task MessageReceivedAsync(IDialogContext context, IAwaitable<IMessageActivity> argument)
        {
            var message = await argument;
            var msgText = message.RemoveRecipientMention().Replace("\n", "").TrimEnd().TrimStart();
            if (msgText == "reset")
            {
                PromptDialog.Confirm(
                    context,
                    AfterResetAsync,
                    "Are you sure you want to reset the count?",
                    "Didn't get that!",
                    promptStyle: PromptStyle.Auto);
            }
            else if(msgText.Equals("auth"))
            {
                // Initialize AuthenticationOptions and forward to AuthDialog for token
                AuthenticationOptions options = new AuthenticationOptions()
                {
                    Authority = "https://login.microsoftonline.com/common",
                    ClientId = "cfca18bf-9031-4fea-9e83-3a19848a3489",
                    ClientSecret = "qWST866]%nusgggVUDV67_-",
                    Scopes = new string[] { "User.ReadWrite" },
                    RedirectUrl = "https://hack005.ngrok.io/callback",
                    MagicNumberView = "/magic.html#{0}"
                };
                
                await context.Forward(new AuthDialog(new MSALAuthProvider(), options), async (IDialogContext authContext, IAwaitable<AuthResult> authResult) =>
                {
                    var result = await authResult;
                    WebApiApplication.UserTokens.TryAdd(authContext.Activity.From.Id, result.AccessToken);

                    // Use token to call into service
                    var json = await new HttpClient().GetWithAuthAsync(result.AccessToken, "https://graph.microsoft.com/v1.0/me");
                    await authContext.PostAsync($"I'm a simple bot that doesn't do much, but I know your name is {json.Value<string>("displayName")} and your UPN is {json.Value<string>("userPrincipalName")}");

                }, message, CancellationToken.None);
                
            }
            else if (msgText.Equals("question"))
            {

              

                using (var _client = new HttpClient())
                {
                    var questionR = await _client.PostAsJsonAsync("https://msopenhackeu.azurewebsites.net/api/trivia/question", new
                    {
                        id = ((Activity)message).From.Properties.GetValue("aadObjectId")
                    });
                    var r = await questionR.Content.ReadAsAsync<QuestionDto>();

                    var opt = r.QuestionOptions.Select(x => new QuestionChoicesDto()
                    {
                        Id = x.Id,
                        Value = x.Text,
                        QuestionId = r.Id
                    }).ToList();

                    var promptOptions = new PromptOptions<QuestionChoicesDto>(r.Text, options: opt);

                    PromptDialog.Choice(context, OnQuestionAnsweredAsync, promptOptions);

                }

            }
            else
            {
                
                await context.PostAsync($"{this.count++}: You said {message.Text}");
                context.Wait(MessageReceivedAsync);
            }
        }

        private async Task OnQuestionAnsweredAsync(IDialogContext context, IAwaitable<object> result)
        {
            //todo process answer
            var confirm = await result as QuestionChoicesDto;


            using (var _client = new HttpClient())
            {
                var questionR = await _client.PostAsJsonAsync("https://msopenhackeu.azurewebsites.net/api/trivia/answer", new
                {
                    UserId = context.Activity.From.Properties.GetValue("aadObjectId"),
                    QuestionID = confirm.QuestionId,
                    AnswerID = confirm.Id
                });

                var r = await questionR.Content.ReadAsAsync<AnswerResultDto>();
                var txtResponse = r.Correct ? "Correct" : "Incorrect";

                string userBadge;
                var userId = new Guid(context.Activity.From.Properties.GetValue("aadObjectId").ToString());
                WebApiApplication.UserBadges.TryGetValue(userId, out userBadge);

                await context.PostAsync($"Result - {context.Activity.From.Name}: {txtResponse} - BotMemory Badge: {userBadge}, API Answer Result Badge: {r.achievementBadge}");

                if (true) //r.Correct)
                {
                    var shouldSendEvent = true;

                    
                    if (WebApiApplication.UserBadges.ContainsKey(userId))
                    {
                        
                        if (string.IsNullOrEmpty(userBadge) || userBadge.Equals(r.achievementBadge))
                            shouldSendEvent = false;
                    }
                    else
                    {
                        WebApiApplication.UserBadges.TryAdd(userId, r.achievementBadge);
                    }

                    if (true)//shouldSendEvent)
                    {

                        string userToken = "";
                        WebApiApplication.UserTokens.TryGetValue(context.Activity.From.Id, out userToken);
                        var data = new List<UserBadgeEventDto>
                        {
                            new UserBadgeEventDto
                            {
                                id = Guid.NewGuid().ToString(),
                                EventTime = DateTime.Now,
                                EventType = "ch5badge",
                                Subject = "ch5badge",

                                Data = new EventDataDto
                                {
                                    UserId = userId.ToString(),
                                    AchievementBadge = r.achievementBadge,
                                    UserName = context.Activity.From.Name,
                                    UserAADID = context.Activity.From.Properties.GetValue("aadObjectId").ToString(),
                                    Token = userToken
                                }
                            }
                        };

                        _client.DefaultRequestHeaders.Add("aeg-sas-key", "OhKhYUMiFjP6O5UMLa/2lohxMRfqGrScPMLx+4AkHmM=");
                        _client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));//ACCEPT header
                        var response = await _client.PostAsJsonAsync("https://ch5badge.northeurope-1.eventgrid.azure.net/api/events", data);
                        await context.PostAsync($"Badge {r.achievementBadge} sent to Azure Event Grid");
                    }

                }
            }


            context.Wait(MessageReceivedAsync);
        }


        public async Task AfterResetAsync(IDialogContext context, IAwaitable<bool> argument)
        {
            var confirm = await argument;
            if (confirm)
            {
                this.count = 1;
                await context.PostAsync("Reset count.");
            }
            else
            {
                await context.PostAsync("Did not reset count.");
            }
            context.Wait(MessageReceivedAsync);
        }



    }
}