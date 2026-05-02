using Tunvix.Models;
using Tunvix.PageModels;

namespace Tunvix.Pages
{
    public partial class MainPage : ContentPage
    {
        public MainPage(MainPageModel model)
        {
            InitializeComponent();
            BindingContext = model;
        }
    }
}