using System;
using System.Threading.Tasks;

using Microsoft.Bot.Connector;
using Microsoft.Bot.Builder.Dialogs;
using System.Net.Http;
using SimpleEchoBot;
using System.Collections.Generic;
using System.Linq;

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

            if (msgText.Equals("question"))
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
                    id = context.Activity.From.Properties.GetValue("aadObjectId"),
                    QuestionID = confirm.QuestionId,
                    AnswerID =  confirm.Id
                });

                var r = await questionR.Content.ReadAsAsync<AnswerResultDto>();
                var txtResponse = r.Correct ? "Correct" : "Incorrect";
                await context.PostAsync($"Result - {context.Activity.From.Name}: {txtResponse}");
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