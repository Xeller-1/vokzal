using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Web.Script.Serialization;

namespace vokzal
{
    public static class HrDataService
    {
        private static readonly string DataDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data");
        private static readonly string DataPath = Path.Combine(DataDirectory, "hr-data.json");

        public static HrDataContainer Load()
        {
            try
            {
                if (!File.Exists(DataPath))
                {
                    return new HrDataContainer();
                }

                var json = File.ReadAllText(DataPath, Encoding.UTF8);
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
            Directory.CreateDirectory(DataDirectory);
            var serializer = new JavaScriptSerializer();
            var json = serializer.Serialize(data);
            File.WriteAllText(DataPath, json, Encoding.UTF8);
        }

        public static bool HasVacationOverlap(int employeeId, DateTime startDate, DateTime endDate)
        {
            var data = Load();
            return data.Vacations.Any(v =>
                v.EmployeeId == employeeId &&
                startDate <= v.EndDate &&
                endDate >= v.StartDate);
        }
    }
}
