using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace vokzal
{
    internal static class SickLeaveManager
    {
        private static readonly object SyncRoot = new object();
        private const string StorageFileName = "sick-leaves.txt";

        private static string StoragePath
        {
            get
            {
                return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, StorageFileName);
            }
        }

        public static HashSet<int> GetSickLeaveEmployeeIds()
        {
            lock (SyncRoot)
            {
                return LoadInternal();
            }
        }

        public static bool IsOnSickLeave(int employeeId)
        {
            if (employeeId <= 0) return false;
            return GetSickLeaveEmployeeIds().Contains(employeeId);
        }

        public static bool OpenSickLeave(int employeeId)
        {
            if (employeeId <= 0) return false;

            lock (SyncRoot)
            {
                var ids = LoadInternal();
                if (!ids.Add(employeeId))
                {
                    return false;
                }

                SaveInternal(ids);
                return true;
            }
        }

        public static bool CloseSickLeave(int employeeId)
        {
            if (employeeId <= 0) return false;

            lock (SyncRoot)
            {
                var ids = LoadInternal();
                if (!ids.Remove(employeeId))
                {
                    return false;
                }

                SaveInternal(ids);
                return true;
            }
        }

        public static void CleanupUnknownEmployees(IEnumerable<int> existingEmployeeIds)
        {
            if (existingEmployeeIds == null)
            {
                return;
            }

            lock (SyncRoot)
            {
                var existingIds = new HashSet<int>(existingEmployeeIds.Where(id => id > 0));
                var ids = LoadInternal();
                ids.RemoveWhere(id => !existingIds.Contains(id));
                SaveInternal(ids);
            }
        }

        private static HashSet<int> LoadInternal()
        {
            try
            {
                if (!File.Exists(StoragePath))
                {
                    return new HashSet<int>();
                }

                var content = File.ReadAllLines(StoragePath, Encoding.UTF8);
                var parsed = new HashSet<int>();

                foreach (var line in content)
                {
                    int employeeId;
                    if (int.TryParse(line?.Trim(), out employeeId) && employeeId > 0)
                    {
                        parsed.Add(employeeId);
                    }
                }

                return parsed;
            }
            catch
            {
                return new HashSet<int>();
            }
        }

        private static void SaveInternal(IEnumerable<int> ids)
        {
            var normalized = (ids ?? Enumerable.Empty<int>())
                .Where(id => id > 0)
                .Distinct()
                .OrderBy(id => id)
                .Select(id => id.ToString())
                .ToArray();

            File.WriteAllLines(StoragePath, normalized, Encoding.UTF8);
        }
    }
}
