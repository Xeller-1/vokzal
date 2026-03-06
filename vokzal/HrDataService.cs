using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Web.Script.Serialization;

namespace vokzal
{
    public static class HrDataService
    {
        private const string DataFileName = "hr-data.json";

        private static readonly string LegacyDataDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data");
        private static readonly string LegacyDataPath = Path.Combine(LegacyDataDirectory, DataFileName);

        private static readonly string UserDataDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "vokzal",
            "Data");
        private static readonly string UserDataPath = Path.Combine(UserDataDirectory, DataFileName);

        public static HrDataContainer Load()
        {
            try
            {
                EnsureDataLocation();

                if (!File.Exists(UserDataPath))
                {
                    return new HrDataContainer();
                }

                var json = File.ReadAllText(UserDataPath, Encoding.UTF8);
                if (string.IsNullOrWhiteSpace(json))
                {
                    return new HrDataContainer();
                }

                var serializer = new JavaScriptSerializer();
                var data = serializer.Deserialize<HrDataContainer>(json) ?? new HrDataContainer();

                data.Vacations = data.Vacations ?? new System.Collections.Generic.List<VacationBooking>();
                data.SickLeaves = data.SickLeaves ?? new System.Collections.Generic.List<SickLeaveRecord>();
                data.PositionHistory = data.PositionHistory ?? new System.Collections.Generic.List<PositionHistoryRecord>();

                return data;
            }
            catch
            {
                return new HrDataContainer();
            }
        }

        public static void Save(HrDataContainer data)
        {
            EnsureDataLocation();

            var serializer = new JavaScriptSerializer();
            var json = serializer.Serialize(data);
            File.WriteAllText(UserDataPath, json, Encoding.UTF8);
        }

        public static bool HasVacationOverlap(int employeeId, DateTime startDate, DateTime endDate)
        {
            var data = Load();
            return data.Vacations.Any(v =>
                v.EmployeeId == employeeId &&
                startDate <= v.EndDate &&
                endDate >= v.StartDate);
        }

        public static bool HasActiveSickLeave(int employeeId, DateTime date)
        {
            var data = Load();
            return data.SickLeaves.Any(s =>
                s.EmployeeId == employeeId &&
                s.StartDate.Date <= date.Date &&
                (!s.EndDate.HasValue || s.EndDate.Value.Date >= date.Date));
        }

        public static SickLeaveRecord GetOpenSickLeave(int employeeId)
        {
            var data = Load();
            return data.SickLeaves
                .Where(s => s.EmployeeId == employeeId && !s.EndDate.HasValue)
                .OrderByDescending(s => s.StartDate)
                .FirstOrDefault();
        }

        public static bool WouldLeavePositionWithoutStaff(int positionId, int employeeId, DateTime startDate, DateTime endDate)
        {
            var context = VokzalEntities.GetContext();
            var employeeIdsByPosition = context.Employees
                .Where(e => e.PositionID == positionId)
                .Select(e => e.EmployeeID)
                .ToList();

            if (employeeIdsByPosition.Count <= 1)
            {
                return true;
            }

            var data = Load();
            var onVacationSamePeriod = data.Vacations
                .Where(v => employeeIdsByPosition.Contains(v.EmployeeId)
                            && v.EmployeeId != employeeId
                            && startDate <= v.EndDate
                            && endDate >= v.StartDate)
                .Select(v => v.EmployeeId)
                .Distinct()
                .Count();

            var onSickSamePeriod = data.SickLeaves
                .Where(s => employeeIdsByPosition.Contains(s.EmployeeId)
                            && s.EmployeeId != employeeId
                            && startDate <= (s.EndDate ?? DateTime.MaxValue)
                            && endDate >= s.StartDate)
                .Select(s => s.EmployeeId)
                .Distinct()
                .Count();

            var totalUnavailable = onVacationSamePeriod + onSickSamePeriod + 1;
            return totalUnavailable >= employeeIdsByPosition.Count;
        }

        private static void EnsureDataLocation()
        {
            Directory.CreateDirectory(UserDataDirectory);

            if (!File.Exists(UserDataPath) && File.Exists(LegacyDataPath))
            {
                File.Copy(LegacyDataPath, UserDataPath, false);
            }
        }
    }
}
