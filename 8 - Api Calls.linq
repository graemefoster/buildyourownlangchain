<Query Kind="Program">
  <NuGetReference Version="1.0.0-beta.6" Prerelease="true">Azure.AI.OpenAI</NuGetReference>
  <Namespace>Azure</Namespace>
  <Namespace>Azure.AI.OpenAI</Namespace>
  <Namespace>Azure.Core</Namespace>
  <Namespace>Microsoft.Identity.Client</Namespace>
  <Namespace>Newtonsoft.Json</Namespace>
  <Namespace>Newtonsoft.Json.Schema</Namespace>
</Query>

static OpenAIClient ai = new Azure.AI.OpenAI.OpenAIClient(
	new Uri("https://cog-grfdemo-bot.openai.azure.com/"),
	new AzureKeyCredential(Util.GetPassword("openai")));

void Main()
{

	var completions = new ChatCompletionsOptions() { Temperature = 0f, NucleusSamplingFactor = 0f, MaxTokens = 100, FrequencyPenalty = 0, PresencePenalty = 0 };

	//Use a JSON Schema to 'tell' OpenAI about the shape of the function we want to call.
	completions.Functions.Add(GetFunctionDefinition());

	//Call Open AI. The response will now contain parameters we can use to call the function.
	completions.Messages.Add(new ChatMessage(ChatRole.User, $"""
What's the balance on account?
"""));

	var output = ai.GetChatCompletions("Gpt35Turbo0613", completions).Value.Choices[0].Message.Dump();
}

FunctionDefinition GetFunctionDefinition()
{
	return new FunctionDefinition("GetAccountInformation")
	{
		Description = "Gets bank account information",
		Parameters = System.BinaryData.FromObjectAsJson(
		new
		{
			type = "object",
			properties = new
			{
				accountNumber = new
				{
					type = "string",
					description = "The bank account number"
				},
				service = new
				{
					type = "string",
					@enum = new string[]
					{
						"balance",
						"details",
						"payments",
						"schedules",
						"unknown"
					},
					description = "What the user wants to do"
				}
			}
		}
)
	};
}