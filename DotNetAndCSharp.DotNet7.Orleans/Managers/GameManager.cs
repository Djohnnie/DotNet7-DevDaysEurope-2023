﻿using DotNet7AndCSharp11.Orleans.Grains;
using DotNet7AndCSharp11.Orleans.Helpers;
using DotNet7AndCSharp11.OrleansContracts;
using PlayerState = DotNet7AndCSharp11.OrleansContracts.PlayerState;

namespace DotNet7AndCSharp11.Orleans.Managers;

public class GameManager
{
    public const int SnakeGameWidth = 30;
    public const int SnakeGameHeight = 16;
    public const int SnakeLength = 5;

    private readonly IGrainFactory _grainFactory;
    private readonly GameCodeHelper _gameCodeHelper;

    public GameManager(
        IGrainFactory grainFactory,
        GameCodeHelper gameCodeHelper)
    {
        _grainFactory = grainFactory;
        _gameCodeHelper = gameCodeHelper;
    }

    public async Task<CreateGameResponse> CreateGame(CreateGameRequest request)
    {
        var gameCode = string.Empty;

        var gamesGrain = _grainFactory.GetGrain<IGamesGrain>(Guid.Empty);

        do
        {
            gameCode = _gameCodeHelper.GenerateGameCode();
        }
        while (await gamesGrain.GameCodeExists(gameCode));

        var game = new Game
        {
            Code = gameCode,
            IsActive = true,
            FoodData = new Food { Bites = new List<Bite>() }.ToFoodData()
        };

        var player = new Player
        {
            Name = request.HostPlayerName,
            SnakeData = Snake.RandomSnake(SnakeGameWidth, SnakeGameHeight, SnakeLength).ToSnakeData()
        };

        await gamesGrain.CreateGame(gameCode, player);

        return new CreateGameResponse(gameCode);
    }

    public async Task<JoinGameResponse?> JoinGame(JoinGameRequest request)
    {
        var gamesGrain = _grainFactory.GetGrain<IGamesGrain>(Guid.Empty);
        var gameCode = await gamesGrain.GetGame(request.GameCode);

        if (gameCode == null)
        {
            return null;
        }

        var player = new Player
        {
            Name = request.PlayerName,
            SnakeData = Snake.RandomSnake(SnakeGameWidth, SnakeGameHeight, SnakeLength).ToSnakeData()
        };

        var gameGrain = _grainFactory.GetGrain<IGameGrain>(gameCode);
        await gameGrain.AddPlayer(player);

        return new JoinGameResponse(gameCode);
    }

    public async Task<ReadyPlayerResponse> ReadyPlayer(ReadyPlayerRequest request)
    {
        var gameGrain = _grainFactory.GetGrain<IGameGrain>(request.GameCode);
        await gameGrain.ReadyPlayer(request.PlayerName);

        return new ReadyPlayerResponse();
    }

    public async Task<GetActiveGamesResponse> GetActiveGames()
    {
        var gamesGrain = _grainFactory.GetGrain<IGamesGrain>(Guid.Empty);
        var activeGames = await gamesGrain.GetActiveGames();

        return new GetActiveGamesResponse(
            activeGames.Select(x => new ActiveGame(x.Code, x.IsReady, x.Players.Select(p => new ActivePlayer(p.Name, p.IsReady, p.SnakeData)).ToList(), x.FoodData)).ToList());
    }

    public async Task<Orientation> GetPlayerOrientation(string gameCode, string playerName, Orientation current)
    {
        var gameGrain = _grainFactory.GetGrain<IGameGrain>(gameCode);
        var orientation = await gameGrain.GetPlayerOrientation(playerName);

        if (orientation != null)
        {
            return orientation.Value;
        }

        return current;
    }

    public async Task<AbandonResponse> Abandon(AbandonRequest request)
    {
        var gamesGrain = _grainFactory.GetGrain<IGamesGrain>(Guid.Empty);
        await gamesGrain.AbandonPlayer(request.GameCode, request.PlayerName);

        return new AbandonResponse();
    }

    public async Task UpdatePlayerStates(ActiveGame activeGame, List<PlayerState> playerStates)
    {
        var gameGrain = _grainFactory.GetGrain<IGameGrain>(activeGame.GameCode);
        await gameGrain.UpdatePlayerStates(playerStates);
    }

    public async Task UpdateFood(ActiveGame activeGame, Food food)
    {
        var gameGrain = _grainFactory.GetGrain<IGameGrain>(activeGame.GameCode);
        await gameGrain.UpdateFood(food.ToFoodData());
    }
}