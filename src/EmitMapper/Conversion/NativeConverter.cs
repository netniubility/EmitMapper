﻿namespace EmitMapper.Conversion;

/// <summary>
///   The native converter.
/// </summary>
internal class NativeConverter
{
  private static readonly Type[] _ConvertTypes =
  {
    Metadata<bool>.Type, Metadata<char>.Type, Metadata<sbyte>.Type, Metadata<byte>.Type, Metadata<short>.Type,
    Metadata<int>.Type, Metadata<long>.Type, Metadata<ushort>.Type, Metadata<uint>.Type, Metadata<ulong>.Type,
    Metadata<float>.Type, Metadata<double>.Type, Metadata<decimal>.Type, Metadata<DateTime>.Type,
    Metadata<string>.Type
  };
  private static readonly MethodInfo ObjectToStringMethod = Metadata<NativeConverter>.Type.GetMethod(
    nameof(ObjectToString),
    BindingFlags.NonPublic | BindingFlags.Static);
  private static readonly MethodInfo ChangeTypeMethod = Metadata<EMConvert>.Type.GetMethod(
    nameof(EMConvert.ChangeType),
    new[] { Metadata<object>.Type, Metadata<Type>.Type, Metadata<Type>.Type });

  private static readonly MethodInfo[] ConvertMethods =
    Metadata.Convert.GetMethods(BindingFlags.Static | BindingFlags.Public);

  private static readonly LazyConcurrentDictionary<TypesPair, bool> IsNativeConvertionPossibleCache =
    new(new TypesPair());

  /// <summary>
  ///   Converts the <see cref="IAstRefOrValue" />.
  /// </summary>
  /// <param name="destinationType">The destination type.</param>
  /// <param name="sourceType">The source type.</param>
  /// <param name="sourceValue">The source value.</param>
  /// <returns>An IAstRefOrValue.</returns>
  public static IAstRefOrValue Convert(Type destinationType, Type sourceType, IAstRefOrValue sourceValue)
  {
    if (destinationType == sourceValue.ItemType)
      return sourceValue;

    if (destinationType == Metadata<string>.Type)
      return new AstCallMethodRef(ObjectToStringMethod, null, new List<IAstStackItem> { sourceValue });

    foreach (var m in ConvertMethods)
      if (m.ReturnType == destinationType)
      {
        var parameters = m.GetParameters();

        if (parameters.Length == 1 && parameters[0].ParameterType == sourceType)
          return AstBuildHelper.CallMethod(m, null, new List<IAstStackItem> { sourceValue });
      }

    return AstBuildHelper.CallMethod(
      ChangeTypeMethod,
      null,
      new List<IAstStackItem>
      {
        sourceValue, new AstTypeof { Type = sourceType }, new AstTypeof { Type = destinationType }
      });
  }

  /// <summary>
  ///   Are the native convertion possible.
  /// </summary>
  /// <param name="f">The f.</param>
  /// <param name="t">The t.</param>
  /// <returns>A bool.</returns>
  public static bool IsNativeConvertionPossible(Type f, Type t)
  {
    return IsNativeConvertionPossibleCache.GetOrAdd(
      new TypesPair(f, t),
      p =>
      {
        var from = p.SourceType;
        var to = p.DestinationType;

        if (from == null || to == null)
          return false;

        if (_ConvertTypes.Contains(from) && _ConvertTypes.Contains(to))
          return true;

        if (to == Metadata<string>.Type)
          return true;

        if (from == Metadata<string>.Type && to == Metadata<Guid>.Type)
          return true;

        if (from.IsEnum && to.IsEnum)
          return true;

        if (from.IsEnum && _ConvertTypes.Contains(to))
          return true;

        if (to.IsEnum && _ConvertTypes.Contains(from))
          return true;

        if (ReflectionHelper.IsNullable(from))
          return IsNativeConvertionPossible(from.GetUnderlyingTypeCache(), to);

        if (ReflectionHelper.IsNullable(to))
          return IsNativeConvertionPossible(from, to.GetUnderlyingTypeCache());

        return false;
      });
  }

  /// <summary>
  ///   Objects the to string.
  /// </summary>
  /// <param name="obj">The obj.</param>
  /// <returns>A string.</returns>
  internal static string ObjectToString(object obj)
  {
    if (obj == null)
      return null;

    return obj.ToString();
  }
}