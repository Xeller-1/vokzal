using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace vokzal
{
    public partial class AddEditCrewPage : Page
    {
        private static readonly string[] RestrictedPositionNames =
        {
            "Охранник",
            "Начальник вокзала",
            "Кассир",
            "Ремонтник подвижного состава",
            "Ремонтник путей",
            "Работник справочной службы",
            "Работник службы подготовки составов",
            "Диспетчер",
            "Дежурный по станции"
        };

        private readonly int? _editingTrainId;
        private List<Employees> _availableEmployees = new List<Employees>();
        private List<Employees> _selectedMembers = new List<Employees>();

        public string PageTitle => _editingTrainId.HasValue
            ? "Редактирование состава бригады"
            : "Создание бригады";

        public AddEditCrewPage(int? editingTrainId)
        {
            InitializeComponent();
            _editingTrainId = editingTrainId;
            DataContext = this;
            LoadData();
        }

        private void LoadData()
        {
            try
            {
                var context = VokzalEntities.GetContext();

                TrainComboBox.ItemsSource = context.Trains
                    .OrderBy(t => t.TrainNumber)
                    .ToList();

                _availableEmployees = context.Employees
                    .Include(e => e.Positions)
                    .ToList()
                    .Where(IsAllowedCrewEmployee)
                    .OrderBy(e => e.LastName)
                    .ThenBy(e => e.FirstName)
                    .ToList();

                if (_editingTrainId.HasValue)
                {
                    TrainComboBox.SelectedValue = _editingTrainId.Value;
                    TrainComboBox.IsEnabled = false;

                    _selectedMembers = context.TrainCrews
                        .Where(c => c.TrainID == _editingTrainId.Value)
                        .Include(c => c.Employees)
                        .Include(c => c.Employees.Positions)
                        .Select(c => c.Employees)
                        .ToList();
                }

                RefreshMembersList();
                RefreshEmployeeCombo();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки данных: {ex.Message}", "Ошибка");
            }
        }

        private bool IsAllowedCrewEmployee(Employees employee)
        {
            var positionName = employee.Positions?.PositionName?.Trim();
            return !string.IsNullOrWhiteSpace(positionName)
                   && !RestrictedPositionNames.Any(r =>
                       string.Equals(r, positionName, StringComparison.OrdinalIgnoreCase));
        }

        private void RefreshMembersList()
        {
            MembersListView.ItemsSource = null;
            MembersListView.ItemsSource = _selectedMembers
                .OrderBy(m => m.LastName)
                .ThenBy(m => m.FirstName)
                .ToList();
        }

        private void RefreshEmployeeCombo()
        {
            var selectedIds = new HashSet<int>(_selectedMembers.Select(m => m.EmployeeID));
            EmployeeComboBox.ItemsSource = _availableEmployees
                .Where(e => !selectedIds.Contains(e.EmployeeID))
                .ToList();
            EmployeeComboBox.SelectedItem = null;
        }

        private void AddMemberBtn_Click(object sender, RoutedEventArgs e)
        {
            var employee = EmployeeComboBox.SelectedItem as Employees;
            if (employee == null)
            {
                MessageBox.Show("Выберите сотрудника для добавления", "Внимание");
                return;
            }

            if (_selectedMembers.Any(m => m.EmployeeID == employee.EmployeeID))
            {
                MessageBox.Show("Этот сотрудник уже добавлен в бригаду", "Внимание");
                return;
            }

            _selectedMembers.Add(employee);
            RefreshMembersList();
            RefreshEmployeeCombo();
        }

        private void RemoveMemberBtn_Click(object sender, RoutedEventArgs e)
        {
            var employee = MembersListView.SelectedItem as Employees;
            if (employee == null)
            {
                MessageBox.Show("Выберите сотрудника для удаления", "Внимание");
                return;
            }

            _selectedMembers.RemoveAll(m => m.EmployeeID == employee.EmployeeID);
            RefreshMembersList();
            RefreshEmployeeCombo();
        }

        private void SaveBtn_Click(object sender, RoutedEventArgs e)
        {
            var errors = new StringBuilder();

            if (TrainComboBox.SelectedValue == null)
            {
                errors.AppendLine("Выберите поезд");
            }

            if (_selectedMembers.Count == 0)
            {
                errors.AppendLine("Добавьте минимум одного сотрудника в бригаду");
            }

            if (errors.Length > 0)
            {
                MessageBox.Show(errors.ToString(), "Ошибка валидации");
                return;
            }

            try
            {
                var context = VokzalEntities.GetContext();
                var trainId = (int)TrainComboBox.SelectedValue;

                var selectedIds = _selectedMembers.Select(m => m.EmployeeID).ToList();
                var employeesInOtherCrews = context.TrainCrews
                    .Where(c => c.TrainID != trainId && selectedIds.Contains(c.EmployeeID))
                    .Select(c => c.EmployeeID)
                    .Distinct()
                    .ToList();

                if (employeesInOtherCrews.Any())
                {
                    var names = context.Employees
                        .Where(e => employeesInOtherCrews.Contains(e.EmployeeID))
                        .Select(e => e.LastName + " " + e.FirstName)
                        .ToList();
                    MessageBox.Show("Некоторые сотрудники уже состоят в другой бригаде:\n" + string.Join("\n", names), "Внимание");
                    return;
                }

                var existingMembers = context.TrainCrews.Where(c => c.TrainID == trainId).ToList();
                context.TrainCrews.RemoveRange(existingMembers);

                foreach (var employeeId in selectedIds.Distinct())
                {
                    context.TrainCrews.Add(new TrainCrews
                    {
                        TrainID = trainId,
                        EmployeeID = employeeId
                    });
                }

                context.SaveChanges();
                MessageBox.Show("Состав бригады сохранен", "Успех");
                Manager.MainFrame.GoBack();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка сохранения: {ex.Message}", "Ошибка");
            }
        }

        private void DeleteCrewBtn_Click(object sender, RoutedEventArgs e)
        {
            if (TrainComboBox.SelectedValue == null)
            {
                MessageBox.Show("Выберите бригаду (поезд), которую нужно удалить", "Внимание");
                return;
            }

            if (MessageBox.Show("Удалить всю бригаду выбранного поезда?", "Подтверждение",
                MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
            {
                return;
            }

            try
            {
                var trainId = (int)TrainComboBox.SelectedValue;
                var context = VokzalEntities.GetContext();
                var members = context.TrainCrews.Where(c => c.TrainID == trainId).ToList();

                if (!members.Any())
                {
                    MessageBox.Show("Для этого поезда бригада отсутствует", "Информация");
                    return;
                }

                context.TrainCrews.RemoveRange(members);
                context.SaveChanges();
                MessageBox.Show("Бригада удалена", "Успех");
                Manager.MainFrame.GoBack();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка удаления: {ex.Message}", "Ошибка");
            }
        }

        private void CancelBtn_Click(object sender, RoutedEventArgs e)
        {
            Manager.MainFrame.GoBack();
        }
    }
}
