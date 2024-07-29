<Query Kind="Statements">
  <NuGetReference Version="1.0.0-beta.17" Prerelease="true">Azure.AI.OpenAI</NuGetReference>
  <NuGetReference>Azure.Identity</NuGetReference>
  <NuGetReference>MathNet.Numerics</NuGetReference>
  <Namespace>Azure</Namespace>
  <Namespace>Azure.AI.OpenAI</Namespace>
  <Namespace>Azure.Core</Namespace>
  <Namespace>Azure.Core.Pipeline</Namespace>
  <Namespace>Azure.Identity</Namespace>
  <Namespace>Microsoft.Identity.Client</Namespace>
  <Namespace>Newtonsoft.Json</Namespace>
  <Namespace>System.Net</Namespace>
  <Namespace>System.Net.Http</Namespace>
  <Namespace>System.Threading.Tasks</Namespace>
</Query>

var client = new Azure.AI.OpenAI.OpenAIClient(
	new Uri(Util.GetPassword("cog-grfdemo-bot-uri")),
	new AzureKeyCredential(Util.GetPassword("cog-grfdemo-bot"))
);

const string AgentOneSystemMessage =
"""
Your agent name is AGENT-ONE.
You are playing a game of guess-my-number with AGENT-TWO.

In the first game, you have the number 53 in your mind. AGENT-TWO will try to guess it.
If AGENT-TWO guesses too high, say 'too high', if AGENT-TWO guesses too low, say 'too low'. If it's correct say 'correct'.

All responses have to be valid JSON

The format must be:
{
	"from": "<your-agent-name>",
	"target": "<agent-who-your-message-is-for>",
	"message": "too high"
}

""";

const string AgentTwoSystemMessage =
"""
Your agent name is AGENT-TWO.
You are playing a game of guess-my-number with AGENT-ONE.

AGENT-ONE has a number in its mind, and you will try to guess it. You can only guess a single number.
If AGENT-ONE says 'too high', you should guess a lower number. If AGENT-ONE says 'too low' you should guess a higher number. 

Start the game off by guessing a number.

All responses have to be JSON

The format must be:
{
	"from": "<your-agent-name>",
	"target": "<agent-who-your-message-is-for>",
	"message": "Is it 17?"
}

""";

var threads = new Dictionary<string, List<string>>();
threads["AGENT-ONE"] = new List<string>();
threads["AGENT-TWO"] = new List<string>();


#region Azure Open AI Calls
async Task<Request> CallAgentONE(IEnumerable<string> messages)
{
	var options = new ChatCompletionsOptions()
	{
		Temperature = .75f,
		MaxTokens = 350,
		FrequencyPenalty = 0,
		PresencePenalty = 0,
		ChoiceCount = 1,
		DeploymentName = "gpt4o",
		User = "graeme",
		ResponseFormat = Azure.AI.OpenAI.ChatCompletionsResponseFormat.JsonObject
	};

	options.Messages.Add(new ChatRequestSystemMessage(AgentOneSystemMessage));

	foreach (var message in messages)
	{
		options.Messages.Add(new ChatRequestUserMessage(message));
	}

	var responseText = (await client.GetChatCompletionsAsync(options)).Value.Choices[0].Message.Content;
	var response = JsonConvert.DeserializeObject<Request>(responseText);
	return response;
}

async Task<Request> CallAgentTWO(IEnumerable<string> messages)
{
	var options = new ChatCompletionsOptions()
	{
		Temperature = .75f,
		MaxTokens = 350,
		FrequencyPenalty = 0,
		PresencePenalty = 0,
		ChoiceCount = 1,
		DeploymentName = "gpt4o",
		User = "graeme",
		ResponseFormat = Azure.AI.OpenAI.ChatCompletionsResponseFormat.JsonObject
	};

	options.Messages.Add(new ChatRequestSystemMessage(AgentTwoSystemMessage));

	foreach (var message in messages)
	{
		options.Messages.Add(new ChatRequestUserMessage(message));
	}

	var responseText = (await client.GetChatCompletionsAsync(options)).Value.Choices[0].Message.Content;
	var response = JsonConvert.DeserializeObject<Request>(responseText);
	return response;
}

var callAgent = async (Request request, Func<IEnumerable<string>, Task<Request>> callAgent) =>
{
	threads[request.Target].Add($"{request.From}: {request.Message}");
	var response = await callAgent(threads["AGENT-ONE"]);
	threads[request.Target].Add($"ME: {response.Message}");
	return response;
};

var agents = new Dictionary<string, Func<Request, Task<Request>>>();
agents["AGENT-ONE"] = s => callAgent(s, CallAgentONE);
agents["AGENT-TWO"] = s => callAgent(s, CallAgentTWO);

#endregion


var nextRequest = new Request
{
	From = "AGENT-ONE",
	Message = "What number am I thinking of?",
	Target = "AGENT-TWO"
};

do
{
	$"{nextRequest.From} -> {nextRequest.Target}: {nextRequest.Message}".Dump();
	nextRequest = await agents[nextRequest.Target](nextRequest);
} while (nextRequest.Message != "correct");

$"{nextRequest.From}: {nextRequest.Message}".Dump();


class Request
{
	public required string From { get; set; }
	public required string Target { get; set; }
	public required string Message { get; set; }
}
