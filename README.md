# Stackdriver Formatter for Serilog

A Stackdriver JSON Formatter for logging with Serilog and .NET.  Useful when a dependency on the Google SDK is not wanted or when a logs are being sent to Stackdriver using a data collector or log shipper (e.g. Fluentd).

## Serilog Sinks

There is no dependency on any particular Serilog Sinks.  Pass in an instance of the `StackdriverJsonFormatter` class into the Sink of your choice.

## Installing

A `netstandard2.0` Nuget package is available [here](https://www.nuget.org/packages/Redbox.Serilog.Stackdriver/).

Or you can install with the dotnet cli:

`dotnet add package Redbox.Serilog.Stackdriver`

## Sample Setup Code

### Directly into a Serilog Instance

```csharp
using Redbox.Serilog.Stackdriver

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
    .Enrich.FromLogContext()
    .WriteTo.Console(new StackdriverJsonFormatter()) // Other sinks can be used to, e.g. File
    .CreateLogger();
```

### In appsettings.json

Be sure to add `.ReadFrom.Configuration(configuration)` to your Serilog setup first!  ([Serilog docs](https://github.com/serilog/serilog-settings-configuration))

```json
"Serilog": {
    "Using": [
        "Serilog.Sinks.Console"
    ],
    "WriteTo": [
    {
        "Name": "Console",
        "Args": {
            "formatter": "Redbox.Serilog.Stackdriver.StackdriverJsonFormatter, Redbox.Serilog.Stackdriver"
        }
    }]
}
```
