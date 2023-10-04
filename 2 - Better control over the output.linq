<Query Kind="Statements">
  <NuGetReference Version="1.0.0-beta.5" Prerelease="true">Azure.AI.OpenAI</NuGetReference>
  <Namespace>Azure</Namespace>
  <Namespace>Azure.AI.OpenAI</Namespace>
  <Namespace>Azure.Core</Namespace>
  <Namespace>Microsoft.Identity.Client</Namespace>
  <Namespace>Newtonsoft.Json</Namespace>
</Query>

#region OpenAIClient
var ai = new Azure.AI.OpenAI.OpenAIClient(
	new Uri("https://cog-grfdemo-bot.openai.azure.com/"), 
	new AzureKeyCredential(Util.GetPassword("openai")));

var options = new ChatCompletionsOptions() { Temperature = 0.5f, NucleusSamplingFactor = 0.75f, MaxTokens = 350, FrequencyPenalty = 0, PresencePenalty = 0,};
#endregion

//The prompt adds a new 'instruction' about the response. This helps to get a more
//deterministic output.
options.Messages.Add(new ChatMessage(
	ChatRole.System, 
	"""
You are a bank teller. Tell me what the customer is trying to do.
Choose only from the following intents. Respond with "Unknown" if you don't know.

INTENTS
-----
Account Information
New Accounts
New Credit Cards
Branch Information

USER INPUT
------
What's my fish?

"""));

ai.GetChatCompletions("Gpt35Turbo0613", options).Value.Choices[0].Message.Content.Dump("Intent");

