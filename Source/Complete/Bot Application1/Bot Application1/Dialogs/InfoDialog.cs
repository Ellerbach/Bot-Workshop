using Microsoft.Bot.Builder.Dialogs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Threading.Tasks;
using Microsoft.Bot.Connector;

namespace Bot_Application1.Dialogs
{
    [Serializable]
    public class InfoDialog : IDialog<object>
    {
        public async Task StartAsync(IDialogContext context)
        {
            var card = new ThumbnailCard()
            {
                Title = "Info",
                Images = new List<CardImage>()
                {
                    new CardImage("https://store-images.s-microsoft.com/image/apps.45734.14515212426102172.115c6a65-afc3-434e-8905-88fa2ce42bcb.8ca60ad0-a729-4fa1-a5e7-1c100a43aaae?w=180&h=180&q=60")
                },
                Text = "The conference is held somewhere on the planet earth. You will find it on a universe map!"
            };

            var reply = context.MakeMessage();
            reply.Attachments.Add(card.ToAttachment());

            await context.PostAsync(reply);

            context.Done(true);
        }
    }
}