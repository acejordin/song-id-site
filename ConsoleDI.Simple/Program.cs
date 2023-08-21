using song_id;
using System.Threading.Channels;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((hostContext, services) => 
    { 
        services.AddSingleton(Channel.CreateUnbounded<int>(new UnboundedChannelOptions() { SingleReader = true }));
        services.AddSingleton(svc => svc.GetRequiredService<Channel<int>>().Reader);
        services.AddSingleton(svc => svc.GetRequiredService<Channel<int>>().Writer);
        services.AddSingleton<MyService>();
        //services.AddSingleton(svc => svc.GetRequiredService<MyService>());
        //services.AddHostedService<WriterService>();
        //services.AddHostedService<ReaderService>();
        services.AddHostedService(svc => svc.GetRequiredService<MyService>());
        services.AddHostedService<MyServiceReader>();
        //services.AddHostedService<SongIdService>();
        //services.AddSingleton<MyService>();
        //services.AddSingleton(svc => svc.GetRequiredService<MyService>());
        services.Configure<SongIdServiceOptions>(hostContext.Configuration.GetSection(nameof(SongIdServiceOptions)));
    })
    .Build();

await host.RunAsync();

class MyService : BackgroundService
{
    private readonly ILogger<MyService> _logger;
    public int Count { get; private set; }

    public MyService(ILogger<MyService> logger)
    {
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken = default)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(1000, stoppingToken);
            Count++;
            _logger.LogInformation("MyService doing something!");
        }
    }
}

class MyServiceReader : BackgroundService
{
    private readonly ILogger<MyService> _logger;
    private readonly MyService _myService;

    public MyServiceReader(ILogger<MyService> logger, MyService myService)
    {
        _logger = logger;
        _myService = myService;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken = default)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(1000, stoppingToken);
            _logger.LogInformation($"MyServiceReader doing something! Count: {_myService.Count}");
        }
    }
}

class WriterService : BackgroundService
{
    private readonly ChannelWriter<int> _channelWriter;
    private readonly ILogger<MyService> _logger;

    public WriterService(ChannelWriter<int> channelWriter, ILogger<MyService> logger)
    {
        _channelWriter = channelWriter;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        int count = 0;
        while(!cancellationToken.IsCancellationRequested)
        {
            await Task.Delay(1000, cancellationToken);
            _logger.LogInformation($"write data {count}");
            await _channelWriter.WriteAsync(count++, cancellationToken);
        }
    }
}

class ReaderService : BackgroundService
{
    private readonly ChannelReader<int> _channelReader;
    private readonly ILogger<MyService> _logger;

    public ReaderService(ChannelReader<int> channelReader, ILogger<MyService> logger)
    {
        _channelReader = channelReader;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        while(!cancellationToken.IsCancellationRequested) 
        {
            await Task.Delay(1000, cancellationToken);
            try
            {
                var result = await _channelReader.ReadAsync(cancellationToken);
                _logger.LogInformation($"read data {result}");
            }
            catch(ChannelClosedException) 
            {
                _logger.LogError("channel was closed");
            }
        }
    }
}