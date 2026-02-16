using System;
using System.Data.Entity;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace vokzal
{
    public partial class CrewManagementPage : Page
    {
        public CrewManagementPage()
        {
            InitializeComponent();
            LoadCrews();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            LoadCrews();
        }

        public void LoadCrews()
        {
            try
            {
                var context = VokzalEntities.GetContext();

                var crews = context.TrainCrews
                    .Include(c => c.Employees)
                    .Include(c => c.Employees.Positions)
                    .Include(c => c.Trains)
                    .ToList();

                CrewListView.ItemsSource = crews;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки данных: {ex.Message}", "Ошибка");
            }
        }

        private void AddCrewBtn_Click(object sender, RoutedEventArgs e)
        {
            var page = new AddEditCrewPage(null);
            Manager.MainFrame.Navigate(page);
        }

        private void EditCrewBtn_Click(object sender, RoutedEventArgs e)
        {
            var selectedCrew = CrewListView.SelectedItem as TrainCrews;
            if (selectedCrew != null)
            {
                var page = new AddEditCrewPage(selectedCrew);
                Manager.MainFrame.Navigate(page);
            }
            else
            {
                MessageBox.Show("Выберите бригаду для редактирования", "Внимание");
            }
        }

        private void DeleteCrewBtn_Click(object sender, RoutedEventArgs e)
        {
            var selectedCrew = CrewListView.SelectedItem as TrainCrews;
            if (selectedCrew != null)
            {
                if (MessageBox.Show("Удалить выбранную бригаду?", "Подтверждение",
                    MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                {
                    try
                    {
                        var context = VokzalEntities.GetContext();
                        context.TrainCrews.Remove(selectedCrew);
                        context.SaveChanges();
                        LoadCrews();
                        MessageBox.Show("Бригада удалена успешно!", "Успех");
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ошибка удаления: {ex.Message}", "Ошибка");
                    }
                }
            }
            else
            {
                MessageBox.Show("Выберите бригаду для удаления", "Внимание");
            }
        }
    }
}