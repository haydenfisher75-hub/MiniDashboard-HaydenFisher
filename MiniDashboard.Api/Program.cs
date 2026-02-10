using System.Reflection;
using MiniDashboard.BL.Classes;
using MiniDashboard.BL.Interfaces;
using MiniDashboard.DAL.Classes;
using MiniDashboard.DAL.Interfaces;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, xmlFile));
});

var dataDir = Path.Combine(AppContext.BaseDirectory, "Data");
var itemsPath = Path.Combine(dataDir, "items.json");
var deletedItemsPath = Path.Combine(dataDir, "deleted-items.json");
var typesPath = Path.Combine(dataDir, "types.json");
var categoriesPath = Path.Combine(dataDir, "categories.json");

builder.Services.AddSingleton<IItemRepository>(_ => new JsonItemRepository(itemsPath, deletedItemsPath));
builder.Services.AddSingleton<ITypeRepository>(_ => new JsonTypeRepository(typesPath));
builder.Services.AddSingleton<ICategoryRepository>(_ => new JsonCategoryRepository(categoriesPath));
builder.Services.AddSingleton<IItemMapper, ItemMapper>();
builder.Services.AddScoped<IItemService, ItemService>();

var app = builder.Build();

// Ensure data directory exists
Directory.CreateDirectory(dataDir);

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.MapControllers();

app.Run();

public partial class Program { }
