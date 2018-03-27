using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;

using System.Data.Common;
using System.Configuration;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;
using Bot_Application1.Services;

namespace Bot_Application1.Dialogs
{
    [Serializable]
    public class RootDialog : IDialog<object>
    {
        private string name;
        private int age;

        public async Task StartAsync(IDialogContext context)
        {
            Debug.Print("Entered RootDialog.StartAsync");
            // Wait until the first message is received from the conversation,
            // then call MessageReceviedAsync to process that message.
            context.Wait(this.MessageReceivedAsync);
            Debug.Print("Leaving RootDialog.StartAsync");
        }

        private async Task MessageReceivedAsync(IDialogContext context, IAwaitable<IMessageActivity> result)
        {
            Debug.Print("Entered RootDialog.MessageReceivedAsync");
            // When MessageReceivedAsync is called, it passes IAwaitable<IMessageActivity>.
            // Await the result (code suspended until the awaited tasks are completed) to get message.
            var message = await result;
            await this.SendWelcomeMessageAsync(context);
            Debug.Print("Leaving RootDialog.MessageReceivedAsync");
        }

        private async Task GetWBSCode(IDialogContext context, IAwaitable<string>result)
        {
            var activity = await result;

            var mongo = new MongoDbService();
            var wbsResult = await mongo.getWbsCode(result.ToString());

            var response = wbsResult != null ? String.Format("WBS code for {0} is: {1}", result.ToString(), wbsResult.code) : "No code was found. Please check the name provided";
            await context.PostAsync(response);

            context.Wait(MessageReceivedAsync);
        }

        private async Task SendWelcomeMessageAsync(IDialogContext context)
        {
            Debug.Print("Entered RootDialog.SendWelcomeMessageAsync");
            await context.PostAsync("Hello, I'm C3PMO. How can I be of service?");
            context.Call(new NameDialog(), this.NameDialogResumeAfter);
            Debug.Print("Leaving RootDialog.SendWelcomeMessageAsync");
        }

        private async Task NameDialogResumeAfter(IDialogContext context, IAwaitable<string> result)
        {
            Debug.Print("Entered RootDialog.NameDialogResumeAfter");
            try
            {
                this.name = await result;
                //context.Call(new AgeDialog(this.name), this.AgeDialogResumeAfter);
                // SQL
                string wbsCode = this.ReadFromSQL(name);

                name = name.First().ToString().ToUpper() + name.Substring(1);

                await context.PostAsync($"Hello { name }, your WBS code {wbsCode}.");
            }
            catch (TooManyAttemptsException)
            {
                await context.PostAsync("I'm sorry, I'm having issues understanding you. Let's try again.");
                await this.SendWelcomeMessageAsync(context);
            }
            Debug.Print("Leaving RootDialog.NameDialogResumeAfter");
        }

        private async Task AgeDialogResumeAfter(IDialogContext context, IAwaitable<int> result)
        {
            Debug.Print("Entered RootDialog.AgeDialogResumeAfter");
            try
            {
                this.age = await result;
                await context.PostAsync($"Your name is { name } and your age is { age }.");

                // SQL
                //List<string> listString = this.ReadFromSQL(name);
                //foreach (string item in listString)
                //{
                //    await context.PostAsync($"Item: {item}.");
                //}
            }
            catch (TooManyAttemptsException)
            {
                await context.PostAsync("I'm sorry, I'm having issues understanding you. Let's try again.");
            }
            finally
            {
                await this.SendWelcomeMessageAsync(context);
            }
            Debug.Print("Leaving RootDialog.AgeDialogResumeAfter");
        }

        // SQL
        private string ReadFromSQL(string name)
        {
            Debug.Print("Entered ReadFromSQL");
            string str = "has not been found";

            string provider = ConfigurationManager.AppSettings["provider"];
            string connectionString = ConfigurationManager.AppSettings["connectionString"];

            DbProviderFactory factory = DbProviderFactories.GetFactory(provider);

            using (DbConnection connection = factory.CreateConnection())
            {
                if (connection == null)
                {
                    Debug.Print("Connection Error");
                    return null;
                }

                connection.ConnectionString = connectionString;

                connection.Open();

                DbCommand command = factory.CreateCommand();

                if (command == null)
                {
                    Debug.Print("Command Error");
                    return null;
                }

                command.Connection = connection;

                command.CommandText = $"SELECT * FROM tableExample WHERE LOWER(colEnterpriseID) LIKE '%{name}%'";

                using (DbDataReader dataReader = command.ExecuteReader())
                {
                    while (dataReader.Read())
                    {
                        str = $"is {dataReader["colWBS"]}";
                        break;
                    }
                }
            }

            Debug.Print("Leaving ReadFromSQL");
            return str;
        }
    }

    [Serializable]
    public class NameDialog : IDialog<string>
    {
        private int attempts = 3;

        public async Task StartAsync(IDialogContext context)
        {
            Debug.Print("Entered NameDialog.StartAsync");
            await context.PostAsync("What is your name?");
            context.Wait(this.MessageReceivedAsync);
            Debug.Print("Leaving NameDialog.StartAsync");
        }

        private async Task MessageReceivedAsync(IDialogContext context, IAwaitable<IMessageActivity> result)
        {
            Debug.Print("Entered NameDialog.MessageReceivedAsync");
            var message = await result;

            bool isAlphaBet = Regex.IsMatch(message.Text, "[a-z]", RegexOptions.IgnoreCase);

            Debug.Print("isAlphaBet: " + isAlphaBet);

            // If message returns a valid name, return it to the calling dialog
            if ((message.Text != null) && (message.Text.Trim().Length > 0) && isAlphaBet)
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
            Debug.Print("Leaving NameDialog.MessageReceivedAsync");
        }
    }

    [Serializable]
    public class AgeDialog : IDialog<int>
    {
        private string name;
        private int attempts = 3;

        public AgeDialog(string name)
        {
            Debug.Print("Entered AgeDialog.AgeDialog");
            this.name = name;
            Debug.Print("Leaving AgeDialog.AgeDialog");
        }

        public async Task StartAsync(IDialogContext context)
        {
            Debug.Print("Entered AgeDialog.StartAsync");
            await context.PostAsync($"{ this.name }, what is your age?");
            context.Wait(this.MessageReceivedAsync);
            Debug.Print("Leaving AgeDialog.StartAsync");
        }

        private async Task MessageReceivedAsync(IDialogContext context, IAwaitable<IMessageActivity> result)
        {
            Debug.Print("Entered AgeDialog.MessageReceivedAsync");
            var message = await result;

            int age;

            if (Int32.TryParse(message.Text, out age) && (age > 0))
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
            Debug.Print("Leaving AgeDialog.MessageReceivedAsync");
        }
    }
}