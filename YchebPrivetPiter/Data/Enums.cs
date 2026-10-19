using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace YchebPrivetPiter.Data
{
    public enum AppointmentStatus { planned, confirmed, cancelled, completed }
    public enum WorkOrderStatus { draft, in_progress, awaiting_parts, completed, cancelled }
    public enum InvoiceStatus { unpaid, partially_paid, paid, cancelled }
    public enum PaymentMethod { cash, card, bank_transfer, online }
    public enum PaymentStatus { pending, completed, failed, refunded }
    public enum ScheduleStatus { available, busy, day_off, vacation }
    public enum PlaceKind { lift, diagnostic_post, tire_zone, body_shop, general }
}
