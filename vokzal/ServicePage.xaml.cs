using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace vokzal
{
    public partial class ServicePage : Page
    {
        int CountRecord;
        private List<Employees> currentPageList = new List<Employees>();
        private List<Employees> TableList;

        public ServicePage()
        {
            InitializeComponent();
            LoadData();
            Sort.SelectedIndex = 0;
            PositionBox.SelectedIndex = 0;
            CountPeopleCBox.SelectedIndex = 3;
        }

        private void LoadData()
        {
            UpdateServices();
        }

        private void UpdateServices()
        {
            try
            {
                var context = VokzalEntities.GetContext();

                // Загружаем данные через Entity Framework
                var employees = context.Employees.ToList();
                var positions = context.Positions.ToList();
                var documents = context.EmployeeDocuments.ToList();
                var educations = context.Education.ToList();

                // Создаем данные для отображения
                var employeeData = employees.Select(emp => new
                {
                    Employee = emp,
                    Position = positions.FirstOrDefault(p => p.PositionID == emp.PositionID),
                    Document = documents.FirstOrDefault(d => d.EmployeeID == emp.EmployeeID),
                    EducationList = educations.Where(e => e.EmployeeID == emp.EmployeeID).ToList()
                }).ToList();

                // Фильтрация по должности
                if (PositionBox.SelectedIndex > 0)
                {
                    string selectedPosition = ((ComboBoxItem)PositionBox.SelectedItem).Content.ToString();
                    employeeData = employeeData.Where(x => x.Position?.PositionName == selectedPosition).ToList();
                }

                // Сортировка
                if (Sort.SelectedItem != null)
                {
                    string selectedSortOption = ((ComboBoxItem)Sort.SelectedItem).Content.ToString();
                    switch (selectedSortOption)
                    {
                        case "По фамилии (А-Я)":
                            employeeData = employeeData.OrderBy(x => x.Employee.LastName).ToList(); break;
                        case "По фамилии (Я-А)":
                            employeeData = employeeData.OrderByDescending(x => x.Employee.LastName).ToList(); break;
                        case "По опыту (возрастание)":
                            employeeData = employeeData.OrderBy(x => x.Employee.Experience ?? 0).ToList(); break;
                        case "По опыту (убывание)":
                            employeeData = employeeData.OrderByDescending(x => x.Employee.Experience ?? 0).ToList(); break;
                        case "По возрасту (возрастание)":
                            employeeData = employeeData.OrderBy(x => x.Employee.Age).ToList(); break;
                        case "По возрасту (убывание)":
                            employeeData = employeeData.OrderByDescending(x => x.Employee.Age).ToList(); break;
                    }
                }

                if (!string.IsNullOrWhiteSpace(Search.Text))
                {
                    string searchText = Search.Text.ToLower();
                    employeeData = employeeData.Where(x =>
                        (x.Employee.FullName ?? "").ToLower().Contains(searchText) ||
                        (x.Employee.Phone ?? "").ToLower().Contains(searchText) ||
                        (x.Employee.Address ?? "").ToLower().Contains(searchText) ||
                        (x.Employee.Registration ?? "").ToLower().Contains(searchText) ||
                        (x.Document?.PassportData ?? "").ToLower().Contains(searchText) ||
                        (x.Document?.INN ?? "").ToLower().Contains(searchText) ||
                        (x.EducationList.Any(e => e.InstitutionName.ToLower().Contains(searchText)))
                    ).ToList();
                }

                ServiceListView.ItemsSource = employeeData;
                TableList = employeeData.Select(x => x.Employee).ToList();
                DisplayRecordsPerPage();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки данных: {ex.Message}", "Ошибка");
            }
        }


        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            UpdateServices();
        }

        private void DeleteBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var button = sender as Button;
                var employeeData = button?.Tag;
                var employeeProperty = employeeData?.GetType().GetProperty("Employee");
                var currentService = employeeProperty?.GetValue(employeeData) as Employees;

                if (currentService == null) return;

                var context = VokzalEntities.GetContext();

                // Проверяем есть ли сотрудник в бригадах
                if (currentService.TrainCrews.Any())
                {
                    MessageBox.Show("Невозможно выполнить удаление, так как сотрудник прикреплен к бригаде!", "Внимание");
                    return;
                }

                if (MessageBox.Show("Вы точно хотите выполнить удаление?", "Внимание!",
                    MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                {
                    // Удаляем сотрудника (каскадное удаление должно удалить связанные записи)
                    context.Employees.Remove(currentService);
                    context.SaveChanges();

                    UpdateServices();
                    MessageBox.Show("Сотрудник успешно удален!", "Успех");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при удалении: {ex.Message}", "Ошибка");
            }
        }

        private void DisplayRecordsPerPage()
        {
            currentPageList.Clear();
            if (TableList == null) return;

            CountRecord = TableList.Count;

            int itemsPerPage = 5;
            switch (CountPeopleCBox.SelectedIndex)
            {
                case 0: itemsPerPage = 5; break;
                case 1: itemsPerPage = 15; break;
                case 2: itemsPerPage = 30; break;
                case 3: itemsPerPage = CountRecord; break;
            }

            // Берем только нужное количество записей
            var pagedData = TableList.Take(itemsPerPage).ToList();

            // Загружаем дополнительные данные для отображаемых записей
            var displayData = new List<object>();

            var context = VokzalEntities.GetContext();
            var positions = context.Positions.ToList();
            var documents = context.EmployeeDocuments.ToList();
            var educations = context.Education.ToList();

            foreach (var emp in pagedData)
            {
                var position = positions.FirstOrDefault(p => p.PositionID == emp.PositionID);
                var document = documents.FirstOrDefault(d => d.EmployeeID == emp.EmployeeID);
                var employeeEducations = educations.Where(e => e.EmployeeID == emp.EmployeeID).ToList();

                displayData.Add(new
                {
                    Employee = emp,
                    Position = position,
                    Document = document,
                    EducationList = employeeEducations
                });
            }

            ServiceListView.ItemsSource = displayData;
        }
        private void CountPeopleCBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            DisplayRecordsPerPage();
        }

        private void Addbtn_Click(object sender, RoutedEventArgs e)
        {
            Manager.MainFrame.Navigate(new AddEditPage(null));
        }

        private void EditBtn_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var employeeData = button?.Tag;
            var employeeProperty = employeeData?.GetType().GetProperty("Employee");
            var employee = employeeProperty?.GetValue(employeeData) as Employees;

            if (employee != null)
            {
                Manager.MainFrame.Navigate(new AddEditPage(employee));
            }
        }

        private void PositionBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateServices();
        }

        private void SearchTBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateServices();
        }

        private void Sort_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateServices();
        }

        private void ManageCrewsBtn_Click(object sender, RoutedEventArgs e)
        {
            Manager.MainFrame.Navigate(new CrewManagementPage());
        }

        private void DocumentsBtn_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var employeeData = button?.Tag;
            var employeeProperty = employeeData?.GetType().GetProperty("Employee");
            var employee = employeeProperty?.GetValue(employeeData) as Employees;

            if (employee != null)
            {
                Manager.MainFrame.Navigate(new EmployeeDocumentsPage(employee));
            }
        }

        private void EducationBtn_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var employeeData = button?.Tag;
            var employeeProperty = employeeData?.GetType().GetProperty("Employee");
            var employee = employeeProperty?.GetValue(employeeData) as Employees;

            if (employee != null)
            {
                Manager.MainFrame.Navigate(new EducationPage(employee));
            }
        }


        private void HrBtn_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var employeeData = button?.Tag;
            var employeeProperty = employeeData?.GetType().GetProperty("Employee");
            var employee = employeeProperty?.GetValue(employeeData) as Employees;

            if (employee != null)
            {
                Manager.MainFrame.Navigate(new EmployeeHrPage(employee));
            }
        }
    }
}