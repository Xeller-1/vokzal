using System;
using System.Collections.Generic;

namespace vokzal
{
    public class VacationBooking
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public int EmployeeId { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string Reason { get; set; }
        public string PdfPath { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }

    public class PositionHistoryRecord
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public int EmployeeId { get; set; }
        public int PositionId { get; set; }
        public string PositionName { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
    }

    public class HrDataContainer
    {
        public List<VacationBooking> Vacations { get; set; } = new List<VacationBooking>();
        public List<PositionHistoryRecord> PositionHistory { get; set; } = new List<PositionHistoryRecord>();
    }
}
