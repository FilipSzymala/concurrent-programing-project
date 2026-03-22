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
        
        UpdateBalls();
    }
    
    public void UpdateBalls()
    {
        var balls = _ballsService.FetchAllBalls();
        
        foreach (var ball in balls)
        {
            var ballListItem = new BallListItem();
            ballListItem.X = ball.X;
            ballListItem.Y = ball.Y;
            ballListItem.Diameter = ball.Diameter;
            
            Balls.Add(ballListItem);
        }
    }
}