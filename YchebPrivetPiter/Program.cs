using Avalonia;
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using YchebPrivetPiter.Data;
using YchebPrivetPiter.Services;
using YchebPrivetPiter.ViewModels;

namespace YchebPrivetPiter;

class Program
{
    public static IServiceProvider Services { get; private set; } = null!;

    [STAThread]
    public static void Main(string[] args)
    {
        Services = ConfigureServices();
        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
#if DEBUG
            .WithDeveloperTools()
#endif
            .WithInterFont()
            .LogToTrace();

    private static IServiceProvider ConfigureServices()
    {
        // ---------- Конфигурация ----------
        var config = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .Build();

        var cs = config.GetConnectionString("Default")
                 ?? "Host=localhost;Port=5432;Database=e;Username=postgres;Password=1234";

        var services = new ServiceCollection();

        // ---------- Npgsql + enum-маппинг ----------
        var dsb = new NpgsqlDataSourceBuilder(cs);
        dsb.MapEnum<AppointmentStatus>("appointment_status");
        dsb.MapEnum<WorkOrderStatus>("work_order_status");
        dsb.MapEnum<InvoiceStatus>("invoice_status");
        dsb.MapEnum<PaymentMethod>("payment_method");
        dsb.MapEnum<PaymentStatus>("payment_status");
        dsb.MapEnum<ScheduleStatus>("schedule_status");
        dsb.MapEnum<PlaceKind>("place_kind");
        var dataSource = dsb.Build();
        services.AddSingleton(dataSource);

        // ---------- DbContext через фабрику ----------
        // Каждый сервис получает свежий AppDbContext на операцию через IDbContextFactory.
        services.AddDbContextFactory<AppDbContext>(opt => opt.UseNpgsql(dataSource));

        // ---------- Сервисы (stateless → Singleton) ----------
        services.AddSingleton<AuthService>();
        services.AddSingleton<ClientService>();
        services.AddSingleton<CarService>();
        services.AddSingleton<AppointmentService>();
        services.AddSingleton<WorkOrderServices>();
        services.AddSingleton<PartService>();
        services.AddSingleton<InvoiceService>();
        services.AddSingleton<PaymentService>();
        services.AddSingleton<AnalyticsService>();
        services.AddSingleton<MechanicScheduleService>();
        services.AddTransient<MechanicScheduleListViewModel>();
        services.AddSingleton<InspectionResultService>();   // ← ЭТА СТРОКА
        services.AddSingleton<MechanicScheduleService>();
        services.AddSingleton<ReviewService>();
        services.AddTransient<ReviewListViewModel>();

        // ---------- ViewModels ----------
        services.AddTransient<MainViewModel>();
        services.AddTransient<LoginViewModel>();
        services.AddTransient<ClientListViewModel>();
        services.AddTransient<CarListViewModel>();
        services.AddTransient<AppointmentListViewModel>();
        services.AddTransient<WorkOrderListViewModel>();
        services.AddTransient<InvoiceListViewModel>();
        services.AddTransient<PaymentListViewModel>();
        services.AddTransient<PartListViewModel>();
        services.AddTransient<DashboardViewModel>();


        return services.BuildServiceProvider();
    }
}