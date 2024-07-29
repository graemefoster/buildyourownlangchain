<Query Kind="Statements">
  <NuGetReference Version="1.0.0-beta.17" Prerelease="true">Azure.AI.OpenAI</NuGetReference>
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

var ai = new Azure.AI.OpenAI.OpenAIClient(
	new Uri(Util.GetPassword("cog-grfdemo-bot-uri")),
	new AzureKeyCredential(Util.GetPassword("cog-grfdemo-bot")));

//Imagine if you could represent meaning using vectors (numbers with direction).....
//Where semantically similar words had similar vectors
//And the same words from different languages had similar vectors...

var input = "What are the opening hours of our stores on Monday?";
var input2 = "Tell me about fish?";

var embeddingsResponse = await ai.GetEmbeddingsAsync(new EmbeddingsOptions("Ada002Embedding", [input, input2]));
var embeddings = embeddingsResponse.Value.Data[0].Embedding;
embeddings.Slice(0, 20).Dump();

embeddingsResponse.GetRawResponse().Headers.Dump();

