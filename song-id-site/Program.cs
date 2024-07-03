using song_id;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();

//bind config section with strongly typed class
builder.Services.Configure<SongIdServiceOptions>(builder.Configuration.GetSection(nameof(SongIdServiceOptions)));
//add SongIdService as a singleton for injection
builder.Services.AddSingleton<SongIdService>();
//also add that same SongIdService as a hosted service because it's also a BackgroundService
builder.Services.AddHostedService(svc => svc.GetRequiredService<SongIdService>());

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseRouting();

app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.Run();
