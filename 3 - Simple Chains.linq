<Query Kind="Program">
  <NuGetReference Version="1.0.0-beta.5" Prerelease="true">Azure.AI.OpenAI</NuGetReference>
  <Namespace>Azure</Namespace>
  <Namespace>Azure.AI.OpenAI</Namespace>
  <Namespace>Azure.Core</Namespace>
  <Namespace>Microsoft.Identity.Client</Namespace>
  <Namespace>Newtonsoft.Json</Namespace>
</Query>

#region Helpers
static OpenAIClient ai = new Azure.AI.OpenAI.OpenAIClient(
	new Uri("https://cog-grfdemo-bot.openai.azure.com/"),
	new AzureKeyCredential(Util.GetPassword("openai")));


static DumpContainer dc = new DumpContainer();
public static int diagnosticsIndent = 0;
public static StringBuilder diagnostics = new StringBuilder();
public static void WriteDiagnostic(string diagnostic)
{
	diagnostics.AppendLine($"{new String(' ', 3 * (diagnosticsIndent++))}>> {diagnostic}");
	dc.UpdateContent(diagnostics.ToString());
}

static IntentResponse GetIntent(string userInput)
{
	var openAiOutput = CallOpenAI(
		new ChatCompletionsOptions() { Temperature = 0f, NucleusSamplingFactor = 0f, MaxTokens = 10, FrequencyPenalty = 0, PresencePenalty = 0 },
		$"""
You are a bank teller. Work out the intent of the customer given their input.
Choose from the following intents. Use Unknown if you don't know.

INTENTS
-----
Account Information
New Accounts
New Credit Cards
Branch Information
Unknown

USER INPUT
------
{userInput}

""");

	WriteDiagnostic($"Calling OpenAI to detect User Sentiment. Detected: {openAiOutput}");

	return new IntentResponse(
		userInput,
		openAiOutput,
		null);

}

public static string CallOpenAI(ChatCompletionsOptions options, string prompt)
{
	options.Messages.Add(new ChatMessage(
		ChatRole.System,
		prompt));

	return ai.GetChatCompletions("Gpt35Turbo0613", options).Value.Choices[0].Message.Content;
}

public record IntentResponse(string userInput, string intent, string suggestedResponse);


#endregion

//We have restrictions on the size of the prompts we can send to a LLM.
//The larger your prompts become the less likely you'll get appropriate outputs.
//So a practice of building smaller succint prompts, and chaining them together is appearing.
//LangChain and Semantic Kernel are good examples of libaries that do this.
// gpt-35-turbo: 4096
// gpt-35-turbo-16k: 16384
// gpt-4: gpt-4
// gpt-4-32k: 32768
void Main()
{
	dc.Dump();

	//GetIntent("What do I want to do?")
	GetIntent("What is my account balance?")
	.ThenIf(
		"Check for unknown intent",
		response => response.intent == "Unknown", 
		response => PromptUserForMoreInformation(response))
	.Dump();
}

static class PromptChainsEx
{
	//Check the given predicate. If true, then run the next prompt. Else return the original result.
	public static T ThenIf<T>(this T input, string friendlyName, Func<T, bool> predicate, Func<T, T> nextPrompt)
	{
		if (predicate(input))
		{
			WriteDiagnostic($"Predicate {friendlyName} returned TRUE. Calling next step");
			return nextPrompt(input);
		}
		else
		{
			WriteDiagnostic($"Predicate {friendlyName} returned FALSE. Returning initial step");
			return input;
		}
	}
}

IntentResponse PromptUserForMoreInformation(IntentResponse response)
{
	WriteDiagnostic("Calling OpenAI to get next suggestion to ask user");

	return new IntentResponse(response.userInput, response.intent, CallOpenAI(
		new ChatCompletionsOptions() { Temperature = 0f, NucleusSamplingFactor = 0f, MaxTokens = 100, FrequencyPenalty = 0, PresencePenalty = 0 },
		$"""
You are a bank teller. You are trying to find the intent of the customer from the below list of intents.
What would you ask the customer next to find their intent, given their current input?

INTENTS
-----
Account Information
New Accounts
New Credit Cards
Branch Information
Unknown

USER INPUT
------
{response.userInput}

"""));

}
