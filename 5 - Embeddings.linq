<Query Kind="Statements">
  <NuGetReference Version="1.0.0-beta.6" Prerelease="true">Azure.AI.OpenAI</NuGetReference>
  <NuGetReference Prerelease="true">Azure.Search.Documents</NuGetReference>
  <Namespace>Azure</Namespace>
  <Namespace>Azure.AI.OpenAI</Namespace>
  <Namespace>Azure.Core</Namespace>
  <Namespace>Microsoft.Identity.Client</Namespace>
  <Namespace>Newtonsoft.Json</Namespace>
  <Namespace>Newtonsoft.Json.Schema</Namespace>
  <Namespace>Azure.Search.Documents</Namespace>
  <Namespace>Azure.Search.Documents.Models</Namespace>
</Query>

var ai = new Azure.AI.OpenAI.OpenAIClient(
	new Uri("https://cog-grfdemo-bot.openai.azure.com/"),
	new AzureKeyCredential(Util.GetPassword("openai")));


//Imagine if you could represent meaning using vectors (numbers with direction).....
//Where semantically similar words had similar vectors
//And the same words from different languages had similar vectors...

var input = "What are the opening hours of our stores on Monday?";
var embeddings = ai.GetEmbeddings("Ada002Embedding", new EmbeddingsOptions(input)).Value.Data[0].Embedding;
embeddings.Take(20).Dump();

