# First Bot with Bot Framework

Download the necessary tools:

* [Visual Studio 2017](https://www.visualstudio.com/)
* [Bot project template for Visual Studio](http://aka.ms/bf-bc-vstemplate)
* [Bot Framework Emulator](https://emulator.botframework.com/)

> Note: If you're running on a Mac, you need to do a little bit more work to make Bot Framework run on ASP.NET Core. It's still under development, but you can use a [step-by-step guide](Bot-Builder-on-Mac.md).

## Basic bot

1. Install Visual Studio 2017.
2. Copy the bot template ZIP to `C:\Users\<user>\Documents\Visual Studio 2017\Templates\ProjectTemplates\Visual C#`. **Don't extract the files**!
3. Install the Bot Framework emulator and run it.
4. Run Visual Studio 2017.
5. Create new bot project (*File > New > Project... > Visual C# > Bot Application*).
6. Run it and note the localhost address of your bot.
7. Set up the Emulator with correct address (such as `http://localhost:3979/api/messages`).
8. Click Connect and talk to your bot.

## More traditional structure

1. Create folder **Dialogs**.

2. Create new class in Dialogs called **MainDialog**.

3. Replace the class:

  ```c#
  [Serializable]
  public class MainDialog : IDialog<object>
  {
      public async Task StartAsync(IDialogContext context)
      {
          context.Wait(MessageReceivedAsync);
      }

      private async Task MessageReceivedAsync(IDialogContext context, IAwaitable<object> activity)
      {
         await context.PostAsync("I'm listening!");
         context.Done(true);
      }
  }
  ```

4. Go to **MessagesController** and invoke MainDialog instead of current implementation.

  ```c#
  public async Task<HttpResponseMessage> Post([FromBody]Activity activity)
  {
      if (activity.Type == ActivityTypes.Message)
      {
          await Conversation.SendAsync(activity, () => new MainDialog());
      }
      else
      {
          HandleSystemMessage(activity);
      }
      var response = Request.CreateResponse(HttpStatusCode.OK);
      return response;
  }
  ```

5. Run the bot application.

6. Open Bot Framework Emulator and connect to your localhost instance.

7. Type something and send it.

## Remember the user

1. Add this code to the **MainDialog** class, **MessageReceivedAsync** method:

  ```c#
  private async Task MessageReceivedAsync(IDialogContext context, IAwaitable<object> activity)
  {
      if (context.ConversationData.TryGetValue("UserName", out string userName))
      {
          await context.PostAsync($"Well, {userName}, good to see you again!");
          context.Done(true);
      }
      else
      {
          PromptDialog.Text(context, AfterNameEntered, "I don't know you, what's your name?");
      }
  }
  ```

2. Implementation of `AfterNameEntered`:

  ```c#
  private async Task AfterNameEntered(IDialogContext context, IAwaitable<string> result)
  {
      var name = await result;
      context.ConversationData.SetValue("UserName", name);
      await context.PostAsync($"OK, I'll remember, you're {name}.");

      context.Done(true);
  }
  ```

If you try your bot now, it will first ask for your name. From then on it will greet you using this name.

## Main menu

1. Under MainDialog class, prepare a class for Main Menu options (including the Emoji):

  ```c#
  public static class Tasks
  {
      public const string Speakers = "😎 Speakers";
      public const string Info = "ℹ️ Info";
  }
  ```

2. Prepare a method to display this menu using `PromptDialog.Choice()`:

  ```c#
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
  ```

3. Implement `AfterTaskSelected` and `AfterTaskCompleted`:

  ```c#
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
  ```

> We haven't yet prepared the subsequent dialogs, so this code will not build.

Add the call to ShowOptions to **MessageReceivedAsync**:

```c#
if (context.ConversationData.TryGetValue("UserName", out string userName))
{
    await context.PostAsync($"Well, {userName}, good to see you again!");
    ShowOptions(context);
}
else
{
    PromptDialog.Text(context, AfterNameEntered, "I don't know you, what's your name?");
}
```

Now get the missing dialogs ready.

1. Add two new classes to the **Dialogs** folder:
   1. SpeakersDialog
   2. InfoDialog
2. Both will be `[Serializable]` and implement the `IDialog<object>` interface.

Let's implement the **InfoDialog** first.

1. Replace the InfoDialog class code with this:

```c#
[Serializable]
public class InfoDialog : IDialog<object>
{
    public async Task StartAsync(IDialogContext context)
    {
        await context.PostAsync("The conference is held somewhere **on the planet earth**. You will find it on a **universe map**!");
        context.Done(true);
    }
}
```

Then do a similar thing with **SpeakersDialog**:

```c#
[Serializable]
public class SpeakersDialog : IDialog<object>
{
    public async Task StartAsync(IDialogContext context)
    {
        await context.PostAsync("We have great speakers. Who would you be interested in?");
        context.Wait(MessageReceivedAsync);
    }

    private Task MessageReceivedAsync(IDialogContext context, IAwaitable<object> result)
    {
        throw new NotImplementedException();
    }
}
```

Test the bot. It should crash on *Speakers* and work fine on *Info*.

## Attachments

How about sending a picture along with the conference info?

1. Go to InfoDialog.cs.
2. Change the StartAsync method to look like this:

```c#
public async Task StartAsync(IDialogContext context)
{
    var reply = context.MakeMessage();

    reply.Attachments.Add(new Attachment() {
        // Or replace by any image
        ContentUrl = "https://store-images.s-microsoft.com/image/apps.45734.14515212426102172.115c6a65-afc3-434e-8905-88fa2ce42bcb.8ca60ad0-a729-4fa1-a5e7-1c100a43aaae?w=180&h=180&q=60",
        ContentType = "image/png",
        Name = "location.png"
    });

    reply.Text = "The conference is held somewhere **on the planet earth**. You will find it on a **universe map**!";
    await context.PostAsync(reply);

    context.Done(true);
}
```

When you test it, you will see that this isn't a particularly great design. Let's make it a little better by using Cards.

```c#
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
```

Not great either, so iterate... iterate... and iterate.

> To explore various types of cards on different channels, you can use the [Channel Inspector](https://docs.botframework.com/en-us/channel-inspector/channels/Skype/).

## QnA Maker

Information about speakers will be provided by the QnA Maker service.

1. Go to [http://qnamaker.ai](http://qnamaker.ai).
2. Click **Create new service** and login with your Microsoft Account.
3. Go to **FAQ FILES** and upload the **Speakers.docx** file from this repo.
4. Click **Create**.
5. **Publish** the service.
6. Make note of **knowledge base ID** and **subscription key**.

POST /knowledgebases/**187bb658-c421-4118-96de-c3d2ec0abe98**/generateAnswer
Host: https://westus.api.cognitive.microsoft.com/qnamaker/v2.0
Ocp-Apim-Subscription-Key: **8BCkeykeykeykeykeyA123**
Content-Type: application/json
{"question":"hi"}

Go back to your bot project and:

1. Create new folder called **Services**.
2. Add new class to this folder. Call it **QnaService**.
3. Replace its code with this:

```c#
public class QnaService
{
    public string KnowledgeBaseId { get; set; }
    public string SubscriptionKey { get; set; }

    public QnaService(string knowledgeBaseId, string subscriptionKey)
    {
        KnowledgeBaseId = knowledgeBaseId;
        SubscriptionKey = subscriptionKey;
    }


    public async Task<string> QnAMakerQueryAsync(string query)
    {
        using (HttpClient hc = new HttpClient())
        {
            string url = $"https://westus.api.cognitive.microsoft.com/qnamaker/v1.0/knowledgebases/{KnowledgeBaseId}/generateAnswer";
            var content = new StringContent($"{{\"question\": \"{query}\"}}", Encoding.UTF8, "application/json");
            hc.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", SubscriptionKey);

            var response = await hc.PostAsync(url, content);

            if (response.IsSuccessStatusCode)
            {
                var answer = JsonConvert.DeserializeObject<QnAMakerResult>(await response.Content.ReadAsStringAsync());

                if (answer.Score >= 0.3)
                {
                    return HttpUtility.HtmlDecode(answer.Answer);
                }
                else
                {
                    return null;
                }
            }
            else
            {
                throw new QnAMakerException();
            }
        }
    }

}

public class QnAMakerResult
{
    /// <summary>
    /// The top answer found in the QnA Service.
    /// </summary>
    [JsonProperty(PropertyName = "answer")]
    public string Answer { get; set; }

    /// <summary>
    /// The score in range [0, 100] corresponding to the top answer found in the QnA    Service.
    /// </summary>
    [JsonProperty(PropertyName = "score")]
    public double Score { get; set; }
}

public class QnAMakerException : Exception { }
```

Change the **MessageReceivedAsync** method in **SpeakersDialog.cs** to look for speaker using QnA Maker:

```c#
private async Task MessageReceivedAsync(IDialogContext context, IAwaitable<IMessageActivity> result)
{
    var mess = await result;

    var qna = new QnaService("<knowledge base ID>", "<key>");

    var answer = await qna.QnAMakerQueryAsync(mess.Text);
    if (answer != null)
    {
        await context.PostAsync(answer);
    }
    else
    {
        await context.PostAsync("I don't know...");
    }

    await context.PostAsync("Who else?");
    context.Wait(MessageReceivedAsync);
}
```

No we get the answer, but we're stuck in an endless loop. Let's fix this and add a "safe" word which will get us back to the MainDialog.

```c#
private async Task MessageReceivedAsync(IDialogContext context, IAwaitable<IMessageActivity> result)
{
    var mess = await result;

    if (mess.Text.ToLower().StartsWith("no one") || mess.Text.ToLower() == "n" || mess.Text.ToLower() == "back")
    {
        context.Done(true);
        return;
    }

    ...
}
```

Try the bot again. You will be able to get back to the Main menu.

## LUIS

1. Go to [Luis.ai](http://luis.ai) and sign in (create a new account if necessary).
2. Create new **App**.
3. Create **Intent** called "*whois*".
4. Add utterances:
   1. *"who is peter?"*
   2. "*what do you know about peter?*"
   3. *"what is petr doing?"*
   4. *"kdo je petr?"*
   5. *"čemu se petr věnuje?"*
Note: 4. and 5. are to support local languages if you want. You can add more sentenses if you want.
5. **Save**.
6. Set "petr" as a new entity called *name* in all utterances.
7. **Save** again.
8. Add more utterances with multiple-word entities. Same as previously, you can enter multiple languages.
   1. *"who is karel karlík?"*
   2. *"kdo je karel karlík?"*
9. Train & Test

We will create two more intents:

* info
  * *"info"*
  * *"when is the conference?"*
  * *"where is the conference?"*
  * *"location"*
  * *"kde je konference?"*
  * *"adresa konference?"*
* program
  * *"program"*
  * *"přednášky"*
  * *"sessions"*

**Publish** the LUIS app and save ID & key.

```
https://westus.api.cognitive.microsoft.com/luis/v2.0/apps/<app>?subscription-key=<key>&verbose=true&timezoneOffset=0&q=
```

Now implement LUIS support in the bot:

1. Create new class in the **Dialogs** folder called **MainLuisDialog**.
2. Build a basic skeleton of the class:

```c#
[Serializable]
[LuisModel("", "")]
public class MainLuisDialog : LuisDialog<object>
{
    [LuisIntent("")]
    [LuisIntent("None")]
    public async Task None(IDialogContext context, LuisResult result)
    {
        await context.PostAsync($"Sorry, I did not understand '{result.Query}'.");

        context.Wait(this.MessageReceived);
    }

    [LuisIntent("whois")]
    public async Task WhoIs(IDialogContext context, IAwaitable<IMessageActivity> activity, LuisResult result)
    {
        var message = await activity;
    }

    [LuisIntent("info")]
    public async Task Info(IDialogContext context, IAwaitable<IMessageActivity> activity, LuisResult result)
    {
        var message = await activity;
    }

    [LuisIntent("program")]
    public async Task Program(IDialogContext context, IAwaitable<IMessageActivity> activity, LuisResult result)
    {
        var message = await activity;
    }
}
```

Fill App ID & Key in the `LuisModel` attribute.

Implement the **whois** intent:

```c#
[LuisIntent("whois")]
public async Task WhoIs(IDialogContext context, IAwaitable<IMessageActivity> activity, LuisResult result)
{
    var message = await activity;

    var name = result.Entities.FirstOrDefault()?.Entity;
    await context.Forward(new SpeakersDialog(name), AfterTaskDone, message);
}
```

Change the **SpeakersDialog** to accept name in constructor and work with it:

```c#
[Serializable]
public class SpeakersDialog : IDialog<object>
{
    private string _entity = null;

    public SpeakersDialog() { }

    public SpeakersDialog(string entity)
    {
        _entity = entity;
    }

    public async Task StartAsync(IDialogContext context)
    {
        context.Wait(MessageReceivedAsync);
    }

    private async Task MessageReceivedAsync(IDialogContext context, IAwaitable<IMessageActivity> result)
    {
        var mess = await result;

        if (mess.Text.ToLower().StartsWith("no one") || mess.Text.ToLower() == "n" || mess.Text.ToLower() == "back")
        {
            context.Done(true);
            return;
        }

        var qna = new QnaService("<knowledge base ID>", "<key>");

        var answer = await qna.QnAMakerQueryAsync(_entity == null ? mess.Text : _entity);
        if (answer != null)
        {
            await context.PostAsync(answer);
        }
        else
        {
            await context.PostAsync("I don't know...");
        }

        _entity = null;

        await context.PostAsync("Who else?");
        context.Wait(MessageReceivedAsync);
    }
}
```

Change **MessageController** to call MainLuisDialog instead of MainDialog.

```c#
//await Conversation.SendAsync(activity, () => new MainDialog());
await Conversation.SendAsync(activity, () => new MainLuisDialog());
```

## Change Default Storage

As default, storage is done in the bot Framework but it's only for testing purpose. You'll need to implement your own storage. As an example, we'll use a temporary, in memory storage.

Add the Microsoft.Bot.Builder.Azure nuget package.
1. Right click on References
2. Manage Nuget Packages
3. Search for "Microsoft.Bot.Builder.Azure"
4. Install the package and accept the condition

In the file Global.asx.cs, add the following references:

```c#
using Autofac;
using Microsoft.Bot.Connector;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Internals;
using Microsoft.Bot.Builder.Azure;
```

In the protected void Application_Start(), add the folling code:

```c#
Conversation.UpdateContainer(
builder =>
{
    var store = new InMemoryDataStore();
    builder.Register(c => store)
                .Keyed<IBotDataStore<BotData>>(AzureModule.Key_DataStore)
                .AsSelf()
                .SingleInstance();
});
```
It does implement an in memory storage for the states (like the UserName we used). It can be replaced by a SQL Storage or Cosmos DB or Table. See the [documentation](https://docs.microsoft.com/en-us/bot-framework/dotnet/bot-builder-dotnet-state) for more information. You can for example create a Table and store everything in it.

## Publish to production

It's always good to test in real client.

1. Register at portal

2. Download ngrok and run it

  ```
  ngrok http 3979 -host-header=rewrite
  ```

3. Fill URL at portal

4. Add to Skype, test

Publish to Azure.