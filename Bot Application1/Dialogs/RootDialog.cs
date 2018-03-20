using System;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;
using System.Text.RegularExpressions;
using System.Linq;
using MongoDB.Driver;

namespace Bot_Application1.Dialogs
{
    [Serializable]
    public class RootDialog : IDialog<object>
    {
        private string name;
        private int age;

        public async Task StartAsync(IDialogContext context)
        {
            // Wait until the first message is received from the conversation,
            // then call MessageReceviedAsync to process that message.
            context.Wait(this.MessageReceivedAsync);
        }

        private async Task MessageReceivedAsync(IDialogContext context, IAwaitable<IMessageActivity> result)
        {
            // When MessageReceivedAsync is called, it passes IAwaitable<IMessageActivity>.
            // Await the result (code suspended until the awaited tasks are completed) to get message.
            var message = await result;
            await this.SendWelcomeMessageAsync(context);
        }

        private async Task SendWelcomeMessageAsync(IDialogContext context)
        {
            await context.PostAsync("Hello, I'm C3PMO. How can I be of service?");
            context.Call(new NameDialog(), this.NameDialogResumeAfter);
        }

        private async Task NameDialogResumeAfter(IDialogContext context, IAwaitable<string> result)
        {
            try
            {
                this.name = await result;
                context.Call(new AgeDialog(this.name), this.AgeDialogResumeAfter);
            }
            catch (TooManyAttemptsException)
            {
                await context.PostAsync("I'm sorry, I'm having issues understanding you. Let's try again.");
                await this.SendWelcomeMessageAsync(context);
            }
        }

        private async Task AgeDialogResumeAfter(IDialogContext context, IAwaitable<int> result)
        {
            try
            {
                this.age = await result;
                await context.PostAsync($"Your name is { name.First().ToString().ToUpper() + name.Substring(1) } and your age is { age } and your WBS code { ReadFromSQL(name) }.");
            }
            catch (TooManyAttemptsException)
            {
                await context.PostAsync("I'm sorry, I'm having issues understanding you. Let's try again.");
            }
        }

        private string ReadFromSQL(string name)
        {
            // Connect to local database.
            var client = new MongoClient("mongodb://localhost:27017");
            IMongoDatabase db = client.GetDatabase("local");

            // Get user data.
            IMongoCollection<User> collection = db.GetCollection<User>("tbl_users");
            var documents = collection.AsQueryable().Where(u => u.ColEnterpriseId.Contains(name)).Select(u => new { u.ColWBS }).ToList();

            if (documents.Count == 1)
            {
                foreach (var user in documents)
                {
                    return("is " + user.ColWBS);
                }
            }
            return("has not been found");
        }
    }

    [Serializable]
    public class NameDialog : IDialog<string>
    {
        private int attempts = 3;

        public async Task StartAsync(IDialogContext context)
        {
            await context.PostAsync("What is your name?");
            context.Wait(this.MessageReceivedAsync);
        }

        private async Task MessageReceivedAsync(IDialogContext context, IAwaitable<IMessageActivity> result)
        {
            var message = await result;

            // If message returns a valid name, return it to the calling dialog.
            if (message.Text != null && message.Text.Trim().Length > 0 && Regex.IsMatch(message.Text, "[a-z]", RegexOptions.IgnoreCase))
            {
                // Complete dialog, remove it from the dialog stack, and return the result to the parent/calling dialog.
                context.Done(message.Text);
            }
            else // Try again by re-prompting the user.
            {
                --attempts;
                if (attempts > 0)
                {
                    await context.PostAsync("I'm sorry, I don't understand your reply. What is your name (e.g. 'Bill', 'Melinda')?");
                    context.Wait(this.MessageReceivedAsync);
                }
                else
                {
                    // Fails the current dialog, removes it from the dialog stack, and returns the exception to the parent/calling dialog.
                    context.Fail(new TooManyAttemptsException("Message was not a string or was an empty string."));
                }
            }
        }
    }

    [Serializable]
    public class AgeDialog : IDialog<int>
    {
        private string name;
        private int attempts = 3;

        public AgeDialog(string name)
        {
            this.name = name;
        }

        public async Task StartAsync(IDialogContext context)
        {
            await context.PostAsync($"{ this.name }, what is your age?");
            context.Wait(this.MessageReceivedAsync);
        }

        private async Task MessageReceivedAsync(IDialogContext context, IAwaitable<IMessageActivity> result)
        {
            var message = await result;

            if (Int32.TryParse(message.Text, out int age) && (age > 0))
            {
                context.Done(age);
            }
            else
            {
                --attempts;
                if (attempts > 0)
                {
                    await context.PostAsync("I'm sorry, I don't understand your reply. What is your age (e.g. '42')?");
                    context.Wait(this.MessageReceivedAsync);
                }
                else
                {
                    context.Fail(new TooManyAttemptsException("Message was not a valid age."));
                }
            }
        }
    }

    public class User
    {
        public string ColEnterpriseId { get; set; }
        public string ColRelease { get; set; }
        public string ColTeam { get; set; }
        public string ColProjectTeam { get; set; }
        public string ColLocation { get; set; }
        public string ColWBS { get; set; }
        public string ColDate1 { get; set; }
        public string ColDate2 { get; set; }
        public string ColDate3 { get; set; }
    }
}