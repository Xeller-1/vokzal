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
                return serializer.Deserialize<HrDataContainer>(json) ?? new HrDataContainer();
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

            var totalOnVacation = onVacationSamePeriod + 1;
            return totalOnVacation >= employeeIdsByPosition.Count;
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
