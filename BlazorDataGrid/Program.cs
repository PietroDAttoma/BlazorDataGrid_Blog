using BlazorDataGrid.Components;      // ✅ Namespace dei componenti Blazor personalizzati
using BlazorDataGrid.Data;            // ✅ Contiene il DbContext e configurazioni dati
using BlazorDataGrid.UoW;             // ✅ Implementazione del pattern Unit of Work
using Microsoft.EntityFrameworkCore;  // ✅ EF Core per accesso ai dati
using Radzen;                         // ✅ Libreria UI Radzen per componenti e servizi

var builder = WebApplication.CreateBuilder(args);

// ✅ Registra il factory per creare DbContext isolati tramite IDbContextFactory<MyContext>
// Questo approccio è ideale per Blazor Server, dove il DbContext non deve essere condiviso tra componenti
builder.Services.AddDbContextFactory<MyContext>(options =>
{
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("MyContext")
        ?? throw new InvalidOperationException("Connection string 'MyContext' not found.") // 🔴 Protezione contro connessione mancante
    );

    // ✅ Abilita il logging dettagliato solo in ambiente di sviluppo
    if (builder.Environment.IsDevelopment())
    {
        options
            .EnableSensitiveDataLogging() // 🔍 Mostra i valori delle entità
            .LogTo(Console.WriteLine,      // 🖥️ Log su console
                new[] {
                DbLoggerCategory.ChangeTracking.Name,   // 🔄 Tracciamento entità
                DbLoggerCategory.Database.Command.Name  // 🗄️ Comandi SQL
                },
                LogLevel.Information); // ℹ️ Livello di dettaglio: Informazioni
    }
});

// ✅ Abilita la pagina degli errori dettagliati per EF Core (solo in sviluppo)
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

// ✅ Registra i componenti e servizi Radzen (DialogService, NotificationService, ecc.)
builder.Services.AddRadzenComponents();

// ✅ Abilita i componenti Razor interattivi lato server (Blazor Server)
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// ✅ Registra UnitOfWork come Transient → ogni componente riceve un'istanza isolata
// 🔥 Questa modifica risolve il crash causato da Scoped DbContext condiviso tra componenti
builder.Services.AddTransient<IUnitOfWork, UnitOfWork>();

var app = builder.Build();

// ✅ Configurazioni specifiche per ambienti non di sviluppo
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true); // 🔒 Gestione globale degli errori
    app.UseHsts(); // 🔐 Abilita HTTP Strict Transport Security
    app.UseMigrationsEndPoint(); // ⚠️ Applica eventuali migrazioni EF Core all'avvio
}

// ✅ Reindirizza automaticamente da HTTP a HTTPS
app.UseHttpsRedirection();

// ✅ Protezione contro attacchi CSRF (Cross-Site Request Forgery)
app.UseAntiforgery();

// ✅ Mappa gli asset statici (CSS, JS, immagini, ecc.)
app.MapStaticAssets();

// ✅ Mappa i componenti Razor e abilita il rendering interattivo lato server
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// ✅ Avvia l'applicazione
app.Run();