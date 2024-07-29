<Query Kind="Statements">
  <NuGetReference Version="1.0.0-beta.14" Prerelease="true">Azure.AI.OpenAI</NuGetReference>
  <NuGetReference Version="1.10.4">Azure.Identity</NuGetReference>
  <NuGetReference Prerelease="true">Azure.Search.Documents</NuGetReference>
  <Namespace>Azure</Namespace>
  <Namespace>Azure.AI.OpenAI</Namespace>
  <Namespace>Azure.Core</Namespace>
  <Namespace>Azure.Search.Documents</Namespace>
  <Namespace>Azure.Search.Documents.Models</Namespace>
  <Namespace>Microsoft.Identity.Client</Namespace>
  <Namespace>Newtonsoft.Json</Namespace>
  <Namespace>Newtonsoft.Json.Schema</Namespace>
  <Namespace>Azure.Identity</Namespace>
</Query>

var ai = new Azure.AI.OpenAI.OpenAIClient(
	new Uri(Util.GetPassword("cog-grfdemo-bot-uri")),
	new AzureKeyCredential(Util.GetPassword("cog-grfdemo-bot"))
);

var options = new ChatCompletionsOptions()
{
	Temperature = 0,
	MaxTokens = 1000,
	FrequencyPenalty = 0,
	PresencePenalty = 0,
	DeploymentName = "gpt4o",
	ResponseFormat = Azure.AI.OpenAI.ChatCompletionsResponseFormat.JsonObject,
};

//var userQuery = "The user wants to know the balance on account 12312312";
var userQuery = "The user wants to know what time their branch will open on Sunday".Dump("User Query");

options.Messages.Add(new ChatRequestSystemMessage(
	$$"""
Given a USER GOAL, you must output a plan how to achieve the goal.
The plan must be VALID JSON. Use the "EXAMPLE OUTPUT" to see an example.

RULES
-------
You DO NOT need to use all skills.
All inputs to a skill in the plan MUST be either extracted from the user goal, or be OUTPUTS from a preceding skill.
All parameters on the input to a skill MUST be provided.
Required skills MUST be ordered using the executionOrder property.

USER GOAL
-------
{{userQuery}}

VALID PARAMETERS
-------
input:Context
input:UserInput
<step-execution-order>:outputs:<output-name>

SKILLS
-------
[
  {
    "skillName": "LockedDownBotSemanticKernel.Skills.Foundational.SummariseContent.SummariseContentFunction+Function",
    "description": "Given a user question, and some content which is the result of a search, this function will answer the user's question from the provided content.",
	"type": "prompt",
    "input": {
      "type": "LockedDownBotSemanticKernel.Skills.Foundational.SummariseContent.SummariseContentFunction+Input",
      "parameters": [
        {
          "name": "Context",
          "type": "String",
          "description": "Operating Context"
        },
        {
          "name": "OriginalAsk",
          "type": "String",
          "description": "What the user asked"
        },
        {
          "name": "Content",
          "type": "String",
          "description": "Content to source response from"
        }
      ]
    },
    "output": {
      "type": "LockedDownBotSemanticKernel.Skills.Foundational.SummariseContent.SummariseContentFunction+Output",
      "parameters": [
        {
          "name": "Summarisation",
          "type": "String",
          "description": "Response to users question"
        }
      ]
    }
  },
  {
    "skillName": "LockedDownBotSemanticKernel.Skills.EnterpriseSearch.SearchBranchInformation+Function",
    "description": "Executes a vector Embeddings search against account branch information content.",
	"type": "search",
    "input": {
      "type": "LockedDownBotSemanticKernel.Skills.EnterpriseSearch.SearchBranchInformation+Input",
      "parameters": [
        {
          "name": "Embeddings",
          "type": "Single[]",
          "description": "Embeddings that come from the LockedDownBotSemanticKernel.Skills.Foundational.GetEmbeddings.GetEmbeddingsFunction+Function skill"
        }
      ]
    },
    "output": {
      "type": "LockedDownBotSemanticKernel.Skills.EnterpriseSearch.SearchBranchInformation+Output",
      "parameters": [
        {
          "name": "OriginalInput",
          "type": "Input",
          "description": "Original Input"
        },
        {
          "name": "Result",
          "type": "String",
          "description": "Best Search Result from index"
        }
      ]
    }
  },
  {
    "skillName": "LockedDownBotSemanticKernel.Skills.Foundational.GetEmbeddings.GetEmbeddingsFunction+Function",
    "description": "Fetches embeddings that can be used to execute a vector search",
	"type": "embeddings",
    "input": {
      "type": "LockedDownBotSemanticKernel.Skills.Foundational.GetEmbeddings.GetEmbeddingsFunction+Input",
      "parameters": [
        {
          "name": "Content",
          "type": "String",
          "description": "Key terms to create Embeddings for"
        }
      ]
    },
    "output": {
      "type": "LockedDownBotSemanticKernel.Skills.Foundational.GetEmbeddings.GetEmbeddingsFunction+Output",
      "parameters": [
        {
          "name": "Content",
          "type": "String",
          "description": "Original content"
        },
        {
          "name": "Embeddings",
          "type": "Single[]",
          "description": "A list of embeddings"
        }
      ]
    }
  },
  {
    "skillName": "LockedDownBotSemanticKernel.Skills.Foundational.ExtractKeyTerms.ExtractKeyTermsFunction+Function",
    "description": "Extracts key terms from a longer User Input.",
	"type": "prompt",
    "input": {
      "type": "LockedDownBotSemanticKernel.Skills.Foundational.ExtractKeyTerms.ExtractKeyTermsFunction+Input",
      "parameters": [
        {
          "name": "Context",
          "type": "String",
          "description": ""
        },
        {
          "name": "UserInput",
          "type": "String",
          "description": ""
        }
      ]
    },
    "output": {
      "type": "LockedDownBotSemanticKernel.Skills.Foundational.ExtractKeyTerms.ExtractKeyTermsFunction+Output",
      "parameters": [
        {
          "name": "KeyTerms",
          "type": "String[]",
          "description": ""
        }
      ]
    }
  },
  {
    "skillName": "LockedDownBotSemanticKernel.Skills.Functions.FunctionCalling.ExtractInformationToCallFunction+Function",
    "description": "Given user input and context, and a function definition, will extract the parameters to call an API with.",
	"type": "prompt",
    "input": {
      "type": "LockedDownBotSemanticKernel.Skills.Functions.FunctionCalling.ExtractInformationToCallFunction+Input",
      "parameters": [
        {
          "name": "Context",
          "type": "String",
          "description": "Operating Context"
        },
        {
          "name": "UserInput",
          "type": "String",
          "description": "Conversation"
        },
        {
          "name": "FunctionDefinition",
          "type": "JsonSchemaFunctionInput",
          "description": "JSON Schema of function"
        }
      ]
    },
    "output": {
      "type": "LockedDownBotSemanticKernel.Skills.Functions.FunctionCalling.ExtractInformationToCallFunction+Output",
      "parameters": [
        {
          "name": "Context",
          "type": "String",
          "description": "Operating Context"
        },
        {
          "name": "UserInput",
          "type": "String",
          "description": "Conversation"
        },
        {
          "name": "FunctionDefinition",
          "type": "JsonSchemaFunctionInput",
          "description": "JSON Schema of function"
        },
        {
          "name": "MatchedAllInputParameters",
          "type": "Boolean",
          "description": "If all parameters were matched"
        },
        {
          "name": "MissingParameters",
          "type": "HashSet`1",
          "description": "Missing parameters"
        },
        {
          "name": "ParameterValues",
          "type": "Dictionary`2",
          "description": "Values of parameters"
        },
        {
          "name": "NextRecommendation",
          "type": "String",
          "description": "What to ask user to get the missing parameters"
        }
      ]
    }
  },
  {
    "skillName": "LockedDownBotSemanticKernel.BespokeSkills.GetAccountDetails.GetAccountDetailsFunction+Function",
    "description": "Fetches a customer's account details given their Account Number in JSON format.",
	"type": "API",
    "input": {
      "type": "LockedDownBotSemanticKernel.BespokeSkills.GetAccountDetails.GetAccountDetailsFunction+Input",
      "parameters": [
        {
          "name": "AccountNumber",
          "type": "String",
          "description": "Account Number"
        }
      ]
    },
    "output": {
      "type": "LockedDownBotSemanticKernel.BespokeSkills.GetAccountDetails.GetAccountDetailsFunction+Output",
      "parameters": [
        {
          "name": "AccountName",
          "type": "String",
          "description": "Account Name"
        },
        {
          "name": "AvailableBalance",
          "type": "Decimal",
          "description": "Available Balance"
        },
        {
          "name": "Balance",
          "type": "Decimal",
          "description": "Account Balance"
        }
      ]
    },,
  {
    "skillName": "LockedDownBotSemanticKernel.BespokeSkills.GetAccountDetails.FindUsersAccounts+Function",
    "description": "Finds the current customer's account numbers.",
	"type": "API",
    "input": {
      "type": "LockedDownBotSemanticKernel.BespokeSkills.GetAccountDetails.FindUsersAccounts+Input",
      "parameters": [
      ]
    },
    "output": {
      "type": "LockedDownBotSemanticKernel.BespokeSkills.GetAccountDetails.FindUsersAccounts+Output",
      "parameters": [
	    {
		  "name": "Accounts",
		  "type": "Array",
		  "description": "An array of account numbers owned by the customer"
		}
      ]
    }
  }
]


EXAMPLE OUTPUT
-----
{
  "skills": [
    {
      "skill": "LockedDownBotSemanticKernel.Skills.Foundational.ExtractKeyTerms.ExtractKeyTermsFunction+Function",
      "executionOrder": <execution-order>,
	  "reason": "Why are you calling this skill?",
      "inputs": {
        "UserInput": "inputs:UserInput",
        "Context": "inputs:Context"
      },
      "outputs": [
        "<execution-order>:outputs:KeyTermsString",
        "<execution-order>:outputs:KeyTerms"
      ]
    }
  ]
}
"""));

var dw = new DumpContainer();
dw.Dump();
var sb = new StringBuilder();
await foreach (var completion in await ai.GetChatCompletionsStreamingAsync(options)) {
	if (!string.IsNullOrWhiteSpace(completion.ContentUpdate)) sb.Append(completion.ContentUpdate);
	dw.UpdateContent(sb.ToString());
}

