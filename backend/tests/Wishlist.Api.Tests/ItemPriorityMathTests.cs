using Wishlist.Api.Features.Items;

namespace Wishlist.Api.Tests;

public sealed class ItemPriorityMathTests
{
  [Fact]
  public void ComputeInsertPriority_EmptyList_ReturnsZero()
  {
    var value = ItemPriorityMath.ComputeInsertPriority(null, null, ItemPriorityMath.DefaultStep);
    Assert.Equal(0m, value);
  }

  [Fact]
  public void ComputeInsertPriority_InsertAtTop_ReturnsNextPlusStep()
  {
    var value = ItemPriorityMath.ComputeInsertPriority(null, 2048m, ItemPriorityMath.DefaultStep);
    Assert.Equal(3072m, value);
  }

  [Fact]
  public void ComputeInsertPriority_InsertAtBottom_ReturnsPrevMinusStep()
  {
    var value = ItemPriorityMath.ComputeInsertPriority(2048m, null, ItemPriorityMath.DefaultStep);
    Assert.Equal(1024m, value);
  }

  [Fact]
  public void ComputeInsertPriority_InsertBetween_ReturnsStrictMidpoint()
  {
    var value = ItemPriorityMath.ComputeInsertPriority(2048m, 1024m, ItemPriorityMath.DefaultStep);
    Assert.Equal(1536m, value);
    Assert.True(value < 2048m);
    Assert.True(value > 1024m);
  }
}
