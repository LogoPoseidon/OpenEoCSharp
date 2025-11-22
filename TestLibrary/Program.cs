using OpenEoClientLib.Models;

const string url = "https://openeo.dataspace.copernicus.eu";

var clientId = Environment.GetEnvironmentVariable("COPERNICUS_CLIENT_ID")!;
var clientSecret = Environment.GetEnvironmentVariable("COPERNICUS_CLIENT_SECRET")!;

try 
{
    var connection = await OpenEo.Connect(url, clientId, clientSecret);
    
    var me = await connection.Client.Me.GetAsMeGetResponseAsync();

    Console.WriteLine($"User ID: {me?.UserId}");
}
catch (Exception ex)
{
    Console.WriteLine($"Error: {ex.Message}");
}