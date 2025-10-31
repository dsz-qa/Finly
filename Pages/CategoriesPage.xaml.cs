using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Finly.Services;

namespace Finly.Views
{
    public partial class CategoriesPage : UserControl
    {
        public CategoriesPage()
        {
            InitializeComponent();
            Loaded += (_, __) => Reload();
            Unloaded += (_, __) => CategoryService.CategoriesChanged -= Reload;

            // nasłuchuj globalnego sygnału "Kategorie się zmieniły"
            CategoryService.CategoriesChanged += Reload;
        }

        private void Reload()
        {
            var uid = UserService.CurrentUserId;
            if (uid <= 0)
            {
                CategoryGrid.ItemsSource = Array.Empty<object>();
                return;
            }

            var data = CategoryService.GetCategorySummary(uid)
                .Select(x => new { x.Name, x.Total })
                .ToList();

            CategoryGrid.ItemsSource = data;
        }
    }
}