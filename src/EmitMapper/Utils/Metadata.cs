namespace EmitMapper.Utils;

/// <summary>
/// <para>2010/12/21</para>
/// <para>THINKPADT61</para>
/// <para>tangjingbo</para>
/// </summary>
public static class Metadata
{
  public static readonly Type Action = Metadata<Action>.Type;

  public static readonly Type Action1 = typeof(Action<>);

  public static readonly Type Action2 = typeof(Action<,>);

  public static readonly Type Action3 = typeof(Action<,,>);

  public static readonly Type Action4 = typeof(Action<,,,>);

  public static readonly Type Action5 = typeof(Action<,,,,>);

  public static readonly Type Action6 = typeof(Action<,,,,,>);

  public static readonly Type Action7 = typeof(Action<,,,,,,>);

  public static readonly Type Convert = typeof(Convert);

  public static readonly Type Dictionary2 = typeof(Dictionary<,>);

  public static readonly Type Func1 = typeof(Func<>);

  public static readonly Type Func2 = typeof(Func<,>);

  public static readonly Type Func3 = typeof(Func<,,>);

  public static readonly Type Func4 = typeof(Func<,,,>);

  public static readonly Type Func5 = typeof(Func<,,,,>);

  public static readonly Type Func6 = typeof(Func<,,,,,>);

  public static readonly Type Func7 = typeof(Func<,,,,,,>);

  public static readonly Type Func8 = typeof(Func<,,,,,,,>);

  public static readonly Type HashSet1 = typeof(HashSet<>);

  public static readonly Type ICollection1 = typeof(ICollection<>);

  public static readonly Type IDictionary2 = typeof(IDictionary<,>);

  public static readonly Type IEnumerable1 = typeof(IEnumerable<>);

  public static readonly Type IList1 = typeof(IList<>);

  public static readonly Type IReadOnlyDictionary2 = typeof(IReadOnlyDictionary<,>);

  public static readonly Type ISet1 = typeof(ISet<>);

  /// <summary>
  /// List&lt;&gt;
  /// </summary>
  public static readonly Type List1 = typeof(List<>);

  public static readonly Type Math = typeof(Math);

  public static readonly Type Nullable1 = typeof(Nullable<>);

  public static readonly Type ReadOnlyDictionary2 = typeof(ReadOnlyDictionary<,>);

  public static readonly Type Void = typeof(void);

  /// <summary>
  /// Gets the cached type.
  /// </summary>
  /// <param name="anyone">The anyone.</param>
  /// <returns>A Type.</returns>
  public static Type GetCachedType<T>(this object anyone)
  {
    return Metadata<T>.Type;
  }

  /// <summary>
  /// Underlines the type.
  /// </summary>
  /// <param name="t">The t.</param>
  /// <returns>A Type.</returns>
  [Obsolete]
  public static Type UnderlineType(Type t)
  {
    return null;
  }
}

/// <summary>
/// The action metadata.
/// </summary>

public class ActionMetadata<T> : Metadata<Action<T>>
{ }

/// <summary>
/// The action metadata.
/// </summary>
/// <typeparam name="T1"></typeparam>
/// <typeparam name="T2"></typeparam>
public class ActionMetadata<T1, T2> : Metadata<Action<T1, T2>>
{ }

/// <summary>
/// The action metadata.
/// </summary>

public class ActionMetadata<T1, T2, T3> : Metadata<Action<T1, T2, T3>>
{ }

/// <summary>
/// The action metadata.
/// </summary>

public class ActionMetadata<T1, T2, T3, T4> : Metadata<Action<T1, T2, T3, T4>>
{ }

/// <summary>
/// The action metadata.
/// </summary>

public class ActionMetadata<T1, T2, T3, T4, T5> : Metadata<Action<T1, T2, T3, T4, T5>>
{ }

/// <summary>
/// The action metadata.
/// </summary>

public class ActionMetadata<T1, T2, T3, T4, T5, T6> : Metadata<Action<T1, T2, T3, T4, T5, T6>>
{ }

/// <summary>
/// The action metadata.
/// </summary>

public class ActionMetadata<T1, T2, T3, T4, T5, T6, T7> : Metadata<Action<T1, T2, T3, T4, T5, T6, T7>>
{ }

/// <summary>
/// The func metadata.
/// </summary>

public class FuncMetadata<T> : Metadata<Func<T>>
{ }

/// <summary>
/// The func metadata.
/// </summary>

public class FuncMetadata<T1, TR> : Metadata<Func<T1, TR>>
{ }

/// <summary>
/// The func metadata.
/// </summary>

public class FuncMetadata<T1, T2, TR> : Metadata<Func<T1, T2, TR>>
{ }

/// <summary>
/// The func metadata.
/// </summary>

public class FuncMetadata<T1, T2, T3, TR> : Metadata<Func<T1, T2, T3, TR>>
{ }

/// <summary>
/// The func metadata.
/// </summary>

public class FuncMetadata<T1, T2, T3, T4, TR> : Metadata<Func<T1, T2, T3, T4, TR>>
{ }

/// <summary>
/// The func metadata.
/// </summary>

public class FuncMetadata<T1, T2, T3, T4, T5, TR> : Metadata<Func<T1, T2, T3, T4, T5, TR>>
{ }

/// <summary>
/// The metadata.
/// </summary>
/// <typeparam name="T"></typeparam>
public class Metadata<T>
{
  public static readonly Type Type = typeof(T);

  /// <summary>
  /// Initializes a new instance of the <see cref="Metadata"/> class.
  /// </summary>
  protected Metadata()
  {
  }

  /// <summary>
  /// Gets the type name.
  /// </summary>
  public static string TypeName => Type.Name;

  /// <summary>
  /// Gets the interfaces cache.
  /// </summary>
  /// <returns>An array of Types</returns>
  public static Type[] GetInterfacesCache()
  {
    return Type.GetInterfacesCache();
  }
}

/// <summary>
/// The func metadata.
/// </summary>

public class FuncMetadata<T1, T2, T3, T4, T5, T6, TR> : Metadata<Func<T1, T2, T3, T4, T5, T6, TR>>
{ }