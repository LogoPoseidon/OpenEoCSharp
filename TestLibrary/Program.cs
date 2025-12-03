using Client.Generated.Jobs;
using Client.Generated.Models;
using OpenEoClientLib.Models;

const string url = "https://openeo.dataspace.copernicus.eu";

var clientId = Environment.GetEnvironmentVariable("COPERNICUS_CLIENT_ID")!;
var clientSecret = Environment.GetEnvironmentVariable("COPERNICUS_CLIENT_SECRET")!;

try 
{
    var connection = await OpenEo.Connect(url, clientId, clientSecret);
    
    var collections = await connection.Client.Collections.GetAsCollectionsGetResponseAsync();

    foreach (var col in collections?.Collections ?? [])
    {
        Console.WriteLine($"{col.Id}: {col.Title}");
    }
    var processGraph = new Dictionary<string, object>
    {
        ["loadcollection"] = new {
            process_id = "load_collection",
            arguments = new {
                id = "SENTINEL2_L2A",
                spatial_extent = new { 
                    west = 9.88, south = 49.77, east = 9.98, north = 49.84
                },
                temporal_extent = new[] { "2024-06-01", "2024-06-30" },
                bands = new[] { "B04", "B03", "B02" }
            },
            result = false
        },
        ["save"] = new {
            process_id = "save_result",
            arguments = new {
                data = new { from_node = "loadcollection" },
                format = "GTiff"
            },
            result = true
        }
    };
    var process = new Process_graph()
    {
        AdditionalData = processGraph,
    };
    var jobRequest = new JobsPostRequestBody
    {
        Process = new Process_graph_with_metadata
        {
            ProcessGraph = process
        }
    };

    await connection.Client.Jobs.PostAsync(jobRequest);

    var x = await connection.Client.Jobs.GetAsJobsGetResponseAsync();
    foreach (var job in x.Jobs)
    {
        if (job.Status == Batch_job_status.Created)
        {
            await connection.Client.Jobs[job.Id].Results.PostAsync();
            var resultsMetadata = await connection.Client.Jobs[job.Id].Results.GetAsResultsGetResponseAsync();

        }
    }
    

}
catch (Exception ex)
{
    Console.WriteLine($"Error: {ex.Message}");
}