using ArchLens.SharedKernel.Application;
using FluentAssertions;

namespace ArchLens.Upload.Tests.SharedKernel;

public class PagedRequestTests
{
    [Fact]
    public void Default_ShouldHavePage1And20PageSize()
    {
        var request = new PagedRequest();

        request.Page.Should().Be(1);
        request.PageSize.Should().Be(20);
        request.Skip.Should().Be(0);
    }

    [Fact]
    public void Page2_ShouldSkip20()
    {
        var request = new PagedRequest(2, 20);

        request.Skip.Should().Be(20);
    }

    [Fact]
    public void Page3_PageSize10_ShouldSkip20()
    {
        var request = new PagedRequest(3, 10);

        request.Skip.Should().Be(20);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public void PageLessThan1_ShouldDefaultTo1(int page)
    {
        var request = new PagedRequest(page, 20);

        request.Page.Should().Be(1);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void PageSizeLessThan1_ShouldDefaultTo20(int pageSize)
    {
        var request = new PagedRequest(1, pageSize);

        request.PageSize.Should().Be(20);
    }

    [Fact]
    public void PageSizeOver100_ShouldClampTo100()
    {
        var request = new PagedRequest(1, 200);

        request.PageSize.Should().Be(100);
    }

    [Fact]
    public void PageSize100_ShouldBeAllowed()
    {
        var request = new PagedRequest(1, 100);

        request.PageSize.Should().Be(100);
    }
}

public class PagedResponseTests
{
    [Fact]
    public void TotalPages_ShouldCalculateCorrectly()
    {
        var response = new PagedResponse<string>(new List<string>(), 1, 10, 25);

        response.TotalPages.Should().Be(3);
    }

    [Fact]
    public void HasPrevious_Page1_ShouldBeFalse()
    {
        var response = new PagedResponse<string>(new List<string>(), 1, 10, 50);

        response.HasPrevious.Should().BeFalse();
    }

    [Fact]
    public void HasPrevious_Page2_ShouldBeTrue()
    {
        var response = new PagedResponse<string>(new List<string>(), 2, 10, 50);

        response.HasPrevious.Should().BeTrue();
    }

    [Fact]
    public void HasNext_LastPage_ShouldBeFalse()
    {
        var response = new PagedResponse<string>(new List<string>(), 5, 10, 50);

        response.HasNext.Should().BeFalse();
    }

    [Fact]
    public void HasNext_NotLastPage_ShouldBeTrue()
    {
        var response = new PagedResponse<string>(new List<string>(), 1, 10, 50);

        response.HasNext.Should().BeTrue();
    }

    [Fact]
    public void TotalPages_ZeroItems_ShouldBeZero()
    {
        var response = new PagedResponse<string>(new List<string>(), 1, 10, 0);

        response.TotalPages.Should().Be(0);
    }

    [Fact]
    public void TotalPages_ExactDivision_ShouldBeCorrect()
    {
        var response = new PagedResponse<string>(new List<string>(), 1, 10, 30);

        response.TotalPages.Should().Be(3);
    }
}
