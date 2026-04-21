namespace Presentation.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    public BoardViewModel Board { get; }

    public MainWindowViewModel(BoardViewModel boardViewModel)
    {
        Board = boardViewModel;
    }
}