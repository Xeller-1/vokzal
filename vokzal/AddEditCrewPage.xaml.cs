using System;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace vokzal
{
    public partial class AddEditCrewPage : Page
    {
        private TrainCrews _currentCrew;

        public string PageTitle => _currentCrew.CrewID == 0 ? "Добавление бригады" : "Редактирование бригады";
        public TrainCrews CurrentCrew => _currentCrew;

        public AddEditCrewPage(TrainCrews selectedCrew)
        {
            InitializeComponent();
            _currentCrew = selectedCrew ?? new TrainCrews();
            LoadComboBoxes();
            DataContext = this;
        }

        private void LoadComboBoxes()
        {
            try
            {
                var context = VokzalEntities.GetContext();

                // Загрузка сотрудников
                var employees = context.Employees
                    .Include("Positions")
                    .ToList();
                EmployeeComboBox.ItemsSource = employees;

                // Загрузка поездов
                var trains = context.Trains.ToList();
                TrainComboBox.ItemsSource = trains;

                // Если редактирование, устанавливаем текущие значения
                if (_currentCrew.CrewID != 0)
                {
                    EmployeeComboBox.SelectedValue = _currentCrew.EmployeeID;
                    TrainComboBox.SelectedValue = _currentCrew.TrainID;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки данных: {ex.Message}", "Ошибка");
            }
        }

        private void SaveBtn_Click(object sender, RoutedEventArgs e)
        {
            StringBuilder errors = new StringBuilder();

            if (EmployeeComboBox.SelectedItem == null)
                errors.AppendLine("Выберите сотрудника");

            if (TrainComboBox.SelectedItem == null)
                errors.AppendLine("Выберите поезд");

            if (errors.Length > 0)
            {
                MessageBox.Show(errors.ToString(), "Ошибка валидации");
                return;
            }

            try
            {
                var context = VokzalEntities.GetContext();

                if (_currentCrew.CrewID == 0)
                {
                    context.TrainCrews.Add(_currentCrew);
                }

                context.SaveChanges();
                MessageBox.Show("Бригада сохранена успешно!", "Успех");
                Manager.MainFrame.GoBack();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка сохранения: {ex.Message}", "Ошибка");
            }
        }

        private void CancelBtn_Click(object sender, RoutedEventArgs e)
        {
            Manager.MainFrame.GoBack();
        }
    }
}