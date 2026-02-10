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

    /// <summary>
    /// Waits for a ComboBox to have items loaded, then selects by text.
    /// </summary>
    private void WaitAndSelectComboBox(Window dialog, string automationId, string itemText)
    {
        var combo = Retry.WhileNull(
            () =>
            {
                var el = dialog.FindFirstDescendant(cf => cf.ByAutomationId(automationId));
                if (el == null) return null;
                var cb = el.AsComboBox();
                if (cb.Items.Length == 0) return null;
                return cb;
            },
            TimeSpan.FromSeconds(10),
            TimeSpan.FromMilliseconds(250)).Result;

        combo.Should().NotBeNull($"{automationId} ComboBox should have items");
        combo!.Select(itemText);
        Wait.UntilInputIsProcessed(TimeSpan.FromMilliseconds(500));
    }

    /// <summary>
    /// Waits for the dialog to close (disappear from modal windows).
    /// </summary>
    private void WaitForDialogToClose(TimeSpan? timeout = null)
    {
        var deadline = DateTime.UtcNow + (timeout ?? TimeSpan.FromSeconds(10));
        while (DateTime.UtcNow < deadline)
        {
            try
            {
                var modalWindows = _fixture.MainWindow.ModalWindows;
                if (modalWindows.Length == 0)
                    return;
            }
            catch { return; }
            Thread.Sleep(250);
        }
    }

    /// <summary>
    /// Types text into a TextBox using keyboard. Focuses and selects all first.
    /// </summary>
    private void TypeIntoTextBox(AutomationElement textBox, string text)
    {
        textBox.Focus();
        Wait.UntilInputIsProcessed(TimeSpan.FromMilliseconds(100));
        Keyboard.TypeSimultaneously(VirtualKeyShort.CONTROL, VirtualKeyShort.KEY_A);
        Keyboard.Type(text);
        Wait.UntilInputIsProcessed(TimeSpan.FromMilliseconds(200));
    }

    /// <summary>
    /// Sets a TextBox value directly via the Value pattern (bypasses keyboard issues with special chars like period).
    /// </summary>
    private void SetTextBoxValue(AutomationElement textBox, string value)
    {
        textBox.AsTextBox().Text = value;
        textBox.Focus();
        Keyboard.Press(VirtualKeyShort.TAB);
        Wait.UntilInputIsProcessed(TimeSpan.FromMilliseconds(200));
    }

    /// <summary>
    /// Scrolls a DataGrid row into view using the ScrollItem pattern.
    /// </summary>
    private void ScrollRowIntoView(AutomationElement row)
    {
        try
        {
            if (row.Patterns.ScrollItem.IsSupported)
            {
                row.Patterns.ScrollItem.Pattern.ScrollIntoView();
                Wait.UntilInputIsProcessed(TimeSpan.FromMilliseconds(300));
            }
        }
        catch { }
    }

    /// <summary>
    /// Finds a row, scrolls into view, selects it via SelectionItemPattern,
    /// then clicks the Edit toolbar button via Invoke pattern.
    /// </summary>
    private void FindSelectAndEdit(AutomationElement grid, string text)
    {
        var row = _fixture.FindRowByText(grid, text);
        row.Should().NotBeNull($"row containing '{text}' should exist");
        ScrollRowIntoView(row!);
        // Select via SelectionItemPattern (UIA, no mouse needed)
        if (row!.Patterns.SelectionItem.IsSupported)
        {
            row.Patterns.SelectionItem.Pattern.Select();
        }
        else
        {
            row.Click();
        }
        Wait.UntilInputIsProcessed(TimeSpan.FromMilliseconds(500));
        // Click Edit button via Invoke pattern (reliable UIA call)
        var editBtn = _fixture.FindById("EditButton");
        editBtn.AsButton().Invoke();
        Wait.UntilInputIsProcessed(TimeSpan.FromMilliseconds(500));
    }

    private void FillDialogAndSave(Window dialog, string? name = null, string? description = null,
        string? typeName = null, string? categoryName = null, string? price = null,
        string? quantity = null, string? discount = null)
    {
        // Wait for form data to load
        Wait.UntilInputIsProcessed(TimeSpan.FromMilliseconds(1000));

        if (name != null)
        {
            var nameBox = dialog.FindFirstDescendant(cf => cf.ByAutomationId("ItemName"));
            nameBox.Should().NotBeNull("ItemName field should exist");
            TypeIntoTextBox(nameBox!, name);
        }

        if (description != null)
        {
            var descBox = dialog.FindFirstDescendant(cf => cf.ByAutomationId("ItemDescription"));
            descBox.Should().NotBeNull("ItemDescription field should exist");
            TypeIntoTextBox(descBox!, description);
        }

        if (typeName != null)
            WaitAndSelectComboBox(dialog, "ItemType", typeName);

        if (categoryName != null)
            WaitAndSelectComboBox(dialog, "ItemCategory", categoryName);

        if (price != null)
        {
            var priceBox = dialog.FindFirstDescendant(cf => cf.ByAutomationId("ItemPrice"));
            priceBox.Should().NotBeNull("ItemPrice field should exist");
            SetTextBoxValue(priceBox!, price);
        }

        if (quantity != null)
        {
            var qtyBox = dialog.FindFirstDescendant(cf => cf.ByAutomationId("ItemQuantity"));
            qtyBox.Should().NotBeNull("ItemQuantity field should exist");
            SetTextBoxValue(qtyBox!, quantity);
        }

        if (discount != null)
        {
            var discBox = dialog.FindFirstDescendant(cf => cf.ByAutomationId("ItemDiscount"));
            discBox.Should().NotBeNull("ItemDiscount field should exist");
            SetTextBoxValue(discBox!, discount);
        }

        // Click Save
        var saveBtn = dialog.FindFirstDescendant(cf => cf.ByAutomationId("SaveButton"));
        saveBtn.Should().NotBeNull("SaveButton should exist");
        saveBtn!.Click();

        // Wait for dialog to close (indicates save succeeded)
        WaitForDialogToClose();
        Wait.UntilInputIsProcessed(TimeSpan.FromMilliseconds(2000));
    }

    // ---- Tests (ordered) ----

    [Fact]
    public void T1_AddItem_AppearsInAllItemsGrid()
    {
        // Click Add button using Invoke pattern for reliability
        var addBtn = _fixture.FindById("AddButton");
        addBtn.AsButton().Invoke();
        Wait.UntilInputIsProcessed(TimeSpan.FromMilliseconds(500));

        // Wait for dialog
        var dialog = _fixture.WaitForDialog("Add Item");
        dialog.Should().NotBeNull("Add Item dialog should appear");

        // Fill in the form
        FillDialogAndSave(dialog!,
            name: "UI Test Item",
            description: "UI Test Description",
            typeName: "Devices",
            categoryName: "Laptop",
            price: "25.99",
            quantity: "5");

        // Verify item appears in AllItemsGrid
        var allGrid = _fixture.GetAllItemsGrid();
        var found = Retry.WhileFalse(
            () => _fixture.GridContainsItem(allGrid, "UI Test Item"),
            TimeSpan.FromSeconds(5),
            TimeSpan.FromMilliseconds(500));
        found.Result.Should().BeTrue("newly added item should appear in All Items grid");
    }

    [Fact]
    public void T2_EditItem_ChangesReflectedInGrid()
    {
        // Find, scroll to, and double-click the item
        var allGrid = _fixture.GetAllItemsGrid();
        FindSelectAndEdit(allGrid, "UI Test Item");

        // Wait for edit dialog
        var dialog = _fixture.WaitForDialog("Edit Item");
        dialog.Should().NotBeNull("Edit Item dialog should appear");

        // Change name and price
        FillDialogAndSave(dialog!,
            name: "UI Test Edited",
            price: "35.99");

        // Verify changes
        allGrid = _fixture.GetAllItemsGrid();
        var found = Retry.WhileFalse(
            () => _fixture.GridContainsItem(allGrid, "UI Test Edited"),
            TimeSpan.FromSeconds(5),
            TimeSpan.FromMilliseconds(500));
        found.Result.Should().BeTrue("edited name should appear");
        _fixture.GridContainsItem(allGrid, "UI Test Item").Should().BeFalse("old name should be gone");
    }

    [Fact]
    public void T3_AddDiscount_ItemAppearsInDiscountedGrid()
    {
        // Double-click item in AllItemsGrid
        var allGrid = _fixture.GetAllItemsGrid();
        FindSelectAndEdit(allGrid, "UI Test Edited");

        var dialog = _fixture.WaitForDialog("Edit Item");
        dialog.Should().NotBeNull();

        // Set discount to 15
        FillDialogAndSave(dialog!, discount: "15");

        // Verify item appears in DiscountedGrid
        var discountedGrid = _fixture.GetDiscountedGrid();
        var found = Retry.WhileFalse(
            () => _fixture.GridContainsItem(discountedGrid, "UI Test Edited"),
            TimeSpan.FromSeconds(5),
            TimeSpan.FromMilliseconds(500));
        found.Result.Should()
            .BeTrue("item with discount should appear in Discounted Items grid");
    }

    [Fact]
    public void T4_RemoveDiscount_ItemDisappearsFromDiscountedGrid()
    {
        // Double-click item in DiscountedGrid
        var discountedGrid = _fixture.GetDiscountedGrid();
        FindSelectAndEdit(discountedGrid, "UI Test Edited");

        var dialog = _fixture.WaitForDialog("Edit Item");
        dialog.Should().NotBeNull();

        // Set discount to 0
        FillDialogAndSave(dialog!, discount: "0");

        // Verify item is no longer in DiscountedGrid
        discountedGrid = _fixture.GetDiscountedGrid();
        Wait.UntilInputIsProcessed(TimeSpan.FromMilliseconds(1000));
        _fixture.GridContainsItem(discountedGrid, "UI Test Edited").Should()
            .BeFalse("item without discount should not appear in Discounted Items grid");
    }

    [Fact]
    public void T5_DeleteItem_RemovedFromGrid()
    {
        // Select the item in AllItemsGrid using SelectionItemPattern
        var allGrid = _fixture.GetAllItemsGrid();
        var row = _fixture.FindRowByText(allGrid, "UI Test Edited");
        row.Should().NotBeNull("row containing 'UI Test Edited' should exist");
        ScrollRowIntoView(row!);
        if (row!.Patterns.SelectionItem.IsSupported)
            row.Patterns.SelectionItem.Pattern.Select();
        else
            row.Click();
        Wait.UntilInputIsProcessed(TimeSpan.FromMilliseconds(500));

        // Click Delete button
        var deleteBtn = _fixture.FindById("DeleteButton");
        deleteBtn.AsButton().Invoke();
        Wait.UntilInputIsProcessed(TimeSpan.FromMilliseconds(1000));

        // Handle the confirmation MessageBox (use WaitForDialog which also checks modal windows)
        var msgBox = _fixture.WaitForDialog("Confirm Delete");

        // If "Confirm Delete" not found, check if "No items selected" dialog appeared
        if (msgBox == null)
        {
            // Check for the info dialog
            var noSelBox = _fixture.WaitForDialog("Delete", TimeSpan.FromSeconds(2));
            if (noSelBox != null)
            {
                // Dismiss the "No items selected" dialog
                var okBtn = noSelBox.FindFirstDescendant(cf => cf.ByName("OK"));
                okBtn?.AsButton().Invoke();
                Wait.UntilInputIsProcessed(TimeSpan.FromMilliseconds(500));
            }

            // Try again: re-select and delete
            allGrid = _fixture.GetAllItemsGrid();
            row = _fixture.FindRowByText(allGrid, "UI Test Edited");
            row.Should().NotBeNull("row should still exist after failed delete");
            ScrollRowIntoView(row!);
            row!.Patterns.SelectionItem.Pattern.Select();
            Wait.UntilInputIsProcessed(TimeSpan.FromMilliseconds(1000));
            deleteBtn = _fixture.FindById("DeleteButton");
            deleteBtn.AsButton().Invoke();
            Wait.UntilInputIsProcessed(TimeSpan.FromMilliseconds(1000));
            msgBox = _fixture.WaitForDialog("Confirm Delete");
        }

        msgBox.Should().NotBeNull("confirmation dialog should appear");

        // Click Yes via Invoke pattern
        var yesButton = msgBox!.FindFirstDescendant(cf => cf.ByName("Yes"));
        yesButton.Should().NotBeNull("Yes button should exist in confirmation dialog");
        yesButton!.AsButton().Invoke();

        Wait.UntilInputIsProcessed(TimeSpan.FromMilliseconds(2000));

        // Verify item is gone
        allGrid = _fixture.GetAllItemsGrid();
        _fixture.GridContainsItem(allGrid, "UI Test Edited").Should()
            .BeFalse("deleted item should no longer appear in the grid");
    }
}
