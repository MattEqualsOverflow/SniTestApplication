using Grpc.Core;
using Grpc.Net.Client;
using Serilog;
using SNI;

const bool CheckMemory = false;
const bool CheckFilesystem = true;

Log.Logger = new LoggerConfiguration()
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.Debug()
    .CreateLogger();

var channel = GrpcChannel.ForAddress(new Uri("http://localhost:8191"));
var devicesClient = new Devices.DevicesClient(channel);
var memoryClient = new DeviceMemory.DeviceMemoryClient(channel);
var filesClient = new DeviceFilesystem.DeviceFilesystemClient(channel);

_ = GetDevice();

Console.ReadLine();

async Task GetDevice()
{
    while (true)
    {
        try
        {
            Log.Information("Waiting for device");
            var deviceResponse = await devicesClient.ListDevicesAsync(new DevicesRequest(),
                new CallOptions(deadline: DateTime.UtcNow.AddSeconds(3)));

            if (deviceResponse.Devices.Any())
            {
                var device = deviceResponse.Devices.First();
                Log.Information("Connecting to device {Name}", device.DisplayName);

                if (CheckMemory)
                {
                    await SendMessages(device.Uri);
                }
                else if (CheckFilesystem)
                {
                    await GetFileSystem(device.Uri);
                }
            }
        }
        catch (Exception e)
        {
            Log.Error(e, "Error with ListDevicesAsync call");
        }
        
        await Task.Delay(TimeSpan.FromSeconds(5));
    }
}

async Task GetFileSystem(string deviceAddress)
{
    Log.Information("Reading directory");
    
    
    var results = await filesClient.ReadDirectoryAsync(new ReadDirectoryRequest()
    {
        Uri = deviceAddress,
        Path = ""
    });
    
    Log.Information("Files: {Files}", string.Join(", ", results.Entries.Select(x => x.Name)));
    
}

async Task SendMessages(string deviceAddress)
{
    while (true)
    {
        Log.Information("Sending ReadMemoryRequest");
        var response = await memoryClient.SingleReadAsync(new SingleReadMemoryRequest()
        {
            Uri = deviceAddress,
            Request = new ReadMemoryRequest()
            {
                RequestAddress = 0xF50010,
                RequestAddressSpace = AddressSpace.FxPakPro,
                RequestMemoryMapping = MemoryMapping.ExHiRom,
                Size = 1
            }
        }, new CallOptions(deadline: DateTime.UtcNow.AddSeconds(3)));
        var state = (GameStates)response.Response.Data[0];
        Log.Information("Game State: {State}", state);
        await Task.Delay(TimeSpan.FromSeconds(3));
    }
}

enum GameStates
{
    RetroArchMenuOrStartup,
    GameSelect,
    CopyPlayer,
    ErasePlayer,
    NamePlayer,
    LoadingGame,
    PreDungeon,
    Dungeon,
    PreOverworld,
    Overworld,
    PreSpecialOverworld,
    SpecialOverworld,
    Unknown,
    BlankScreen,
    Text,
    ClosingSpotlight,
    OpeningSpotlight,
    FallingDownHole,
    Death,
    BossVictory,
    History,
    MagicMirror,
    RefillStatsAfterboss,
    SaveAndQuit,
    GanonExitsAga,
    TriforceRoom,
    EndSequence,
    Select
}