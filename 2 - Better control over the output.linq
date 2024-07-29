<Query Kind="Statements">
  <NuGetReference Version="1.0.0-beta.14" Prerelease="true">Azure.AI.OpenAI</NuGetReference>
  <Namespace>Azure</Namespace>
  <Namespace>Azure.AI.OpenAI</Namespace>
  <Namespace>Azure.Core</Namespace>
  <Namespace>Microsoft.Identity.Client</Namespace>
  <Namespace>Newtonsoft.Json</Namespace>
</Query>

#region OpenAIClient
var ai = new Azure.AI.OpenAI.OpenAIClient(
	new Uri(Util.GetPassword("cog-grfdemo-bot-uri")),
	new AzureKeyCredential(Util.GetPassword("cog-grfdemo-bot")));

var options = new ChatCompletionsOptions() { Temperature = 0.5f, NucleusSamplingFactor = 0.75f, MaxTokens = 350, FrequencyPenalty = 0, PresencePenalty = 0, DeploymentName = "gpt4o"};
#endregion

//The prompt adds a new 'instruction' about the response. This helps to get a more
//deterministic output.
options.Messages.Add(
	new ChatRequestSystemMessage(
	"""
You are a bank teller. 
Tell me what the customer is trying to do from the following options.
Choose only from the following intents. Respond with "Unknown" if you don't know.

INTENTS
-----
Account Information
New Accounts
New Credit Cards
Branch Information
"""));

options.Messages.Add(
	new ChatRequestUserMessage(
	"What's my bank balance?"
));

ai.GetChatCompletions(options).Value.Choices[0].Message.Content.Dump("Intent");

