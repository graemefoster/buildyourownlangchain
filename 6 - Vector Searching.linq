<Query Kind="Statements">
  <NuGetReference Version="1.0.0-beta.14" Prerelease="true">Azure.AI.OpenAI</NuGetReference>
  <NuGetReference Version="11.6.0-beta.3" Prerelease="true">Azure.Search.Documents</NuGetReference>
  <Namespace>Azure</Namespace>
  <Namespace>Azure.AI.OpenAI</Namespace>
  <Namespace>Azure.Core</Namespace>
  <Namespace>Azure.Search.Documents</Namespace>
  <Namespace>Azure.Search.Documents.Models</Namespace>
  <Namespace>Microsoft.Identity.Client</Namespace>
  <Namespace>Newtonsoft.Json</Namespace>
  <Namespace>Newtonsoft.Json.Schema</Namespace>
</Query>

#region Search

var ai = new Azure.AI.OpenAI.OpenAIClient(
	new Uri(Util.GetPassword("cog-grfdemo-bot-uri")),
	new AzureKeyCredential(Util.GetPassword("cog-grfdemo-bot")));

#endregion

//Organisations have tons of unstructured content... How would we leverage OpenAI to help make sense of it...
//The prompt size limits means we cannot pass masses of content into OpenAI prompts.
//
//Retrieval Augemented Generation which uses a technique called Document Cracking to help solve this problem.
// - we break our content down into Chunks
// - use our LLMs ahead of time to calculate embeddings
// - load those embeddings into a database or search engine
//
//We can then search over unstructured content...
//
//There's a lot of interest around the best document cracking techniques... Speak to your friendly Data Scientist.

//var input = "What are the opening hours of our stores on Monday?";
//var input = "What are the colours of the rainbow?";
//var input = "Qu'est ce les coloures dans une rainbow?";
var input = "What is an embedding?";
var embeddings = ai.GetEmbeddings(new EmbeddingsOptions("Ada002Embedding", [input])).Value.Data[0].Embedding;

//Perform the search to find documents 'closest' to the users input.
SearchUsingAzureCognitiveSearch(
	input,
	embeddings)
.Dump();


SearchDocument SearchUsingAzureCognitiveSearch(string input, ReadOnlyMemory<float> embeddings)
{
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