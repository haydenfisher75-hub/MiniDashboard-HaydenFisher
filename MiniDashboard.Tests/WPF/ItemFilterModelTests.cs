using FluentAssertions;
using MiniDashboard.DTOs.Classes;
using MiniDashboard.App.ViewModels;
using Xunit;

namespace MiniDashboard.Tests.WPF;

public class ItemFilterModelTests
{
    private readonly ItemFilterModel _filter = new();

    private static ItemDto MakeItem(
        string productCode = "PHN-001",
        string name = "Test Phone",
        string description = "A test phone",
        string typeName = "Electronics",
        string categoryName = "Phones",
        decimal price = 99.99m,
        int quantity = 10,
        decimal discount = 0,
        DateTime? discountDate = null,
        DateTime? createdAt = null,
        DateTime? updatedAt = null)
    {
        return new ItemDto
        {
            ProductCode = productCode,
            Name = name,
            Description = description,
            TypeName = typeName,
            CategoryName = categoryName,
            Price = price,
            Quantity = quantity,
            Discount = discount,
            DiscountDate = discountDate,
            CreatedAt = createdAt ?? new DateTime(2024, 6, 1),
            UpdatedAt = updatedAt
        };
    }

    [Fact]
    public void MatchesItem_NoFilters_ReturnsTrue()
    {
        var item = MakeItem();

        _filter.MatchesItem(item).Should().BeTrue();
    }

    [Fact]
    public void MatchesItem_NameFilter_MatchesCaseInsensitive()
    {
        var item = MakeItem(name: "Test Phone");
        _filter.Name = "test";

        _filter.MatchesItem(item).Should().BeTrue();
    }

    [Fact]
    public void MatchesItem_NameFilter_NoMatch_ReturnsFalse()
    {
        var item = MakeItem(name: "Test Phone");
        _filter.Name = "laptop";

        _filter.MatchesItem(item).Should().BeFalse();
    }

    [Fact]
    public void MatchesItem_ProductCodeFilter_Matches()
    {
        var item = MakeItem(productCode: "PHN-001");
        _filter.ProductCode = "PHN";

        _filter.MatchesItem(item).Should().BeTrue();
    }

    [Fact]
    public void MatchesItem_DescriptionFilter_Matches()
    {
        var item = MakeItem(description: "A premium smartphone");
        _filter.Description = "premium";

        _filter.MatchesItem(item).Should().BeTrue();
    }

    [Fact]
    public void MatchesItem_TypeNameFilter_Matches()
    {
        var item = MakeItem(typeName: "Electronics");
        _filter.TypeName = "elect";

        _filter.MatchesItem(item).Should().BeTrue();
    }

    [Fact]
    public void MatchesItem_CategoryNameFilter_Matches()
    {
        var item = MakeItem(categoryName: "Phones");
        _filter.CategoryName = "phone";

        _filter.MatchesItem(item).Should().BeTrue();
    }

    [Fact]
    public void MatchesItem_PriceMinFilter_ExcludesBelow()
    {
        var item = MakeItem(price: 50m);
        _filter.PriceMin = "100";

        _filter.MatchesItem(item).Should().BeFalse();
    }

    [Fact]
    public void MatchesItem_PriceMinFilter_IncludesAbove()
    {
        var item = MakeItem(price: 150m);
        _filter.PriceMin = "100";

        _filter.MatchesItem(item).Should().BeTrue();
    }

    [Fact]
    public void MatchesItem_PriceMaxFilter_ExcludesAbove()
    {
        var item = MakeItem(price: 200m);
        _filter.PriceMax = "100";

        _filter.MatchesItem(item).Should().BeFalse();
    }

    [Fact]
    public void MatchesItem_PriceRange_IncludesWithinRange()
    {
        var item = MakeItem(price: 75m);
        _filter.PriceMin = "50";
        _filter.PriceMax = "100";

        _filter.MatchesItem(item).Should().BeTrue();
    }

    [Fact]
    public void MatchesItem_QuantityMinFilter_ExcludesBelow()
    {
        var item = MakeItem(quantity: 3);
        _filter.QuantityMin = "5";

        _filter.MatchesItem(item).Should().BeFalse();
    }

    [Fact]
    public void MatchesItem_QuantityMaxFilter_ExcludesAbove()
    {
        var item = MakeItem(quantity: 20);
        _filter.QuantityMax = "10";

        _filter.MatchesItem(item).Should().BeFalse();
    }

    [Fact]
    public void MatchesItem_DiscountRange_IncludesWithinRange()
    {
        var item = MakeItem(discount: 15);
        _filter.DiscountMin = "10";
        _filter.DiscountMax = "20";

        _filter.MatchesItem(item).Should().BeTrue();
    }

    [Fact]
    public void MatchesItem_CreatedDateFrom_ExcludesBefore()
    {
        var item = MakeItem(createdAt: new DateTime(2024, 1, 1));
        _filter.CreatedFrom = new DateTime(2024, 6, 1);

        _filter.MatchesItem(item).Should().BeFalse();
    }

    [Fact]
    public void MatchesItem_CreatedDateTo_ExcludesAfter()
    {
        var item = MakeItem(createdAt: new DateTime(2024, 12, 1));
        _filter.CreatedTo = new DateTime(2024, 6, 30);

        _filter.MatchesItem(item).Should().BeFalse();
    }

    [Fact]
    public void MatchesItem_CreatedDateRange_IncludesWithin()
    {
        var item = MakeItem(createdAt: new DateTime(2024, 6, 15));
        _filter.CreatedFrom = new DateTime(2024, 6, 1);
        _filter.CreatedTo = new DateTime(2024, 6, 30);

        _filter.MatchesItem(item).Should().BeTrue();
    }

    [Fact]
    public void MatchesItem_DiscountDateFrom_NullDate_ReturnsFalse()
    {
        var item = MakeItem(discountDate: null);
        _filter.DiscountDateFrom = new DateTime(2024, 1, 1);

        _filter.MatchesItem(item).Should().BeFalse();
    }

    [Fact]
    public void MatchesItem_MultipleFilters_AllMustMatch()
    {
        var item = MakeItem(name: "Premium Phone", typeName: "Electronics", price: 999m);

        _filter.Name = "Premium";
        _filter.TypeName = "Electronics";
        _filter.PriceMin = "500";

        _filter.MatchesItem(item).Should().BeTrue();
    }

    [Fact]
    public void MatchesItem_MultipleFilters_OneFailsReturnsFalse()
    {
        var item = MakeItem(name: "Premium Phone", typeName: "Electronics", price: 999m);

        _filter.Name = "Premium";
        _filter.TypeName = "Furniture";

        _filter.MatchesItem(item).Should().BeFalse();
    }

    [Fact]
    public void MatchesItem_InvalidNumericFilter_IsIgnored()
    {
        var item = MakeItem(price: 50m);
        _filter.PriceMin = "not_a_number";

        _filter.MatchesItem(item).Should().BeTrue();
    }

    [Fact]
    public void Clear_ResetsAllFilters()
    {
        _filter.Name = "test";
        _filter.ProductCode = "PHN";
        _filter.PriceMin = "10";
        _filter.PriceMax = "100";
        _filter.CreatedFrom = DateTime.Now;

        _filter.ClearCommand.Execute(null);

        _filter.Name.Should().BeEmpty();
        _filter.ProductCode.Should().BeEmpty();
        _filter.PriceMin.Should().BeEmpty();
        _filter.PriceMax.Should().BeEmpty();
        _filter.CreatedFrom.Should().BeNull();
    }

    [Fact]
    public void PropertyChanged_InvokesFiltersChanged()
    {
        var invoked = false;
        _filter.FiltersChanged = () => invoked = true;

        _filter.Name = "test";

        invoked.Should().BeTrue();
    }
}
