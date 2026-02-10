# MiniDashboard

**MiniDashboard** is a .NET 8.0 inventory management system consisting of a WPF desktop client and an ASP.NET Core Web API backend. It supports full CRUD operations, discount management, advanced filtering, drag-and-drop interactions, and offline caching.

## Architecture

The solution follows a layered architecture with clear separation of concerns:

```
MiniDashboard.slnx
├── MiniDashboard.Api              ASP.NET Core Web API
│   ├── Controllers/               API endpoints (Items, Categories, Types)
│   ├── BL/                        Business logic layer
│   │   ├── Interfaces/            IItemService, IItemMapper
│   │   └── Classes/               ItemService, ItemMapper
│   ├── DAL/                       Data access layer (JSON file persistence)
│   │   ├── Interfaces/            IItemRepository, ICategoryRepository, ITypeRepository
│   │   ├── Classes/               JSON-backed repository implementations
│   │   └── ObjectClasses/         Domain entities (Item, Category, ItemType)
│   └── Data/                      JSON data files (items, categories, types)
│
├── MiniDashboard.App              WPF desktop client
│   ├── ViewModels/                MVVM ViewModels (DashboardViewModel, ItemDialogViewModel, ItemFilterModel)
│   ├── Views/                     XAML views and windows
│   ├── Behaviors/                 WPF behaviors (drag-and-drop, keyboard shortcuts)
│   └── Services/                  API client services (ItemApiService, CachedItemApiService)
│
├── MiniDashboard.DTOs             Shared data transfer objects
│   └── Classes/                   ItemDto, CreateItemDto, UpdateItemDto, TypeDto, CategoryDto
│
├── MiniDashboard.Tests            Unit tests (xUnit + Moq)
│   ├── BL/                        Business logic tests
│   ├── Controllers/               API controller tests
│   └── WPF/                       ViewModel, service, and filter tests
│
├── MiniDashboard.IntegrationTests Integration tests (xUnit + WebApplicationFactory)
│
└── MiniDashboard.UITest           UI automation tests (xUnit + FlaUI)
```

### Key Design Patterns

- **MVVM** — The WPF client uses CommunityToolkit.Mvvm with `ObservableObject`, `[ObservableProperty]`, and `[RelayCommand]` for clean data binding with no code-behind logic
- **Repository Pattern** — Data access is abstracted behind `IItemRepository`, `ICategoryRepository`, and `ITypeRepository` interfaces with JSON file-backed implementations
- **Service Layer** — `ItemService` contains business rules (uniqueness validation, product code generation, discount date management)
- **Decorator Pattern** — `CachedItemApiService` wraps `ItemApiService` to provide transparent offline caching with JSON file fallback
- **Dependency Injection** — Both the API and WPF projects use Microsoft.Extensions.DependencyInjection

> **Note:** `INotifyPropertyChanged` is not implemented on DTO/collection item classes because all data mutations go through the API and reload entire collections via `LoadDataAsync()`. Individual item property changes are never made in-place in the UI, making per-item change notification unnecessary.

### API Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/items` | List all items |
| GET | `/api/items/{id}` | Get item by ID |
| GET | `/api/items/search?query=` | Search items |
| POST | `/api/items` | Create item |
| PUT | `/api/items/{id}` | Update item |
| DELETE | `/api/items/{id}` | Delete item |
| GET | `/api/types` | List all types |
| GET | `/api/categories` | List categories (optional `?typeId=`) |

### Business Rules

- Item names and descriptions must be unique (case-insensitive)
- Product codes are auto-generated from category prefix (e.g. `PHN-001`, `LPT-002`)
- Discount dates are automatically managed: set when a discount is first applied, cleared when removed, preserved when updated

## Features

- Two-grid interface: Discounted Items and All Items
- Advanced per-column filtering (text, numeric ranges, date ranges)
- Drag-and-drop discount management between grids
- Add/Edit/Delete items via dialogs
- Loading spinners with overlay
- Offline mode with local JSON file caching
- Swagger API documentation (auto-opens on API launch)
- Keyboard shortcuts: **Ctrl + Plus** (Add), **Enter** (Edit selected), **Delete** (Delete selected)

## Prerequisites

- .NET 8.0 SDK
- Windows (WPF requires Windows)

## Getting Started

1. Clone the repository
2. Open `MiniDashboard.slnx` in Visual Studio
3. Set multiple startup projects: **MiniDashboard.Api** and **MiniDashboard.App**
4. Press F5 to run both projects

The API starts on `https://localhost:7233` with Swagger UI opening automatically. The WPF client connects to this address.

## Testing

The solution includes 124 automated tests across two test projects, plus 5 UI automation tests, using **xUnit**, **Moq**, **FluentAssertions**, and **FlaUI**.

### Running Tests

```bash
# Run all tests
dotnet test

# Run unit tests only
dotnet test MiniDashboard.Tests

# Run integration tests only
dotnet test MiniDashboard.IntegrationTests

# Run UI automation tests (requires no running instances of the API or App)
dotnet test MiniDashboard.UITest
```

### Unit Tests (MiniDashboard.Tests) — 98 tests

**API Controller Tests** (14 tests)
- `ItemsControllerTests` — Verifies each action method returns correct HTTP result types (Ok, NotFound, CreatedAtAction, Conflict, NoContent) with mocked `IItemService`
- `CategoriesControllerTests` — Tests category retrieval with and without type filtering
- `TypesControllerTests` — Tests type listing

**Business Logic Tests** (23 tests)
- `ItemServiceTests` — Tests all business rules: uniqueness validation (name/description, case-insensitive), product code generation and incrementing, discount date lifecycle (set/clear/preserve), search behavior, entity mapping and enrichment with type/category names
- `ItemMapperTests` — Tests pure mapping between entities and DTOs, including discount date assignment on creation

**WPF ViewModel Tests** (27 tests)
- `ItemDialogViewModelTests` — Tests constructor modes (add/edit), form validation (5 rules), save operations (create/update), type-to-category filtering, error handling for HttpRequestException, ApiException, and generic exceptions
- `DashboardViewModelTests` — Tests initialization, drop-on-all-items discount removal, and error handling paths

**WPF Service Tests** (12 tests)
- `CachedItemApiServiceTests` — Tests online API passthrough, disk caching on success, offline fallback to cached data, empty list when no cache exists, IsOffline flag toggling, type-specific cache files, and write operation delegation

**WPF Filter Tests** (22 tests)
- `ItemFilterModelTests` — Tests all filter types: string contains (case-insensitive), numeric ranges (price/quantity/discount), date ranges (created/updated/discount date), null date handling, multiple combined filters, invalid numeric input handling, clear command, and FiltersChanged callback

### Integration Tests (MiniDashboard.IntegrationTests) — 26 tests

Uses `WebApplicationFactory<Program>` with isolated temporary data directories to test the full HTTP request/response cycle without affecting real data.

**Items Endpoint Tests** (22 tests)
- Full CRUD lifecycle (create, read, update, delete)
- Model validation (missing name returns 400, price=0 returns 400, discount>100 returns 400)
- Business rule enforcement (duplicate name/description returns 409 Conflict)
- Not found scenarios (get/update/delete non-existent items returns 404)
- Discount date behavior (set when applied, cleared when removed)
- Search functionality (matching and non-matching queries)
- End-to-end CRUD flow test

**Categories Endpoint Tests** (3 tests)
- Returns all seeded categories
- Filters by typeId
- Returns empty for non-existent typeId

**Types Endpoint Tests** (1 test)
- Returns all seeded types

### UI Automation Tests (MiniDashboard.UITest) — 5 tests

Uses **FlaUI** (UIA3) to drive the full application end-to-end. The test fixture automatically builds the solution, starts the API, cleans up leftover test data, launches the WPF app, and tears everything down after all tests complete.

- Add item via dialog, verify it appears in All Items grid
- Edit item via toolbar Edit button, verify changes reflected in grid
- Add discount to item, verify it appears in Discounted Items grid
- Remove discount, verify item disappears from Discounted Items grid
- Delete item with confirmation dialog, verify removal from grid
