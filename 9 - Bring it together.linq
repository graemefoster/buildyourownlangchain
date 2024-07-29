<Query Kind="Program">
  <NuGetReference Version="1.0.0-beta.14" Prerelease="true">Azure.AI.OpenAI</NuGetReference>
  <NuGetReference Prerelease="true">Azure.Search.Documents</NuGetReference>
  <Namespace>Azure</Namespace>
  <Namespace>Azure.AI.OpenAI</Namespace>
  <Namespace>Azure.Core</Namespace>
  <Namespace>Azure.Search.Documents</Namespace>
  <Namespace>Azure.Search.Documents.Models</Namespace>
  <Namespace>Microsoft.Identity.Client</Namespace>
  <Namespace>Newtonsoft.Json</Namespace>
  <Namespace>Newtonsoft.Json.Schema</Namespace>
</Query>

#region Helpers
static OpenAIClient ai = new Azure.AI.OpenAI.OpenAIClient(
	new Uri(Util.GetPassword("cog-grfdemo-bot-uri")),
	new AzureKeyCredential(Util.GetPassword("cog-grfdemo-bot")));

static DumpContainer dc = new DumpContainer();
public static DateTimeOffset start = DateTimeOffset.UtcNow;
public static int diagnosticsIndent = 0;
public static StringBuilder diagnostics = new StringBuilder();
public static void WriteDiagnostic(string diagnostic)
{
	diagnostics.AppendLine($"{new String(' ', 3 * (diagnosticsIndent++))}>> {diagnostic} : {DateTimeOffset.UtcNow.Subtract(start).TotalMilliseconds}ms");
	dc.UpdateContent(diagnostics.ToString());
}

static IntentResponse GetIntent(string userInput)
{
	var openAiOutput = CallOpenAI(
		new ChatCompletionsOptions() { Temperature = 0f, NucleusSamplingFactor = 0f, MaxTokens = 10, FrequencyPenalty = 0, PresencePenalty = 0, DeploymentName = "Gpt35Turbo0613" },
		$"""
You are a bank teller. Work out the intent of the customer given their input.
Choose from the following intents. Use Unknown if you don't know.

INTENTS
-----
Account Information
New Accounts
New Credit Cards
Branch Information
OpenAI Queries
""",
userInput);

	WriteDiagnostic($"Calling OpenAI to detect User Sentiment. Detected: {openAiOutput}");


	return new IntentResponse(
		userInput,
		openAiOutput,
		null);

}

public static string CallOpenAI(ChatCompletionsOptions options, string systemPrompt, string userPrompt  = null)
{
	options.Messages.Add(new ChatRequestSystemMessage(systemPrompt));
	if (!string.IsNullOrWhiteSpace(userPrompt)) options.Messages.Add(new ChatRequestUserMessage(userPrompt));

	return ai.GetChatCompletions(options).Value.Choices[0].Message.Content;
}


IntentResponse PromptUserForMoreIntentInformation(IntentResponse response)
{
	WriteDiagnostic("Calling OpenAI to get next suggestion to ask user");

	return new IntentResponse(response.userInput, response.intent, CallOpenAI(
		new ChatCompletionsOptions() { Temperature = 0f, NucleusSamplingFactor = 0f, MaxTokens = 100, FrequencyPenalty = 0, PresencePenalty = 0, DeploymentName = "Gpt35Turbo0613" },
		$"""
You are a bank teller. You are trying to find the intent of the customer from the below list of intents.
What would you ask the customer next to find their intent, given their current input?
Respond in the user's language.

INTENTS
-----
Account Information
New Accounts
New Credit Cards
Branch Information
Unknown
""",
response.userInput
).Dump("Next Suggestion Response"));

}

IntentResponse PromptUserForMoreInformation(IntentResponse initialResponse, ApiCallResponse apiResponse)
{
	WriteDiagnostic("Calling OpenAI to get next suggestion to ask user");

	var apiCallResponse = CallOpenAI(
			new ChatCompletionsOptions() { Temperature = 0f, NucleusSamplingFactor = 0f, MaxTokens = 100, FrequencyPenalty = 0, PresencePenalty = 0, DeploymentName = "Gpt35Turbo0613" },
		$"""
You are a bank teller. The user is trying to call the function "GetAccountInformation" but is missing some parameters.
What would you ask them next to get the missing information? 

Only ask them to provide information for parameters defined in the function. 
You must respond in the user's language.
""",
initialResponse.userInput
);

	return initialResponse with
	{
		suggestedResponse = apiCallResponse
	};

}


static class PromptChainsEx
{
	//Check the given predicate. If true, then run the next prompt. Else return the original result.
	public static T2 ThenIf<T1, T2>(this T1 input, string friendlyName, Func<T1, bool> predicate, Func<T1, T2> truePrompt, Func<T1, T2> falsePrompt)
	{
		if (predicate(input))
		{
			WriteDiagnostic($"Predicate {friendlyName} returned TRUE. Calling true step");
			return truePrompt(input);
		}
		else
		{
			WriteDiagnostic($"Predicate {friendlyName} returned FALSE. Calling false step");
			return falsePrompt(input);
		}
	}

	//Check the given predicate. If true, then run the next prompt. Else return the original result.
	public static T ThenIf<T>(this T input, string friendlyName, Func<T, bool> predicate, Func<T, T> truePrompt)
	{
		if (predicate(input))
		{
			WriteDiagnostic($"Predicate {friendlyName} returned TRUE. Calling true step");
			return truePrompt(input);
		}
		else
		{
			WriteDiagnostic($"Predicate {friendlyName} returned FALSE.");
			return input;
		}
	}

	//Check the given predicate. If true, then run the next prompt. Else return the original result.
	public static T ElseIf<T>(this T input, string friendlyName, Func<T, bool> predicate, Func<T, T> truePrompt)
	{
		if (predicate(input))
		{
			WriteDiagnostic($"Predicate {friendlyName} returned TRUE. Calling true step");
			return truePrompt(input);
		}
		else
		{
			WriteDiagnostic($"Predicate {friendlyName} returned FALSE");
			return input;
		}
	}

	//Check the given predicate. If true, then run the next prompt. Else return the original result.
	public static T2 Then<T1, T2>(this T1 input, Func<T1, T2> next)
	{
		return next(input);
	}
}

public record IntentResponse(string userInput, string intent, string suggestedResponse);
public record ApiCallResponse(string userInput, Dictionary<string, string> parameters, string suggestedResponse, object apiCallResponse);

FunctionDefinition function = new FunctionDefinition("GetAccountInformation")
{
	Description = "Gets bank account information",
	Parameters = System.BinaryData.FromObjectAsJson(functionParameters)

};

static dynamic functionParameters = new
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
};

ReadOnlyMemory<float> GetTextEmbeddings(string userInput)
{
	WriteDiagnostic("Fetching Text Embeddings for user input");
	return ai.GetEmbeddings(new EmbeddingsOptions("Ada002Embedding", [userInput])).Value.Data[0].Embedding;
}

SearchDocument Search(string input, ReadOnlyMemory<float> embeddings)
{
	WriteDiagnostic("Executing search against Vector Index");

	var searchClient = new Azure.Search.Documents.SearchClient(new Uri("https://srch-grfdemo-bot.search.windows.net"), "info-idx", new AzureKeyCredential(Util.GetPassword("cogsearch")));

	var vectorSearch = new VectorSearchOptions();
	var vectorQuery = new VectorizedQuery(embeddings)
	{
		KNearestNeighborsCount = 3
	};
	vectorQuery.Fields.Add("contentVector");
	vectorSearch.Queries.Add(vectorQuery);

	var searchOptions = new SearchOptions
	{
		Size = 10,
		Select = { "metadata_storage_name", "content" },
		VectorSearch = vectorSearch,
	};
	return searchClient.Search<SearchDocument>(input, searchOptions).Value.GetResults().First().Document;
}

ApiCallResponse GetFunctionCallParameters(string userInput)
{
	WriteDiagnostic("Calling OpenAI to get function parameters");
	var completions = new ChatCompletionsOptions() { Temperature = 0f, NucleusSamplingFactor = 0f, MaxTokens = 100, FrequencyPenalty = 0, PresencePenalty = 0, DeploymentName = "Gpt35Turbo0613" };
	completions.Functions.Add(function);
	completions.Messages.Add(new ChatRequestUserMessage(userInput));

	var response = CallOpenAI(completions, userInput);

	var result = ai.GetChatCompletions(completions).Value.Choices[0].Message;
	var arguments = JsonConvert.DeserializeObject<Dictionary<string, string>>(result.FunctionCall.Arguments);
	return new ApiCallResponse(userInput, arguments, null, null);

}

static IntentResponse PresentResults(IntentResponse intent, SearchDocument result)
{
	WriteDiagnostic("Calling OpenAI to present search result");

	var openAiOutput = CallOpenAI(
		new ChatCompletionsOptions() { Temperature = 0f, NucleusSamplingFactor = 0f, MaxTokens = 100, FrequencyPenalty = 0, PresencePenalty = 0, DeploymentName = "Gpt35Turbo0613" },
		$"""
You are an expert summariser. Extract the relevant content to answer the customer's question from the excerpt below, and summarise it.
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

ApiCallResponse CallFunction(ApiCallResponse input)
{
	WriteDiagnostic($"Calling API to retrieve {input.parameters["service"]}");
	return input.parameters["service"] switch
	{
		"balance" => input with { apiCallResponse = new { account = input.parameters["accountNumber"], balance = 123456m } },
		"payments" => input with { apiCallResponse = new { account = input.parameters["accountNumber"], payments = new[] { new { date = DateTime.Now.AddDays(-7), amount = 1021.12m }, new { date = DateTime.Now.AddDays(-7), amount = 21.65m }, } } },
		_ => throw new NotSupportedException()
	};
}

static IntentResponse PresentApiResults(IntentResponse intent, ApiCallResponse response)
{
	WriteDiagnostic("Presenting API call result");

	var openAiOutput = CallOpenAI(
		new ChatCompletionsOptions() { Temperature = 0f, NucleusSamplingFactor = 0f, MaxTokens = 100, FrequencyPenalty = 0, PresencePenalty = 0, DeploymentName = "Gpt35Turbo0613" },
		$"""
You are a bank teller. You received the following data in response to a user's ask.
Format the data in a concise way to answers the user's query. Respond in the user's language.

DATA
------
{JsonConvert.SerializeObject(response.apiCallResponse, Newtonsoft.Json.Formatting.Indented)}

USER INPUT
------
{intent.userInput}
""");

	return intent with
	{
		suggestedResponse = openAiOutput
	};
}

#endregion

void Main()
{
	dc.Dump();

	GetIntent("What time does my branch open on Friday?")
	//GetIntent("What is an embedding?")
	//GetIntent("What is the balance on my account?")
	//GetIntent("What are the last few payments on my account 123456789?")
#region More Interesting prompts
	//GetIntent("¿Dónde están los peces?")
	//GetIntent("¿Cuál es el saldo de mi cuenta 123456789?")
	//GetIntent("¿Cuál es el saldo de mi cuenta?")
	//GetIntent("What is the balance on my account 123456789?")
	//GetIntent("À quelle heure ouvert la succursale a vendredi?")
#endregion
	.ThenIf("Unknown intent detected",
		response => response.intent == "Unknown",
		response => PromptUserForMoreIntentInformation(response))
		
	.ElseIf("RAG intent detected",
		response => response.intent == "Branch Information" || response.intent == "OpenAI Queries",
		response =>
			GetTextEmbeddings(response.userInput)
			.Then(embeddings => Search(response.userInput, embeddings))
			.Then(searchResults => PresentResults(response, searchResults)))
			
	.ElseIf("Account Information intent detected",
		response => response.intent == "Account Information",
		response =>
			GetFunctionCallParameters(response.userInput)
			.ThenIf(
				"Check for missing parameters",
				response => response.parameters.Count != 2,
				apiResponse => PromptUserForMoreInformation(response, apiResponse),
				apiResponse => CallFunction(apiResponse)
							   .Then(apiResults => PresentApiResults(response, apiResults))))
	.Dump();
}
