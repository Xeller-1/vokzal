using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace vokzal
{
    internal static class SickLeaveManager
    {
        private static readonly string StoragePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "sick-leaves.txt");

        public static HashSet<int> GetSickLeaveEmployeeIds()
        {
            try
            {
                if (!File.Exists(StoragePath))
                {
                    return new HashSet<int>();
                }

                return new HashSet<int>(
                    File.ReadAllLines(StoragePath)
                        .Select(line =>
                        {
                            int employeeId;
                            return int.TryParse(line, out employeeId) ? employeeId : -1;
                        })
                        .Where(id => id > 0));
            }
            catch
            {
                return new HashSet<int>();
            }
        }

        public static bool IsOnSickLeave(int employeeId)
        {
            return GetSickLeaveEmployeeIds().Contains(employeeId);
        }

        public static bool OpenSickLeave(int employeeId)
        {
            if (employeeId <= 0) return false;

            var ids = GetSickLeaveEmployeeIds();
            if (!ids.Add(employeeId))
            {
                return false;
            }

            Save(ids);
            return true;
        }

        public static bool CloseSickLeave(int employeeId)
        {
            if (employeeId <= 0) return false;

            var ids = GetSickLeaveEmployeeIds();
            if (!ids.Remove(employeeId))
            {
                return false;
            }

            Save(ids);
            return true;
        }

        private static void Save(IEnumerable<int> ids)
        {
            File.WriteAllLines(StoragePath, ids.Distinct().OrderBy(id => id).Select(id => id.ToString()));
        }
    }
}
