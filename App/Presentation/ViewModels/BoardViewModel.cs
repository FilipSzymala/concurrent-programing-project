using System.Collections.ObjectModel;
using Logic;
using Presentation.Models;

namespace Presentation.ViewModels;

public class BoardViewModel
{
    private readonly BallsService _ballsService;
    
    public ObservableCollection<BallListItem> Balls { get; }
    
    public BoardViewModel(BallsService ballsService)
    {
        _ballsService = ballsService;
        
        Balls = new ObservableCollection<BallListItem>();
        
        GenerateRandomBalls();
    }

    public void GenerateRandomBalls()
    {
        _ballsService.GenerateRandomBalls();
        
        var balls = _ballsService.FetchAllBalls();
        
        foreach (var ball in balls)
        {
            var ballListItem = new BallListItem();
            ballListItem.X = ball.X;
            ballListItem.Y = ball.Y;
            ballListItem.VelocityX = ball.VelocityX;
            ballListItem.VelocityY = ball.VelocityY;
            ballListItem.Diameter = ball.Diameter;
            ballListItem.Color = Avalonia.Media.Color.FromRgb(ball.R, ball.G, ball.B);
            
            
            Balls.Add(ballListItem);
        }
        
    }
    
    public void UpdateBalls()
    {
        var balls = _ballsService.FetchAllBalls();
        
        foreach (var ball in balls)
        {
            var ballListItem = new BallListItem();
            ballListItem.X = ball.X;
            ballListItem.Y = ball.Y;
            ballListItem.VelocityX = ball.VelocityX;
            ballListItem.VelocityY = ball.VelocityY;
            ballListItem.Diameter = ball.Diameter;
            
            Balls.Add(ballListItem);
        }
    }
}