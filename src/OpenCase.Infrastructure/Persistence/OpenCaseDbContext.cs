using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using OpenCase.Domain.Entities;

namespace OpenCase.Infrastructure.Persistence;

public class OpenCaseDbContext(DbContextOptions<OpenCaseDbContext> options) : DbContext(options)
{
    public DbSet<Card> Cards => Set<Card>();
    public DbSet<Pawn> Pawns => Set<Pawn>();
    public DbSet<Room> Rooms => Set<Room>();
    public DbSet<Player> Players => Set<Player>();
    public DbSet<Game> Games => Set<Game>();
    public DbSet<GameEvent> GameEvents => Set<GameEvent>();
    public DbSet<Hint> Hints => Set<Hint>();
    public DbSet<BoardTemplate> BoardTemplates => Set<BoardTemplate>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(OpenCaseDbContext).Assembly);
    }

    internal static ValueConverter<T, string> JsonConverter<T>() where T : new() =>
        new(
            value => JsonSerializer.Serialize(value, JsonSerializerOptions.Default),
            json => JsonSerializer.Deserialize<T>(json, JsonSerializerOptions.Default) ?? new T());

    internal static ValueComparer<T> JsonComparer<T>() where T : new() =>
        new(
            (a, b) => JsonSerializer.Serialize(a, JsonSerializerOptions.Default)
                == JsonSerializer.Serialize(b, JsonSerializerOptions.Default),
            v => JsonSerializer.Serialize(v, JsonSerializerOptions.Default).GetHashCode(),
            v => JsonSerializer.Deserialize<T>(
                JsonSerializer.Serialize(v, JsonSerializerOptions.Default), JsonSerializerOptions.Default)!);
}
