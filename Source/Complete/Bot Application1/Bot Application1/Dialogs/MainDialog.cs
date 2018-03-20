using Microsoft.Bot.Builder.Dialogs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Threading.Tasks;

namespace Bot_Application1.Dialogs
{
    [Serializable]
    public class MainDialog : IDialog<object>
    {
        public async Task StartAsync(IDialogContext context)
        {
            context.Wait(MessageReceivedAsync);
        }

        private async Task MessageReceivedAsync(IDialogContext context, IAwaitable<object> activity)
        {
            if (context.ConversationData.TryGetValue("UserName", out string userName))
            {
                await context.PostAsync($"Well, {userName}, good to see you again!");
                ShowOptions(context);
            }
            else
            {
                PromptDialog.Text(context, AfterNameEntered, "I don't know you, what's your name?");
            }
        }

        private async Task AfterNameEntered(IDialogContext context, IAwaitable<string> result)
        {
            var name = await result;
            context.ConversationData.SetValue("UserName", name);
            await context.PostAsync($"OK, I'll remember, you're {name}.");

            context.Done(true);
        }

        private void ShowOptions(IDialogContext context)
        {
            var choices = new List<string>() {
                Tasks.Speakers,
                Tasks.Info
            };

            PromptDialog.Choice(context,
                AfterTaskSelected,
                choices,
                "What do you care about?",
                promptStyle: PromptStyle.Keyboard,
                attempts: 99
            );
        }

        private async Task AfterTaskSelected(IDialogContext context, IAwaitable<string> result)
        {
            var res = await result;
            switch (res)
            {
                case Tasks.Speakers:
                    context.Call(new SpeakersDialog(), AfterTaskCompleted);
                    break;
                case Tasks.Info:
                    context.Call(new InfoDialog(), AfterTaskCompleted);
                    break;
            }
        }

        private async Task AfterTaskCompleted(IDialogContext context, IAwaitable<object> result)
        {
            ShowOptions(context);
        }

    }

    public static class Tasks
    {
        public const string Speakers = "😎 Speakers";
        public const string Info = "ℹ️ Info";
    }
}