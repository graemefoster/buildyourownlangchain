<Query Kind="Program">
  <NuGetReference Version="1.0.0-beta.14" Prerelease="true">Azure.AI.OpenAI</NuGetReference>
  <NuGetReference>Azure.Identity</NuGetReference>
  <Namespace>Azure</Namespace>
  <Namespace>Azure.AI.OpenAI</Namespace>
  <Namespace>Azure.Core</Namespace>
  <Namespace>Azure.Identity</Namespace>
  <Namespace>Microsoft.Identity.Client</Namespace>
  <Namespace>Newtonsoft.Json</Namespace>
  <Namespace>System.Threading.Tasks</Namespace>
</Query>

#region Helpers
static OpenAIClient ai = new Azure.AI.OpenAI.OpenAIClient(
	new Uri(Util.GetPassword("cog-grfdemo-bot-uri")),
	new AzureKeyCredential(Util.GetPassword("cog-grfdemo-bot")));

static DumpContainer dc = new DumpContainer();
public static int diagnosticsIndent = 0;
public static StringBuilder diagnostics = new StringBuilder();
public static void WriteDiagnostic(string diagnostic)
{
	diagnostics.AppendLine($"{new String(' ', 3 * (diagnosticsIndent++))}>> {diagnostic}");
	dc.UpdateContent(diagnostics.ToString());
}

static IntentResponse GetIntent(ChatRequestMessage[] memory)
{
	var openAiOutput = CallOpenAI(
		new ChatCompletionsOptions() { Temperature = 0f, NucleusSamplingFactor = 0f, MaxTokens = 10, FrequencyPenalty = 0, PresencePenalty = 0, DeploymentName = "Gpt35Turbo0613" },
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
""",
memory);

	WriteDiagnostic($"Calling OpenAI to detect User Sentiment. Detected: {openAiOutput}");

	return new IntentResponse(
		memory,
		openAiOutput,
		null);

}

public static string CallOpenAI(ChatCompletionsOptions options, string systemPrompt, ChatRequestMessage[] memory)
{
	options.Messages.Add(new ChatRequestSystemMessage(systemPrompt));
	
	foreach (var mem in memory)	
	{	
		options.Messages.Add(mem);
	}

	return ai.GetChatCompletions(options).Value.Choices[0].Message.Content;
}


IntentResponse PromptUserForMoreInformation(IntentResponse response)
{
	WriteDiagnostic("Calling OpenAI to get next suggestion to ask user");

	return new IntentResponse(response.memory, response.intent, CallOpenAI(
		new ChatCompletionsOptions() { Temperature = 0f, NucleusSamplingFactor = 0f, MaxTokens = 100, FrequencyPenalty = 0, PresencePenalty = 0, DeploymentName = "Gpt35Turbo0613" },
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
""",
response.memory));

}

public record IntentResponse(ChatRequestMessage[] memory, string intent, string suggestedResponse);

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
	memory.StoreMemory(new ChatRequestUserMessage("What do I want to do?"));
	memory.StoreMemory(new ChatRequestAssistantMessage("Can you please provide more specific information about what you are looking to do?"));
	memory.StoreMemory(new ChatRequestUserMessage("Where can I catch a fish?"));
	//memory.StoreMemory(new ChatRequestUserMessage("What's my account balance?"));

	GetIntent(memory.Retrieve())
	.ThenIf(
		"Check for unknown intent",
		response => response.intent == "Unknown",
		response => PromptUserForMoreInformation(response))
	.Dump();
}

class SlidingWindowChatMemory
{
	private IList<ChatRequestMessage> _memory = new List<ChatRequestMessage>();
	public SlidingWindowChatMemory StoreMemory(ChatRequestMessage memory)
	{
		if (_memory.Count > 10) { _memory.RemoveAt(0); }
		_memory.Add(memory);
		return this;
	}

	public ChatRequestMessage[] Retrieve()
	{
		return _memory.ToArray();
	}
}
