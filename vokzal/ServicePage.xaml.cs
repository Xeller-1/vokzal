using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace vokzal
{
    public partial class ServicePage : Page
    {
        private const string AllPositionsLabel = "Все должности";

        private List<EmployeeDisplayItem> _fullList = new List<EmployeeDisplayItem>();
        private List<EmployeeDisplayItem> _filteredList = new List<EmployeeDisplayItem>();
        private int _currentPage = 1;
        private int _totalPages = 1;

        public ServicePage()
        {
            InitializeComponent();
            ConfigureFilters();
            Sort.SelectedIndex = 0;
            CountPeopleCBox.SelectedIndex = 3;
            UpdateServices();
        }

        private void ConfigureFilters()
        {
            var context = VokzalEntities.GetContext();
            var positions = context.Positions
                .AsNoTracking()
                .OrderBy(p => p.PositionName)
                .ToList();

            var filterItems = new List<PositionFilterItem>
            {
                new PositionFilterItem { PositionId = null, PositionName = AllPositionsLabel }
            };
            filterItems.AddRange(positions.Select(p => new PositionFilterItem
            {
                PositionId = p.PositionID,
                PositionName = p.PositionName
            }));

            PositionBox.ItemsSource = filterItems;
            PositionBox.DisplayMemberPath = "PositionName";
            PositionBox.SelectedValuePath = "PositionId";
            PositionBox.SelectedIndex = 0;
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            UpdateServices();
        }

        private void UpdateServices()
        {
            try
            {
                var context = VokzalEntities.GetContext();
                var employees = context.Employees
                    .AsNoTracking()
                    .Include(e => e.Positions)
                    .Include(e => e.EmployeeDocuments)
                    .Include(e => e.Education)
                    .ToList();

                var hrData = HrDataService.Load();
                var today = DateTime.Today;
                var sickEmployeeIds = new HashSet<int>(hrData.SickLeaves
                    .Where(sl => sl.StartDate.Date <= today && (!sl.EndDate.HasValue || sl.EndDate.Value.Date >= today))
                    .Select(sl => sl.EmployeeId)
                    .Distinct());

                _fullList = employees.Select(emp => new EmployeeDisplayItem
                {
                    Employee = emp,
                    Position = emp.Positions,
                    Document = emp.EmployeeDocuments.FirstOrDefault(),
                    EducationList = emp.Education.ToList(),
                    IsOnSickLeave = sickEmployeeIds.Contains(emp.EmployeeID),
                    StatusText = sickEmployeeIds.Contains(emp.EmployeeID) ? "На больничном" : "Активен"
                }).ToList();

                _currentPage = 1;
                _filteredList = ApplyFilters(_fullList);
                ApplyPagination();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки данных: {ex.Message}", "Ошибка");
            }
        }

        private List<EmployeeDisplayItem> ApplyFilters(List<EmployeeDisplayItem> source)
        {
            var data = source;

            if (PositionBox.SelectedItem is PositionFilterItem selectedPosition && selectedPosition.PositionId.HasValue)
            {
                data = data.Where(x => x.Employee.PositionID == selectedPosition.PositionId.Value).ToList();
            }

            if (Sort.SelectedItem is ComboBoxItem sortItem)
            {
                switch (sortItem.Content.ToString())
                {
                    case "По фамилии (А-Я)":
                        data = data.OrderBy(x => x.Employee.LastName).ToList();
                        break;
                    case "По фамилии (Я-А)":
                        data = data.OrderByDescending(x => x.Employee.LastName).ToList();
                        break;
                    case "По опыту (возрастание)":
                        data = data.OrderBy(x => x.Employee.Experience ?? 0).ToList();
                        break;
                    case "По опыту (убывание)":
                        data = data.OrderByDescending(x => x.Employee.Experience ?? 0).ToList();
                        break;
                    case "По возрасту (возрастание)":
                        data = data.OrderBy(x => x.Employee.Age).ToList();
                        break;
                    case "По возрасту (убывание)":
                        data = data.OrderByDescending(x => x.Employee.Age).ToList();
                        break;
                }
            }

            if (!string.IsNullOrWhiteSpace(Search.Text))
            {
                var searchText = Search.Text.ToLower();
                data = data.Where(x =>
                    (x.Employee.FullName ?? string.Empty).ToLower().Contains(searchText) ||
                    (x.Employee.Phone ?? string.Empty).ToLower().Contains(searchText) ||
                    (x.Employee.Address ?? string.Empty).ToLower().Contains(searchText) ||
                    (x.Employee.Registration ?? string.Empty).ToLower().Contains(searchText) ||
                    (x.Document?.PassportData ?? string.Empty).ToLower().Contains(searchText) ||
                    (x.Document?.INN ?? string.Empty).ToLower().Contains(searchText) ||
                    x.EducationList.Any(e => (e.InstitutionName ?? string.Empty).ToLower().Contains(searchText))
                ).ToList();
            }

            return data;
        }

        private void ApplyPagination()
        {
            if (_filteredList == null)
            {
                ServiceListView.ItemsSource = null;
                RecordsInfoText.Text = "Найдено сотрудников: 0";
                PageInfoText.Text = "Страница 1/1";
                PrevPageBtn.IsEnabled = false;
                NextPageBtn.IsEnabled = false;
                return;
            }

            var recordsCount = _filteredList.Count;
            var itemsPerPage = GetItemsPerPage(recordsCount);

            if (itemsPerPage <= 0)
            {
                itemsPerPage = recordsCount == 0 ? 1 : recordsCount;
            }

            _totalPages = Math.Max(1, (int)Math.Ceiling(recordsCount / (double)itemsPerPage));
            if (_currentPage > _totalPages)
            {
                _currentPage = _totalPages;
            }
            if (_currentPage < 1)
            {
                _currentPage = 1;
            }

            var skip = (_currentPage - 1) * itemsPerPage;
            var pagedData = _filteredList.Skip(skip).Take(itemsPerPage).ToList();

            ServiceListView.ItemsSource = pagedData;
            RecordsInfoText.Text = $"Найдено сотрудников: {recordsCount}";
            PageInfoText.Text = $"Страница {_currentPage}/{_totalPages}";
            PrevPageBtn.IsEnabled = _currentPage > 1;
            NextPageBtn.IsEnabled = _currentPage < _totalPages;
        }

        private int GetItemsPerPage(int recordsCount)
        {
            switch (CountPeopleCBox.SelectedIndex)
            {
                case 0:
                    return 5;
                case 1:
                    return 15;
                case 2:
                    return 30;
                default:
                    return recordsCount;
            }
        }

        private void DeleteBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var currentService = ExtractEmployee(sender);
                if (currentService == null)
                {
                    return;
                }

                var context = VokzalEntities.GetContext();

                if (currentService.TrainCrews.Any())
                {
                    MessageBox.Show("Невозможно выполнить удаление, так как сотрудник прикреплен к бригаде!", "Внимание");
                    return;
                }

                if (MessageBox.Show("Вы точно хотите выполнить удаление?", "Внимание!",
                    MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                {
                    context.Employees.Attach(currentService);
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

        private static Employees ExtractEmployee(object sender)
        {
            var button = sender as Button;
            var employeeData = button?.Tag as EmployeeDisplayItem;
            return employeeData?.Employee;
        }

        private void CountPeopleCBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _currentPage = 1;
            ApplyPagination();
        }

        private void Addbtn_Click(object sender, RoutedEventArgs e)
        {
            Manager.MainFrame.Navigate(new AddEditPage(null));
        }

        private void EditBtn_Click(object sender, RoutedEventArgs e)
        {
            var employee = ExtractEmployee(sender);
            if (employee != null)
            {
                Manager.MainFrame.Navigate(new AddEditPage(employee));
            }
        }

        private void PositionBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _currentPage = 1;
            _filteredList = ApplyFilters(_fullList);
            ApplyPagination();
        }

        private void SearchTBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            _currentPage = 1;
            _filteredList = ApplyFilters(_fullList);
            ApplyPagination();
        }

        private void Sort_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _currentPage = 1;
            _filteredList = ApplyFilters(_fullList);
            ApplyPagination();
        }

        private void ManageCrewsBtn_Click(object sender, RoutedEventArgs e)
        {
            Manager.MainFrame.Navigate(new CrewManagementPage());
        }

        private void DocumentsBtn_Click(object sender, RoutedEventArgs e)
        {
            var employee = ExtractEmployee(sender);
            if (employee != null)
            {
                Manager.MainFrame.Navigate(new EmployeeDocumentsPage(employee));
            }
        }

        private void EducationBtn_Click(object sender, RoutedEventArgs e)
        {
            var employee = ExtractEmployee(sender);
            if (employee != null)
            {
                Manager.MainFrame.Navigate(new EducationPage(employee));
            }
        }

        private void HrBtn_Click(object sender, RoutedEventArgs e)
        {
            var employee = ExtractEmployee(sender);
            if (employee != null)
            {
                Manager.MainFrame.Navigate(new EmployeeHrPage(employee));
            }
        }

        private void ResetFiltersBtn_Click(object sender, RoutedEventArgs e)
        {
            Search.Text = string.Empty;
            Sort.SelectedIndex = 0;
            PositionBox.SelectedIndex = 0;
            CountPeopleCBox.SelectedIndex = 3;

            _currentPage = 1;
            _filteredList = ApplyFilters(_fullList);
            ApplyPagination();
        }

        private void PrevPageBtn_Click(object sender, RoutedEventArgs e)
        {
            if (_currentPage > 1)
            {
                _currentPage--;
                ApplyPagination();
            }
        }

        private void NextPageBtn_Click(object sender, RoutedEventArgs e)
        {
            if (_currentPage < _totalPages)
            {
                _currentPage++;
                ApplyPagination();
            }
        }

        private sealed class PositionFilterItem
        {
            public int? PositionId { get; set; }
            public string PositionName { get; set; }
        }

        private sealed class EmployeeDisplayItem
        {
            public Employees Employee { get; set; }
            public Positions Position { get; set; }
            public EmployeeDocuments Document { get; set; }
            public List<Education> EducationList { get; set; }
            public bool IsOnSickLeave { get; set; }
            public string StatusText { get; set; }
        }
    }
}
