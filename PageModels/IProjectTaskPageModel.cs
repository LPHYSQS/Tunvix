using CommunityToolkit.Mvvm.Input;
using Tunvix.Models;

namespace Tunvix.PageModels
{
    public interface IProjectTaskPageModel
    {
        IAsyncRelayCommand<ProjectTask> NavigateToTaskCommand { get; }
        bool IsBusy { get; }
    }
}