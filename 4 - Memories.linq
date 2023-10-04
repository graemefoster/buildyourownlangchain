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
Choose from the following intents. Response with "Unknown" if you don't know.

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


IntentResponse PromptUserForMoreInformation(IntentResponse response)
{
	WriteDiagnostic("Calling OpenAI to get next suggestion to ask user");

	return new IntentResponse(response.userInput, response.intent, CallOpenAI(
		new ChatCompletionsOptions() { Temperature = 0f, NucleusSamplingFactor = 0f, MaxTokens = 100, FrequencyPenalty = 0, PresencePenalty = 0 },
		$"""
You are a bank teller who only talks abount banking. You are trying to find the intent of the customer from the below list of intents.
Ask the customer something to find their intent given the conversation so-far?

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

public record IntentResponse(string userInput, string intent, string suggestedResponse);

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


	//Check the given predicate. If true, then run the next prompt. Else return the original result.
	public static T2 Then<T1, T2>(this T1 input, Func<T1, T2> next)
	{
		return next(input);
	}
}


#endregion

//Many Generative AI applications are stateless, and require multiple inputs from a user.
//Memories are a foundational building block allowing us to persist state between prompting
//
//A good example would be a Chat application, that remembered each input from the user without 
//round-tripping it to the user's machine
void Main()
{
	dc.Dump();
	
	var memory = new SlidingWindowChatMemory();
	memory.StoreMemory("User\n-----\nWhat do I want to do?\n");
	memory.StoreMemory("Assistant\n-----\nCan you please provide more specific information about what you are looking to do.\n");
	memory.StoreMemory("User\n-----\nWhere can I catch a fish\n");

	GetIntent(memory.Retrieve())
	.ThenIf(
		"Check for unknown intent",
		response => response.intent == "Unknown",
		response => PromptUserForMoreInformation(response))
	.Dump();
}

class SlidingWindowChatMemory
{
	private IList<string> _memory = new List<string>();
	public SlidingWindowChatMemory StoreMemory(string memory)
	{
		if (_memory.Count > 10) { _memory.RemoveAt(0); }
		_memory.Add(memory);
		return this;
	}

	public string Retrieve()
	{
		return string.Join(Environment.NewLine, _memory);
	}
}
