var builder = DistributedApplication.CreateBuilder(args);

// PostgreSQL con persistencia de datos entre reinicios
var postgres = builder.AddPostgres("postgres")
    .WithLifetime(ContainerLifetime.Persistent)
    .WithDataVolume("ceiba-postgres-data");

// Base de datos Ceiba
var database = postgres.AddDatabase("ceiba");

// Aplicación web Blazor Server
var web = builder.AddProject<Projects.Ceiba_Web>("ceiba-web")
    .WithReference(database)
    .WaitFor(database)
    .WithExternalHttpEndpoints();

builder.Build().Run();
