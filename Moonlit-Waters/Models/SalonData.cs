using System.Globalization;

namespace Moonlit_Waters.Models;

public class Profile
{
    public Guid Id { get; set; }
    public string Username { get; set; } = "";
    public string FullName { get; set; } = "";
    public string Role { get; set; } = "";
    public bool IsAdmin => Role == "admin";
    public string RoleText => IsAdmin ? "Администратор" : "Мастер";
}

public class LoginResult
{
    public bool Ok { get; set; }
    public string Message { get; set; } = "";
    public string Token { get; set; } = "";
    public Profile Profile { get; set; } = new();
}

public class Client
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public string Phone { get; set; } = "";
    public string Email { get; set; } = "";
    public int Visits { get; set; }
    public DateTimeOffset? LastVisit { get; set; }
    public string? LastService { get; set; }
    public string? LastMaster { get; set; }
    public string LastVisitText => LastVisit?.ToOffset(TimeSpan.FromHours(3)).ToString("dd.MM.yyyy") ?? "—";
    public override string ToString() => Name;
}

public class Service
{
    public Guid Id { get; set; }
    public string Category { get; set; } = "";
    public string Name { get; set; } = "";
    public int DurationMinutes { get; set; }
    public decimal Price { get; set; }
    public Guid? MasterId { get; set; }
    public string DurationText => $"{DurationMinutes} мин";
    public string PriceText => Price.ToString("N0", CultureInfo.GetCultureInfo("ru-RU")) + " ₽";
    public override string ToString() => Name;
}

public class Master
{
    public Guid Id { get; set; }
    public string Username { get; set; } = "";
    public string FullName { get; set; } = "";
    public string Phone { get; set; } = "";
    public string Specialization { get; set; } = "";
    public string BankAccount { get; set; } = "";
    public TimeSpan WorkStart { get; set; }
    public TimeSpan WorkEnd { get; set; }
    public bool IsDayOff { get; set; }
    public int AppointmentCount { get; set; }
    public string Initial => FullName.Length == 0 ? "М" : FullName[..1];
    public string ScheduleText => IsDayOff ? "Выходной" : $"График · {WorkStart:hh\\:mm}–{WorkEnd:hh\\:mm}";
    public string CountText => $"Записей: {AppointmentCount}";
    public override string ToString() => FullName;
}

public class Appointment
{
    public Guid Id { get; set; }
    public Guid ClientId { get; set; }
    public Guid ServiceId { get; set; }
    public Guid MasterId { get; set; }
    public string ClientName { get; set; } = "";
    public string ClientPhone { get; set; } = "";
    public string ServiceName { get; set; } = "";
    public string MasterName { get; set; } = "";
    public DateTimeOffset StartsAt { get; set; }
    public DateTimeOffset EndsAt { get; set; }
    public decimal Price { get; set; }
    public string Status { get; set; } = "confirmed";
    public string TimeText => StartsAt.ToOffset(TimeSpan.FromHours(3)).ToString("HH:mm");
    public string ShortMasterName => MasterName.Split(' ')[0];
    public string StatusText => Status switch { "waiting" => "Ожидает", "in_progress" => "В работе", "completed" => "Завершена", "cancelled" => "Отменена", _ => "Подтверждена" };
    public string StatusColor => Status switch { "in_progress" => "#3B8C66", "completed" => "#3B8C66", "cancelled" => "#A34B42", _ => "#152024" };
}

public class SalonData
{
    public List<Client> Clients { get; set; } = new();
    public List<Service> Services { get; set; } = new();
    public List<Master> Staff { get; set; } = new();
    public List<Appointment> Appointments { get; set; } = new();
}

public class MasterAccess
{
    public Guid Id { get; set; }
    public string Username { get; set; } = "";
    public string? Password { get; set; }
}
