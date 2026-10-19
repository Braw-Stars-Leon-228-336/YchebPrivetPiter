using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace YchebPrivetPiter.Data;

public partial class AppDbContext : DbContext
{
    public AppDbContext()
    {
    }

    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Appointment> Appointments { get; set; }
    public virtual DbSet<Car> Cars { get; set; }
    public virtual DbSet<CarBrand> CarBrands { get; set; }
    public virtual DbSet<CarCategory> CarCategories { get; set; }
    public virtual DbSet<Client> Clients { get; set; }
    public virtual DbSet<Department> Departments { get; set; }
    public virtual DbSet<InspectionResult> InspectionResults { get; set; }
    public virtual DbSet<Invoice> Invoices { get; set; }
    public virtual DbSet<Mechanic> Mechanics { get; set; }
    public virtual DbSet<MechanicSchedule> MechanicSchedules { get; set; }
    public virtual DbSet<Part> Parts { get; set; }
    public virtual DbSet<Payment> Payments { get; set; }
    public virtual DbSet<RepairPlace> RepairPlaces { get; set; }
    public virtual DbSet<Review> Reviews { get; set; }
    public virtual DbSet<Role> Roles { get; set; }
    public virtual DbSet<Service> Services { get; set; }
    public virtual DbSet<Specialization> Specializations { get; set; }
    public virtual DbSet<Supplier> Suppliers { get; set; }
    public virtual DbSet<User> Users { get; set; }
    public virtual DbSet<WorkOrder> WorkOrders { get; set; }
    public virtual DbSet<WorkOrderPart> WorkOrderParts { get; set; }
    public virtual DbSet<WorkOrderService> WorkOrderServices { get; set; }

    // ВАЖНО: OnConfiguring УБРАН.
    // Строка подключения и маппинг enum'ов настраиваются в Program.cs
    // через NpgsqlDataSourceBuilder + AddDbContext.

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // ---------- PostgreSQL ENUM-типы ----------
        modelBuilder
            .HasPostgresEnum<AppointmentStatus>("public", "appointment_status")
            .HasPostgresEnum<InvoiceStatus>("public", "invoice_status")
            .HasPostgresEnum<PaymentMethod>("public", "payment_method")
            .HasPostgresEnum<PaymentStatus>("public", "payment_status")
            .HasPostgresEnum<PlaceKind>("public", "place_kind")
            .HasPostgresEnum<ScheduleStatus>("public", "schedule_status")
            .HasPostgresEnum<WorkOrderStatus>("public", "work_order_status")
            .HasPostgresExtension("pgcrypto");

        // ============================================================
        // APPOINTMENTS
        // ============================================================
        modelBuilder.Entity<Appointment>(entity =>
        {
            entity.HasKey(e => e.AppointmentId).HasName("appointments_pkey");
            entity.ToTable("appointments");

            entity.HasIndex(e => e.ClientId, "ix_appt_client");
            entity.HasIndex(e => e.ScheduledAt, "ix_appt_date");
            entity.HasIndex(e => e.MechanicId, "ix_appt_mechanic");
            entity.HasIndex(e => e.Status, "ix_appt_status"); // ← восстановлено

            entity.Property(e => e.AppointmentId).HasColumnName("appointment_id");
            entity.Property(e => e.CarId).HasColumnName("car_id");
            entity.Property(e => e.ClientId).HasColumnName("client_id");
            entity.Property(e => e.Comment).HasColumnName("comment");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");
            entity.Property(e => e.MechanicId).HasColumnName("mechanic_id");
            entity.Property(e => e.RepairPlaceId).HasColumnName("repair_place_id");
            entity.Property(e => e.ScheduledAt).HasColumnName("scheduled_at");
            entity.Property(e => e.ServiceId).HasColumnName("service_id");
            entity.Property(e => e.Status)
                .HasConversion<string>()
                .HasColumnName("status");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("updated_at");

            entity.HasOne(d => d.Car).WithMany(p => p.Appointments)
                .HasForeignKey(d => d.CarId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("appointments_car_id_fkey");

            entity.HasOne(d => d.Client).WithMany(p => p.Appointments)
                .HasForeignKey(d => d.ClientId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("appointments_client_id_fkey");

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.Appointments)
                .HasForeignKey(d => d.CreatedBy)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("appointments_created_by_fkey");

            entity.HasOne(d => d.Mechanic).WithMany(p => p.Appointments)
                .HasForeignKey(d => d.MechanicId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("appointments_mechanic_id_fkey");

            entity.HasOne(d => d.RepairPlace).WithMany(p => p.Appointments)
                .HasForeignKey(d => d.RepairPlaceId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("appointments_repair_place_id_fkey");

            entity.HasOne(d => d.Service).WithMany(p => p.Appointments)
                .HasForeignKey(d => d.ServiceId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("appointments_service_id_fkey");
        });

        // ============================================================
        // CARS
        // ============================================================
        modelBuilder.Entity<Car>(entity =>
        {
            entity.HasKey(e => e.CarId).HasName("cars_pkey");
            entity.ToTable("cars");

            entity.HasIndex(e => e.PlateNumber, "cars_plate_number_key").IsUnique();
            entity.HasIndex(e => e.Vin, "cars_vin_key").IsUnique();
            entity.HasIndex(e => e.BrandId, "ix_cars_brand");
            entity.HasIndex(e => e.ClientId, "ix_cars_client");

            entity.Property(e => e.CarId).HasColumnName("car_id");
            entity.Property(e => e.BrandId).HasColumnName("brand_id");
            entity.Property(e => e.CategoryId).HasColumnName("category_id");
            entity.Property(e => e.ClientId).HasColumnName("client_id");
            entity.Property(e => e.Color).HasMaxLength(48).HasColumnName("color");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()").HasColumnName("created_at");
            entity.Property(e => e.Mileage)
                .HasDefaultValue(0).HasColumnName("mileage");
            entity.Property(e => e.Model).HasMaxLength(100).HasColumnName("model");
            entity.Property(e => e.PlateNumber).HasMaxLength(16).HasColumnName("plate_number");
            entity.Property(e => e.ProductionYear).HasColumnName("production_year");
            entity.Property(e => e.Vin).HasMaxLength(17).HasColumnName("vin");

            entity.HasOne(d => d.Brand).WithMany(p => p.Cars)
                .HasForeignKey(d => d.BrandId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("cars_brand_id_fkey");

            entity.HasOne(d => d.Category).WithMany(p => p.Cars)
                .HasForeignKey(d => d.CategoryId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("cars_category_id_fkey");

            entity.HasOne(d => d.Client).WithMany(p => p.Cars)
                .HasForeignKey(d => d.ClientId)
                .HasConstraintName("cars_client_id_fkey");
        });

        // ============================================================
        // CAR_BRANDS / CAR_CATEGORIES
        // ============================================================
        modelBuilder.Entity<CarBrand>(entity =>
        {
            entity.HasKey(e => e.BrandId).HasName("car_brands_pkey");
            entity.ToTable("car_brands");
            entity.HasIndex(e => e.Name, "car_brands_name_key").IsUnique();
            entity.Property(e => e.BrandId).HasColumnName("brand_id");
            entity.Property(e => e.Country).HasMaxLength(64).HasColumnName("country");
            entity.Property(e => e.Name).HasMaxLength(100).HasColumnName("name");
        });

        modelBuilder.Entity<CarCategory>(entity =>
        {
            entity.HasKey(e => e.CategoryId).HasName("car_categories_pkey");
            entity.ToTable("car_categories");
            entity.HasIndex(e => e.Name, "car_categories_name_key").IsUnique();
            entity.Property(e => e.CategoryId).HasColumnName("category_id");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.Name).HasMaxLength(64).HasColumnName("name");
        });

        // ============================================================
        // CLIENTS
        // ============================================================
        modelBuilder.Entity<Client>(entity =>
        {
            entity.HasKey(e => e.ClientId).HasName("clients_pkey");
            entity.ToTable("clients");

            entity.HasIndex(e => e.Email, "ix_clients_email");
            entity.HasIndex(e => e.FullName, "ix_clients_fullname");
            entity.HasIndex(e => e.Phone, "uq_clients_phone").IsUnique();

            entity.Property(e => e.ClientId).HasColumnName("client_id");
            entity.Property(e => e.Address).HasMaxLength(255).HasColumnName("address");
            entity.Property(e => e.BirthDate).HasColumnName("birth_date");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()").HasColumnName("created_at");
            entity.Property(e => e.Email).HasMaxLength(150).HasColumnName("email");
            entity.Property(e => e.FullName).HasMaxLength(150).HasColumnName("full_name");
            entity.Property(e => e.Notes).HasColumnName("notes");
            entity.Property(e => e.Phone).HasMaxLength(32).HasColumnName("phone");
        });

        // ============================================================
        // DEPARTMENTS
        // ============================================================
        modelBuilder.Entity<Department>(entity =>
        {
            entity.HasKey(e => e.DepartmentId).HasName("departments_pkey");
            entity.ToTable("departments");
            entity.HasIndex(e => e.Name, "departments_name_key").IsUnique();
            entity.Property(e => e.DepartmentId).HasColumnName("department_id");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.Name).HasMaxLength(100).HasColumnName("name");
        });

        // ============================================================
        // INSPECTION_RESULTS
        // ============================================================
        modelBuilder.Entity<InspectionResult>(entity =>
        {
            entity.HasKey(e => e.InspectionId).HasName("inspection_results_pkey");
            entity.ToTable("inspection_results");
            entity.HasIndex(e => e.WorkOrderId, "ix_insp_wo");

            entity.Property(e => e.InspectionId).HasColumnName("inspection_id");
            entity.Property(e => e.BrakeCondition).HasColumnName("brake_condition");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()").HasColumnName("created_at");
            entity.Property(e => e.ElectricalCondition).HasColumnName("electrical_condition");
            entity.Property(e => e.EngineCondition).HasColumnName("engine_condition");
            entity.Property(e => e.MechanicId).HasColumnName("mechanic_id");
            entity.Property(e => e.Recommendations).HasColumnName("recommendations");
            entity.Property(e => e.SuspensionCondition).HasColumnName("suspension_condition");
            entity.Property(e => e.WorkOrderId).HasColumnName("work_order_id");

            entity.HasOne(d => d.Mechanic).WithMany(p => p.InspectionResults)
                .HasForeignKey(d => d.MechanicId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("inspection_results_mechanic_id_fkey");

            entity.HasOne(d => d.WorkOrder).WithMany(p => p.InspectionResults)
                .HasForeignKey(d => d.WorkOrderId)
                .HasConstraintName("inspection_results_work_order_id_fkey");
        });

        // ============================================================
        // INVOICES
        // ============================================================
        modelBuilder.Entity<Invoice>(entity =>
        {
            entity.HasKey(e => e.InvoiceId).HasName("invoices_pkey");
            entity.ToTable("invoices");

            entity.HasIndex(e => e.Number, "invoices_number_key").IsUnique();
            entity.HasIndex(e => e.WorkOrderId, "ix_inv_wo");
            entity.HasIndex(e => e.Status, "ix_inv_status"); // ← восстановлено

            entity.Property(e => e.InvoiceId).HasColumnName("invoice_id");
            entity.Property(e => e.ClientId).HasColumnName("client_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()").HasColumnName("created_at");
            entity.Property(e => e.DiscountAmount)
                .HasPrecision(12, 2).HasColumnName("discount_amount");
            entity.Property(e => e.DueDate).HasColumnName("due_date");
            entity.Property(e => e.Number).HasMaxLength(32).HasColumnName("number");
            entity.Property(e => e.Status)
                .HasConversion<string>()
                .HasColumnName("status");
            entity.Property(e => e.Subtotal)
                .HasPrecision(12, 2).HasColumnName("subtotal");
            entity.Property(e => e.TaxRate)
                .HasPrecision(5, 4).HasColumnName("tax_rate");
            entity.Property(e => e.TotalAmount)
                .HasPrecision(12, 2)
                .HasComputedColumnSql(
                    "round(((subtotal - discount_amount) * ((1)::numeric + tax_rate)), 2)",
                    true)
                .HasColumnName("total_amount");
            entity.Property(e => e.WorkOrderId).HasColumnName("work_order_id");

            entity.HasOne(d => d.Client).WithMany(p => p.Invoices)
                .HasForeignKey(d => d.ClientId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("invoices_client_id_fkey");

            entity.HasOne(d => d.WorkOrder).WithMany(p => p.Invoices)
                .HasForeignKey(d => d.WorkOrderId)
                .HasConstraintName("invoices_work_order_id_fkey");
        });

        // ============================================================
        // MECHANICS
        // ============================================================
        modelBuilder.Entity<Mechanic>(entity =>
        {
            entity.HasKey(e => e.MechanicId).HasName("mechanics_pkey");
            entity.ToTable("mechanics");
            entity.HasIndex(e => e.UserId, "mechanics_user_id_key").IsUnique();

            entity.Property(e => e.MechanicId).HasColumnName("mechanic_id");
            entity.Property(e => e.HireDate).HasColumnName("hire_date");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true).HasColumnName("is_active");
            entity.Property(e => e.SpecializationId).HasColumnName("specialization_id");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.Specialization).WithMany(p => p.Mechanics)
                .HasForeignKey(d => d.SpecializationId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("mechanics_specialization_id_fkey");

            entity.HasOne(d => d.User).WithOne(p => p.Mechanic)
                .HasForeignKey<Mechanic>(d => d.UserId)
                .HasConstraintName("mechanics_user_id_fkey");

            entity.HasMany(d => d.Services).WithMany(p => p.Mechanics)
                .UsingEntity<Dictionary<string, object>>(
                    "MechanicService",
                    r => r.HasOne<Service>().WithMany()
                        .HasForeignKey("ServiceId")
                        .HasConstraintName("mechanic_services_service_id_fkey"),
                    l => l.HasOne<Mechanic>().WithMany()
                        .HasForeignKey("MechanicId")
                        .HasConstraintName("mechanic_services_mechanic_id_fkey"),
                    j =>
                    {
                        j.HasKey("MechanicId", "ServiceId").HasName("mechanic_services_pkey");
                        j.ToTable("mechanic_services");
                        j.IndexerProperty<int>("MechanicId").HasColumnName("mechanic_id");
                        j.IndexerProperty<int>("ServiceId").HasColumnName("service_id");
                    });
        });

        // ============================================================
        // MECHANIC_SCHEDULES
        // ============================================================
        modelBuilder.Entity<MechanicSchedule>(entity =>
        {
            entity.HasKey(e => e.ScheduleId).HasName("mechanic_schedules_pkey");
            entity.ToTable("mechanic_schedules");

            entity.HasIndex(e => e.WorkDate, "ix_sched_date");
            entity.HasIndex(e => new { e.MechanicId, e.WorkDate, e.StartTime }, "uq_sched_slot").IsUnique();

            entity.Property(e => e.ScheduleId).HasColumnName("schedule_id");
            entity.Property(e => e.EndTime).HasColumnName("end_time");
            entity.Property(e => e.MechanicId).HasColumnName("mechanic_id");
            entity.Property(e => e.RepairPlaceId).HasColumnName("repair_place_id");
            entity.Property(e => e.StartTime).HasColumnName("start_time");
            entity.Property(e => e.Status)
                .HasConversion<string>()
                .HasColumnName("status");
            entity.Property(e => e.WorkDate).HasColumnName("work_date");

            entity.HasOne(d => d.Mechanic).WithMany(p => p.MechanicSchedules)
                .HasForeignKey(d => d.MechanicId)
                .HasConstraintName("mechanic_schedules_mechanic_id_fkey");

            entity.HasOne(d => d.RepairPlace).WithMany(p => p.MechanicSchedules)
                .HasForeignKey(d => d.RepairPlaceId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("mechanic_schedules_repair_place_id_fkey");
        });

        // ============================================================
        // PARTS
        // ============================================================
        modelBuilder.Entity<Part>(entity =>
        {
            entity.HasKey(e => e.PartId).HasName("parts_pkey");
            entity.ToTable("parts");

            entity.HasIndex(e => e.StockQuantity, "ix_parts_stock");
            entity.HasIndex(e => e.SupplierId, "ix_parts_supplier");
            entity.HasIndex(e => e.Article, "parts_article_key").IsUnique();

            entity.Property(e => e.PartId).HasColumnName("part_id");
            entity.Property(e => e.Article).HasMaxLength(64).HasColumnName("article");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true).HasColumnName("is_active");
            entity.Property(e => e.MinStock)
                .HasDefaultValue(0).HasColumnName("min_stock");
            entity.Property(e => e.Name).HasMaxLength(200).HasColumnName("name");
            entity.Property(e => e.PurchasePrice)
                .HasPrecision(12, 2).HasColumnName("purchase_price");
            entity.Property(e => e.SalePrice)
                .HasPrecision(12, 2).HasColumnName("sale_price");
            entity.Property(e => e.StockQuantity)
                .HasDefaultValue(0).HasColumnName("stock_quantity");
            entity.Property(e => e.SupplierId).HasColumnName("supplier_id");

            entity.HasOne(d => d.Supplier).WithMany(p => p.Parts)
                .HasForeignKey(d => d.SupplierId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("parts_supplier_id_fkey");
        });

        // ============================================================
        // PAYMENTS
        // ============================================================
        modelBuilder.Entity<Payment>(entity =>
        {
            entity.HasKey(e => e.PaymentId).HasName("payments_pkey");
            entity.ToTable("payments");

            entity.HasIndex(e => e.InvoiceId, "ix_pay_invoice");
            entity.HasIndex(e => e.TransactionNumber, "payments_transaction_number_key").IsUnique();

            entity.Property(e => e.PaymentId).HasColumnName("payment_id");
            entity.Property(e => e.Amount)
                .HasPrecision(12, 2).HasColumnName("amount");
            entity.Property(e => e.InvoiceId).HasColumnName("invoice_id");
            entity.Property(e => e.Method)
                .HasConversion<string>()
                .HasColumnName("method");
            entity.Property(e => e.PaidAt)
                .HasDefaultValueSql("now()").HasColumnName("paid_at");
            entity.Property(e => e.Status)
                .HasConversion<string>()
                .HasColumnName("status");
            entity.Property(e => e.TransactionNumber)
                .HasMaxLength(64).HasColumnName("transaction_number");

            entity.HasOne(d => d.Invoice).WithMany(p => p.Payments)
                .HasForeignKey(d => d.InvoiceId)
                .HasConstraintName("payments_invoice_id_fkey");
        });

        // ============================================================
        // REPAIR_PLACES
        // ============================================================
        modelBuilder.Entity<RepairPlace>(entity =>
        {
            entity.HasKey(e => e.PlaceId).HasName("repair_places_pkey");
            entity.ToTable("repair_places");
            entity.HasIndex(e => e.Name, "repair_places_name_key").IsUnique();

            entity.Property(e => e.PlaceId).HasColumnName("place_id");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true).HasColumnName("is_active");
            entity.Property(e => e.Kind)
                .HasConversion<string>()
                .HasColumnName("kind");
            entity.Property(e => e.Name).HasMaxLength(100).HasColumnName("name");
        });

        // ============================================================
        // REVIEWS
        // ============================================================
        modelBuilder.Entity<Review>(entity =>
        {
            entity.HasKey(e => e.ReviewId).HasName("reviews_pkey");
            entity.ToTable("reviews");
            entity.HasIndex(e => new { e.ClientId, e.WorkOrderId }, "uq_review").IsUnique();

            entity.Property(e => e.ReviewId).HasColumnName("review_id");
            entity.Property(e => e.ClientId).HasColumnName("client_id");
            entity.Property(e => e.Comment).HasColumnName("comment");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()").HasColumnName("created_at");
            entity.Property(e => e.Rating).HasColumnName("rating");
            entity.Property(e => e.WorkOrderId).HasColumnName("work_order_id");

            entity.HasOne(d => d.Client).WithMany(p => p.Reviews)
                .HasForeignKey(d => d.ClientId)
                .HasConstraintName("reviews_client_id_fkey");

            entity.HasOne(d => d.WorkOrder).WithMany(p => p.Reviews)
                .HasForeignKey(d => d.WorkOrderId)
                .HasConstraintName("reviews_work_order_id_fkey");
        });

        // ============================================================
        // ROLES
        // ============================================================
        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(e => e.RoleId).HasName("roles_pkey");
            entity.ToTable("roles");
            entity.HasIndex(e => e.Code, "roles_code_key").IsUnique();
            entity.HasIndex(e => e.Name, "roles_name_key").IsUnique();

            entity.Property(e => e.RoleId).HasColumnName("role_id");
            entity.Property(e => e.Code).HasMaxLength(32).HasColumnName("code");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.Name).HasMaxLength(64).HasColumnName("name");
        });

        // ============================================================
        // SERVICES
        // ============================================================
        modelBuilder.Entity<Service>(entity =>
        {
            entity.HasKey(e => e.ServiceId).HasName("services_pkey");
            entity.ToTable("services");

            entity.HasIndex(e => e.DurationMinutes, "ix_services_duration");
            entity.HasIndex(e => e.BasePrice, "ix_services_price");
            entity.HasIndex(e => e.Code, "services_code_key").IsUnique();

            entity.Property(e => e.ServiceId).HasColumnName("service_id");
            entity.Property(e => e.BasePrice)
                .HasPrecision(12, 2).HasColumnName("base_price");
            entity.Property(e => e.Code).HasMaxLength(32).HasColumnName("code");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.DurationMinutes).HasColumnName("duration_minutes");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true).HasColumnName("is_active");
            entity.Property(e => e.Name).HasMaxLength(150).HasColumnName("name");
        });

        // ============================================================
        // SPECIALIZATIONS
        // ============================================================
        modelBuilder.Entity<Specialization>(entity =>
        {
            entity.HasKey(e => e.SpecializationId).HasName("specializations_pkey");
            entity.ToTable("specializations");
            entity.HasIndex(e => e.Name, "specializations_name_key").IsUnique();

            entity.Property(e => e.SpecializationId).HasColumnName("specialization_id");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.Name).HasMaxLength(100).HasColumnName("name");
        });

        // ============================================================
        // SUPPLIERS
        // ============================================================
        modelBuilder.Entity<Supplier>(entity =>
        {
            entity.HasKey(e => e.SupplierId).HasName("suppliers_pkey");
            entity.ToTable("suppliers");
            entity.HasIndex(e => e.Name, "suppliers_name_key").IsUnique();

            entity.Property(e => e.SupplierId).HasColumnName("supplier_id");
            entity.Property(e => e.Address).HasMaxLength(255).HasColumnName("address");
            entity.Property(e => e.Email).HasMaxLength(150).HasColumnName("email");
            entity.Property(e => e.Inn).HasMaxLength(12).HasColumnName("inn");
            entity.Property(e => e.Name).HasMaxLength(150).HasColumnName("name");
            entity.Property(e => e.Phone).HasMaxLength(32).HasColumnName("phone");
        });

        // ============================================================
        // USERS
        // ============================================================
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.UserId).HasName("users_pkey");
            entity.ToTable("users");
            entity.HasIndex(e => e.Email, "users_email_key").IsUnique();
            entity.HasIndex(e => e.Login, "users_login_key").IsUnique();

            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()").HasColumnName("created_at");
            entity.Property(e => e.DepartmentId).HasColumnName("department_id");
            entity.Property(e => e.Email).HasMaxLength(150).HasColumnName("email");
            entity.Property(e => e.FullName).HasMaxLength(150).HasColumnName("full_name");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true).HasColumnName("is_active");
            entity.Property(e => e.Login).HasMaxLength(64).HasColumnName("login");
            entity.Property(e => e.PasswordHash).HasMaxLength(255).HasColumnName("password_hash");
            entity.Property(e => e.Phone).HasMaxLength(32).HasColumnName("phone");
            entity.Property(e => e.RoleId).HasColumnName("role_id");

            entity.HasOne(d => d.Department).WithMany(p => p.Users)
                .HasForeignKey(d => d.DepartmentId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("users_department_id_fkey");

            entity.HasOne(d => d.Role).WithMany(p => p.Users)
                .HasForeignKey(d => d.RoleId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("users_role_id_fkey");
        });

        // ============================================================
        // WORK_ORDERS
        // ============================================================
        modelBuilder.Entity<WorkOrder>(entity =>
        {
            entity.HasKey(e => e.WorkOrderId).HasName("work_orders_pkey");
            entity.ToTable("work_orders");

            entity.HasIndex(e => e.ClientId, "ix_wo_client");
            entity.HasIndex(e => e.OpenedAt, "ix_wo_created");
            entity.HasIndex(e => e.MechanicId, "ix_wo_mechanic");
            entity.HasIndex(e => e.Status, "ix_wo_status"); // ← восстановлено
            entity.HasIndex(e => e.AppointmentId, "work_orders_appointment_id_key").IsUnique();

            entity.Property(e => e.WorkOrderId).HasColumnName("work_order_id");
            entity.Property(e => e.AppointmentId).HasColumnName("appointment_id");
            entity.Property(e => e.CarId).HasColumnName("car_id");
            entity.Property(e => e.ClientId).HasColumnName("client_id");
            entity.Property(e => e.ClosedAt).HasColumnName("closed_at");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.MechanicId).HasColumnName("mechanic_id");
            entity.Property(e => e.OpenedAt)
                .HasDefaultValueSql("now()").HasColumnName("opened_at");
            entity.Property(e => e.Status)
                .HasConversion<string>()
                .HasColumnName("status");

            entity.HasOne(d => d.Appointment).WithOne(p => p.WorkOrder)
                .HasForeignKey<WorkOrder>(d => d.AppointmentId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("work_orders_appointment_id_fkey");

            entity.HasOne(d => d.Car).WithMany(p => p.WorkOrders)
                .HasForeignKey(d => d.CarId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("work_orders_car_id_fkey");

            entity.HasOne(d => d.Client).WithMany(p => p.WorkOrders)
                .HasForeignKey(d => d.ClientId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("work_orders_client_id_fkey");

            entity.HasOne(d => d.Mechanic).WithMany(p => p.WorkOrders)
                .HasForeignKey(d => d.MechanicId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("work_orders_mechanic_id_fkey");
        });

        // ============================================================
        // WORK_ORDER_PARTS
        // ============================================================
        modelBuilder.Entity<WorkOrderPart>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("work_order_parts_pkey");
            entity.ToTable("work_order_parts");

            entity.HasIndex(e => e.WorkOrderId, "ix_wop_wo");
            entity.HasIndex(e => new { e.WorkOrderId, e.PartId }, "uq_wop").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.PartId).HasColumnName("part_id");
            entity.Property(e => e.Quantity).HasColumnName("quantity");
            entity.Property(e => e.UnitPrice)
                .HasPrecision(12, 2).HasColumnName("unit_price");
            entity.Property(e => e.WorkOrderId).HasColumnName("work_order_id");

            entity.HasOne(d => d.Part).WithMany(p => p.WorkOrderParts)
                .HasForeignKey(d => d.PartId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("work_order_parts_part_id_fkey");

            entity.HasOne(d => d.WorkOrder).WithMany(p => p.WorkOrderParts)
                .HasForeignKey(d => d.WorkOrderId)
                .HasConstraintName("work_order_parts_work_order_id_fkey");
        });

        // ============================================================
        // WORK_ORDER_SERVICES
        // ============================================================
        modelBuilder.Entity<WorkOrderService>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("work_order_services_pkey");
            entity.ToTable("work_order_services");

            entity.HasIndex(e => e.WorkOrderId, "ix_wos_wo");
            entity.HasIndex(e => new { e.WorkOrderId, e.ServiceId, e.MechanicId }, "uq_wos").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.MechanicId).HasColumnName("mechanic_id");
            entity.Property(e => e.Quantity)
                .HasDefaultValue(1).HasColumnName("quantity");
            entity.Property(e => e.ServiceId).HasColumnName("service_id");
            entity.Property(e => e.UnitPrice)
                .HasPrecision(12, 2).HasColumnName("unit_price");
            entity.Property(e => e.WorkOrderId).HasColumnName("work_order_id");

            entity.HasOne(d => d.Mechanic).WithMany(p => p.WorkOrderServices)
                .HasForeignKey(d => d.MechanicId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("work_order_services_mechanic_id_fkey");

            entity.HasOne(d => d.Service).WithMany(p => p.WorkOrderServices)
                .HasForeignKey(d => d.ServiceId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("work_order_services_service_id_fkey");

            entity.HasOne(d => d.WorkOrder).WithMany(p => p.WorkOrderServices)
                .HasForeignKey(d => d.WorkOrderId)
                .HasConstraintName("work_order_services_work_order_id_fkey");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}