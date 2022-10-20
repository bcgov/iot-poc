using IotApis.Data;
using IotApis.Service;
using IotCommon;
using Microsoft.Azure.Cosmos;
using Microsoft.EntityFrameworkCore;
using IotApis.HttpClients;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var weatherConnString = builder.Configuration.GetConnectionString(Constants.WeatherDBConnString);
builder.Services.AddDbContextFactory<WeatherContext>(optionsBuilder =>
  optionsBuilder
    .UseCosmos(
      connectionString: weatherConnString,
      databaseName: Constants.WeatherDB,
      cosmosOptionsAction: options =>
      {
          options.ConnectionMode(ConnectionMode.Direct);
      }));

var cameraConnString = builder.Configuration.GetConnectionString(Constants.CameraDBConnString);
builder.Services.AddDbContextFactory<CameraContext>(optionsBuilder =>
  optionsBuilder
    .UseCosmos(
      connectionString: cameraConnString,
      databaseName: Constants.CameraDB,
      cosmosOptionsAction: options =>
      {
          options.ConnectionMode(ConnectionMode.Direct);
      }));

builder.Services.AddScoped<IDeviceTelemetryService, DeviceTelemetryService>();
builder.Services.AddScoped<IDeviceImageDataService, DeviceImageDataService>();
builder.Services.AddHttpClients(builder.Configuration);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
