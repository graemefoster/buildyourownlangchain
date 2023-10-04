<Query Kind="Program">
  <NuGetReference Version="1.0.0-beta.6" Prerelease="true">Azure.AI.OpenAI</NuGetReference>
  <NuGetReference Prerelease="true">Azure.Search.Documents</NuGetReference>
  <Namespace>Azure</Namespace>
  <Namespace>Azure.AI.OpenAI</Namespace>
  <Namespace>Azure.Core</Namespace>
  <Namespace>Microsoft.Identity.Client</Namespace>
  <Namespace>Newtonsoft.Json</Namespace>
  <Namespace>Newtonsoft.Json.Schema</Namespace>
  <Namespace>Azure.Search.Documents.Models</Namespace>
  <Namespace>Azure.Search.Documents</Namespace>
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
General Information
My Account Details
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

""").Dump("Next Suggestion Response"));

}



static class PromptChainsEx
{
	//Check the given predicate. If true, then run the next prompt. Else return the original result.
	public static T ThenIf<T>(this T input, string friendlyName, Func<T, bool> predicate, Func<T, T> truePrompt, Func<T, T> falsePrompt)
	{
		if (predicate(input))
		{
			WriteDiagnostic($"Predicate {friendlyName} returned TRUE. Calling truePrompt");
			return truePrompt(input);
		}
		else
		{
			WriteDiagnostic($"Predicate {friendlyName} returned FALSE. Calling falsePrompt");
			return falsePrompt(input);
		}
	}
	
	//Check the given predicate. If true, then run the next prompt. Else return the original result.
	public static T2 Then<T1, T2>(this T1 input, Func<T1, T2> next)
	{
		return next(input);
	}
}

public record IntentResponse(string userInput, string intent, string suggestedResponse);

//Steps in LangChain or Semantic Kernel can interact with other systems, or just execute language code.
//This example uses a step which can call Cognitive Search.
//We then use the LLM to extract the relevant information from the document chunk, and present the result back to the user.

SearchDocument Search(string input, IReadOnlyList<float> embeddings)
{
	WriteDiagnostic("Executing search against Vector Index");

	var searchClient = new Azure.Search.Documents.SearchClient(new Uri("https://srch-grfdemo-bot.search.windows.net"), "info-idx", new AzureKeyCredential(Util.GetPassword("cogsearch")));
	var vector = new SearchQueryVector { KNearestNeighborsCount = 3, Value = embeddings };
	vector.Fields.Add("contentVector");
	var searchOptions = new SearchOptions
	{
		Size = 1,
		Select = { "metadata_storage_name", "content" },
	};
	searchOptions.Vectors.Add(vector);
	return searchClient.Search<SearchDocument>(input, searchOptions).Value.GetResults().First().Document;
}


IReadOnlyList<float> GetTextEmbeddings(string userInput)
{
	WriteDiagnostic("Fetching Text Embeddings for user input");
	return ai.GetEmbeddings("Ada002Embedding", new EmbeddingsOptions(userInput)).Value.Data[0].Embedding;
}

#endregion

// Chains can call Open AI but at the end of the day they just use simple functions.
// So we can incorporate any function calls we need into our chains.
// In this example we run a Vector Search using embeddings we got from Azure Open AI.
// We then ask OpenAI to present the important bit of the search result, given the context of the User's query.
void Main()
{
	dc.Dump();

	//GetIntent("What time does my branch open on Friday?")
	//GetIntent("À quelle heure ouvert la succursale a vendredi?")
	GetIntent("Cuéntame sobre Exposición al polvo?")
	.ThenIf(
		"Check for unknown intent", 
		response => response.intent == "Unknown", 
		response => PromptUserForMoreInformation(response),
		response => 
			GetTextEmbeddings(response.userInput)
			.Then(embeddings => Search(response.userInput, embeddings))
			.Then(searchResults => PresentResults(response, searchResults)))
	.Dump();
}

static IntentResponse PresentResults(IntentResponse intent, SearchDocument result)
{
	WriteDiagnostic("Calling OpenAI to present search result");

	var openAiOutput = CallOpenAI(
		new ChatCompletionsOptions() { Temperature = 0f, NucleusSamplingFactor = 0f, MaxTokens = 200, FrequencyPenalty = 0, PresencePenalty = 0 },
		$"""
You are a bank teller. Extract the relevant content from the excerpt below and summarise it.
Only use the content below. If you do not know the answer then explain why.
You must respond in the user's input language.

EXCERPT
------
{result["content"]}

USER INPUT
------
{intent.userInput}

""");

	return intent with
	{
		suggestedResponse = openAiOutput
	};

}

