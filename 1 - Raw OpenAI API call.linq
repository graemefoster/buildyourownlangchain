<Query Kind="Statements">
  <NuGetReference Version="1.0.0-beta.17" Prerelease="true">Azure.AI.OpenAI</NuGetReference>
  <NuGetReference>Azure.Identity</NuGetReference>
  <Namespace>Azure</Namespace>
  <Namespace>Azure.AI.OpenAI</Namespace>
  <Namespace>Azure.Core</Namespace>
  <Namespace>Azure.Identity</Namespace>
  <Namespace>Microsoft.Identity.Client</Namespace>
  <Namespace>Newtonsoft.Json</Namespace>
</Query>

//Using the OpenAI Client to simplify API calls to OpenAI service
var ai = new Azure.AI.OpenAI.OpenAIClient(
	new Uri(Util.GetPassword("cog-grfdemo-bot-uri")),
	new AzureKeyCredential(Util.GetPassword("cog-grfdemo-bot")));

//Parameters to alter the 'creativity' of the response!
var options = new ChatCompletionsOptions()
{
	Temperature = 0.5f,
	NucleusSamplingFactor = 0.75f, 
	MaxTokens = 350, 
	FrequencyPenalty = 0, 
	PresencePenalty = 0,
	DeploymentName = "gpt4o"
};

//Send a chat-message to OpenAI to which it will respond
options.Messages.Add(
	new ChatRequestSystemMessage(
	"""
You are a bank teller. 
Tell me what the customer is trying to do from the following options.

INTENTS
-----
Account Information
New Accounts
New Credit Cards
Branch Information
"""));

options.Messages.Add(
	new ChatRequestUserMessage(
	"What's the balance on my account?"
));

//Send the call to OpenAI
await ai.GetChatCompletionsAsync(options).Dump();
