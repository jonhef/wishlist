namespace Wishlist.Api.Features.Items;

public static class ItemPriorityMath
{
  public const decimal DefaultStep = 1024m;
  public const decimal DefaultDensityEpsilon = 0.000000001m;

  public static decimal ComputeInsertPriority(
    decimal? prevPriority,
    decimal? nextPriority,
    decimal step = DefaultStep)
  {
    if (step <= 0)
    {
      throw new ArgumentOutOfRangeException(nameof(step), "Step must be greater than zero.");
    }

    if (prevPriority is null && nextPriority is null)
    {
      return 0m;
    }

    if (prevPriority is null)
    {
      return nextPriority!.Value + step;
    }

    if (nextPriority is null)
    {
      return prevPriority.Value - step;
    }

    return (prevPriority.Value + nextPriority.Value) / 2m;
  }

  public static bool IsTooDense(
    decimal prevPriority,
    decimal nextPriority,
    decimal epsilon = DefaultDensityEpsilon)
  {
    if (epsilon <= 0)
    {
      throw new ArgumentOutOfRangeException(nameof(epsilon), "Epsilon must be greater than zero.");
    }

    return Math.Abs(prevPriority - nextPriority) < epsilon;
  }
}
