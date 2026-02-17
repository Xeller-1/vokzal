using System;
using System.Collections.Generic;
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
                var grouped = context.TrainCrews
                    .Include(c => c.Employees)
                    .Include(c => c.Employees.Positions)
                    .Include(c => c.Trains)
                    .AsEnumerable()
                    .GroupBy(c => c.TrainID)
                    .Select(g => new CrewListItem
                    {
                        TrainId = g.Key,
                        TrainNumber = g.First().Trains?.TrainNumber ?? "Неизвестно",
                        CrewTitle = $"Бригада поезда {g.First().Trains?.TrainNumber}",
                        MembersCount = g.Count(),
                        MembersSummary = string.Join(", ", g
                            .OrderBy(x => x.Employees?.LastName)
                            .Select(x => $"{x.Employees?.FullName} ({x.Employees?.Positions?.PositionName})"))
                    })
                    .OrderBy(x => x.TrainNumber)
                    .ToList();

                CrewListView.ItemsSource = grouped;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки данных: {ex.Message}", "Ошибка");
            }
        }

        private void AddCrewBtn_Click(object sender, RoutedEventArgs e)
        {
            Manager.MainFrame.Navigate(new AddEditCrewPage(null));
        }

        private void EditCrewBtn_Click(object sender, RoutedEventArgs e)
        {
            var selectedCrew = CrewListView.SelectedItem as CrewListItem;
            if (selectedCrew == null)
            {
                MessageBox.Show("Выберите бригаду для редактирования", "Внимание");
                return;
            }

            Manager.MainFrame.Navigate(new AddEditCrewPage(selectedCrew.TrainId));
        }

        private void DeleteCrewBtn_Click(object sender, RoutedEventArgs e)
        {
            var selectedCrew = CrewListView.SelectedItem as CrewListItem;
            if (selectedCrew == null)
            {
                MessageBox.Show("Выберите бригаду для удаления", "Внимание");
                return;
            }

            if (MessageBox.Show("Удалить всю бригаду этого поезда?", "Подтверждение",
                MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
            {
                return;
            }

            try
            {
                var context = VokzalEntities.GetContext();
                var members = context.TrainCrews.Where(c => c.TrainID == selectedCrew.TrainId).ToList();

                if (!members.Any())
                {
                    MessageBox.Show("Бригада уже удалена", "Информация");
                    LoadCrews();
                    return;
                }

                context.TrainCrews.RemoveRange(members);
                context.SaveChanges();
                LoadCrews();
                MessageBox.Show("Бригада удалена успешно!", "Успех");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка удаления: {ex.Message}", "Ошибка");
            }
        }

        public class CrewListItem
        {
            public int TrainId { get; set; }
            public string TrainNumber { get; set; }
            public string CrewTitle { get; set; }
            public string MembersSummary { get; set; }
            public int MembersCount { get; set; }
        }
    }
}
