using FlaUI.Core.AutomationElements;
using FlaUI.Core.Input;
using FlaUI.Core.Tools;
using FlaUI.Core.WindowsAPI;
using FluentAssertions;
using Xunit;

namespace MiniDashboard.UITest;

[Collection("UI Tests")]
[TestCaseOrderer("MiniDashboard.UITest.AlphabeticalOrderer", "MiniDashboard.UITest")]
public class DashboardUITests : IClassFixture<AppFixture>
{
    private readonly AppFixture _fixture;

    public DashboardUITests(AppFixture fixture)
    {
        _fixture = fixture;
    }

    // ---- Helpers ----

    private AutomationElement[] GetGridRows(AutomationElement grid)
    {
        var dataGrid = grid.AsGrid();
        return dataGrid.Rows;
    }

    private bool GridContainsItem(AutomationElement grid, string name)
    {
        var rows = GetGridRows(grid);
        return rows.Any(r =>
        {
            var cells = r.AsGridRow().Cells;
            return cells.Any(c => c.AsLabel().Text.Contains(name, StringComparison.OrdinalIgnoreCase));
        });
    }

    private AutomationElement? FindRowByName(AutomationElement grid, string name)
    {
        var rows = GetGridRows(grid);
        return rows.FirstOrDefault(r =>
        {
            var cells = r.AsGridRow().Cells;
            return cells.Any(c => c.AsLabel().Text.Contains(name, StringComparison.OrdinalIgnoreCase));
        });
    }

    private string GetCellText(AutomationElement row, int columnIndex)
    {
        var cells = row.AsGridRow().Cells;
        return cells[columnIndex].AsLabel().Text;
    }

    private void FillDialogAndSave(Window dialog, string? name = null, string? description = null,
        string? typeName = null, string? categoryName = null, string? price = null,
        string? quantity = null, string? discount = null)
    {
        if (name != null)
        {
            var nameBox = dialog.FindFirstDescendant(cf => cf.ByAutomationId("ItemName")).AsTextBox();
            nameBox.Text = "";
            nameBox.Enter(name);
        }

        if (description != null)
        {
            var descBox = dialog.FindFirstDescendant(cf => cf.ByAutomationId("ItemDescription")).AsTextBox();
            descBox.Text = "";
            descBox.Enter(description);
        }

        if (typeName != null)
        {
            var typeCombo = dialog.FindFirstDescendant(cf => cf.ByAutomationId("ItemType")).AsComboBox();
            typeCombo.Select(typeName);
            Wait.UntilInputIsProcessed(TimeSpan.FromMilliseconds(500));
        }

        if (categoryName != null)
        {
            var catCombo = dialog.FindFirstDescendant(cf => cf.ByAutomationId("ItemCategory")).AsComboBox();
            catCombo.Select(categoryName);
        }

        if (price != null)
        {
            var priceBox = dialog.FindFirstDescendant(cf => cf.ByAutomationId("ItemPrice")).AsTextBox();
            priceBox.Text = "";
            priceBox.Enter(price);
        }

        if (quantity != null)
        {
            var qtyBox = dialog.FindFirstDescendant(cf => cf.ByAutomationId("ItemQuantity")).AsTextBox();
            qtyBox.Text = "";
            qtyBox.Enter(quantity);
        }

        if (discount != null)
        {
            var discBox = dialog.FindFirstDescendant(cf => cf.ByAutomationId("ItemDiscount")).AsTextBox();
            discBox.Text = "";
            discBox.Enter(discount);
        }

        // Click Save
        var saveBtn = dialog.FindFirstDescendant(cf => cf.ByAutomationId("SaveButton")).AsButton();
        saveBtn.Invoke();
        Wait.UntilInputIsProcessed(TimeSpan.FromMilliseconds(1000));
    }

    // ---- Tests (ordered) ----

    [Fact]
    public void T1_AddItem_AppearsInAllItemsGrid()
    {
        // Click Add button
        var addBtn = _fixture.FindById("AddButton").AsButton();
        addBtn.Invoke();

        // Wait for dialog
        var dialog = _fixture.WaitForDialog("Add Item");
        dialog.Should().NotBeNull("Add Item dialog should appear");

        // Fill in the form
        FillDialogAndSave(dialog,
            name: "UI Test Item",
            description: "Test Description",
            typeName: "Devices",
            categoryName: "Laptop",
            price: "25.99",
            quantity: "5");

        // Wait for data reload
        Wait.UntilInputIsProcessed(TimeSpan.FromMilliseconds(2000));

        // Verify item appears in AllItemsGrid
        var allGrid = _fixture.GetAllItemsGrid();
        GridContainsItem(allGrid, "UI Test Item").Should().BeTrue("newly added item should appear in All Items grid");
    }

    [Fact]
    public void T2_EditItem_ChangesReflectedInGrid()
    {
        // Find and double-click the item in AllItemsGrid
        var allGrid = _fixture.GetAllItemsGrid();
        var row = FindRowByName(allGrid, "UI Test Item");
        row.Should().NotBeNull("item should exist in grid before editing");
        row!.DoubleClick();

        // Wait for edit dialog
        var dialog = _fixture.WaitForDialog("Edit Item");
        dialog.Should().NotBeNull("Edit Item dialog should appear");

        // Change name and price
        FillDialogAndSave(dialog,
            name: "UI Test Edited",
            price: "35.99");

        // Wait for data reload
        Wait.UntilInputIsProcessed(TimeSpan.FromMilliseconds(2000));

        // Verify changes
        allGrid = _fixture.GetAllItemsGrid();
        GridContainsItem(allGrid, "UI Test Edited").Should().BeTrue("edited name should appear");
        GridContainsItem(allGrid, "UI Test Item").Should().BeFalse("old name should be gone");
    }

    [Fact]
    public void T3_AddDiscount_ItemAppearsInDiscountedGrid()
    {
        // Double-click item in AllItemsGrid
        var allGrid = _fixture.GetAllItemsGrid();
        var row = FindRowByName(allGrid, "UI Test Edited");
        row.Should().NotBeNull();
        row!.DoubleClick();

        var dialog = _fixture.WaitForDialog("Edit Item");
        dialog.Should().NotBeNull();

        // Set discount to 15
        FillDialogAndSave(dialog, discount: "15");

        Wait.UntilInputIsProcessed(TimeSpan.FromMilliseconds(2000));

        // Verify item appears in DiscountedGrid
        var discountedGrid = _fixture.GetDiscountedGrid();
        GridContainsItem(discountedGrid, "UI Test Edited").Should()
            .BeTrue("item with discount should appear in Discounted Items grid");
    }

    [Fact]
    public void T4_RemoveDiscount_ItemDisappearsFromDiscountedGrid()
    {
        // Double-click item in DiscountedGrid
        var discountedGrid = _fixture.GetDiscountedGrid();
        var row = FindRowByName(discountedGrid, "UI Test Edited");
        row.Should().NotBeNull("item should be in discounted grid");
        row!.DoubleClick();

        var dialog = _fixture.WaitForDialog("Edit Item");
        dialog.Should().NotBeNull();

        // Set discount to 0
        FillDialogAndSave(dialog, discount: "0");

        Wait.UntilInputIsProcessed(TimeSpan.FromMilliseconds(2000));

        // Verify item is no longer in DiscountedGrid
        discountedGrid = _fixture.GetDiscountedGrid();
        GridContainsItem(discountedGrid, "UI Test Edited").Should()
            .BeFalse("item without discount should not appear in Discounted Items grid");
    }

    [Fact]
    public void T5_FilterByName_ShowsMatchingResults()
    {
        // Type into the AllItems Name filter
        var nameFilter = _fixture.FindById("AllItemsNameFilter").AsTextBox();
        nameFilter.Text = "";
        nameFilter.Enter("UI Test Edited");

        Wait.UntilInputIsProcessed(TimeSpan.FromMilliseconds(1000));

        // Verify only matching rows are shown
        var allGrid = _fixture.GetAllItemsGrid();
        var rows = GetGridRows(allGrid);
        rows.Should().HaveCountGreaterOrEqualTo(1, "at least one row should match the filter");
        foreach (var row in rows)
        {
            var cells = row.AsGridRow().Cells;
            var hasMatch = cells.Any(c =>
                c.AsLabel().Text.Contains("UI Test Edited", StringComparison.OrdinalIgnoreCase));
            hasMatch.Should().BeTrue("all visible rows should contain the filter text");
        }

        // Clear the filter
        nameFilter.Text = "";
        Wait.UntilInputIsProcessed(TimeSpan.FromMilliseconds(500));
    }

    [Fact]
    public void T6_FilterByPrice_ShowsMatchingResults()
    {
        // Set price range filter
        var priceMin = _fixture.FindById("AllItemsPriceMinFilter").AsTextBox();
        var priceMax = _fixture.FindById("AllItemsPriceMaxFilter").AsTextBox();

        priceMin.Text = "";
        priceMin.Enter("30");
        priceMax.Text = "";
        priceMax.Enter("40");

        Wait.UntilInputIsProcessed(TimeSpan.FromMilliseconds(1000));

        // Verify our item (price 35.99) is in the results
        var allGrid = _fixture.GetAllItemsGrid();
        GridContainsItem(allGrid, "UI Test Edited").Should()
            .BeTrue("item with price 35.99 should be visible when filtering 30-40");

        // Clear filters
        priceMin.Text = "";
        priceMax.Text = "";
        Wait.UntilInputIsProcessed(TimeSpan.FromMilliseconds(500));
    }

    [Fact]
    public void T7_DeleteItem_RemovedFromGrid()
    {
        // Select the item in AllItemsGrid
        var allGrid = _fixture.GetAllItemsGrid();
        var row = FindRowByName(allGrid, "UI Test Edited");
        row.Should().NotBeNull("item should exist before deletion");
        row!.Click();

        // Click Delete button
        var deleteBtn = _fixture.FindById("DeleteButton").AsButton();
        deleteBtn.Invoke();

        // Handle the confirmation MessageBox
        var msgBox = _fixture.WaitForMessageBox("Confirm Delete");
        msgBox.Should().NotBeNull("confirmation dialog should appear");

        // Click Yes
        var yesButton = msgBox.FindFirstDescendant(cf => cf.ByName("Yes"))?.AsButton();
        yesButton.Should().NotBeNull("Yes button should exist in confirmation dialog");
        yesButton!.Invoke();

        Wait.UntilInputIsProcessed(TimeSpan.FromMilliseconds(2000));

        // Verify item is gone
        allGrid = _fixture.GetAllItemsGrid();
        GridContainsItem(allGrid, "UI Test Edited").Should()
            .BeFalse("deleted item should no longer appear in the grid");
    }
}
