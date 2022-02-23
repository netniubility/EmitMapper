using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Threading;
using EmitMapper.Utils;

namespace EmitMapper;

/// <summary>
///   Compiles expression to delegate ~20 times faster than Expression.Compile.
///   Partial to extend with your things when used as source file.
/// </summary>
// ReSharper disable once PartialTypeWithSinglePart
public static partial class ExpressionCompiler
{
  #region Expression.CompileFast overloads for Delegate, Func, and Action

  /// <summary>
  ///   Compiles lambda expression to TDelegate type. Use ifFastFailedReturnNull parameter to Not fallback to
  ///   Expression.Compile, useful for testing.
  /// </summary>
  public static TDelegate CompileFast<TDelegate>(this LambdaExpression lambdaExpr,
    bool ifFastFailedReturnNull = false, CompilerFlags flags = CompilerFlags.Default) where TDelegate : class =>
    (TDelegate)(TryCompileBoundToFirstClosureParam(
      Metadata<TDelegate>.Type == Metadata<Delegate>.Type ? lambdaExpr.Type : Metadata<TDelegate>.Type,
      lambdaExpr.Body,
#if LIGHT_EXPRESSION
                lambdaExpr, GetClosureTypeToParamTypes(lambdaExpr),
#else
      lambdaExpr.Parameters,
      GetClosureTypeToParamTypes(lambdaExpr.Parameters),
#endif
      lambdaExpr.ReturnType,
      flags) ?? (ifFastFailedReturnNull ? null : lambdaExpr.CompileSys()));

  /// Compiles a static method to the passed IL Generator.
  /// Could be used as alternative for `CompileToMethod` like this
  /// <code><![CDATA[funcExpr.CompileFastToIL(methodBuilder.GetILGenerator())]]></code>
  /// .
  /// Check `IssueTests.Issue179_Add_something_like_LambdaExpression_CompileToMethod.cs` for example.
  public static bool CompileFastToIL(this LambdaExpression lambdaExpr, ILGenerator il,
    bool ifFastFailedReturnNull = false,
    CompilerFlags flags = CompilerFlags.Default)
  {
    var closureInfo = new ClosureInfo(ClosureStatus.ShouldBeStaticMethod);

    if (!EmittingVisitor.TryEmit(
          lambdaExpr.Body,
#if LIGHT_EXPRESSION
                lambdaExpr,
#else
          lambdaExpr.Parameters,
#endif
          il,
          ref closureInfo,
          flags,
          lambdaExpr.ReturnType == Metadata.Void ? ParentFlags.IgnoreResult : ParentFlags.Empty))
      return false;

    il.Emit(OpCodes.Ret);

    return true;
  }

  /// <summary>
  ///   Compiles lambda expression to delegate. Use ifFastFailedReturnNull parameter to Not fallback to
  ///   Expression.Compile, useful for testing.
  /// </summary>
  public static Delegate CompileFast(this LambdaExpression lambdaExpr, bool ifFastFailedReturnNull = false,
    CompilerFlags flags = CompilerFlags.Default) =>
    (Delegate)TryCompileBoundToFirstClosureParam(
      lambdaExpr.Type,
      lambdaExpr.Body,
#if LIGHT_EXPRESSION
            lambdaExpr, GetClosureTypeToParamTypes(lambdaExpr),
#else
      lambdaExpr.Parameters,
      GetClosureTypeToParamTypes(lambdaExpr.Parameters),
#endif
      lambdaExpr.ReturnType,
      flags) ?? (ifFastFailedReturnNull ? null : lambdaExpr.CompileSys());

  /// <summary>Unifies Compile for System.Linq.Expressions and FEC.LightExpression</summary>
  public static TDelegate CompileSys<TDelegate>(this Expression<TDelegate> lambdaExpr) where TDelegate : Delegate =>
    lambdaExpr
#if LIGHT_EXPRESSION
            .ToLambdaExpression()
#endif
      .Compile();

  /// <summary>Unifies Compile for System.Linq.Expressions and FEC.LightExpression</summary>
  public static Delegate CompileSys(this LambdaExpression lambdaExpr) =>
    lambdaExpr
#if LIGHT_EXPRESSION
            .ToLambdaExpression()
#endif
      .Compile();

  /// <summary>
  ///   Compiles lambda expression to TDelegate type. Use ifFastFailedReturnNull parameter to Not fallback to
  ///   Expression.Compile, useful for testing.
  /// </summary>
  public static TDelegate CompileFast<TDelegate>(this Expression<TDelegate> lambdaExpr,
    bool ifFastFailedReturnNull = false,
    CompilerFlags flags = CompilerFlags.Default) where TDelegate : Delegate
  {
    return ((LambdaExpression)lambdaExpr).CompileFast<TDelegate>(ifFastFailedReturnNull, flags);
  }

  /// <summary>
  ///   Compiles lambda expression to delegate. Use ifFastFailedReturnNull parameter to Not fallback to
  ///   Expression.Compile, useful for testing.
  /// </summary>
  public static Func<R> CompileFast<R>(this Expression<Func<R>> lambdaExpr, bool ifFastFailedReturnNull = false,
    CompilerFlags flags = CompilerFlags.Default) =>
    (Func<R>)TryCompileBoundToFirstClosureParam(
      Metadata<Func<R>>.Type,
      lambdaExpr.Body,
#if LIGHT_EXPRESSION
                lambdaExpr,
#else
      lambdaExpr.Parameters,
#endif
      _closureAsASingleParamType,
      Metadata<R>.Type,
      flags) ?? (ifFastFailedReturnNull ? null : lambdaExpr.CompileSys());

  /// <summary>
  ///   Compiles lambda expression to delegate. Use ifFastFailedReturnNull parameter to Not fallback to
  ///   Expression.Compile, useful for testing.
  /// </summary>
  public static Func<T1, R> CompileFast<T1, R>(this Expression<Func<T1, R>> lambdaExpr,
    bool ifFastFailedReturnNull = false, CompilerFlags flags = CompilerFlags.Default) =>
    (Func<T1, R>)TryCompileBoundToFirstClosureParam(
      Metadata<Func<T1, R>>.Type,
      lambdaExpr.Body,
#if LIGHT_EXPRESSION
                lambdaExpr,
#else
      lambdaExpr.Parameters,
#endif
      new[] { Metadata<ArrayClosure>.Type, Metadata<T1>.Type },
      Metadata<R>.Type,
      flags) ?? (ifFastFailedReturnNull ? null : lambdaExpr.CompileSys());

  /// <summary>
  ///   Compiles lambda expression to TDelegate type. Use ifFastFailedReturnNull parameter to Not fallback to
  ///   Expression.Compile, useful for testing.
  /// </summary>
  public static Func<T1, T2, R> CompileFast<T1, T2, R>(this Expression<Func<T1, T2, R>> lambdaExpr,
    bool ifFastFailedReturnNull = false, CompilerFlags flags = CompilerFlags.Default) =>
    (Func<T1, T2, R>)TryCompileBoundToFirstClosureParam(
      Metadata<Func<T1, T2, R>>.Type,
      lambdaExpr.Body,
#if LIGHT_EXPRESSION
                lambdaExpr,
#else
      lambdaExpr.Parameters,
#endif
      new[] { Metadata<ArrayClosure>.Type, Metadata<T1>.Type, Metadata<T2>.Type },
      Metadata<R>.Type,
      flags) ?? (ifFastFailedReturnNull ? null : lambdaExpr.CompileSys());

  /// <summary>
  ///   Compiles lambda expression to delegate. Use ifFastFailedReturnNull parameter to Not fallback to
  ///   Expression.Compile, useful for testing.
  /// </summary>
  public static Func<T1, T2, T3, R> CompileFast<T1, T2, T3, R>(
    this Expression<Func<T1, T2, T3, R>> lambdaExpr, bool ifFastFailedReturnNull = false,
    CompilerFlags flags = CompilerFlags.Default) =>
    (Func<T1, T2, T3, R>)TryCompileBoundToFirstClosureParam(
      Metadata<Func<T1, T2, T3, R>>.Type,
      lambdaExpr.Body,
#if LIGHT_EXPRESSION
                lambdaExpr,
#else
      lambdaExpr.Parameters,
#endif
      new[] { Metadata<ArrayClosure>.Type, Metadata<T1>.Type, Metadata<T2>.Type, Metadata<T3>.Type },
      Metadata<R>.Type,
      flags)
    ?? (ifFastFailedReturnNull ? null : lambdaExpr.CompileSys());

  /// <summary>
  ///   Compiles lambda expression to TDelegate type. Use ifFastFailedReturnNull parameter to Not fallback to
  ///   Expression.Compile, useful for testing.
  /// </summary>
  public static Func<T1, T2, T3, T4, R> CompileFast<T1, T2, T3, T4, R>(
    this Expression<Func<T1, T2, T3, T4, R>> lambdaExpr, bool ifFastFailedReturnNull = false,
    CompilerFlags flags = CompilerFlags.Default) =>
    (Func<T1, T2, T3, T4, R>)TryCompileBoundToFirstClosureParam(
      Metadata<Func<T1, T2, T3, T4, R>>.Type,
      lambdaExpr.Body,
#if LIGHT_EXPRESSION
                lambdaExpr,
#else
      lambdaExpr.Parameters,
#endif
      new[] { Metadata<ArrayClosure>.Type, Metadata<T1>.Type, Metadata<T2>.Type, Metadata<T3>.Type, Metadata<T4>.Type },
      Metadata<R>.Type,
      flags)
    ?? (ifFastFailedReturnNull ? null : lambdaExpr.CompileSys());

  /// <summary>
  ///   Compiles lambda expression to delegate. Use ifFastFailedReturnNull parameter to Not fallback to
  ///   Expression.Compile, useful for testing.
  /// </summary>
  public static Func<T1, T2, T3, T4, T5, R> CompileFast<T1, T2, T3, T4, T5, R>(
    this Expression<Func<T1, T2, T3, T4, T5, R>> lambdaExpr, bool ifFastFailedReturnNull = false,
    CompilerFlags flags = CompilerFlags.Default) =>
    (Func<T1, T2, T3, T4, T5, R>)TryCompileBoundToFirstClosureParam(
      Metadata<Func<T1, T2, T3, T4, T5, R>>.Type,
      lambdaExpr.Body,
#if LIGHT_EXPRESSION
                lambdaExpr,
#else
      lambdaExpr.Parameters,
#endif
      new[]
      {
        Metadata<ArrayClosure>.Type, Metadata<T1>.Type, Metadata<T2>.Type, Metadata<T3>.Type, Metadata<T4>.Type,
        Metadata<T5>.Type
      },
      Metadata<R>.Type,
      flags)
    ?? (ifFastFailedReturnNull ? null : lambdaExpr.CompileSys());

  /// <summary>
  ///   Compiles lambda expression to delegate. Use ifFastFailedReturnNull parameter to Not fallback to
  ///   Expression.Compile, useful for testing.
  /// </summary>
  public static Func<T1, T2, T3, T4, T5, T6, R> CompileFast<T1, T2, T3, T4, T5, T6, R>(
    this Expression<Func<T1, T2, T3, T4, T5, T6, R>> lambdaExpr, bool ifFastFailedReturnNull = false,
    CompilerFlags flags = CompilerFlags.Default) =>
    (Func<T1, T2, T3, T4, T5, T6, R>)TryCompileBoundToFirstClosureParam(
      Metadata<Func<T1, T2, T3, T4, T5, T6, R>>.Type,
      lambdaExpr.Body,
#if LIGHT_EXPRESSION
                lambdaExpr,
#else
      lambdaExpr.Parameters,
#endif
      new[]
      {
        Metadata<ArrayClosure>.Type, Metadata<T1>.Type, Metadata<T2>.Type, Metadata<T3>.Type, Metadata<T4>.Type,
        Metadata<T5>.Type, Metadata<T6>.Type
      },
      Metadata<R>.Type,
      flags)
    ?? (ifFastFailedReturnNull ? null : lambdaExpr.CompileSys());

  /// <summary>
  ///   Compiles lambda expression to delegate. Use ifFastFailedReturnNull parameter to Not fallback to
  ///   Expression.Compile, useful for testing.
  /// </summary>
  public static Action CompileFast(this Expression<Action> lambdaExpr, bool ifFastFailedReturnNull = false,
    CompilerFlags flags = CompilerFlags.Default) =>
    (Action)TryCompileBoundToFirstClosureParam(
      Metadata<Action>.Type,
      lambdaExpr.Body,
#if LIGHT_EXPRESSION
                lambdaExpr,
#else
      lambdaExpr.Parameters,
#endif
      _closureAsASingleParamType,
      Metadata.Void,
      flags) ?? (ifFastFailedReturnNull ? null : lambdaExpr.CompileSys());

  /// <summary>
  ///   Compiles lambda expression to delegate. Use ifFastFailedReturnNull parameter to Not fallback to
  ///   Expression.Compile, useful for testing.
  /// </summary>
  public static Action<T1> CompileFast<T1>(this Expression<Action<T1>> lambdaExpr,
    bool ifFastFailedReturnNull = false, CompilerFlags flags = CompilerFlags.Default) =>
    (Action<T1>)TryCompileBoundToFirstClosureParam(
      Metadata<Action<T1>>.Type,
      lambdaExpr.Body,
#if LIGHT_EXPRESSION
                lambdaExpr,
#else
      lambdaExpr.Parameters,
#endif
      new[] { Metadata<ArrayClosure>.Type, Metadata<T1>.Type },
      Metadata.Void,
      flags) ?? (ifFastFailedReturnNull ? null : lambdaExpr.CompileSys());

  /// <summary>
  ///   Compiles lambda expression to delegate. Use ifFastFailedReturnNull parameter to Not fallback to
  ///   Expression.Compile, useful for testing.
  /// </summary>
  public static Action<T1, T2> CompileFast<T1, T2>(this Expression<Action<T1, T2>> lambdaExpr,
    bool ifFastFailedReturnNull = false, CompilerFlags flags = CompilerFlags.Default) =>
    (Action<T1, T2>)TryCompileBoundToFirstClosureParam(
      Metadata<Action<T1, T2>>.Type,
      lambdaExpr.Body,
#if LIGHT_EXPRESSION
                lambdaExpr,
#else
      lambdaExpr.Parameters,
#endif
      new[] { Metadata<ArrayClosure>.Type, Metadata<T1>.Type, Metadata<T2>.Type },
      Metadata.Void,
      flags) ?? (ifFastFailedReturnNull ? null : lambdaExpr.CompileSys());

  /// <summary>
  ///   Compiles lambda expression to delegate. Use ifFastFailedReturnNull parameter to Not fallback to
  ///   Expression.Compile, useful for testing.
  /// </summary>
  public static Action<T1, T2, T3> CompileFast<T1, T2, T3>(this Expression<Action<T1, T2, T3>> lambdaExpr,
    bool ifFastFailedReturnNull = false, CompilerFlags flags = CompilerFlags.Default) =>
    (Action<T1, T2, T3>)TryCompileBoundToFirstClosureParam(
      Metadata<Action<T1, T2, T3>>.Type,
      lambdaExpr.Body,
#if LIGHT_EXPRESSION
                lambdaExpr,
#else
      lambdaExpr.Parameters,
#endif
      new[] { Metadata<ArrayClosure>.Type, Metadata<T1>.Type, Metadata<T2>.Type, Metadata<T3>.Type },
      Metadata.Void,
      flags)
    ?? (ifFastFailedReturnNull ? null : lambdaExpr.CompileSys());

  /// <summary>
  ///   Compiles lambda expression to delegate. Use ifFastFailedReturnNull parameter to Not fallback to
  ///   Expression.Compile, useful for testing.
  /// </summary>
  public static Action<T1, T2, T3, T4> CompileFast<T1, T2, T3, T4>(
    this Expression<Action<T1, T2, T3, T4>> lambdaExpr, bool ifFastFailedReturnNull = false,
    CompilerFlags flags = CompilerFlags.Default) =>
    (Action<T1, T2, T3, T4>)TryCompileBoundToFirstClosureParam(
      Metadata<Action<T1, T2, T3, T4>>.Type,
      lambdaExpr.Body,
#if LIGHT_EXPRESSION
                lambdaExpr,
#else
      lambdaExpr.Parameters,
#endif
      new[] { Metadata<ArrayClosure>.Type, Metadata<T1>.Type, Metadata<T2>.Type, Metadata<T3>.Type, Metadata<T4>.Type },
      Metadata.Void,
      flags)
    ?? (ifFastFailedReturnNull ? null : lambdaExpr.CompileSys());

  /// <summary>
  ///   Compiles lambda expression to delegate. Use ifFastFailedReturnNull parameter to Not fallback to
  ///   Expression.Compile, useful for testing.
  /// </summary>
  public static Action<T1, T2, T3, T4, T5> CompileFast<T1, T2, T3, T4, T5>(
    this Expression<Action<T1, T2, T3, T4, T5>> lambdaExpr, bool ifFastFailedReturnNull = false,
    CompilerFlags flags = CompilerFlags.Default) =>
    (Action<T1, T2, T3, T4, T5>)TryCompileBoundToFirstClosureParam(
      Metadata<Action<T1, T2, T3, T4, T5>>.Type,
      lambdaExpr.Body,
#if LIGHT_EXPRESSION
                lambdaExpr,
#else
      lambdaExpr.Parameters,
#endif
      new[]
      {
        Metadata<ArrayClosure>.Type, Metadata<T1>.Type, Metadata<T2>.Type, Metadata<T3>.Type, Metadata<T4>.Type,
        Metadata<T5>.Type
      },
      Metadata.Void,
      flags)
    ?? (ifFastFailedReturnNull ? null : lambdaExpr.CompileSys());

  /// <summary>
  ///   Compiles lambda expression to delegate. Use ifFastFailedReturnNull parameter to Not fallback to
  ///   Expression.Compile, useful for testing.
  /// </summary>
  public static Action<T1, T2, T3, T4, T5, T6> CompileFast<T1, T2, T3, T4, T5, T6>(
    this Expression<Action<T1, T2, T3, T4, T5, T6>> lambdaExpr, bool ifFastFailedReturnNull = false,
    CompilerFlags flags = CompilerFlags.Default) =>
    (Action<T1, T2, T3, T4, T5, T6>)TryCompileBoundToFirstClosureParam(
      Metadata<Action<T1, T2, T3, T4, T5, T6>>.Type,
      lambdaExpr.Body,
#if LIGHT_EXPRESSION
                lambdaExpr,
#else
      lambdaExpr.Parameters,
#endif
      new[]
      {
        Metadata<ArrayClosure>.Type, Metadata<T1>.Type, Metadata<T2>.Type, Metadata<T3>.Type, Metadata<T4>.Type,
        Metadata<T5>.Type, Metadata<T6>.Type
      },
      Metadata.Void,
      flags)
    ?? (ifFastFailedReturnNull ? null : lambdaExpr.CompileSys());

  #endregion

  /// <summary>Tries to compile lambda expression to <typeparamref name="TDelegate" /></summary>
  public static TDelegate TryCompile<TDelegate>(this LambdaExpression lambdaExpr,
    CompilerFlags flags = CompilerFlags.Default)
    where TDelegate : class =>
    (TDelegate)TryCompileBoundToFirstClosureParam(
      Metadata<TDelegate>.Type == Metadata<Delegate>.Type ? lambdaExpr.Type : Metadata<TDelegate>.Type,
      lambdaExpr.Body,
#if LIGHT_EXPRESSION
            lambdaExpr, GetClosureTypeToParamTypes(lambdaExpr),
#else
      lambdaExpr.Parameters,
      GetClosureTypeToParamTypes(lambdaExpr.Parameters),
#endif
      lambdaExpr.ReturnType,
      flags);

  /// <summary>
  ///   Tries to compile lambda expression to <typeparamref name="TDelegate" />
  ///   with the provided closure object and constant expressions (or lack there of) -
  ///   Constant expression should be the in order of Fields in closure object!
  ///   Note 1: Use it on your own risk - FEC won't verify the expression is compile-able with passed closure, it is up to
  ///   you!
  ///   Note 2: The expression with NESTED LAMBDA IS NOT SUPPORTED!
  ///   Note 3: `Label` and `GoTo` are not supported in this case, because they need first round to collect out-of-order
  ///   labels
  /// </summary>
  public static TDelegate TryCompileWithPreCreatedClosure<TDelegate>(this LambdaExpression lambdaExpr,
    params ConstantExpression[] closureConstantsExprs) where TDelegate : class
  {
    return lambdaExpr.TryCompileWithPreCreatedClosure<TDelegate>(closureConstantsExprs, CompilerFlags.Default);
  }

  /// <summary>
  ///   Tries to compile lambda expression to <typeparamref name="TDelegate" />
  ///   with the provided closure object and constant expressions (or lack there of)
  /// </summary>
  public static TDelegate TryCompileWithPreCreatedClosure<TDelegate>(this LambdaExpression lambdaExpr,
    ConstantExpression[] closureConstantsExprs, CompilerFlags flags)
    where TDelegate : class
  {
    var closureConstants = new object[closureConstantsExprs.Length];

    for (var i = 0; i < closureConstants.Length; i++)
      closureConstants[i] = closureConstantsExprs[i].Value;

    var closureInfo = new ClosureInfo(ClosureStatus.UserProvided | ClosureStatus.HasClosure, closureConstants);

    return TryCompileWithPreCreatedClosure<TDelegate>(lambdaExpr, ref closureInfo, flags);
  }

  internal static TDelegate TryCompileWithPreCreatedClosure<TDelegate>(
    this LambdaExpression lambdaExpr, ref ClosureInfo closureInfo, CompilerFlags flags) where TDelegate : class
  {
#if LIGHT_EXPRESSION
            var closurePlusParamTypes = GetClosureTypeToParamTypes(lambdaExpr);
#else
    var closurePlusParamTypes = GetClosureTypeToParamTypes(lambdaExpr.Parameters);
#endif
    var method = new DynamicMethod(
      string.Empty,
      lambdaExpr.ReturnType,
      closurePlusParamTypes,
      Type,
      true);

    var il = method.GetILGenerator();

    EmittingVisitor.EmitLoadConstantsAndNestedLambdasIntoVars(il, ref closureInfo);

    var parent = lambdaExpr.ReturnType == Metadata.Void ? ParentFlags.IgnoreResult : ParentFlags.Empty;

    if (!EmittingVisitor.TryEmit(
          lambdaExpr.Body,
#if LIGHT_EXPRESSION
                lambdaExpr,
#else
          lambdaExpr.Parameters,
#endif
          il,
          ref closureInfo,
          flags,
          parent))
      return null;

    il.Emit(OpCodes.Ret);

    var delegateType = Metadata<TDelegate>.Type != Metadata<Delegate>.Type ? Metadata<TDelegate>.Type : lambdaExpr.Type;

    var @delegate = (TDelegate)(object)method.CreateDelegate(
      delegateType,
      new ArrayClosure(closureInfo.Constants.Items));

    ReturnClosureTypeToParamTypesToPool(closurePlusParamTypes);

    return @delegate;
  }

  private static readonly Type Type = typeof(ExpressionCompiler);

  /// <summary>Tries to compile expression to "static" delegate, skipping the step of collecting the closure object.</summary>
  public static TDelegate TryCompileWithoutClosure<TDelegate>(this LambdaExpression lambdaExpr,
    CompilerFlags flags = CompilerFlags.Default) where TDelegate : class
  {
    var closureInfo = new ClosureInfo(ClosureStatus.UserProvided);
#if LIGHT_EXPRESSION
            var closurePlusParamTypes = GetClosureTypeToParamTypes(lambdaExpr);
#else
    var closurePlusParamTypes = GetClosureTypeToParamTypes(lambdaExpr.Parameters);
#endif
    var method = new DynamicMethod(
      string.Empty,
      lambdaExpr.ReturnType,
      closurePlusParamTypes,
      Metadata<ArrayClosure>.Type,
      true);

    var il = method.GetILGenerator();

    if (!EmittingVisitor.TryEmit(
          lambdaExpr.Body,
#if LIGHT_EXPRESSION
                lambdaExpr,
#else
          lambdaExpr.Parameters,
#endif
          il,
          ref closureInfo,
          flags,
          lambdaExpr.ReturnType == Metadata.Void ? ParentFlags.IgnoreResult : ParentFlags.Empty))
      return null;

    il.Emit(OpCodes.Ret);

    var delegateType = Metadata<TDelegate>.Type != Metadata<Delegate>.Type ? Metadata<TDelegate>.Type : lambdaExpr.Type;
    var @delegate = (TDelegate)(object)method.CreateDelegate(delegateType, EmptyArrayClosure);
    ReturnClosureTypeToParamTypesToPool(closurePlusParamTypes);

    return @delegate;
  }

#if LIGHT_EXPRESSION
        internal static object TryCompileBoundToFirstClosureParam(Type delegateType, Expression bodyExpr, IParameterProvider paramExprs, 
            Type[] closurePlusParamTypes, Type returnType, CompilerFlags flags)
#else
  internal static object TryCompileBoundToFirstClosureParam(Type delegateType, Expression bodyExpr,
    IReadOnlyList<ParameterExpression> paramExprs,
    Type[] closurePlusParamTypes, Type returnType, CompilerFlags flags)
#endif
  {
    var closureInfo = new ClosureInfo(ClosureStatus.ToBeCollected);

    if (!TryCollectBoundConstants(ref closureInfo, bodyExpr, paramExprs, false, ref closureInfo, flags))
      return null;

    var nestedLambdas = closureInfo.NestedLambdas;

    if (nestedLambdas.Length != 0)
      for (var i = 0; i < nestedLambdas.Length; ++i)
        if (!TryCompileNestedLambda(ref closureInfo, i, flags))
          return null;

    ArrayClosure closure;

    if ((flags & CompilerFlags.EnableDelegateDebugInfo) == 0)
    {
      closure = (closureInfo.Status & ClosureStatus.HasClosure) == 0
        ? EmptyArrayClosure
        : new ArrayClosure(closureInfo.GetArrayOfConstantsAndNestedLambdas());
    }
    else
    {
      var debugExpr = Expression.Lambda(
        delegateType,
        bodyExpr,
        paramExprs?.ToReadOnlyList() ?? Tools.Empty<ParameterExpression>());

      closure = (closureInfo.Status & ClosureStatus.HasClosure) == 0
        ? new DebugArrayClosure(null, debugExpr)
        : new DebugArrayClosure(closureInfo.GetArrayOfConstantsAndNestedLambdas(), debugExpr);
    }

    var method = new DynamicMethod(
      string.Empty,
      returnType,
      closurePlusParamTypes,
      Metadata<ArrayClosure>.Type,
      true);

    var il = method.GetILGenerator();

    if (closure.ConstantsAndNestedLambdas != null)
      EmittingVisitor.EmitLoadConstantsAndNestedLambdasIntoVars(il, ref closureInfo);

    var parent = returnType == Metadata.Void ? ParentFlags.IgnoreResult : ParentFlags.Empty;

    if (!EmittingVisitor.TryEmit(bodyExpr, paramExprs, il, ref closureInfo, flags, parent))
      return null;

    il.Emit(OpCodes.Ret);

    var @delegate = method.CreateDelegate(delegateType, closure);
    ReturnClosureTypeToParamTypesToPool(closurePlusParamTypes);

    return @delegate;
  }

  private static readonly Type[] _closureAsASingleParamType = { Metadata<ArrayClosure>.Type };
  private static readonly Type[][] _closureTypePlusParamTypesPool = new Type[8][];

#if LIGHT_EXPRESSION
        private static Type[] GetClosureTypeToParamTypes(IParameterProvider paramExprs)
        {
            var count = paramExprs.ParameterCount;
#else
  private static Type[] GetClosureTypeToParamTypes(IReadOnlyList<ParameterExpression> paramExprs)
  {
    var count = paramExprs.Count;
#endif
    if (count == 0)
      return _closureAsASingleParamType;

    if (count < 8)
    {
      var pooledClosureAndParamTypes = Interlocked.Exchange(ref _closureTypePlusParamTypesPool[count], null);

      if (pooledClosureAndParamTypes != null)
      {
        for (var i = 0; i < count; i++)
        {
          var parameterExpr = paramExprs.GetParameter(i);

          pooledClosureAndParamTypes[i + 1] =
            parameterExpr.IsByRef ? parameterExpr.Type.MakeByRefType() : parameterExpr.Type;
        }

        return pooledClosureAndParamTypes;
      }
    }

    // todo: @perf the code maybe simplified and then will be the candidate for the inlining
    var closureAndParamTypes = new Type[count + 1];
    closureAndParamTypes[0] = Metadata<ArrayClosure>.Type;

    for (var i = 0; i < count; i++)
    {
      var parameterExpr = paramExprs.GetParameter(i);
      closureAndParamTypes[i + 1] = parameterExpr.IsByRef ? parameterExpr.Type.MakeByRefType() : parameterExpr.Type;
    }

    return closureAndParamTypes;
  }

  private static void ReturnClosureTypeToParamTypesToPool(Type[] closurePlusParamTypes)
  {
    var paramCount = closurePlusParamTypes.Length - 1;

    if (paramCount != 0 && paramCount < 8)
      Interlocked.Exchange(ref _closureTypePlusParamTypesPool[paramCount], closurePlusParamTypes);
  }

  private struct BlockInfo
  {
    public object VarExprs; // ParameterExpression  | IReadOnlyList<PE>
    public int[] VarIndexes;
  }

  [Flags]
  internal enum ClosureStatus : byte
  {
    ToBeCollected = 1,
    UserProvided = 1 << 1,
    HasClosure = 1 << 2,
    ShouldBeStaticMethod = 1 << 3
  }

  internal struct LabelInfo
  {
    public short InlinedLambdaInvokeIndex;
    public Label Label;
    public Label ReturnLabel;
    public short ReturnVariableIndexPlusOneAndIsDefined;
    public object Target; // label target is the link between the goto and the label.
  }

  /// Track the info required to build a closure object + some context information not directly related to closure.
  internal struct ClosureInfo
  {
    /// Constant expressions to find an index (by reference) of constant expression from compiled expression.
    public LiveCountArray<object> Constants;
    // todo: @perf combine Constants and Usage to save the memory
    /// Constant usage count and variable index
    public LiveCountArray<int> ConstantUsageThenVarIndex;
    public bool LastEmitIsAddress;

    /// All nested lambdas recursively nested in expression
    public NestedLambdaInfo[] NestedLambdas; // todo: @perf optimize for a single nested lambda

    /// Parameters not passed through lambda parameter list But used inside lambda body.
    /// The top expression should Not contain not passed parameters.
    public ParameterExpression[] NonPassedParameters; // todo: @perf optimize for a single non passed parameter

    public ClosureStatus Status;
    internal short CurrentInlinedLambdaInvokeIndex;

    /// Map of the links between Labels and Goto's
    internal LiveCountArray<LabelInfo> Labels;

    /// Tracks the stack of blocks where are we in emit phase
    private LiveCountArray<BlockInfo> _blockStack;

    /// <summary>
    ///   Populates info directly with provided closure object and constants.
    ///   If provided, the <paramref name="constUsage" /> should be the size of <paramref name="constValues" />
    /// </summary>
    public ClosureInfo(ClosureStatus status, object[] constValues = null, int[] constUsage = null)
    {
      Status = status;

      Constants = new LiveCountArray<object>(
        constValues ?? Tools.Empty<object>()); //todo: @perf combine constValues != null conditions

      ConstantUsageThenVarIndex = new LiveCountArray<int>(
        constValues == null ? Tools.Empty<int>() : constUsage ?? new int[constValues.Length]);

      NonPassedParameters = Tools.Empty<ParameterExpression>();
      NestedLambdas = Tools.Empty<NestedLambdaInfo>();

      LastEmitIsAddress = false;
      CurrentInlinedLambdaInvokeIndex = -1;
      Labels = new LiveCountArray<LabelInfo>(Tools.Empty<LabelInfo>());
      _blockStack = new LiveCountArray<BlockInfo>(Tools.Empty<BlockInfo>());
    }

    public void AddConstantOrIncrementUsageCount(object value, Type type)
    {
      Status |= ClosureStatus.HasClosure;

      var constItems = Constants.Items;
      var constIndex = Constants.Count - 1;

      while (constIndex != -1 && !ReferenceEquals(constItems[constIndex], value))
        --constIndex;

      if (constIndex == -1)
      {
        Constants.PushSlot(value);
        ConstantUsageThenVarIndex.PushSlot(1);
      }
      else
      {
        ++ConstantUsageThenVarIndex.Items[constIndex];
      }
    }

    public short AddInlinedLambdaInvoke(InvocationExpression e)
    {
      var index = GetLabelOrInvokeIndex(e);

      if (index == -1)
      {
        ref var label = ref Labels.PushSlot();
        label.Target = e;
        index = (short)(Labels.Count - 1);
      }

      return index;
    }

    public void AddLabel(LabelTarget labelTarget, short inlinedLambdaInvokeIndex = -1)
    {
      if (GetLabelOrInvokeIndex(labelTarget) == -1)
      {
        ref var label = ref Labels.PushSlot();
        label.Target = labelTarget;
        label.InlinedLambdaInvokeIndex = inlinedLambdaInvokeIndex;
      }
    }

    public void AddNestedLambda(NestedLambdaInfo nestedLambdaInfo)
    {
      Status |= ClosureStatus.HasClosure;

      var nestedLambdas = NestedLambdas;
      var count = nestedLambdas.Length;

      if (count == 0)
      {
        NestedLambdas = new[] { nestedLambdaInfo };
      }
      else if (count == 1)
      {
        NestedLambdas = new[] { nestedLambdas[0], nestedLambdaInfo };
      }
      else if (count == 2)
      {
        NestedLambdas = new[] { nestedLambdas[0], nestedLambdas[1], nestedLambdaInfo };
      }
      else
      {
        var newNestedLambdas = new NestedLambdaInfo[count + 1];
        Array.Copy(nestedLambdas, 0, newNestedLambdas, 0, count);
        newNestedLambdas[count] = nestedLambdaInfo;
        NestedLambdas = newNestedLambdas;
      }
    }

    public void AddNonPassedParam(ParameterExpression expr)
    {
      Status |= ClosureStatus.HasClosure;

      if (NonPassedParameters.Length == 0)
      {
        NonPassedParameters = new[] { expr }; // todo: @perf optimize for a single non passed parameter

        return;
      }

      var count = NonPassedParameters.Length;

      for (var i = 0; i < count; ++i)
        if (ReferenceEquals(NonPassedParameters[i], expr))
          return;

      if (NonPassedParameters.Length == 1)
      {
        NonPassedParameters = new[] { NonPassedParameters[0], expr };
      }
      else if (NonPassedParameters.Length == 2)
      {
        NonPassedParameters = new[] { NonPassedParameters[0], NonPassedParameters[1], expr };
      }
      else
      {
        var newItems = new ParameterExpression[count + 1];
        Array.Copy(NonPassedParameters, 0, newItems, 0, count);
        newItems[count] = expr;
        NonPassedParameters = newItems;
      }
    }

    public bool ContainsConstantsOrNestedLambdas()
    {
      return Constants.Count > 0 || NestedLambdas.Length > 0;
    }

    public object[] GetArrayOfConstantsAndNestedLambdas()
    {
      var constCount = Constants.Count;
      var nestedLambdas = NestedLambdas;

      if (constCount == 0)
      {
        if (nestedLambdas.Length == 0)
          return null; // we may rely on this null below when checking for the nested lambda constants

        var nestedLambdaItems = new object[nestedLambdas.Length];

        for (var i = 0; i < nestedLambdas.Length; i++)
        {
          var nestedLambda = nestedLambdas[i];

          if (nestedLambda.ClosureInfo.NonPassedParameters.Length == 0 ||
              nestedLambda.ClosureInfo.ContainsConstantsOrNestedLambdas() == false)
            nestedLambdaItems[i] = nestedLambda.Lambda;
          else
            nestedLambdaItems[i] = new NestedLambdaWithConstantsAndNestedLambdas(
              nestedLambda.Lambda,
              nestedLambda.ClosureInfo.GetArrayOfConstantsAndNestedLambdas());
        }

        return nestedLambdaItems;
      }

      // if constants `count != 0`
      var constItems = Constants.Items;

      if (nestedLambdas.Length == 0)
      {
        Array.Resize(ref constItems, constCount);

        return constItems;
      }

      var itemCount = constCount + nestedLambdas.Length;

      var closureItems = constItems;

      if (itemCount > constItems.Length)
      {
        closureItems = new object[itemCount];

        for (var i = 0; i < constCount; ++i)
          closureItems[i] = constItems[i];
      }
      else
      {
        // shrink the items to the actual item count
        Array.Resize(ref constItems, itemCount);
      }

      for (var i = 0; i < nestedLambdas.Length; i++)
      {
        var nestedLambda = nestedLambdas[i];

        if (nestedLambda.ClosureInfo.NonPassedParameters.Length == 0 ||
            nestedLambda.ClosureInfo.ContainsConstantsOrNestedLambdas() == false)
          closureItems[constCount + i] = nestedLambda.Lambda;
        else
          closureItems[constCount + i] = new NestedLambdaWithConstantsAndNestedLambdas(
            nestedLambda.Lambda,
            nestedLambda.ClosureInfo.GetArrayOfConstantsAndNestedLambdas());
      }

      return closureItems;
    }

    public Label GetDefinedLabel(int index, ILGenerator il)
    {
      ref var label = ref Labels.Items[index];

      if ((label.ReturnVariableIndexPlusOneAndIsDefined & 1) == 0)
      {
        label.ReturnVariableIndexPlusOneAndIsDefined |= 1;
        label.Label = il.DefineLabel();
      }

      return label.Label;
    }

    public int GetDefinedLocalVarOrDefault(ParameterExpression varParamExpr)
    {
      for (var i = _blockStack.Count - 1; i > -1; --i)
      {
        ref var block = ref _blockStack.Items[i];
        var varExprObj = block.VarExprs;

        if (ReferenceEquals(varExprObj, varParamExpr))
          return block.VarIndexes[0];

        if (varExprObj is IReadOnlyList<ParameterExpression> varExprs)
          for (var j = 0; j < varExprs.Count; j++)
            if (ReferenceEquals(varExprs[j], varParamExpr))
              return block.VarIndexes[j];
      }

      return -1;
    }

    public short GetLabelOrInvokeIndex(object labelTarget)
    {
      var count = Labels.Count;
      var items = Labels.Items;

      for (short i = 0; i < count; ++i)
        if (items[i].Target == labelTarget)
          return i;

      return -1;
    }

    public bool IsLocalVar(object varParamExpr)
    {
      for (var i = _blockStack.Count - 1; i > -1; --i)
      {
        var varExprObj = _blockStack.Items[i].VarExprs;

        if (ReferenceEquals(varExprObj, varParamExpr))
          return true;

        if (varExprObj is IReadOnlyList<ParameterExpression> varExprs)
          for (var j = 0; j < varExprs.Count; j++)
            if (ReferenceEquals(varExprs[j], varParamExpr))
              return true;
      }

      return false;
    }

    public void PopBlock()
    {
      _blockStack.Pop();
    }

    public void PushBlockAndConstructLocalVars(IReadOnlyList<ParameterExpression> blockVarExprs, ILGenerator il)
    {
      var localVars = new int[blockVarExprs.Count];

      for (var i = 0; i < localVars.Length; i++)
        localVars[i] = il.GetNextLocalVarIndex(blockVarExprs[i].Type);

      PushBlockWithVars(blockVarExprs, localVars);
    }

    /// LocalVar maybe a `null` in a collecting phase when we only need to decide if ParameterExpression is an actual parameter or variable
    public void PushBlockWithVars(ParameterExpression blockVarExpr)
    {
      ref var block = ref _blockStack.PushSlot();
      block.VarExprs = blockVarExpr;
    }

    public void PushBlockWithVars(ParameterExpression blockVarExpr, int varIndex)
    {
      ref var block = ref _blockStack.PushSlot();
      block.VarExprs = blockVarExpr;
      block.VarIndexes = new[] { varIndex };
    }

    /// LocalVars maybe a `null` in collecting phase when we only need to decide if ParameterExpression is an actual parameter or variable
    public void PushBlockWithVars(IReadOnlyList<ParameterExpression> blockVarExprs, int[] localVarIndexes = null)
    {
      ref var block = ref _blockStack.PushSlot();
      block.VarExprs = blockVarExprs;
      block.VarIndexes = localVarIndexes;
    }

    public void TryMarkDefinedLabel(int index, ILGenerator il)
    {
      ref var label = ref Labels.Items[index];

      if ((label.ReturnVariableIndexPlusOneAndIsDefined & 1) == 1)
      {
        il.MarkLabel(label.Label);
      }
      else
      {
        label.ReturnVariableIndexPlusOneAndIsDefined |= 1;
        il.MarkLabel(label.Label = il.DefineLabel());
      }
    }
  }

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

  public static readonly ArrayClosure EmptyArrayClosure = new(null);

  public static FieldInfo ArrayClosureArrayField =
    Metadata<ArrayClosure>.Type.GetField(nameof(ArrayClosure.ConstantsAndNestedLambdas));

  public static FieldInfo ArrayClosureWithNonPassedParamsField =
    Metadata<ArrayClosureWithNonPassedParams>.Type.GetField(nameof(ArrayClosureWithNonPassedParams.NonPassedParams));

  private static readonly ConstructorInfo[] _nonPassedParamsArrayClosureCtors =
    Metadata<ArrayClosureWithNonPassedParams>.Type.GetConstructors();

  public static ConstructorInfo ArrayClosureWithNonPassedParamsConstructor = _nonPassedParamsArrayClosureCtors[0];

  public static ConstructorInfo ArrayClosureWithNonPassedParamsConstructorWithoutConstants =
    _nonPassedParamsArrayClosureCtors[1];

  public class ArrayClosure
  {
    public readonly object[]
      ConstantsAndNestedLambdas; // todo: @feature split into two to reduce copying - it mostly need to set up nested lambdas and constants externally without closure collecting phase

    public ArrayClosure(object[] constantsAndNestedLambdas)
    {
      ConstantsAndNestedLambdas = constantsAndNestedLambdas;
    }
  }

  public sealed class DebugArrayClosure : ArrayClosure, IDelegateDebugInfo
  {
    private readonly Lazy<string> _csharpString;

    private readonly Lazy<string> _expressionString;

    public DebugArrayClosure(object[] constantsAndNestedLambdas, LambdaExpression expr) : base(
      constantsAndNestedLambdas)
    {
      Expression = expr;
      _expressionString = new Lazy<string>(() => Expression?.ToExpressionString() ?? "<expression is not available>");
      _csharpString = new Lazy<string>(() => Expression?.ToCSharpString() ?? "<expression is not available>");
    }

    public string CSharpString => _csharpString.Value;
    public LambdaExpression Expression { get; internal set; }
    public string ExpressionString => _expressionString.Value;
  }

  // todo: @perf better to move the case with no constants to another class OR we can reuse ArrayClosure but now ConstantsAndNestedLambdas will hold NonPassedParams
  public sealed class ArrayClosureWithNonPassedParams : ArrayClosure
  {
    public readonly object[] NonPassedParams;

    public ArrayClosureWithNonPassedParams(object[] constantsAndNestedLambdas, object[] nonPassedParams) : base(
      constantsAndNestedLambdas)
    {
      NonPassedParams = nonPassedParams;
    }

    // todo: @perf optimize for this case
    public ArrayClosureWithNonPassedParams(object[] nonPassedParams) : base(null)
    {
      NonPassedParams = nonPassedParams;
    }
  }

  // todo: @perf this class is required until we move to a single constants list per lambda hierarchy 
  public sealed class NestedLambdaWithConstantsAndNestedLambdas
  {
    public static FieldInfo ConstantsAndNestedLambdasField =
      Metadata<NestedLambdaWithConstantsAndNestedLambdas>.Type.GetTypeInfo()
        .GetDeclaredField(nameof(ConstantsAndNestedLambdas));
    public static FieldInfo NestedLambdaField =
      Metadata<NestedLambdaWithConstantsAndNestedLambdas>.Type.GetTypeInfo().GetDeclaredField(nameof(NestedLambda));
    public readonly object ConstantsAndNestedLambdas;

    public readonly object NestedLambda;

    public NestedLambdaWithConstantsAndNestedLambdas(object nestedLambda, object constantsAndNestedLambdas)
    {
      NestedLambda = nestedLambda;
      ConstantsAndNestedLambdas = constantsAndNestedLambdas;
    }
  }

  internal sealed class NestedLambdaInfo
  {
    public readonly LambdaExpression LambdaExpression;
    public ClosureInfo ClosureInfo;
    public object Lambda;
    public int LambdaVarIndex;

    public NestedLambdaInfo(LambdaExpression lambdaExpression)
    {
      LambdaExpression = lambdaExpression;
      ClosureInfo = new ClosureInfo(ClosureStatus.ToBeCollected);
      Lambda = null;
    }
  }

  internal static class CurryClosureFuncs
  {
    public static readonly MethodInfo[] Methods = _Type.GetMethods();
    private static readonly Type _Type = typeof(CurryClosureFuncs);

    public static Func<R> Curry<C, R>(Func<C, R> f, C c)
    {
      return () => f(c);
    }

    public static Func<T1, R> Curry<C, T1, R>(Func<C, T1, R> f, C c)
    {
      return t1 => f(c, t1);
    }

    public static Func<T1, T2, R> Curry<C, T1, T2, R>(Func<C, T1, T2, R> f, C c)
    {
      return (t1, t2) => f(c, t1, t2);
    }

    public static Func<T1, T2, T3, R> Curry<C, T1, T2, T3, R>(Func<C, T1, T2, T3, R> f, C c)
    {
      return (t1, t2, t3) => f(c, t1, t2, t3);
    }

    public static Func<T1, T2, T3, T4, R> Curry<C, T1, T2, T3, T4, R>(Func<C, T1, T2, T3, T4, R> f, C c)
    {
      return (t1, t2, t3, t4) => f(c, t1, t2, t3, t4);
    }

    public static Func<T1, T2, T3, T4, T5, R> Curry<C, T1, T2, T3, T4, T5, R>(Func<C, T1, T2, T3, T4, T5, R> f,
      C c)
    {
      return (t1, t2, t3, t4, t5) => f(c, t1, t2, t3, t4, t5);
    }

    public static Func<T1, T2, T3, T4, T5, T6, R>
      Curry<C, T1, T2, T3, T4, T5, T6, R>(Func<C, T1, T2, T3, T4, T5, T6, R> f, C c)
    {
      return (t1, t2, t3, t4, t5, t6) => f(c, t1, t2, t3, t4, t5, t6);
    }

    public static Func<T1, T2, T3, T4, T5, T6, T7, R>
      Curry<C, T1, T2, T3, T4, T5, T6, T7, R>(Func<C, T1, T2, T3, T4, T5, T6, T7, R> f, C c)
    {
      return (t1, t2, t3, t4, t5, t6, t7) => f(c, t1, t2, t3, t4, t5, t6, t7);
    }

    public static Func<T1, T2, T3, T4, T5, T6, T7, T8, R>
      Curry<C, T1, T2, T3, T4, T5, T6, T7, T8, R>(Func<C, T1, T2, T3, T4, T5, T6, T7, T8, R> f, C c)
    {
      return (t1, t2, t3, t4, t5, t6, t7, t8) => f(c, t1, t2, t3, t4, t5, t6, t7, t8);
    }

    public static Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, R>
      Curry<C, T1, T2, T3, T4, T5, T6, T7, T8, T9, R>(Func<C, T1, T2, T3, T4, T5, T6, T7, T8, T9, R> f, C c)
    {
      return (t1, t2, t3, t4, t5, t6, t7, t8, t9) => f(c, t1, t2, t3, t4, t5, t6, t7, t8, t9);
    }

    public static Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, R>
      Curry<C, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, R>(Func<C, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, R> f, C c)
    {
      return (t1, t2, t3, t4, t5, t6, t7, t8, t9, t10) => f(c, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10);
    }
  }

  internal static class CurryClosureActions
  {
    public static readonly MethodInfo[] Methods = _Type.GetMethods();
    private static readonly Type _Type = typeof(CurryClosureActions);

    public static Action Curry<C>(Action<C> a, C c)
    {
      return () => a(c);
    }

    public static Action<T1> Curry<C, T1>(Action<C, T1> f, C c)
    {
      return t1 => f(c, t1);
    }

    public static Action<T1, T2> Curry<C, T1, T2>(Action<C, T1, T2> f, C c)
    {
      return (t1, t2) => f(c, t1, t2);
    }

    public static Action<T1, T2, T3> Curry<C, T1, T2, T3>(Action<C, T1, T2, T3> f, C c)
    {
      return (t1, t2, t3) => f(c, t1, t2, t3);
    }

    public static Action<T1, T2, T3, T4> Curry<C, T1, T2, T3, T4>(Action<C, T1, T2, T3, T4> f, C c)
    {
      return (t1, t2, t3, t4) => f(c, t1, t2, t3, t4);
    }

    public static Action<T1, T2, T3, T4, T5> Curry<C, T1, T2, T3, T4, T5>(Action<C, T1, T2, T3, T4, T5> f,
      C c)
    {
      return (t1, t2, t3, t4, t5) => f(c, t1, t2, t3, t4, t5);
    }

    public static Action<T1, T2, T3, T4, T5, T6>
      Curry<C, T1, T2, T3, T4, T5, T6>(Action<C, T1, T2, T3, T4, T5, T6> f, C c)
    {
      return (t1, t2, t3, t4, t5, t6) => f(c, t1, t2, t3, t4, t5, t6);
    }

    public static Action<T1, T2, T3, T4, T5, T6, T7>
      Curry<C, T1, T2, T3, T4, T5, T6, T7>(Action<C, T1, T2, T3, T4, T5, T6, T7> f, C c)
    {
      return (t1, t2, t3, t4, t5, t6, t7) => f(c, t1, t2, t3, t4, t5, t6, t7);
    }

    public static Action<T1, T2, T3, T4, T5, T6, T7, T8>
      Curry<C, T1, T2, T3, T4, T5, T6, T7, T8>(Action<C, T1, T2, T3, T4, T5, T6, T7, T8> f, C c)
    {
      return (t1, t2, t3, t4, t5, t6, t7, t8) => f(c, t1, t2, t3, t4, t5, t6, t7, t8);
    }

    public static Action<T1, T2, T3, T4, T5, T6, T7, T8, T9>
      Curry<C, T1, T2, T3, T4, T5, T6, T7, T8, T9>(Action<C, T1, T2, T3, T4, T5, T6, T7, T8, T9> f, C c)
    {
      return (t1, t2, t3, t4, t5, t6, t7, t8, t9) => f(c, t1, t2, t3, t4, t5, t6, t7, t8, t9);
    }

    public static Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>
      Curry<C, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(Action<C, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> f, C c)
    {
      return (t1, t2, t3, t4, t5, t6, t7, t8, t9, t10) => f(c, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10);
    }
  }

  #region Collect Bound Constants

  /// Helps to identify constants as the one to be put into the Closure
  public static bool IsClosureBoundConstant(object value, Type type)
  {
    return value is Delegate || type.IsArray ||
           !type.IsPrimitive && !type.IsEnum && value is string == false && value is Type == false &&
           value is decimal == false;
  }

  // @paramExprs is required for nested lambda compilation
#if LIGHT_EXPRESSION
        private static bool TryCollectBoundConstants(ref ClosureInfo closure, Expression expr, IParameterProvider paramExprs, bool isNestedLambda, 
            ref ClosureInfo rootClosure, CompilerFlags flags)
        {
#else
  private static bool TryCollectBoundConstants(ref ClosureInfo closure, Expression expr,
    IReadOnlyList<ParameterExpression> paramExprs, bool isNestedLambda,
    ref ClosureInfo rootClosure, CompilerFlags flags)
  {
#endif
    while (true)
    {
      if (expr == null)
        return false;

      switch (expr.NodeType)
      {
        case ExpressionType.Constant:
#if LIGHT_EXPRESSION
                        if (expr is IntConstantExpression n)
                            return true;
#endif
          var constantExpr = (ConstantExpression)expr;
          var value = constantExpr.Value;

          if (value != null)
          {
            // todo: @perf find the way to speed-up this
            var valueType = value.GetType();

            if (IsClosureBoundConstant(value, valueType))
              closure.AddConstantOrIncrementUsageCount(value, valueType);
          }

          return true;

        case ExpressionType.Parameter:
        {
#if LIGHT_EXPRESSION
                        var paramCount = paramExprs.ParameterCount;
#else
          var paramCount = paramExprs.Count;
#endif
          // if parameter is used BUT is not in passed parameters and not in local variables,
          // it means parameter is provided by outer lambda and should be put in closure for current lambda
          var p = paramCount - 1;
          while (p != -1 && !ReferenceEquals(paramExprs.GetParameter(p), expr)) --p;

          if (p == -1 && !closure.IsLocalVar(expr))
          {
            if (!isNestedLambda)
              return false;

            closure.AddNonPassedParam((ParameterExpression)expr);
          }

          return true;
        }
        case ExpressionType.Call:
        {
          var callExpr = (MethodCallExpression)expr;
          var callObjectExpr = callExpr.Object;

#if SUPPORTS_ARGUMENT_PROVIDER
          var callArgs = (IArgumentProvider)callExpr;
          var argCount = callArgs.ArgumentCount;
#else
          var callArgs = callExpr.Arguments;
          var argCount = callArgs.Count;
#endif
          if (argCount == 0)
          {
            if (callObjectExpr != null)
            {
              expr = callObjectExpr;

              continue;
            }

            return true;
          }

          if (callObjectExpr != null &&
              !TryCollectBoundConstants(
                ref closure,
                callExpr.Object,
                paramExprs,
                isNestedLambda,
                ref rootClosure,
                flags))
            return false;

          var lastArgIndex = argCount - 1;

          for (var i = 0; i < lastArgIndex; i++)
            if (!TryCollectBoundConstants(
                  ref closure,
                  callArgs.GetArgument(i),
                  paramExprs,
                  isNestedLambda,
                  ref rootClosure,
                  flags))
              return false;

          expr = callArgs.GetArgument(lastArgIndex);

          continue;
        }

        case ExpressionType.MemberAccess:
          var memberExpr = ((MemberExpression)expr).Expression;

          if (memberExpr == null)
            return true;

          expr = memberExpr;

          continue;

        case ExpressionType.New:
        {
          var newExpr = (NewExpression)expr;
#if SUPPORTS_ARGUMENT_PROVIDER
          var ctorArgs = (IArgumentProvider)newExpr;
          var argCount = ctorArgs.ArgumentCount;
#else
          var ctorArgs = newExpr.Arguments;
          var argCount = ctorArgs.Count;
#endif
          if (argCount == 0)
            return true;

          var lastArgIndex = argCount - 1;

          for (var i = 0; i < lastArgIndex; i++)
            if (!TryCollectBoundConstants(
                  ref closure,
                  ctorArgs.GetArgument(i),
                  paramExprs,
                  isNestedLambda,
                  ref rootClosure,
                  flags))
              return false;

          expr = ctorArgs.GetArgument(lastArgIndex);

          continue;
        }
        case ExpressionType.NewArrayBounds:
        case ExpressionType.NewArrayInit:
          if (expr.NodeType == ExpressionType.NewArrayInit)
            // todo: @feature multi-dimensional array initializers are not supported yet, they also are not supported by the hoisted expression
            if (expr.Type.GetArrayRank() > 1)
            {
              if ((flags & CompilerFlags.ThrowOnNotSupportedExpression) != 0)
                throw new NotSupportedExpressionException(NotSupported.NewArrayInit_MultidimensionalArray);

              return false;
            }
#if LIGHT_EXPRESSION
                        var arrElems = (IArgumentProvider)expr;
                        var elemCount = arrElems.ArgumentCount;
#else
          var arrElems = ((NewArrayExpression)expr).Expressions;
          var elemCount = arrElems.Count;
#endif
          if (elemCount == 0)
            return true;

          for (var i = 0; i < elemCount - 1; i++)
            if (!TryCollectBoundConstants(
                  ref closure,
                  arrElems.GetArgument(i),
                  paramExprs,
                  isNestedLambda,
                  ref rootClosure,
                  flags))
              return false;

          expr = arrElems.GetArgument(elemCount - 1);

          continue;

        case ExpressionType.MemberInit:
          return TryCollectMemberInitExprConstants(
            ref closure,
            (MemberInitExpression)expr,
            paramExprs,
            isNestedLambda,
            ref rootClosure,
            flags);

        case ExpressionType.ListInit:
          return TryCollectListInitExprConstants(
            ref closure,
            (ListInitExpression)expr,
            paramExprs,
            isNestedLambda,
            ref rootClosure,
            flags);

        case ExpressionType.Lambda:
          var nestedLambdaExpr = (LambdaExpression)expr;

          // Look for the already collected lambdas and if we have the same lambda, start from the root
          var nestedLambdas = rootClosure.NestedLambdas;

          if (nestedLambdas.Length != 0)
          {
            var foundLambdaInfo = FindAlreadyCollectedNestedLambdaInfo(
              nestedLambdas,
              nestedLambdaExpr,
              out var foundInLambdas);

            if (foundLambdaInfo != null)
            {
              // if the lambda is not found on the same level, then add it
              if (foundInLambdas != closure.NestedLambdas)
              {
                closure.AddNestedLambda(foundLambdaInfo);
                var foundLambdaNonPassedParams = foundLambdaInfo.ClosureInfo.NonPassedParameters;

                if (foundLambdaNonPassedParams.Length != 0)
#if LIGHT_EXPRESSION
                                        PropagateNonPassedParamsToOuterLambda(ref closure, paramExprs, nestedLambdaExpr, foundLambdaNonPassedParams);
#else
                  PropagateNonPassedParamsToOuterLambda(
                    ref closure,
                    paramExprs,
                    nestedLambdaExpr.Parameters,
                    foundLambdaNonPassedParams);
#endif
              }

              return true;
            }
          }

          var nestedLambdaInfo = new NestedLambdaInfo(nestedLambdaExpr);
#if LIGHT_EXPRESSION
                        if (!TryCollectBoundConstants(ref nestedLambdaInfo.ClosureInfo, nestedLambdaExpr.Body, nestedLambdaExpr, 
                            true, ref rootClosure, flags))
#else
          if (!TryCollectBoundConstants(
                ref nestedLambdaInfo.ClosureInfo,
                nestedLambdaExpr.Body,
                nestedLambdaExpr.Parameters,
                true,
                ref rootClosure,
                flags))
#endif
            return false;

          closure.AddNestedLambda(nestedLambdaInfo);

          var nestedNonPassedParams =
            nestedLambdaInfo.ClosureInfo
              .NonPassedParameters; // todo: @bug ? currently it propagates variables used by the nested lambda but defined in current lambda

          if (nestedNonPassedParams.Length != 0)
#if LIGHT_EXPRESSION
                            PropagateNonPassedParamsToOuterLambda(ref closure, paramExprs, nestedLambdaExpr, nestedNonPassedParams);
#else
            PropagateNonPassedParamsToOuterLambda(
              ref closure,
              paramExprs,
              nestedLambdaExpr.Parameters,
              nestedNonPassedParams);
#endif
          return true;

        case ExpressionType.Invoke:
        {
          var invokeExpr = (InvocationExpression)expr;
#if SUPPORTS_ARGUMENT_PROVIDER
          var invokeArgs = (IArgumentProvider)invokeExpr;
          var argCount = invokeArgs.ArgumentCount;
#else
          var invokeArgs = invokeExpr.Arguments;
          var argCount = invokeArgs.Count;
#endif
          var invokedExpr = invokeExpr.Expression;

          if ((flags & CompilerFlags.NoInvocationLambdaInlining) == 0 && invokedExpr is LambdaExpression la)
          {
            var oldIndex = closure.CurrentInlinedLambdaInvokeIndex;
            closure.CurrentInlinedLambdaInvokeIndex = closure.AddInlinedLambdaInvoke(invokeExpr);

            if (argCount == 0)
              if (!TryCollectBoundConstants(ref closure, la.Body, paramExprs, isNestedLambda, ref rootClosure, flags))
                return false;

            // To inline the lambda we will wrap its body into a block, parameters into the block variables, 
            // and the invocation arguments into the variable assignments, see #278.
            // Note: we do the same in the `TryEmitInvoke`

            // We don't optimize the memory with IParameterProvider because anyway we materialize the parameters into the block below
#if LIGHT_EXPRESSION
                            var pars = (IParameterProvider)la;
                            var paramCount = paramExprs.ParameterCount;
#else
            var pars = la.Parameters;
            var paramCount = paramExprs.Count;
#endif
            var exprs = new Expression[argCount + 1];
            List<ParameterExpression> vars = null;

            for (var i = 0; i < argCount; i++)
            {
              var p = pars.GetParameter(i);
              // Check for the case of reusing the parameters in the different lambdas, 
              // see test `Hmm_I_can_use_the_same_parameter_for_outer_and_nested_lambda`
              var j = paramCount - 1;
              while (j != -1 && !ReferenceEquals(p, paramExprs.GetParameter(j))) --j;

              if (j != -1 || closure.IsLocalVar(
                    p)) // don't forget to check the variable in case of upper inlined lambda already moved the parameters into the block variables
              {
                // if we found the same parameter let's move the non-found (new) parameters into the separate `vars` list
                if (vars == null)
                {
                  vars = new List<ParameterExpression>();

                  for (var k = 0; k < i; k++)
                    vars.Add(pars.GetParameter(k));
                }
              }
              else if (vars != null)
              {
                vars.Add(p);
              }

              exprs[i] = Expression.Assign(p, invokeArgs.GetArgument(i));
            }

            exprs[argCount] = la.Body;
            expr = Expression.Block(vars ?? pars.ToReadOnlyList(), exprs);

            if (!TryCollectBoundConstants(ref closure, expr, paramExprs, isNestedLambda, ref rootClosure, flags))
              return false;

            closure.CurrentInlinedLambdaInvokeIndex = oldIndex;

            return true;
          }

          if (argCount == 0)
          {
            expr = invokedExpr;

            continue;
          }

          if (!TryCollectBoundConstants(ref closure, invokedExpr, paramExprs, isNestedLambda, ref rootClosure, flags))
            return false;

          var lastArgIndex = argCount - 1;

          for (var i = 0; i < lastArgIndex; i++)
            if (!TryCollectBoundConstants(
                  ref closure,
                  invokeArgs.GetArgument(i),
                  paramExprs,
                  isNestedLambda,
                  ref rootClosure,
                  flags))
              return false;

          expr = invokeArgs.GetArgument(lastArgIndex);

          continue;
        }
        case ExpressionType.Conditional:
          var condExpr = (ConditionalExpression)expr;

          if (!TryCollectBoundConstants(
                ref closure,
                condExpr.Test,
                paramExprs,
                isNestedLambda,
                ref rootClosure,
                flags) ||
              !TryCollectBoundConstants(
                ref closure,
                condExpr.IfFalse,
                paramExprs,
                isNestedLambda,
                ref rootClosure,
                flags))
            return false;

          expr = condExpr.IfTrue;

          continue;

        case ExpressionType.Block:
          var blockExpr = (BlockExpression)expr;
          var blockExprs = blockExpr.Expressions;
          var blockExprCount = blockExprs.Count;

          if (blockExprCount == 0)
            return true; // yeah, this is the real case

          var varExprs = blockExpr.Variables;
          var varExprCount = varExprs.Count;

          if (varExprCount == 1)
            closure.PushBlockWithVars(varExprs[0]);
          else if (varExprCount != 0)
            closure.PushBlockWithVars(varExprs);

          for (var i = 0; i < blockExprCount - 1; i++)
            if (!TryCollectBoundConstants(
                  ref closure,
                  blockExprs[i],
                  paramExprs,
                  isNestedLambda,
                  ref rootClosure,
                  flags))
              return false;

          expr = blockExprs[blockExprCount - 1];

          if (varExprCount == 0) // in case of no variables we can collect the last exp without recursion
            continue;

          if (!TryCollectBoundConstants(ref closure, expr, paramExprs, isNestedLambda, ref rootClosure, flags))
            return false;

          closure.PopBlock();

          return true;

        case ExpressionType.Loop:
          var loopExpr = (LoopExpression)expr;
          closure.AddLabel(loopExpr.BreakLabel);
          closure.AddLabel(loopExpr.ContinueLabel);
          expr = loopExpr.Body;

          continue;

        case ExpressionType.Index:
          var indexExpr = (IndexExpression)expr;
#if SUPPORTS_ARGUMENT_PROVIDER
          var indexArgs = (IArgumentProvider)indexExpr;
          var indexArgCount = indexArgs.ArgumentCount;
#else
          var indexArgs = indexExpr.Arguments;
          var indexArgCount = indexArgs.Count;
#endif
          for (var i = 0; i < indexArgCount; i++)
            if (!TryCollectBoundConstants(
                  ref closure,
                  indexArgs.GetArgument(i),
                  paramExprs,
                  isNestedLambda,
                  ref rootClosure,
                  flags))
              return false;

          if (indexExpr.Object == null)
            return true;

          expr = indexExpr.Object;

          continue;

        case ExpressionType.Try:
          return TryCollectTryExprConstants(
            ref closure,
            (TryExpression)expr,
            paramExprs,
            isNestedLambda,
            ref rootClosure,
            flags);

        case ExpressionType.Label:
          var labelExpr = (LabelExpression)expr;
          closure.AddLabel(labelExpr.Target, closure.CurrentInlinedLambdaInvokeIndex);

          if (labelExpr.DefaultValue == null)
            return true;

          expr = labelExpr.DefaultValue;

          continue;

        case ExpressionType.Goto:
          var gotoExpr = (GotoExpression)expr;

          if (gotoExpr.Value == null)
            return true;

          expr = gotoExpr.Value;

          continue;

        case ExpressionType.Switch:
          var switchExpr = (SwitchExpression)expr;

          if (!TryCollectBoundConstants(
                ref closure,
                switchExpr.SwitchValue,
                paramExprs,
                isNestedLambda,
                ref rootClosure,
                flags) ||
              switchExpr.DefaultBody != null &&
              !TryCollectBoundConstants(
                ref closure,
                switchExpr.DefaultBody,
                paramExprs,
                isNestedLambda,
                ref rootClosure,
                flags))
            return false;

          var switchCases = switchExpr.Cases;

          for (var i = 0; i < switchCases.Count - 1; i++)
            if (!TryCollectBoundConstants(
                  ref closure,
                  switchCases[i].Body,
                  paramExprs,
                  isNestedLambda,
                  ref rootClosure,
                  flags))
              return false;

          expr = switchCases[switchCases.Count - 1].Body;

          continue;

        case ExpressionType.Extension:
          expr = expr.Reduce();

          continue;

        case ExpressionType.Default:
          return true;

        case ExpressionType.TypeIs:
        case ExpressionType.TypeEqual:
          expr = ((TypeBinaryExpression)expr).Expression;

          continue;

        case ExpressionType.Quote: // todo: @feature - is not supported yet
          if ((flags & CompilerFlags.ThrowOnNotSupportedExpression) != 0)
            throw new NotSupportedExpressionException(NotSupported.Quote);

          return false;
        case ExpressionType.Dynamic: // todo: @feature - is not supported yet
          if ((flags & CompilerFlags.ThrowOnNotSupportedExpression) != 0)
            throw new NotSupportedExpressionException(NotSupported.Dynamic);

          return false;
        case ExpressionType.RuntimeVariables: // todo: @feature - is not supported yet
          if ((flags & CompilerFlags.ThrowOnNotSupportedExpression) != 0)
            throw new NotSupportedExpressionException(NotSupported.RuntimeVariables);

          return false;

        case ExpressionType.DebugInfo: // todo: @feature - is not supported yet
          return true; // todo: @unclear - just ignoring the info for now

        default:
          if (expr is UnaryExpression unaryExpr)
          {
            expr = unaryExpr.Operand;

            continue;
          }

          if (expr is BinaryExpression binaryExpr)
          {
            if (!TryCollectBoundConstants(
                  ref closure,
                  binaryExpr.Left,
                  paramExprs,
                  isNestedLambda,
                  ref rootClosure,
                  flags))
              return false;

            expr = binaryExpr.Right;

            continue;
          }

          return false;
      }
    }
  }

#if LIGHT_EXPRESSION
        private static void PropagateNonPassedParamsToOuterLambda(ref ClosureInfo closure,
            IParameterProvider paramExprs, IParameterProvider nestedLambdaParamExprs, ParameterExpression[] nestedNonPassedParams)
        {
            var paramExprCount = paramExprs.ParameterCount;
            var nestedLambdaParamExprCount = nestedLambdaParamExprs.ParameterCount;
#else
  private static void PropagateNonPassedParamsToOuterLambda(ref ClosureInfo closure,
    IReadOnlyList<ParameterExpression> paramExprs, IReadOnlyList<ParameterExpression> nestedLambdaParamExprs,
    ParameterExpression[] nestedNonPassedParams)
  {
    var paramExprCount = paramExprs.Count;
    var nestedLambdaParamExprCount = nestedLambdaParamExprs.Count;
#endif
    // If nested non passed parameter is not matched with any outer passed parameter, 
    // then ensure it goes to outer non passed parameter.
    // But check that having a non-passed parameter in root expression is invalid.
    for (var i = 0; i < nestedNonPassedParams.Length; i++)
    {
      var nestedNonPassedParam = nestedNonPassedParams[i];

      var isInNestedLambda = false;

      if (nestedLambdaParamExprCount != 0)
        for (var p = 0; !isInNestedLambda && p < nestedLambdaParamExprCount; ++p)
          isInNestedLambda = ReferenceEquals(nestedLambdaParamExprs.GetParameter(p), nestedNonPassedParam);

      var isInOuterLambda = false;

      if (paramExprCount != 0)
        for (var p = 0; !isInOuterLambda && p < paramExprCount; ++p)
          isInOuterLambda = ReferenceEquals(paramExprs.GetParameter(p), nestedNonPassedParam);

      if (!isInNestedLambda && !isInOuterLambda)
        closure.AddNonPassedParam(nestedNonPassedParam);
    }
  }

  private static NestedLambdaInfo FindAlreadyCollectedNestedLambdaInfo(
    NestedLambdaInfo[] nestedLambdas, LambdaExpression nestedLambdaExpr, out NestedLambdaInfo[] foundInLambdas)
  {
    for (var i = 0; i < nestedLambdas.Length; i++)
    {
      var lambdaInfo = nestedLambdas[i];

      if (ReferenceEquals(lambdaInfo.LambdaExpression, nestedLambdaExpr))
      {
        foundInLambdas = nestedLambdas;

        return lambdaInfo;
      }

      var deeperNestedLambdas = lambdaInfo.ClosureInfo.NestedLambdas;

      if (deeperNestedLambdas.Length != 0)
      {
        var deeperLambdaInfo = FindAlreadyCollectedNestedLambdaInfo(
          deeperNestedLambdas,
          nestedLambdaExpr,
          out foundInLambdas);

        if (deeperLambdaInfo != null)
          return deeperLambdaInfo;
      }
    }

    foundInLambdas = null;

    return null;
  }

  private static bool TryCompileNestedLambda(ref ClosureInfo outerClosureInfo, int nestedLambdaIndex,
    CompilerFlags setup)
  {
    // 1. Try to compile nested lambda in place
    // 2. Check that parameters used in compiled lambda are passed or closed by outer lambda
    // 3. Add the compiled lambda to closure of outer lambda for later invocation
    var nestedLambdaInfo = outerClosureInfo.NestedLambdas[nestedLambdaIndex];

    if (nestedLambdaInfo.Lambda != null)
      return true;

    var nestedLambdaExpr = nestedLambdaInfo.LambdaExpression;
    ref var nestedClosureInfo = ref nestedLambdaInfo.ClosureInfo;

#if LIGHT_EXPRESSION
            var nestedLambdaParamExprs = (IParameterProvider)nestedLambdaExpr;
#else
    var nestedLambdaParamExprs = nestedLambdaExpr.Parameters;
#endif

    var nestedLambdaNestedLambdas = nestedClosureInfo.NestedLambdas;

    if (nestedLambdaNestedLambdas.Length != 0)
      for (var i = 0; i < nestedLambdaNestedLambdas.Length; ++i)
        if (!TryCompileNestedLambda(ref nestedClosureInfo, i, setup))
          return false;

    ArrayClosure nestedLambdaClosure = null;

    if (nestedClosureInfo.NonPassedParameters.Length == 0)
    {
      if ((nestedClosureInfo.Status & ClosureStatus.HasClosure) == 0)
        nestedLambdaClosure = EmptyArrayClosure;
      else
        nestedLambdaClosure = new ArrayClosure(nestedClosureInfo.GetArrayOfConstantsAndNestedLambdas());
    }

    var nestedReturnType = nestedLambdaExpr.ReturnType;
    var closurePlusParamTypes = GetClosureTypeToParamTypes(nestedLambdaParamExprs);

    var method = new DynamicMethod(
      string.Empty,
      nestedReturnType,
      closurePlusParamTypes,
      Metadata<ArrayClosure>.Type,
      true);

    var il = method.GetILGenerator();

    if ((nestedClosureInfo.Status & ClosureStatus.HasClosure) != 0 &&
        nestedClosureInfo.ContainsConstantsOrNestedLambdas())
      EmittingVisitor.EmitLoadConstantsAndNestedLambdasIntoVars(il, ref nestedClosureInfo);

    var parent = nestedReturnType == Metadata.Void ? ParentFlags.IgnoreResult : ParentFlags.Empty;

    if (!EmittingVisitor.TryEmit(
          nestedLambdaExpr.Body,
          nestedLambdaParamExprs,
          il,
          ref nestedClosureInfo,
          setup,
          parent))
      return false;

    il.Emit(OpCodes.Ret);

    if (nestedLambdaClosure != null)
      nestedLambdaInfo.Lambda = method.CreateDelegate(nestedLambdaExpr.Type, nestedLambdaClosure);
    else
      // Otherwise create a static or an open delegate to pass closure later with `TryEmitNestedLambda`,
      // constructing the new closure with non-passed arguments and the rest of items
      nestedLambdaInfo.Lambda = method.CreateDelegate(
        Tools.GetFuncOrActionType(closurePlusParamTypes, nestedReturnType),
        null);

    ReturnClosureTypeToParamTypesToPool(closurePlusParamTypes);

    return true;
  }

#if LIGHT_EXPRESSION
        private static bool TryCollectMemberInitExprConstants(ref ClosureInfo closure, MemberInitExpression expr,
            IParameterProvider paramExprs, bool isNestedLambda, ref ClosureInfo rootClosure, CompilerFlags flags)
        {
            var newExpr = expr.Expression;
            var binds = (IArgumentProvider<MemberBinding>)expr;
            var count = binds.ArgumentCount;
#else
  private static bool TryCollectMemberInitExprConstants(ref ClosureInfo closure, MemberInitExpression expr,
    IReadOnlyList<ParameterExpression> paramExprs, bool isNestedLambda, ref ClosureInfo rootClosure,
    CompilerFlags flags)
  {
    var newExpr = expr.NewExpression;
    var binds = expr.Bindings;
    var count = binds.Count;
#endif
    if (!TryCollectBoundConstants(ref closure, newExpr, paramExprs, isNestedLambda, ref rootClosure, flags))
      return false;

    for (var i = 0; i < count; ++i)
    {
      var b = binds.GetArgument(i);

      if (b.BindingType != MemberBindingType.Assignment)
      {
        if ((flags & CompilerFlags.ThrowOnNotSupportedExpression) != 0)
          throw new NotSupportedExpressionException(
            b.BindingType == MemberBindingType.MemberBinding
              ? NotSupported.MemberInit_MemberBinding
              : NotSupported.MemberInit_ListBinding);

        return false; // todo: @feature MemberMemberBinding and the MemberListBinding is not supported yet.
      }

      if (!TryCollectBoundConstants(
            ref closure,
            ((MemberAssignment)b).Expression,
            paramExprs,
            isNestedLambda,
            ref rootClosure,
            flags))
        return false;
    }

    return true;
  }

#if LIGHT_EXPRESSION
        private static bool TryCollectListInitExprConstants(ref ClosureInfo closure, ListInitExpression expr,
            IParameterProvider paramExprs, bool isNestedLambda, ref ClosureInfo rootClosure, CompilerFlags flags)
#else
  private static bool TryCollectListInitExprConstants(ref ClosureInfo closure, ListInitExpression expr,
    IReadOnlyList<ParameterExpression> paramExprs, bool isNestedLambda, ref ClosureInfo rootClosure,
    CompilerFlags flags)
#endif
  {
    var newExpr = expr.NewExpression;
    var inits = expr.Initializers;
    var count = inits.Count;

    if (!TryCollectBoundConstants(ref closure, newExpr, paramExprs, isNestedLambda, ref rootClosure, flags))
      return false;

    for (var i = 0; i < count; ++i)
    {
      var elemInit = inits.GetArgument(i);
      var args = elemInit.Arguments;
      var argCount = args.Count;

      for (var a = 0; a < argCount; ++a)
        if (!TryCollectBoundConstants(
              ref closure,
              args.GetArgument(a),
              paramExprs,
              isNestedLambda,
              ref rootClosure,
              flags))
          return false;
    }

    return true;
  }

#if LIGHT_EXPRESSION
        private static bool TryCollectTryExprConstants(ref ClosureInfo closure, TryExpression tryExpr,
            IParameterProvider paramExprs, bool isNestedLambda, ref ClosureInfo rootClosure, CompilerFlags flags)
#else
  private static bool TryCollectTryExprConstants(ref ClosureInfo closure, TryExpression tryExpr,
    IReadOnlyList<ParameterExpression> paramExprs, bool isNestedLambda, ref ClosureInfo rootClosure,
    CompilerFlags flags)
#endif
  {
    if (!TryCollectBoundConstants(ref closure, tryExpr.Body, paramExprs, isNestedLambda, ref rootClosure, flags))
      return false;

    var catchBlocks = tryExpr.Handlers;

    for (var i = 0; i < catchBlocks.Count; i++)
    {
      var catchBlock = catchBlocks[i];
      var catchExVar = catchBlock.Variable;

      if (catchExVar != null)
      {
        closure.PushBlockWithVars(catchExVar);

        if (!TryCollectBoundConstants(ref closure, catchExVar, paramExprs, isNestedLambda, ref rootClosure, flags))
          return false;
      }

      if (catchBlock.Filter != null &&
          !TryCollectBoundConstants(ref closure, catchBlock.Filter, paramExprs, isNestedLambda, ref rootClosure, flags))
        return false;

      if (!TryCollectBoundConstants(ref closure, catchBlock.Body, paramExprs, isNestedLambda, ref rootClosure, flags))
        return false;

      if (catchExVar != null)
        closure.PopBlock();
    }

    if (tryExpr.Finally != null &&
        !TryCollectBoundConstants(ref closure, tryExpr.Finally, paramExprs, isNestedLambda, ref rootClosure, flags))
      return false;

    return true;
  }

  #endregion

  // The minimal context-aware flags set by parent
  [Flags]
  internal enum ParentFlags : ushort
  {
    Empty = 0,
    IgnoreResult = 1 << 1,
    Call = 1 << 2,
    MemberAccess = 1 << 3, // Any Parent Expression is a MemberExpression
    Arithmetic = 1 << 4,
    Coalesce = 1 << 5,
    InstanceAccess = 1 << 6,
    DupMemberOwner = 1 << 7,
    TryCatch = 1 << 8,
    InstanceCall = Call | InstanceAccess,
    CtorCall = Call | (1 << 9),
    IndexAccess = 1 << 10,
    InlinedLambdaInvoke = 1 << 11
  }

  [MethodImpl((MethodImplOptions)256)]
  internal static bool IgnoresResult(this ParentFlags parent)
  {
    return (parent & ParentFlags.IgnoreResult) != 0;
  }

  internal static bool EmitPopIfIgnoreResult(this ILGenerator il, ParentFlags parent)
  {
    if ((parent & ParentFlags.IgnoreResult) != 0)
      il.Emit(OpCodes.Pop);

    return true;
  }

  /// <summary>
  ///   Supports emitting of selected expressions, e.g. lambdaExpr are not supported yet.
  ///   When emitter find not supported expression it will return false from <see cref="TryEmit" />, so I could fallback
  ///   to normal and slow Expression.Compile.
  /// </summary>
  private static class EmittingVisitor
  {
    private static readonly MethodInfo _getTypeFromHandleMethod =
      ((Func<RuntimeTypeHandle, Type>)Type.GetTypeFromHandle).Method;

    private static readonly MethodInfo _objectEqualsMethod =
      ((Func<object, object, bool>)Equals).Method;

#if LIGHT_EXPRESSION
            public static bool TryEmit(Expression expr, IParameterProvider paramExprs,
                ILGenerator il, ref ClosureInfo closure, CompilerFlags setup, ParentFlags parent, int byRefIndex = -1)
            {
#else
    public static bool TryEmit(Expression expr, IReadOnlyList<ParameterExpression> paramExprs,
      ILGenerator il, ref ClosureInfo closure, CompilerFlags setup, ParentFlags parent, int byRefIndex = -1)
    {
#endif
      while (true)
      {
        closure.LastEmitIsAddress = false;

        switch (expr.NodeType)
        {
          case ExpressionType.Parameter:
            return (parent & ParentFlags.IgnoreResult) != 0 ||
                   TryEmitParameter((ParameterExpression)expr, paramExprs, il, ref closure, parent, byRefIndex);

          case ExpressionType.TypeAs:
          case ExpressionType.IsTrue:
          case ExpressionType.IsFalse:
          case ExpressionType.Increment:
          case ExpressionType.Decrement:
          case ExpressionType.Negate:
          case ExpressionType.NegateChecked:
          case ExpressionType.OnesComplement:
          case ExpressionType.UnaryPlus:
          case ExpressionType.Unbox:
            return TryEmitSimpleUnaryExpression((UnaryExpression)expr, paramExprs, il, ref closure, setup, parent);

          case ExpressionType.TypeIs:
          case ExpressionType.TypeEqual:
            return TryEmitTypeIsOrEqual((TypeBinaryExpression)expr, paramExprs, il, ref closure, setup, parent);

          case ExpressionType.Not:
            return TryEmitNot((UnaryExpression)expr, paramExprs, il, ref closure, setup, parent);

          case ExpressionType.Convert:
          case ExpressionType.ConvertChecked:
            return TryEmitConvert((UnaryExpression)expr, paramExprs, il, ref closure, setup, parent);

          case ExpressionType.ArrayIndex:
            var arrIndexExpr = (BinaryExpression)expr;

            return TryEmit(arrIndexExpr.Left, paramExprs, il, ref closure, setup, parent | ParentFlags.IndexAccess)
                   && TryEmit(
                     arrIndexExpr.Right,
                     paramExprs,
                     il,
                     ref closure,
                     setup,
                     parent | ParentFlags.IndexAccess) // #265
                   && TryEmitArrayIndex(expr.Type, il, parent, ref closure);

          case ExpressionType.ArrayLength:
            if (!TryEmit(((UnaryExpression)expr).Operand, paramExprs, il, ref closure, setup, parent))
              return false;

            if ((parent & ParentFlags.IgnoreResult) == 0)
              il.Emit(OpCodes.Ldlen);

            return true;

          case ExpressionType.Constant:
            if ((parent & ParentFlags.IgnoreResult) != 0)
              return true;
#if LIGHT_EXPRESSION
                            if (expr is IntConstantExpression n)
                            {
                                EmitLoadConstantInt(il, n.IntValue);
                                return true;
                            }
#endif
            var constExpr = (ConstantExpression)expr;

            if (constExpr.Value == null)
            {
              if (constExpr.Type.IsValueType)
                EmitLoadLocalVariable(
                  il,
                  InitValueTypeVariable(il, constExpr.Type)); // yep, this is a proper way to emit the Nullable null
              else
                il.Emit(OpCodes.Ldnull);

              return true;
            }

            return TryEmitConstantOfNotNullValue(
              closure.ContainsConstantsOrNestedLambdas(),
              constExpr.Type,
              constExpr.Value,
              il,
              ref closure);

          case ExpressionType.Call:
            return TryEmitMethodCall(expr, paramExprs, il, ref closure, setup, parent);

          case ExpressionType.MemberAccess:
            return TryEmitMemberAccess((MemberExpression)expr, paramExprs, il, ref closure, setup, parent, byRefIndex);

          case ExpressionType.New:
            return TryEmitNew(expr, paramExprs, il, ref closure, setup, parent);

          case ExpressionType.NewArrayBounds:
            return EmitNewArrayBounds((NewArrayExpression)expr, paramExprs, il, ref closure, setup, parent);

          case ExpressionType.NewArrayInit:
            return EmitNewArrayInit((NewArrayExpression)expr, paramExprs, il, ref closure, setup, parent);

          case ExpressionType.MemberInit:
            return EmitMemberInit((MemberInitExpression)expr, paramExprs, il, ref closure, setup, parent);

          case ExpressionType.ListInit:
            return TryEmitListInit((ListInitExpression)expr, paramExprs, il, ref closure, setup, parent);

          case ExpressionType.Lambda:
            return TryEmitNestedLambda((LambdaExpression)expr, paramExprs, il, ref closure);

          case ExpressionType.Invoke:
            return TryEmitInvoke((InvocationExpression)expr, paramExprs, il, ref closure, setup, parent);

          case ExpressionType.GreaterThan:
          case ExpressionType.GreaterThanOrEqual:
          case ExpressionType.LessThan:
          case ExpressionType.LessThanOrEqual:
          case ExpressionType.Equal:
          case ExpressionType.NotEqual:
            var binaryExpr = (BinaryExpression)expr;

            return TryEmitComparison(
              binaryExpr.Left,
              binaryExpr.Right,
              binaryExpr.NodeType,
              expr.Type,
              paramExprs,
              il,
              ref closure,
              setup,
              parent);

          case ExpressionType.Add:
          case ExpressionType.AddChecked:
          case ExpressionType.Subtract:
          case ExpressionType.SubtractChecked:
          case ExpressionType.Multiply:
          case ExpressionType.MultiplyChecked:
          case ExpressionType.Divide:
          case ExpressionType.Modulo:
          case ExpressionType.Power:
          case ExpressionType.And:
          case ExpressionType.Or:
          case ExpressionType.ExclusiveOr:
          case ExpressionType.LeftShift:
          case ExpressionType.RightShift:
            return TryEmitArithmetic((BinaryExpression)expr, expr.NodeType, paramExprs, il, ref closure, setup, parent);

          case ExpressionType.AndAlso:
          case ExpressionType.OrElse:
            return TryEmitLogicalOperator((BinaryExpression)expr, paramExprs, il, ref closure, setup, parent);

          case ExpressionType.Coalesce:
            return TryEmitCoalesceOperator((BinaryExpression)expr, paramExprs, il, ref closure, setup, parent);

          case ExpressionType.Conditional:
            return TryEmitConditional((ConditionalExpression)expr, paramExprs, il, ref closure, setup, parent);

          case ExpressionType.PostIncrementAssign:
          case ExpressionType.PreIncrementAssign:
          case ExpressionType.PostDecrementAssign:
          case ExpressionType.PreDecrementAssign:
            return TryEmitIncDecAssign(
              (UnaryExpression)expr,
              expr.NodeType,
              paramExprs,
              il,
              ref closure,
              setup,
              parent);

          case ExpressionType.AddAssign:
          case ExpressionType.AddAssignChecked:
          case ExpressionType.SubtractAssign:
          case ExpressionType.SubtractAssignChecked:
          case ExpressionType.MultiplyAssign:
          case ExpressionType.MultiplyAssignChecked:
          case ExpressionType.DivideAssign:
          case ExpressionType.ModuloAssign:
          case ExpressionType.PowerAssign:
          case ExpressionType.AndAssign:
          case ExpressionType.OrAssign:
          case ExpressionType.ExclusiveOrAssign:
          case ExpressionType.LeftShiftAssign:
          case ExpressionType.RightShiftAssign:
          case ExpressionType.Assign:
            return TryEmitAssign((BinaryExpression)expr, paramExprs, il, ref closure, setup, parent);

          case ExpressionType.Block:
          {
            var blockExpr = (BlockExpression)expr;
            var blockVarExprs = blockExpr.Variables;
            var blockVarCount = blockVarExprs.Count;

            if (blockVarCount == 1)
              closure.PushBlockWithVars(blockVarExprs[0], il.GetNextLocalVarIndex(blockVarExprs[0].Type));
            else if (blockVarCount > 1)
              closure.PushBlockAndConstructLocalVars(blockVarExprs, il);

            var statementExprs = blockExpr.Expressions; // Trim the expressions after the Throw - #196
            var statementCount = statementExprs.Count;

            if (statementCount == 0)
              return true; // yeah, it is a valid thing

            expr = statementExprs[statementCount - 1]; // The last (result) statement in block will provide the result

            // Try to trim the statements up to the Throw (if any)
            if (statementCount > 1)
            {
              var throwIndex = statementCount - 1;

              while (throwIndex != -1 && statementExprs[throwIndex].NodeType != ExpressionType.Throw)
                --throwIndex;

              // If we have a Throw and it is not the last one
              if (throwIndex != -1 && throwIndex != statementCount - 1)
              {
                // Change the Throw return type to match the one for the Block, and adjust the statement count
                expr = Expression.Throw(((UnaryExpression)statementExprs[throwIndex]).Operand, blockExpr.Type);
                statementCount = throwIndex + 1;
              }
            }

            // handle the all statements in block excluding the last one
            if (statementCount > 1)
              for (var i = 0; i < statementCount - 1; i++)
              {
                var stExpr = statementExprs[i];

                if (stExpr.NodeType == ExpressionType.Default && stExpr.Type == Metadata.Void)
                  continue;

                // This is basically the return pattern (see #237), so we don't care for the rest of expressions
                if (stExpr is GotoExpression gt && gt.Kind == GotoExpressionKind.Return &&
                    statementExprs[i + 1] is LabelExpression label && label.Target == gt.Target)
                {
                  if ((parent & ParentFlags.TryCatch) != 0)
                  {
                    if ((setup & CompilerFlags.ThrowOnNotSupportedExpression) != 0)
                      throw new NotSupportedExpressionException(NotSupported.Try_GotoReturnToTheFollowupLabel);

                    return
                      false; // todo: @feature return from the TryCatch with the internal label is not supported, though it is the unlikely case
                  }

                  // todo: @wip use `gt.Value ?? label.DefaultValue` instead
                  // we are generating the return value and ensuring here that it is not popped-out
                  var gtOrLabelValue = gt.Value ?? label.DefaultValue;

                  if (gtOrLabelValue != null)
                  {
                    if (!TryEmit(
                          gtOrLabelValue,
                          paramExprs,
                          il,
                          ref closure,
                          setup,
                          parent & ~ParentFlags.IgnoreResult))
                      return false;

                    if ((parent & ParentFlags.InlinedLambdaInvoke) != 0)
                    {
                      var index = closure.GetLabelOrInvokeIndex(gt.Target);
                      var invokeIndex = closure.Labels.Items[index].InlinedLambdaInvokeIndex;

                      if (invokeIndex == -1)
                        return false;

                      ref var invokeInfo = ref closure.Labels.Items[invokeIndex];
                      var varIndex = (short)((invokeInfo.ReturnVariableIndexPlusOneAndIsDefined >> 1) - 1);

                      if (varIndex == -1)
                      {
                        varIndex = (short)il.GetNextLocalVarIndex(gtOrLabelValue.Type);
                        invokeInfo.ReturnVariableIndexPlusOneAndIsDefined = (short)((varIndex + 1) << 1);
                        invokeInfo.ReturnLabel = il.DefineLabel();
                      }

                      EmitStoreLocalVariable(il, varIndex);
                      il.Emit(OpCodes.Br, invokeInfo.ReturnLabel);
                    }
                    else
                    {
                      // @hack (related to #237) if `IgnoreResult` set, that means the external/calling code won't planning on returning and
                      // emitting the double `OpCodes.Ret` (usually for not the last statement in block), so we can safely emit our own `Ret` here.
                      // And vice-versa, if `IgnoreResult` not set then the external code planning to emit `Ret` (the last block statement), 
                      // so we should avoid it on our side.
                      if ((parent & ParentFlags.IgnoreResult) != 0)
                        il.Emit(OpCodes.Ret);
                    }
                  }

                  return true;
                }

                if (!TryEmit(stExpr, paramExprs, il, ref closure, setup, parent | ParentFlags.IgnoreResult))
                  return false;
              }

            if (blockVarCount == 0)
              continue; // OMG! no recursion, continue with the last expression

            if (!TryEmit(expr, paramExprs, il, ref closure, setup, parent))
              return false;

            closure.PopBlock();

            return true;
          }
          case ExpressionType.Loop:
            return TryEmitLoop((LoopExpression)expr, paramExprs, il, ref closure, setup, parent);

          case ExpressionType.Try:
            return TryEmitTryCatchFinallyBlock(
              (TryExpression)expr,
              paramExprs,
              il,
              ref closure,
              setup,
              parent | ParentFlags.TryCatch);

          case ExpressionType.Throw:
          {
            if (!TryEmit(
                  ((UnaryExpression)expr).Operand,
                  paramExprs,
                  il,
                  ref closure,
                  setup,
                  parent & ~ParentFlags.IgnoreResult))
              return false;

            il.Emit(OpCodes.Throw);

            return true;
          }

          case ExpressionType.Default:
            if (expr.Type != Metadata.Void && (parent & ParentFlags.IgnoreResult) == 0)
              EmitDefault(expr.Type, il);

            return true;

          case ExpressionType.Index:
            return TryEmitIndex(
              (IndexExpression)expr,
              paramExprs,
              il,
              ref closure,
              setup,
              parent | ParentFlags.IndexAccess);

          case ExpressionType.Goto:
            return TryEmitGoto((GotoExpression)expr, paramExprs, il, ref closure, setup, parent);

          case ExpressionType.Label:
            return TryEmitLabel((LabelExpression)expr, paramExprs, il, ref closure, setup, parent);

          case ExpressionType.Switch:
            return TryEmitSwitch((SwitchExpression)expr, paramExprs, il, ref closure, setup, parent);

          case ExpressionType.Extension:
            expr = expr.Reduce();

            continue;

          case ExpressionType.DebugInfo: // todo: @feature - is not supported yet
            return true; // todo: @unclear - just ignoring the info for now

          case ExpressionType.Quote: // todo: @feature - is not supported yet
          default:
            return false;
        }
      }
    }

#if LIGHT_EXPRESSION
            private static bool TryEmitNew(Expression expr, IParameterProvider paramExprs, ILGenerator il, ref ClosureInfo closure, 
                CompilerFlags setup, ParentFlags parent)
#else
    private static bool TryEmitNew(Expression expr, IReadOnlyList<ParameterExpression> paramExprs, ILGenerator il,
      ref ClosureInfo closure,
      CompilerFlags setup, ParentFlags parent)
#endif
    {
      parent |= ParentFlags.CtorCall;
      var newExpr = (NewExpression)expr;
#if SUPPORTS_ARGUMENT_PROVIDER
      var argExprs = (IArgumentProvider)newExpr;
      var argCount = argExprs.ArgumentCount;
#else
      var argExprs = newExpr.Arguments;
      var argCount = argExprs.Count;
#endif
      if (argCount > 0)
      {
        var args = newExpr.Constructor.GetParameters();

        for (var i = 0; i < args.Length; ++i)
          if (!TryEmit(
                argExprs.GetArgument(i),
                paramExprs,
                il,
                ref closure,
                setup,
                parent,
                args[i].ParameterType.IsByRef ? i : -1))
            return false;
      }

      // ReSharper disable once ConditionIsAlwaysTrueOrFalse
      if (newExpr.Constructor != null)
        il.Emit(OpCodes.Newobj, newExpr.Constructor);
      else if (newExpr.Type.IsValueType)
        EmitLoadLocalVariable(il, InitValueTypeVariable(il, newExpr.Type));
      else
        return false;

      return true;
    }

#if LIGHT_EXPRESSION
            private static bool TryEmitLoop(LoopExpression loopExpr, IParameterProvider paramExprs, ILGenerator il, ref ClosureInfo closure, 
                CompilerFlags setup, ParentFlags parent)
#else
    private static bool TryEmitLoop(LoopExpression loopExpr, IReadOnlyList<ParameterExpression> paramExprs,
      ILGenerator il, ref ClosureInfo closure,
      CompilerFlags setup, ParentFlags parent)
#endif
    {
      // Mark the start of the loop body:
      var loopBodyLabel = il.DefineLabel();
      il.MarkLabel(loopBodyLabel);

      if (loopExpr.ContinueLabel != null)
        closure.TryMarkDefinedLabel(closure.GetLabelOrInvokeIndex(loopExpr.ContinueLabel), il);

      if (!TryEmit(loopExpr.Body, paramExprs, il, ref closure, setup, parent))
        return false;

      // If loop hasn't exited, jump back to start of its body:
      il.Emit(OpCodes.Br, loopBodyLabel);

      if (loopExpr.BreakLabel != null)
        closure.TryMarkDefinedLabel(closure.GetLabelOrInvokeIndex(loopExpr.BreakLabel), il);

      return true;
    }

#if LIGHT_EXPRESSION
            private static bool TryEmitIndex(IndexExpression indexExpr, IParameterProvider paramExprs, ILGenerator il, ref ClosureInfo closure, 
                CompilerFlags setup, ParentFlags parent)
#else
    private static bool TryEmitIndex(IndexExpression indexExpr, IReadOnlyList<ParameterExpression> paramExprs,
      ILGenerator il, ref ClosureInfo closure,
      CompilerFlags setup, ParentFlags parent)
#endif
    {
      if (indexExpr.Object != null && !TryEmit(indexExpr.Object, paramExprs, il, ref closure, setup, parent))
        return false;

#if SUPPORTS_ARGUMENT_PROVIDER
      var indexArgs = (IArgumentProvider)indexExpr;
      var indexArgCount = indexArgs.ArgumentCount;
#else
      var indexArgs = indexExpr.Arguments;
      var indexArgCount = indexArgs.Count;
#endif
      var indexerProp = indexExpr.Indexer;
      MethodInfo indexerPropGetter = null;

      if (indexerProp != null)
        indexerPropGetter = indexerProp.GetMethod;

      var p = parent | ParentFlags.IndexAccess;

      if (indexerPropGetter == null)
      {
        for (var i = 0; i < indexArgCount; i++)
          if (!TryEmit(indexArgs.GetArgument(i), paramExprs, il, ref closure, setup, p))
            return false;
      }
      else
      {
        var types = indexerPropGetter.GetParameters();

        for (var i = 0; i < indexArgCount; i++)
          if (!TryEmit(
                indexArgs.GetArgument(i),
                paramExprs,
                il,
                ref closure,
                setup,
                p,
                types[i].ParameterType.IsByRef ? i : -1))
            return false;
      }

      if (indexerPropGetter != null)
        return EmitMethodCallOrVirtualCall(il, indexerPropGetter);

      if (indexArgCount == 1) // one-dimensional array
        return TryEmitArrayIndex(indexExpr.Type, il, parent, ref closure);

      indexerPropGetter = indexExpr.Object?.Type.FindMethod("Get"); // multi-dimensional array

      return indexerPropGetter != null && EmitMethodCallOrVirtualCall(il, indexerPropGetter);
    }

#if LIGHT_EXPRESSION
            private static bool TryEmitLabel(LabelExpression expr, IParameterProvider paramExprs, ILGenerator il, ref ClosureInfo closure, 
                CompilerFlags setup, ParentFlags parent)
#else
    private static bool TryEmitLabel(LabelExpression expr, IReadOnlyList<ParameterExpression> paramExprs,
      ILGenerator il, ref ClosureInfo closure,
      CompilerFlags setup, ParentFlags parent)
#endif
    {
      var index = closure.GetLabelOrInvokeIndex(expr.Target);

      if (index == -1)
        return false; // should be found in first collecting constants round

      ref var label = ref closure.Labels.Items[index];

      if ((label.ReturnVariableIndexPlusOneAndIsDefined & 1) == 1)
      {
        il.MarkLabel(label.Label);
      }
      else
      {
        label.ReturnVariableIndexPlusOneAndIsDefined |= 1;
        il.MarkLabel(label.Label = il.DefineLabel());
      }

      var defaultValue = expr.DefaultValue;

      if (defaultValue != null)
        TryEmit(defaultValue, paramExprs, il, ref closure, setup, parent);

      // get the TryCatch variable from the LabelInfo - if it is not 0:
      // first if label has the default value then store into this return variable the defaultValue which is currently on stack
      // mark the associated TryCatch return label here and load the variable if parent does not ignore the result, otherwise don't load
      var returnVariableIndexPlusOne = label.ReturnVariableIndexPlusOneAndIsDefined >> 1;

      if (returnVariableIndexPlusOne != 0)
      {
        if (defaultValue != null)
          EmitStoreLocalVariable(il, returnVariableIndexPlusOne - 1);

        il.MarkLabel(label.ReturnLabel);

        if (!parent.IgnoresResult())
          EmitLoadLocalVariable(il, returnVariableIndexPlusOne - 1);
      }

      return true;
    }

#if LIGHT_EXPRESSION
            private static bool TryEmitGoto(GotoExpression expr, IParameterProvider paramExprs, ILGenerator il, ref ClosureInfo closure, 
                CompilerFlags setup, ParentFlags parent)
#else
    private static bool TryEmitGoto(GotoExpression expr, IReadOnlyList<ParameterExpression> paramExprs, ILGenerator il,
      ref ClosureInfo closure,
      CompilerFlags setup, ParentFlags parent)
#endif
    {
      var index = closure.GetLabelOrInvokeIndex(expr.Target);

      if (index == -1)
      {
        if ((closure.Status & ClosureStatus.ToBeCollected) == 0)
          return false; // if no collection cycle then the labels may be not collected

        throw new InvalidOperationException($"Cannot jump, no labels found for the target `{expr.Target}`");
      }

      var gotoValue = expr.Value;

      if (gotoValue != null &&
          !TryEmit(gotoValue, paramExprs, il, ref closure, setup, parent & ~ParentFlags.IgnoreResult))
        return false;

      switch (expr.Kind)
      {
        case GotoExpressionKind.Break:
        case GotoExpressionKind.Continue:
          // use label defined by Label expression or define its own to use by subsequent Label
          il.Emit(OpCodes.Br, closure.GetDefinedLabel(index, il));

          return true;

        case GotoExpressionKind.Goto:
          if (gotoValue != null)
            goto case GotoExpressionKind.Return;

          // use label defined by Label expression or define its own to use by subsequent Label
          il.Emit(OpCodes.Br, closure.GetDefinedLabel(index, il));

          return true;

        case GotoExpressionKind.Return:
          if ((parent & ParentFlags.TryCatch) != 0)
          {
            if (gotoValue != null)
            {
              // for TryCatch get the variable for saving the result from the LabelInfo
              // store the return expression result into the that variable
              // emit OpCodes.Leave to the special label with the result which should be marked after the label to jump over its default value
              ref var label = ref closure.Labels.Items[index];
              var varIndex = (short)(label.ReturnVariableIndexPlusOneAndIsDefined >> 1) - 1;

              if (varIndex == -1)
              {
                varIndex = il.GetNextLocalVarIndex(gotoValue.Type);
                label.ReturnVariableIndexPlusOneAndIsDefined = (short)((varIndex + 1) << 1);
                label.ReturnLabel = il.DefineLabel();
              }

              EmitStoreLocalVariable(il, varIndex);
              il.Emit(OpCodes.Leave, label.ReturnLabel);
            }
            else
            {
              il.Emit(
                OpCodes.Leave,
                closure.GetDefinedLabel(index, il)); // if there is no return value just leave to the original label
            }
          }
          else if ((parent & ParentFlags.InlinedLambdaInvoke) != 0)
          {
            if (gotoValue != null)
            {
              var invokeIndex = closure.Labels.Items[index].InlinedLambdaInvokeIndex;

              if (invokeIndex == -1)
                return false;

              ref var invokeInfo = ref closure.Labels.Items[invokeIndex];
              var varIndex = (short)(invokeInfo.ReturnVariableIndexPlusOneAndIsDefined >> 1) - 1;

              if (varIndex == -1)
              {
                varIndex = il.GetNextLocalVarIndex(gotoValue.Type);
                invokeInfo.ReturnVariableIndexPlusOneAndIsDefined = (short)((varIndex + 1) << 1);
                invokeInfo.ReturnLabel = il.DefineLabel();
              }

              EmitStoreLocalVariable(il, varIndex);
              il.Emit(OpCodes.Br, invokeInfo.ReturnLabel);
            }
          }
          else
          {
            il.Emit(OpCodes.Ret);
          }

          return true;

        default:
          return false;
      }
    }

#if LIGHT_EXPRESSION
            private static bool TryEmitCoalesceOperator(BinaryExpression exprObj, IParameterProvider paramExprs, ILGenerator il, ref ClosureInfo closure, 
                CompilerFlags setup, ParentFlags parent)
#else
    private static bool TryEmitCoalesceOperator(BinaryExpression exprObj, IReadOnlyList<ParameterExpression> paramExprs,
      ILGenerator il, ref ClosureInfo closure,
      CompilerFlags setup, ParentFlags parent)
#endif
    {
      var labelFalse = il.DefineLabel();
      var labelDone = il.DefineLabel();

      var left = exprObj.Left;
      var right = exprObj.Right;

      // we won't OpCodes.Pop inside the Coalesce as it may leave the Il in invalid state - instead we will pop at the end here (#284)
      var flags = (parent & ~ParentFlags.IgnoreResult) | ParentFlags.Coalesce;

      if (!TryEmit(left, paramExprs, il, ref closure, setup, flags))
        return false;

      var leftType = left.Type;

      if (leftType.IsValueType) // Nullable -> It's the only ValueType comparable to null
      {
        var varIndex = EmitStoreAndLoadLocalVariableAddress(il, leftType);
        il.Emit(OpCodes.Call, leftType.FindNullableHasValueGetterMethod());

        il.Emit(OpCodes.Brfalse, labelFalse);
        EmitLoadLocalVariableAddress(il, varIndex);
        il.Emit(OpCodes.Call, leftType.FindNullableGetValueOrDefaultMethod());

        il.Emit(OpCodes.Br, labelDone);
        il.MarkLabel(labelFalse);

        if (!TryEmit(right, paramExprs, il, ref closure, setup, flags))
          return false;

        il.MarkLabel(labelDone);
      }
      else
      {
        il.Emit(
          OpCodes.Dup); // duplicate left, if it's not null, after the branch this value will be on the top of the stack

        il.Emit(OpCodes.Brtrue, labelFalse); // automates the chain of the Ldnull, Ceq, Brfalse
        il.Emit(OpCodes.Pop); // left is null, pop its value from the stack

        if (!TryEmit(right, paramExprs, il, ref closure, setup, flags))
          return false;

        if (right.Type != exprObj.Type)
          if (right.Type.IsValueType)
            il.Emit(OpCodes.Box, right.Type);

        if (left.Type == exprObj.Type)
        {
          il.MarkLabel(labelFalse);
        }
        else
        {
          il.Emit(OpCodes.Br, labelDone);

          il.MarkLabel(
            labelFalse); // todo: @bug? should we insert the boxing for the Nullable value type before the Castclass

          il.Emit(OpCodes.Castclass, exprObj.Type);
          il.MarkLabel(labelDone);
        }
      }

      return il.EmitPopIfIgnoreResult(parent);
    }

    private static void EmitDefault(Type type, ILGenerator il)
    {
      if (!type.GetTypeInfo().IsValueType)
      {
        il.Emit(OpCodes.Ldnull);
      }
      else if (
        type == Metadata<bool>.Type ||
        type == Metadata<byte>.Type ||
        type == Metadata<char>.Type ||
        type == Metadata<sbyte>.Type ||
        type == Metadata<int>.Type ||
        type == Metadata<uint>.Type ||
        type == Metadata<short>.Type ||
        type == Metadata<ushort>.Type)
      {
        il.Emit(OpCodes.Ldc_I4_0);
      }
      else if (
        type == Metadata<long>.Type ||
        type == Metadata<ulong>.Type)
      {
        il.Emit(OpCodes.Ldc_I4_0);
        il.Emit(OpCodes.Conv_I8);
      }
      else if (type == Metadata<float>.Type)
      {
        il.Emit(OpCodes.Ldc_R4, default(float));
      }
      else if (type == Metadata<double>.Type)
      {
        il.Emit(OpCodes.Ldc_R8, default(double));
      }
      else
      {
        EmitLoadLocalVariable(il, InitValueTypeVariable(il, type));
      }
    }

#if LIGHT_EXPRESSION
            private static bool TryEmitTryCatchFinallyBlock(TryExpression tryExpr, IParameterProvider paramExprs, ILGenerator il, ref ClosureInfo closure, 
                CompilerFlags setup, ParentFlags parent)
#else
    private static bool TryEmitTryCatchFinallyBlock(TryExpression tryExpr,
      IReadOnlyList<ParameterExpression> paramExprs, ILGenerator il, ref ClosureInfo closure,
      CompilerFlags setup, ParentFlags parent)
#endif
    {
      il.BeginExceptionBlock();

      if (!TryEmit(tryExpr.Body, paramExprs, il, ref closure, setup, parent))
        return false;

      var exprType = tryExpr.Type;
      var returnsResult = exprType != Metadata.Void && !parent.IgnoresResult();
      var resultVarIndex = -1;

      if (returnsResult)
        EmitStoreLocalVariable(il, resultVarIndex = il.GetNextLocalVarIndex(exprType));

      var catchBlocks = tryExpr.Handlers;

      for (var i = 0; i < catchBlocks.Count; i++)
      {
        var catchBlock = catchBlocks[i];

        if (catchBlock.Filter != null)
          return false; // todo: Add support for filters in catch expression

        il.BeginCatchBlock(catchBlock.Test);

        // at the beginning of catch the Exception value is on the stack,
        // we will store into local variable.
        var exVarExpr = catchBlock.Variable;

        if (exVarExpr != null)
        {
          var exVarIndex = il.GetNextLocalVarIndex(exVarExpr.Type);
          closure.PushBlockWithVars(exVarExpr, exVarIndex);
          EmitStoreLocalVariable(il, exVarIndex);
        }

        if (!TryEmit(catchBlock.Body, paramExprs, il, ref closure, setup, parent))
          return false;

        if (exVarExpr != null)
          closure.PopBlock();

        if (returnsResult)
          EmitStoreLocalVariable(il, resultVarIndex);
      }

      var finallyExpr = tryExpr.Finally;

      if (finallyExpr != null)
      {
        il.BeginFinallyBlock();

        if (!TryEmit(finallyExpr, paramExprs, il, ref closure, setup, parent))
          return false;
      }

      il.EndExceptionBlock();

      if (returnsResult)
        EmitLoadLocalVariable(il, resultVarIndex);

      return true;
    }

#if LIGHT_EXPRESSION
            private static bool TryEmitParameter(ParameterExpression paramExpr, IParameterProvider paramExprs, 
            ILGenerator il, ref ClosureInfo closure, ParentFlags parent, int byRefIndex = -1)
            {
                var paramExprCount = paramExprs.ParameterCount;
#else
    private static bool TryEmitParameter(ParameterExpression paramExpr, IReadOnlyList<ParameterExpression> paramExprs,
      ILGenerator il, ref ClosureInfo closure, ParentFlags parent, int byRefIndex = -1)
    {
      var paramExprCount = paramExprs.Count;
#endif
      // if parameter is passed through, then just load it on stack
      var paramType = paramExpr.Type;
      var isParamByRef = paramExpr.IsByRef;

      var paramIndex = paramExprCount - 1;

      while (paramIndex != -1 && !ReferenceEquals(paramExprs.GetParameter(paramIndex), paramExpr))
        --paramIndex;

      if (paramIndex != -1)
      {
        var isArgByRef = byRefIndex != -1;

        closure.LastEmitIsAddress = !isParamByRef &&
                                    (isArgByRef || paramType.IsValueType &&
                                      (parent & ParentFlags.InstanceAccess) !=
                                      0 && // means the parameter is the instance for what method is called or the instance for the member access, see #274, #283 
                                      (parent & ParentFlags.IndexAccess) ==
                                      0); // but the parameter is not used as an index #281

        if ((closure.Status & ClosureStatus.ShouldBeStaticMethod) == 0)
          ++paramIndex; // shift parameter index by one, because the first one will be closure

        if (closure.LastEmitIsAddress)
          EmitLoadArgAddress(il, paramIndex);
        else
          EmitLoadArg(il, paramIndex);

        if (isParamByRef)
        {
          if (paramType.IsValueType)
          {
            // #248 - skip the cases with `ref param.Field` were we are actually want to load the `Field` address not the `param`
            if (!isArgByRef &&
                // this means the parameter is the argument to the method call and not the instance in the method call or member access
                (parent & ParentFlags.Call) != 0 && (parent & ParentFlags.InstanceAccess) == 0 ||
                (parent & ParentFlags.Arithmetic) != 0)
              EmitValueTypeDereference(il, paramType);
          }
          else
          {
            if (!isArgByRef && (parent & ParentFlags.Call) != 0 ||
                (parent & (ParentFlags.MemberAccess | ParentFlags.Coalesce | ParentFlags.IndexAccess)) != 0)
              il.Emit(OpCodes.Ldind_Ref);
          }
        }

        return true;
      }

      // If parameter isn't passed, then it is passed into some outer lambda or it is a local variable,
      // so it should be loaded from closure or from the locals. Then the closure is null will be an invalid state.
      // Parameter may represent a variable, so first look if this is the case
      var varIndex = closure.GetDefinedLocalVarOrDefault(paramExpr);

      if (varIndex != -1)
      {
        if (byRefIndex != -1 ||
            paramType.IsValueType &&
            (parent & ParentFlags.IndexAccess) == 0 && // #265, #281
            (parent & (ParentFlags.MemberAccess | ParentFlags.InstanceAccess)) != 0)
        {
          EmitLoadLocalVariableAddress(il, varIndex);
          closure.LastEmitIsAddress = true;
        }
        else
        {
          EmitLoadLocalVariable(il, varIndex);
        }

        return true;
      }

      if (isParamByRef)
      {
        EmitLoadLocalVariableAddress(il, byRefIndex);

        //todo: @bug? `closure.LastEmitIsAddress = true;` should we do it too as in above code with the variable 
        return true;
      }

      // the only possibility that we are here is because we are in the nested lambda,
      // and it uses the parameter or variable from the outer lambda
      var nonPassedParams = closure.NonPassedParameters;
      var nonPassedParamIndex = nonPassedParams.Length - 1;

      while (nonPassedParamIndex != -1 && !ReferenceEquals(nonPassedParams[nonPassedParamIndex], paramExpr))
        --nonPassedParamIndex;

      if (nonPassedParamIndex == -1)
        return false; // what??? no chance

      // Load non-passed argument from Closure - closure object is always a first argument
      il.Emit(OpCodes.Ldarg_0);
      il.Emit(OpCodes.Ldfld, ArrayClosureWithNonPassedParamsField);
      EmitLoadConstantInt(il, nonPassedParamIndex);
      il.Emit(OpCodes.Ldelem_Ref);

      // source type is object, NonPassedParams is object array
      if (paramType.IsValueType)
        il.Emit(OpCodes.Unbox_Any, paramType);

      return true;
    }

    private static void EmitValueTypeDereference(ILGenerator il, Type type)
    {
      if (type == Metadata<int>.Type)
        il.Emit(OpCodes.Ldind_I4);
      else if (type == Metadata<long>.Type)
        il.Emit(OpCodes.Ldind_I8);
      else if (type == Metadata<short>.Type)
        il.Emit(OpCodes.Ldind_I2);
      else if (type == Metadata<sbyte>.Type)
        il.Emit(OpCodes.Ldind_I1);
      else if (type == Metadata<float>.Type)
        il.Emit(OpCodes.Ldind_R4);
      else if (type == Metadata<double>.Type)
        il.Emit(OpCodes.Ldind_R8);
      else if (type == Metadata<IntPtr>.Type)
        il.Emit(OpCodes.Ldind_I);
      else if (type == Metadata<UIntPtr>.Type)
        il.Emit(OpCodes.Ldind_I);
      else if (type == Metadata<byte>.Type)
        il.Emit(OpCodes.Ldind_U1);
      else if (type == Metadata<ushort>.Type)
        il.Emit(OpCodes.Ldind_U2);
      else if (type == Metadata<uint>.Type)
        il.Emit(OpCodes.Ldind_U4);
      else
        il.Emit(OpCodes.Ldobj, type);
      //todo: UInt64 as there is no OpCodes? Ldind_Ref?
    }

#if LIGHT_EXPRESSION
            private static bool TryEmitSimpleUnaryExpression(UnaryExpression expr, IParameterProvider paramExprs, 
                ILGenerator il, ref ClosureInfo closure, CompilerFlags setup, ParentFlags parent)
            {
#else
    private static bool TryEmitSimpleUnaryExpression(UnaryExpression expr,
      IReadOnlyList<ParameterExpression> paramExprs,
      ILGenerator il, ref ClosureInfo closure, CompilerFlags setup, ParentFlags parent)
    {
#endif
      var exprType = expr.Type;

      if (!TryEmit(expr.Operand, paramExprs, il, ref closure, setup, parent))
        return false;

      if (expr.NodeType == ExpressionType.TypeAs)
      {
        il.Emit(OpCodes.Isinst, exprType);

        if (exprType.IsValueType)
          il.Emit(OpCodes.Unbox_Any, exprType);
      }
      else if (expr.NodeType == ExpressionType.IsFalse)
      {
        var falseLabel = il.DefineLabel();
        var continueLabel = il.DefineLabel();
        il.Emit(OpCodes.Brfalse, falseLabel);
        il.Emit(OpCodes.Ldc_I4_0);
        il.Emit(OpCodes.Br, continueLabel);
        il.MarkLabel(falseLabel);
        il.Emit(OpCodes.Ldc_I4_1);
        il.MarkLabel(continueLabel);
      }
      else if (expr.NodeType == ExpressionType.Increment)
      {
        var typeInfo = exprType.GetTypeInfo();

        if (typeInfo.IsPrimitive)
        {
          if (!TryEmitNumberOne(il, exprType))
            return false;

          il.Emit(OpCodes.Add);
        }
        else
        {
          var method = typeInfo.GetDeclaredMethod("op_Increment");

          if (method == null)
            return false;

          il.Emit(OpCodes.Call, method);
        }
      }
      else if (expr.NodeType == ExpressionType.Decrement)
      {
        var typeInfo = exprType.GetTypeInfo();

        if (typeInfo.IsPrimitive)
        {
          if (!TryEmitNumberOne(il, exprType))
            return false;

          il.Emit(OpCodes.Sub);
        }
        else
        {
          var method = typeInfo.GetDeclaredMethod("op_Decrement");

          if (method == null)
            return false;

          il.Emit(OpCodes.Call, method);
        }
      }
      else if (expr.NodeType == ExpressionType.Negate || expr.NodeType == ExpressionType.NegateChecked)
      {
        var typeInfo = exprType.GetTypeInfo();

        if (typeInfo.IsPrimitive)
        {
          il.Emit(OpCodes.Neg);
        }
        else
        {
          var method = typeInfo.GetDeclaredMethod("op_UnaryNegation");

          if (method == null)
            return false;

          il.Emit(OpCodes.Call, method);
        }
      }
      else if (expr.NodeType == ExpressionType.OnesComplement)
      {
        il.Emit(OpCodes.Not);
      }
      else if (expr.NodeType == ExpressionType.Unbox)
      {
        il.Emit(OpCodes.Unbox_Any, exprType);
      }
      // else if (expr.NodeType == ExpressionType.IsTrue) { }
      // else if (expr.NodeType == ExpressionType.UnaryPlus) { }

      return il.EmitPopIfIgnoreResult(parent);
    }

#if LIGHT_EXPRESSION
            private static bool TryEmitTypeIsOrEqual(TypeBinaryExpression expr, IParameterProvider paramExprs, 
                ILGenerator il, ref ClosureInfo closure, CompilerFlags setup, ParentFlags parent)
            {
#else
    private static bool TryEmitTypeIsOrEqual(TypeBinaryExpression expr, IReadOnlyList<ParameterExpression> paramExprs,
      ILGenerator il, ref ClosureInfo closure, CompilerFlags setup, ParentFlags parent)
    {
#endif
      if (!TryEmit(expr.Expression, paramExprs, il, ref closure, setup, parent))
        return false;

      if ((parent & ParentFlags.IgnoreResult) != 0) return true;

      if (expr.NodeType == ExpressionType.TypeIs)
      {
        il.Emit(OpCodes.Isinst, expr.TypeOperand);
        il.Emit(OpCodes.Ldnull);
        il.Emit(OpCodes.Cgt_Un);

        return true;
      }

      if ((setup & CompilerFlags.ThrowOnNotSupportedExpression) != 0)
        throw new NotSupportedExpressionException(NotSupported.TypeEqual);

      return false;
    }

#if LIGHT_EXPRESSION
            private static bool TryEmitNot(UnaryExpression expr, IParameterProvider paramExprs, ILGenerator il, ref ClosureInfo closure, 
                CompilerFlags setup, ParentFlags parent)
            {
#else
    private static bool TryEmitNot(UnaryExpression expr, IReadOnlyList<ParameterExpression> paramExprs, ILGenerator il,
      ref ClosureInfo closure,
      CompilerFlags setup, ParentFlags parent)
    {
#endif
      if (expr.Operand.NodeType == ExpressionType.Equal)
      {
        var equalExpr = (BinaryExpression)expr.Operand;

        return TryEmitComparison(
          equalExpr.Left,
          equalExpr.Right,
          ExpressionType.NotEqual,
          equalExpr.Type,
          paramExprs,
          il,
          ref closure,
          setup,
          parent);
      }

      if (!TryEmit(expr.Operand, paramExprs, il, ref closure, setup, parent))
        return false;

      if ((parent & ParentFlags.IgnoreResult) != 0)
      {
        il.Emit(OpCodes.Pop);
      }
      else
      {
        if (expr.Type == Metadata<bool>.Type)
        {
          il.Emit(OpCodes.Ldc_I4_0);
          il.Emit(OpCodes.Ceq);
        }
        else
        {
          il.Emit(OpCodes.Not);
        }
      }

      return true;
    }

#if LIGHT_EXPRESSION
            private static bool TryEmitConvert(UnaryExpression expr, IParameterProvider paramExprs, ILGenerator il, ref ClosureInfo closure,
                CompilerFlags setup, ParentFlags parent)
            {
#else
    private static bool TryEmitConvert(UnaryExpression expr, IReadOnlyList<ParameterExpression> paramExprs,
      ILGenerator il, ref ClosureInfo closure,
      CompilerFlags setup, ParentFlags parent)
    {
#endif
      var opExpr = expr.Operand;
      var method = expr.Method;

      if (method != null && method.Name != "op_Implicit" && method.Name != "op_Explicit")
      {
        if (!TryEmit(
              opExpr,
              paramExprs,
              il,
              ref closure,
              setup,
              (parent & ~ParentFlags.IgnoreResult) | ParentFlags.InstanceCall))
          return false;

        return EmitMethodCallOrVirtualCall(il, method);
      }

      var sourceType = opExpr.Type;
      var sourceTypeIsNullable = sourceType.IsNullable();
      var underlyingNullableSourceType = sourceType.GetUnderlyingTypeCache();
      var targetType = expr.Type;

      if (targetType.IsAssignableFrom(sourceType) && (parent & ParentFlags.IgnoreResult) != 0)
        // quick path for ignored result & conversion which can't cause exception: just do nothing
        return TryEmit(opExpr, paramExprs, il, ref closure, setup, parent);

      if (sourceTypeIsNullable && targetType == underlyingNullableSourceType)
      {
        if (!TryEmit(
              opExpr,
              paramExprs,
              il,
              ref closure,
              setup,
              (parent & ~ParentFlags.IgnoreResult) | ParentFlags.InstanceAccess))
          return false;

        if (!closure.LastEmitIsAddress)
          EmitStoreAndLoadLocalVariableAddress(il, sourceType);

        il.Emit(OpCodes.Call, sourceType.FindValueGetterMethod());

        return il.EmitPopIfIgnoreResult(parent);
      }

      if (!TryEmit(
            opExpr,
            paramExprs,
            il,
            ref closure,
            setup,
            parent & ~ParentFlags.IgnoreResult & ~ParentFlags.InstanceAccess))
        return false;

      var targetTypeIsNullable = targetType.IsNullable();
      var underlyingNullableTargetType = targetType.GetUnderlyingTypeCache();

      if (targetTypeIsNullable && sourceType == underlyingNullableTargetType)
      {
        il.Emit(OpCodes.Newobj, targetType.GetTypeInfo().DeclaredConstructors.GetFirst());

        return true;
      }

      if (sourceType == targetType || targetType == Metadata<object>.Type)
      {
        if (targetType == Metadata<object>.Type && sourceType.IsValueType)
          il.Emit(OpCodes.Box, sourceType);

        return il.EmitPopIfIgnoreResult(parent);
      }

      // check implicit / explicit conversion operators on source and target types
      // for non-primitives and for non-primitive nullable - #73
      if (!sourceTypeIsNullable && !sourceType.IsPrimitive)
      {
        var actualTargetType = targetTypeIsNullable ? underlyingNullableTargetType : targetType;
        var convertOpMethod = method ?? sourceType.FindConvertOperator(sourceType, actualTargetType);

        if (convertOpMethod != null)
        {
          il.Emit(OpCodes.Call, convertOpMethod);

          if (targetTypeIsNullable)
            il.Emit(OpCodes.Newobj, targetType.GetTypeInfo().DeclaredConstructors.GetFirst());

          return il.EmitPopIfIgnoreResult(parent);
        }
      }
      else if (!targetTypeIsNullable)
      {
        if (method != null && method.DeclaringType == targetType &&
            method.GetParameters()[0].ParameterType == sourceType)
        {
          il.Emit(OpCodes.Call, method);

          return il.EmitPopIfIgnoreResult(parent);
        }

        var actualSourceType = sourceTypeIsNullable ? underlyingNullableSourceType : sourceType;
        var convertOpMethod = method ?? actualSourceType.FindConvertOperator(actualSourceType, targetType);

        if (convertOpMethod != null)
        {
          if (sourceTypeIsNullable)
          {
            EmitStoreAndLoadLocalVariableAddress(il, sourceType);
            il.Emit(OpCodes.Call, sourceType.FindValueGetterMethod());
          }

          il.Emit(OpCodes.Call, convertOpMethod);

          return il.EmitPopIfIgnoreResult(parent);
        }
      }

      if (!targetTypeIsNullable && !targetType.IsPrimitive)
      {
        if (method != null && method.DeclaringType == targetType &&
            method.GetParameters()[0].ParameterType == sourceType)
        {
          il.Emit(OpCodes.Call, method);

          return il.EmitPopIfIgnoreResult(parent);
        }

        var actualSourceType = sourceTypeIsNullable ? underlyingNullableSourceType : sourceType;
        // ReSharper disable once ConstantNullCoalescingCondition
        var convertOpMethod = method ?? targetType.FindConvertOperator(actualSourceType, targetType);

        if (convertOpMethod != null)
        {
          if (sourceTypeIsNullable)
          {
            EmitStoreAndLoadLocalVariableAddress(il, sourceType);
            il.Emit(OpCodes.Call, sourceType.FindValueGetterMethod());
          }

          il.Emit(OpCodes.Call, convertOpMethod);

          return il.EmitPopIfIgnoreResult(parent);
        }
      }
      else if (!sourceTypeIsNullable)
      {
        var actualTargetType = targetTypeIsNullable ? underlyingNullableTargetType : targetType;
        var convertOpMethod = method ?? actualTargetType.FindConvertOperator(sourceType, actualTargetType);

        if (convertOpMethod != null)
        {
          il.Emit(OpCodes.Call, convertOpMethod);

          if (targetTypeIsNullable)
            il.Emit(OpCodes.Newobj, targetType.GetTypeInfo().DeclaredConstructors.GetFirst());

          return il.EmitPopIfIgnoreResult(parent);
        }
      }

      if (sourceType == Metadata<object>.Type && targetType.IsValueType)
      {
        il.Emit(OpCodes.Unbox_Any, targetType);
      }
      else if (targetTypeIsNullable)
      {
        // Conversion to Nullable: `new Nullable<T>(T val);`
        if (!sourceTypeIsNullable)
        {
          if (!underlyingNullableTargetType
                .IsEnum && // todo: @clarify hope the source type is convertible to enum, huh 
              !TryEmitValueConvert(underlyingNullableTargetType, il, false))
            return false;

          il.Emit(OpCodes.Newobj, targetType.GetTypeInfo().DeclaredConstructors.GetFirst());
        }
        else
        {
          var sourceVarIndex = EmitStoreAndLoadLocalVariableAddress(il, sourceType);
          il.Emit(OpCodes.Call, sourceType.FindNullableHasValueGetterMethod());

          var labelSourceHasValue = il.DefineLabel();
          il.Emit(OpCodes.Brtrue_S, labelSourceHasValue); // jump where source has a value

          // otherwise, emit and load a `new Nullable<TTarget>()` struct (that's why a Init instead of New)
          EmitLoadLocalVariable(il, InitValueTypeVariable(il, targetType));

          // jump to completion
          var labelDone = il.DefineLabel();
          il.Emit(OpCodes.Br_S, labelDone);

          // if source nullable has a value:
          il.MarkLabel(labelSourceHasValue);
          EmitLoadLocalVariableAddress(il, sourceVarIndex);
          il.Emit(OpCodes.Call, sourceType.FindNullableGetValueOrDefaultMethod());

          if (!TryEmitValueConvert(
                underlyingNullableTargetType,
                il,
                expr.NodeType == ExpressionType.ConvertChecked))
          {
            var convertOpMethod = method ?? underlyingNullableTargetType.FindConvertOperator(
              underlyingNullableSourceType,
              underlyingNullableTargetType);

            if (convertOpMethod == null)
              return false; // nor conversion nor conversion operator is found

            il.Emit(OpCodes.Call, convertOpMethod);
          }

          il.Emit(OpCodes.Newobj, targetType.GetTypeInfo().DeclaredConstructors.GetFirst());
          il.MarkLabel(labelDone);
        }
      }
      else
      {
        if (targetType.IsEnum)
          targetType = Enum.GetUnderlyingType(targetType);

        // fixes #159
        if (sourceTypeIsNullable)
        {
          EmitStoreAndLoadLocalVariableAddress(il, sourceType);
          il.Emit(OpCodes.Call, sourceType.FindValueGetterMethod());
        }

        // cast as the last resort and let's it fail if unlucky
        if (!TryEmitValueConvert(targetType, il, expr.NodeType == ExpressionType.ConvertChecked))
        {
          if (sourceType.IsValueType)
            il.Emit(OpCodes.Box, sourceType);

          il.Emit(OpCodes.Castclass, targetType);
        }
      }

      return il.EmitPopIfIgnoreResult(parent);
    }

    private static bool TryEmitValueConvert(Type targetType, ILGenerator il, bool isChecked)
    {
      if (targetType == Metadata<int>.Type)
        il.Emit(isChecked ? OpCodes.Conv_Ovf_I4 : OpCodes.Conv_I4);
      else if (targetType == Metadata<float>.Type)
        il.Emit(OpCodes.Conv_R4);
      else if (targetType == Metadata<uint>.Type)
        il.Emit(isChecked ? OpCodes.Conv_Ovf_U4 : OpCodes.Conv_U4);
      else if (targetType == Metadata<sbyte>.Type)
        il.Emit(isChecked ? OpCodes.Conv_Ovf_I1 : OpCodes.Conv_I1);
      else if (targetType == Metadata<byte>.Type)
        il.Emit(isChecked ? OpCodes.Conv_Ovf_U1 : OpCodes.Conv_U1);
      else if (targetType == Metadata<short>.Type)
        il.Emit(isChecked ? OpCodes.Conv_Ovf_I2 : OpCodes.Conv_I2);
      else if (targetType == Metadata<ushort>.Type || targetType == Metadata<char>.Type)
        il.Emit(isChecked ? OpCodes.Conv_Ovf_U2 : OpCodes.Conv_U2);
      else if (targetType == Metadata<long>.Type)
        il.Emit(isChecked ? OpCodes.Conv_Ovf_I8 : OpCodes.Conv_I8);
      else if (targetType == Metadata<ulong>.Type)
        il.Emit(
          isChecked
            ? OpCodes.Conv_Ovf_U8
            : OpCodes.Conv_U8); // should we consider if sourceType.IsUnsigned == false and using the OpCodes.Conv_I8 (seems like the System.Compile does it)
      else if (targetType == Metadata<double>.Type)
        il.Emit(OpCodes.Conv_R8);
      else
        return false;

      return true;
    }

    private static bool TryEmitConstantOfNotNullValue(
      bool considerClosure, Type exprType, object constantValue, ILGenerator il, ref ClosureInfo closure)
    {
      var constValueType = constantValue.GetType();

      if (considerClosure && IsClosureBoundConstant(constantValue, constValueType))
      {
        var constItems = closure.Constants.Items;
        var constIndex = closure.Constants.Count - 1;

        while (constIndex != -1 && !ReferenceEquals(constItems[constIndex], constantValue))
          --constIndex;

        if (constIndex == -1)
          return false;

        var varIndex = closure.ConstantUsageThenVarIndex.Items[constIndex] - 1;

        if (varIndex > 0)
        {
          EmitLoadLocalVariable(il, varIndex);
        }
        else
        {
          il.Emit(
            OpCodes.Ldloc_0); // load constants array from the 0 variable // todo: @perf until we optimize for a single constant case - then we need a check here for number of constants

          EmitLoadConstantInt(il, constIndex);
          il.Emit(OpCodes.Ldelem_Ref);
          if (exprType.IsValueType) il.Emit(OpCodes.Unbox_Any, exprType);
        }
      }
      else
      {
        if (constantValue is string s)
        {
          il.Emit(OpCodes.Ldstr, s);

          return true;
        }

        if (constantValue is Type t)
        {
          il.Emit(OpCodes.Ldtoken, t);
          il.Emit(OpCodes.Call, _getTypeFromHandleMethod);

          return true;
        }

        // get raw enum type to light
        if (constValueType.IsEnum)
          constValueType = Enum.GetUnderlyingType(constValueType);

        if (!TryEmitNumberConstant(il, constantValue, constValueType))
          return false;
      }

      var underlyingNullableType = exprType.GetUnderlyingTypeCache();

      if (underlyingNullableType != null)
        il.Emit(OpCodes.Newobj, exprType.GetConstructors().GetFirst());

      // boxing the value type, otherwise we can get a strange result when 0 is treated as Null.
      else if (exprType == Metadata<object>.Type && constValueType.IsValueType)
        il.Emit(OpCodes.Box, constantValue.GetType()); // using normal type for Enum instead of underlying type

      return true;
    }

    // todo: @perf can we do something about boxing?
    private static bool TryEmitNumberConstant(ILGenerator il, object constantValue, Type constValueType)
    {
      if (constValueType == Metadata<int>.Type)
        EmitLoadConstantInt(il, (int)constantValue);
      else if (constValueType == Metadata<char>.Type)
        EmitLoadConstantInt(il, (char)constantValue);
      else if (constValueType == Metadata<short>.Type)
        EmitLoadConstantInt(il, (short)constantValue);
      else if (constValueType == Metadata<byte>.Type)
        EmitLoadConstantInt(il, (byte)constantValue);
      else if (constValueType == Metadata<ushort>.Type)
        EmitLoadConstantInt(il, (ushort)constantValue);
      else if (constValueType == Metadata<sbyte>.Type)
        EmitLoadConstantInt(il, (sbyte)constantValue);
      else if (constValueType == Metadata<uint>.Type)
        unchecked
        {
          EmitLoadConstantInt(il, (int)(uint)constantValue);
        }
      else if (constValueType == Metadata<long>.Type)
        il.Emit(OpCodes.Ldc_I8, (long)constantValue);
      else if (constValueType == Metadata<ulong>.Type)
        unchecked
        {
          il.Emit(OpCodes.Ldc_I8, (long)(ulong)constantValue);
        }
      else if (constValueType == Metadata<float>.Type)
        il.Emit(OpCodes.Ldc_R4, (float)constantValue);
      else if (constValueType == Metadata<double>.Type)
        il.Emit(OpCodes.Ldc_R8, (double)constantValue);
      else if (constValueType == Metadata<bool>.Type)
        il.Emit((bool)constantValue ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0);
      else if (constValueType == Metadata<IntPtr>.Type)
        il.Emit(OpCodes.Ldc_I8, ((IntPtr)constantValue).ToInt64());
      else if (constValueType == Metadata<UIntPtr>.Type)
        unchecked
        {
          il.Emit(OpCodes.Ldc_I8, (long)((UIntPtr)constantValue).ToUInt64());
        }
      else if (constValueType == Metadata<decimal>.Type)
        EmitDecimalConstant((decimal)constantValue, il);
      else
        return false;

      return true;
    }

    internal static bool TryEmitNumberOne(ILGenerator il, Type type)
    {
      if (type == Metadata<int>.Type || type == Metadata<char>.Type || type == Metadata<short>.Type ||
          type == Metadata<byte>.Type || type == Metadata<ushort>.Type || type == Metadata<sbyte>.Type ||
          type == Metadata<uint>.Type)
        il.Emit(OpCodes.Ldc_I4_1);
      else if (type == Metadata<long>.Type || type == Metadata<ulong>.Type ||
               type == Metadata<IntPtr>.Type || type == Metadata<UIntPtr>.Type)
        il.Emit(OpCodes.Ldc_I8, (long)1);
      else if (type == Metadata<float>.Type)
        il.Emit(OpCodes.Ldc_R4, 1f);
      else if (type == Metadata<double>.Type)
        il.Emit(OpCodes.Ldc_R8, 1d);
      else
        return false;

      return true;
    }

    internal static void EmitLoadConstantsAndNestedLambdasIntoVars(ILGenerator il, ref ClosureInfo closure)
    {
      // todo: @perf load the field to `var` only if the constants are more than 1
      // Load constants array field from Closure and store it into the variable
      il.Emit(OpCodes.Ldarg_0);
      il.Emit(OpCodes.Ldfld, ArrayClosureArrayField);
      EmitStoreLocalVariable(il, il.GetNextLocalVarIndex(Metadata<object[]>.Type)); // always does Stloc_0

      var constItems =
        closure.Constants
          .Items; // todo: @perf why do we getting when non constants is stored but just a nested lambda is present?

      var constCount = closure.Constants.Count;
      var constUsage = closure.ConstantUsageThenVarIndex.Items;

      int varIndex;

      for (var i = 0; i < constCount; i++)
        if (constUsage[i] >
            1) // todo: @perf should we proceed to do this or simplify and remove the usages for the closure info?
        {
          il.Emit(OpCodes.Ldloc_0); // SHOULD BE always at 0 locaton; load array field variable on the stack
          EmitLoadConstantInt(il, i);
          il.Emit(OpCodes.Ldelem_Ref);

          var varType = constItems[i].GetType();

          if (varType.IsValueType)
            il.Emit(OpCodes.Unbox_Any, varType);

          varIndex = il.GetNextLocalVarIndex(varType);
          constUsage[i] = varIndex + 1; // to distinguish from the default 1
          EmitStoreLocalVariable(il, varIndex);
        }

      var nestedLambdas = closure.NestedLambdas;

      for (var i = 0; i < nestedLambdas.Length; i++)
      {
        il.Emit(OpCodes.Ldloc_0); // SHOULD BE always at 0 locaton; load array field variable on the stack
        EmitLoadConstantInt(il, constCount + i);
        il.Emit(OpCodes.Ldelem_Ref);

        // store the nested lambda in the local variable 
        var nestedLambda = nestedLambdas[i];
        varIndex = il.GetNextLocalVarIndex(nestedLambda.Lambda.GetType());
        nestedLambda.LambdaVarIndex = varIndex; // save the var index
        EmitStoreLocalVariable(il, varIndex);
      }
    }

    private static void EmitDecimalConstant(decimal value, ILGenerator il)
    {
      //check if decimal has decimal places, if not use shorter IL code (constructor from int or long)
      if (value % 1 == 0)
      {
        if (value >= int.MinValue && value <= int.MaxValue)
        {
          EmitLoadConstantInt(il, decimal.ToInt32(value));
          il.Emit(OpCodes.Newobj, Metadata<decimal>.Type.FindSingleParamConstructor(Metadata<int>.Type));

          return;
        }

        if (value >= long.MinValue && value <= long.MaxValue)
        {
          il.Emit(OpCodes.Ldc_I8, decimal.ToInt64(value));
          il.Emit(OpCodes.Newobj, Metadata<decimal>.Type.FindSingleParamConstructor(Metadata<long>.Type));

          return;
        }
      }

      if (value == decimal.MinValue)
      {
        il.Emit(OpCodes.Ldsfld, Metadata<decimal>.Type.GetField(nameof(decimal.MinValue)));

        return;
      }

      if (value == decimal.MaxValue)
      {
        il.Emit(OpCodes.Ldsfld, Metadata<decimal>.Type.GetField(nameof(decimal.MaxValue)));

        return;
      }

      var parts = decimal.GetBits(value);
      var sign = (parts[3] & 0x80000000) != 0;
      var scale = (byte)((parts[3] >> 16) & 0x7F);

      EmitLoadConstantInt(il, parts[0]);
      EmitLoadConstantInt(il, parts[1]);
      EmitLoadConstantInt(il, parts[2]);

      il.Emit(sign ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0);
      EmitLoadConstantInt(il, scale);

      il.Emit(OpCodes.Conv_U1);

      il.Emit(OpCodes.Newobj, _decimalCtor.Value);
    }

    private static readonly Lazy<ConstructorInfo> _decimalCtor = new(
      () =>
      {
        foreach (var ctor in Metadata<decimal>.Type.GetTypeInfo().DeclaredConstructors)
          if (ctor.GetParameters().Length == 5)
            return ctor;

        return null;
      });

    private static int InitValueTypeVariable(ILGenerator il, Type exprType)
    {
      var locVarIndex = il.GetNextLocalVarIndex(exprType);
      EmitLoadLocalVariableAddress(il, locVarIndex);
      il.Emit(OpCodes.Initobj, exprType);

      return locVarIndex;
    }

#if LIGHT_EXPRESSION
            private static bool EmitNewArrayBounds(NewArrayExpression expr, IParameterProvider paramExprs, 
                ILGenerator il, ref ClosureInfo closure, CompilerFlags setup, ParentFlags parent)
            {
                var bounds = (IArgumentProvider)expr;
                var boundCount = bounds.ArgumentCount;
#else
    private static bool EmitNewArrayBounds(NewArrayExpression expr, IReadOnlyList<ParameterExpression> paramExprs,
      ILGenerator il, ref ClosureInfo closure, CompilerFlags setup, ParentFlags parent)
    {
      var bounds = expr.Expressions;
      var boundCount = bounds.Count;
#endif
      if (boundCount == 1)
      {
        if (!TryEmit(bounds.GetArgument(0), paramExprs, il, ref closure, setup, parent))
          return false;

        var elemType = expr.Type.GetElementType();

        if (elemType == null)
          return false;

        il.Emit(OpCodes.Newarr, elemType);
      }
      else
      {
        for (var i = 0; i < boundCount; i++)
          if (!TryEmit(bounds.GetArgument(i), paramExprs, il, ref closure, setup, parent))
            return false;

        il.Emit(OpCodes.Newobj, expr.Type.GetTypeInfo().DeclaredConstructors.GetFirst());
      }

      return true;
    }

#if LIGHT_EXPRESSION
            private static bool EmitNewArrayInit(NewArrayExpression expr, IParameterProvider paramExprs, 
                ILGenerator il, ref ClosureInfo closure, CompilerFlags setup, ParentFlags parent)
            {
#else
    private static bool EmitNewArrayInit(NewArrayExpression expr, IReadOnlyList<ParameterExpression> paramExprs,
      ILGenerator il, ref ClosureInfo closure, CompilerFlags setup, ParentFlags parent)
    {
#endif
      var arrayType = expr.Type;

      if (arrayType.GetArrayRank() > 1)
        return
          false; // todo: @feature multi-dimensional array initializers are not supported yet, they also are not supported by the hoisted expression

      var elemType = arrayType.GetElementType();

      if (elemType == null)
        return false;

#if LIGHT_EXPRESSION
                var elems = (IArgumentProvider)expr;
                var elemCount = elems.ArgumentCount;
#else
      var elems = expr.Expressions;
      var elemCount = elems.Count;
#endif
      EmitLoadConstantInt(
        il,
        elemCount); // emit the length of the array calculated from the number of initializer elements

      il.Emit(OpCodes.Newarr, elemType);

      var isElemOfValueType = elemType.IsValueType;

      for (var i = 0; i < elemCount; i++)
      {
        il.Emit(OpCodes.Dup);
        EmitLoadConstantInt(il, i);

        if (isElemOfValueType) // loading element address for later copying of value into it.
        {
          il.Emit(OpCodes.Ldelema, elemType);

          if (!TryEmit(elems.GetArgument(i), paramExprs, il, ref closure, setup, parent))
            return false;

          il.Emit(OpCodes.Stobj, elemType); // store element of value type by array element address
        }
        else
        {
          if (!TryEmit(elems.GetArgument(i), paramExprs, il, ref closure, setup, parent))
            return false;

          il.Emit(OpCodes.Stelem_Ref);
        }
      }

      return true;
    }

    private static bool TryEmitArrayIndex(Type type, ILGenerator il, ParentFlags parent, ref ClosureInfo closure)
    {
      if (!type.IsValueType)
      {
        il.Emit(OpCodes.Ldelem_Ref);

        return true;
      }

      // access the value type by address when it is used later for the member access or as instance in the method call
      if ((parent & (ParentFlags.MemberAccess | ParentFlags.InstanceAccess)) != 0)
      {
        il.Emit(OpCodes.Ldelema, type);
        closure.LastEmitIsAddress = true;

        return true;
      }

      if (type == Metadata<int>.Type)
        il.Emit(OpCodes.Ldelem_I4);
      else if (type == Metadata<long>.Type)
        il.Emit(OpCodes.Ldelem_I8);
      else if (type == Metadata<short>.Type)
        il.Emit(OpCodes.Ldelem_I2);
      else if (type == Metadata<sbyte>.Type)
        il.Emit(OpCodes.Ldelem_I1);
      else if (type == Metadata<float>.Type)
        il.Emit(OpCodes.Ldelem_R4);
      else if (type == Metadata<double>.Type)
        il.Emit(OpCodes.Ldelem_R8);
      else if (type == Metadata<IntPtr>.Type)
        il.Emit(OpCodes.Ldelem_I);
      else if (type == Metadata<UIntPtr>.Type)
        il.Emit(OpCodes.Ldelem_I);
      else if (type == Metadata<byte>.Type)
        il.Emit(OpCodes.Ldelem_U1);
      else if (type == Metadata<ushort>.Type)
        il.Emit(OpCodes.Ldelem_U2);
      else if (type == Metadata<uint>.Type)
        il.Emit(OpCodes.Ldelem_U4);
      else
        il.Emit(OpCodes.Ldelem, type);

      return true;
    }

#if LIGHT_EXPRESSION
            private static bool EmitMemberInit(MemberInitExpression expr, IParameterProvider paramExprs, ILGenerator il, ref ClosureInfo closure, 
                CompilerFlags setup, ParentFlags parent)
#else
    private static bool EmitMemberInit(MemberInitExpression expr, IReadOnlyList<ParameterExpression> paramExprs,
      ILGenerator il, ref ClosureInfo closure,
      CompilerFlags setup, ParentFlags parent)
#endif
    {
      var valueVarIndex = -1;

      if (expr.Type.IsValueType)
        valueVarIndex = il.GetNextLocalVarIndex(expr.Type);

      var newExpr = expr.NewExpression;
#if LIGHT_EXPRESSION
                if (newExpr == null)
                {
                    if (!TryEmit(expr.Expression, paramExprs, il, ref closure, setup, parent))
                        return false;
                }
                else
#endif
      {
#if SUPPORTS_ARGUMENT_PROVIDER
        var argExprs = (IArgumentProvider)newExpr;
        var argCount = argExprs.ArgumentCount;
#else
        var argExprs = newExpr.Arguments;
        var argCount = argExprs.Count;
#endif
        if (argCount > 0)
        {
          var args = newExpr.Constructor.GetParameters();

          for (var i = 0; i < argCount; i++)
            if (!TryEmit(
                  argExprs.GetArgument(i),
                  paramExprs,
                  il,
                  ref closure,
                  setup,
                  parent,
                  args[i].ParameterType.IsByRef ? i : -1))
              return false;
        }

        // ReSharper disable once ConditionIsAlwaysTrueOrFalse
        if (newExpr.Constructor != null)
        {
          il.Emit(OpCodes.Newobj, newExpr.Constructor);
        }
        else if (newExpr.Type.IsValueType)
        {
          if (valueVarIndex == -1)
            valueVarIndex = il.GetNextLocalVarIndex(expr.Type);

          EmitLoadLocalVariableAddress(il, valueVarIndex);
          il.Emit(OpCodes.Initobj, newExpr.Type);
        }
        else
        {
          return false; // null constructor and not a value type, better to fallback
        }
      }

#if LIGHT_EXPRESSION
                var bindings = (IArgumentProvider<MemberBinding>)expr;
                var bindCount = bindings.ArgumentCount;
#else
      var bindings = expr.Bindings;
      var bindCount = bindings.Count;
#endif
      for (var i = 0; i < bindCount; i++)
      {
        var binding = bindings.GetArgument(i);

        if (binding.BindingType != MemberBindingType.Assignment) // todo: @feature is not supported yet
          return false;

        if (valueVarIndex != -1) // load local value address, to set its members
          EmitLoadLocalVariableAddress(il, valueVarIndex);
        else
          il.Emit(OpCodes.Dup); // duplicate member owner on stack

        if (!TryEmit(((MemberAssignment)binding).Expression, paramExprs, il, ref closure, setup, parent) ||
            !EmitMemberAssign(il, binding.Member))
          return false;
      }

      if (valueVarIndex != -1)
        EmitLoadLocalVariable(il, valueVarIndex);

      return true;
    }

    private static bool EmitMemberAssign(ILGenerator il, MemberInfo member)
    {
      if (member is PropertyInfo prop)
      {
        var method = prop.SetMethod;

        return method != null && EmitMethodCallOrVirtualCall(il, method);
      }

      if (member is FieldInfo field)
      {
        il.Emit(field.IsStatic ? OpCodes.Stsfld : OpCodes.Stfld, field);

        return true;
      }

      return false;
    }

#if LIGHT_EXPRESSION
            private static bool TryEmitListInit(ListInitExpression expr, IParameterProvider paramExprs, ILGenerator il, ref ClosureInfo closure, 
                CompilerFlags setup, ParentFlags parent)
#else
    private static bool TryEmitListInit(ListInitExpression expr, IReadOnlyList<ParameterExpression> paramExprs,
      ILGenerator il, ref ClosureInfo closure,
      CompilerFlags setup, ParentFlags parent)
#endif
    {
      var valueVarIndex = -1;

      if (expr.Type.IsValueType)
        valueVarIndex = il.GetNextLocalVarIndex(expr.Type);

      var newExpr = expr.NewExpression;
      var exprType = newExpr.Type;
#if SUPPORTS_ARGUMENT_PROVIDER
      var argExprs = (IArgumentProvider)newExpr;
      var argCount = argExprs.ArgumentCount;
#else
      var argExprs = newExpr.Arguments;
      var argCount = argExprs.Count;
#endif
      if (argCount > 0)
      {
        var args = newExpr.Constructor.GetParameters();

        for (var i = 0; i < argCount; i++)
          if (!TryEmit(
                argExprs.GetArgument(i),
                paramExprs,
                il,
                ref closure,
                setup,
                parent,
                args[i].ParameterType.IsByRef ? i : -1))
            return false;
      }

      // ReSharper disable once ConditionIsAlwaysTrueOrFalse
      if (newExpr.Constructor != null)
      {
        il.Emit(OpCodes.Newobj, newExpr.Constructor);
      }
      else if (exprType.IsValueType)
      {
        if (valueVarIndex == -1)
          valueVarIndex = il.GetNextLocalVarIndex(expr.Type);

        EmitLoadLocalVariableAddress(il, valueVarIndex);
        il.Emit(OpCodes.Initobj, exprType);
      }
      else
      {
        return false; // null constructor and not a value type, better to fallback
      }

      var inits = expr.Initializers;
      var initCount = inits.Count;

      // see the TryEmitMethodCall for the reason of the callFlags
      var callFlags = (parent & ~ParentFlags.IgnoreResult & ~ParentFlags.MemberAccess & ~ParentFlags.InstanceAccess) |
                      ParentFlags.Call;

      for (var i = 0; i < initCount; ++i)
      {
        if (valueVarIndex != -1) // load local value address, to set its members
          EmitLoadLocalVariableAddress(il, valueVarIndex);
        else
          il.Emit(OpCodes.Dup); // duplicate member owner on stack

        var elemInit = inits.GetArgument(i);
        var method = elemInit.AddMethod;
        var methodParams = method.GetParameters();
#if LIGHT_EXPRESSION
                    var addArgs = (IArgumentProvider)elemInit;
                    var addArgCount = elemInit.ArgumentCount;
#else
        var addArgs = elemInit.Arguments;
        var addArgCount = addArgs.Count;
#endif
        for (var a = 0; a < addArgCount; ++a)
        {
          var arg = addArgs.GetArgument(a);

          if (!TryEmit(
                addArgs.GetArgument(a),
                paramExprs,
                il,
                ref closure,
                setup,
                callFlags,
                methodParams[a].ParameterType.IsByRef ? a : -1))
            return false;
        }

        if (!exprType.IsValueType)
        {
          EmitMethodCallOrVirtualCall(il, method);
        }
        else if (!method.IsVirtual) // #251 - no need for constrain or virtual call because it is already by-ref
        {
          EmitMethodCall(il, method);
        }
        else if (method.DeclaringType == exprType)
        {
          EmitMethodCall(il, method);
        }
        else
        {
          il.Emit(
            OpCodes.Constrained,
            exprType); // todo: @check it is a value type so... can we de-virtualize the call?

          il.Emit(OpCodes.Callvirt, method);
        }
      }

      if (valueVarIndex != -1)
        EmitLoadLocalVariable(il, valueVarIndex);

      return true;
    }

#if LIGHT_EXPRESSION
            private static bool TryEmitIncDecAssign(UnaryExpression expr, ExpressionType nodeType, IParameterProvider paramExprs, ILGenerator il, ref ClosureInfo closure, 
                CompilerFlags setup, ParentFlags parent)
            {
#else
    private static bool TryEmitIncDecAssign(UnaryExpression expr, ExpressionType nodeType,
      IReadOnlyList<ParameterExpression> paramExprs, ILGenerator il, ref ClosureInfo closure,
      CompilerFlags setup, ParentFlags parent)
    {
#endif
      var operandExpr = expr.Operand;

      var resultVar =
        il.GetNextLocalVarIndex(
          expr.Type); // todo: @perf here is the opportunity to reuse the variable because is only needed in the local scope 

      if (operandExpr is ParameterExpression p)
      {
#if LIGHT_EXPRESSION
                    var paramExprCount = paramExprs.ParameterCount;
#else
        var paramExprCount = paramExprs.Count;
#endif
        var paramIndex = -1;
        var localVarIndex = closure.GetDefinedLocalVarOrDefault(p);

        if (localVarIndex != -1)
        {
          EmitLoadLocalVariable(il, localVarIndex);
        }
        else
        {
          paramIndex = paramExprCount - 1;

          while (paramIndex != -1 && !ReferenceEquals(paramExprs.GetParameter(paramIndex), p))
            --paramIndex;

          if (paramIndex == -1)
            return false;

          if ((closure.Status & ClosureStatus.ShouldBeStaticMethod) == 0)
            ++paramIndex;

          EmitLoadArg(il, paramIndex);

          if (p.IsByRef)
            EmitValueTypeDereference(il, p.Type);
        }

        if (nodeType == ExpressionType.PostIncrementAssign || nodeType == ExpressionType.PostDecrementAssign)
          EmitStoreAndLoadLocalVariable(il, resultVar); // save the non-incremented value for the later further use

        il.Emit(OpCodes.Ldc_I4_1);

        il.Emit(
          nodeType == ExpressionType.PostIncrementAssign || nodeType == ExpressionType.PreIncrementAssign
            ? OpCodes.Add
            : OpCodes.Sub);

        if (nodeType == ExpressionType.PreIncrementAssign || nodeType == ExpressionType.PreDecrementAssign)
          EmitStoreAndLoadLocalVariable(il, resultVar); // save the non-incremented value for the later further use

        if (localVarIndex != -1)
        {
          EmitStoreLocalVariable(il, localVarIndex); // store incremented value into the local value;
        }
        else if (p.IsByRef)
        {
          var incrementedVar = il.GetNextLocalVarIndex(expr.Type);
          EmitStoreLocalVariable(il, incrementedVar);
          EmitLoadArg(il, paramIndex);
          EmitLoadLocalVariable(il, incrementedVar);
          EmitStoreByRefValueType(il, expr.Type);
        }
        else
        {
          il.Emit(OpCodes.Starg_S, paramIndex);
        }
      }
      else if (operandExpr is MemberExpression m)
      {
        if (!TryEmitMemberAccess(m, paramExprs, il, ref closure, setup, parent | ParentFlags.DupMemberOwner))
          return false;

        if (nodeType == ExpressionType.PostIncrementAssign || nodeType == ExpressionType.PostDecrementAssign)
          EmitStoreAndLoadLocalVariable(il, resultVar); // save the non-incremented value for the later further use

        il.Emit(OpCodes.Ldc_I4_1);

        il.Emit(
          nodeType == ExpressionType.PostIncrementAssign || nodeType == ExpressionType.PreIncrementAssign
            ? OpCodes.Add
            : OpCodes.Sub);

        if (nodeType == ExpressionType.PreIncrementAssign || nodeType == ExpressionType.PreDecrementAssign)
          EmitStoreAndLoadLocalVariable(il, resultVar); // save the non-incremented value for the later further use

        if (!EmitMemberAssign(il, m.Member))
          return false;
      }
      else if (operandExpr is IndexExpression i)
      {
        if (!TryEmitIndex(i, paramExprs, il, ref closure, setup, parent | ParentFlags.IndexAccess))
          return false;

        if (nodeType == ExpressionType.PostIncrementAssign || nodeType == ExpressionType.PostDecrementAssign)
          EmitStoreAndLoadLocalVariable(il, resultVar); // save the non-incremented value for the later further use

        il.Emit(OpCodes.Ldc_I4_1);

        il.Emit(
          nodeType == ExpressionType.PostIncrementAssign || nodeType == ExpressionType.PreIncrementAssign
            ? OpCodes.Add
            : OpCodes.Sub);

        if (nodeType == ExpressionType.PreIncrementAssign || nodeType == ExpressionType.PreDecrementAssign)
          EmitStoreAndLoadLocalVariable(il, resultVar); // save the non-incremented value for the later further use

        if (!TryEmitIndexAssign(i, i.Object?.Type, expr.Type, il))
          return false;
      }
      else
      {
        return false; // not_supported_expression
      }

      if ((parent & ParentFlags.IgnoreResult) == 0)
        EmitLoadLocalVariable(
          il,
          resultVar); // todo: @perf here is the opportunity to reuse the variable because is only needed in the local scope

      return true;
    }

#if LIGHT_EXPRESSION
            private static bool TryEmitAssign(BinaryExpression expr, IParameterProvider paramExprs, ILGenerator il, ref ClosureInfo closure, 
                CompilerFlags setup, ParentFlags parent)
#else
    private static bool TryEmitAssign(BinaryExpression expr, IReadOnlyList<ParameterExpression> paramExprs,
      ILGenerator il, ref ClosureInfo closure,
      CompilerFlags setup, ParentFlags parent)
#endif
    {
      var left = expr.Left;
      var right = expr.Right;
      var leftNodeType = expr.Left.NodeType;
      var nodeType = expr.NodeType;

      // if this assignment is part of a single body-less expression or the result of a block
      // we should put its result to the evaluation stack before the return, otherwise we are
      // somewhere inside the block, so we shouldn't return with the result
      var flags = parent & ~ParentFlags.IgnoreResult;

      switch (leftNodeType)
      {
        case ExpressionType.Parameter:
          var leftParamExpr = (ParameterExpression)left;
#if LIGHT_EXPRESSION
                        var paramExprCount = paramExprs.ParameterCount;
#else
          var paramExprCount = paramExprs.Count;
#endif
          var paramIndex = paramExprCount - 1;

          while (paramIndex != -1 && !ReferenceEquals(paramExprs.GetParameter(paramIndex), leftParamExpr))
            --paramIndex;

          var arithmeticNodeType = nodeType;

          switch (nodeType)
          {
            case ExpressionType.AddAssign:
              arithmeticNodeType = ExpressionType.Add;

              break;
            case ExpressionType.AddAssignChecked:
              arithmeticNodeType = ExpressionType.AddChecked;

              break;
            case ExpressionType.SubtractAssign:
              arithmeticNodeType = ExpressionType.Subtract;

              break;
            case ExpressionType.SubtractAssignChecked:
              arithmeticNodeType = ExpressionType.SubtractChecked;

              break;
            case ExpressionType.MultiplyAssign:
              arithmeticNodeType = ExpressionType.Multiply;

              break;
            case ExpressionType.MultiplyAssignChecked:
              arithmeticNodeType = ExpressionType.MultiplyChecked;

              break;
            case ExpressionType.DivideAssign:
              arithmeticNodeType = ExpressionType.Divide;

              break;
            case ExpressionType.ModuloAssign:
              arithmeticNodeType = ExpressionType.Modulo;

              break;
            case ExpressionType.PowerAssign:
              arithmeticNodeType = ExpressionType.Power;

              break;
            case ExpressionType.AndAssign:
              arithmeticNodeType = ExpressionType.And;

              break;
            case ExpressionType.OrAssign:
              arithmeticNodeType = ExpressionType.Or;

              break;
            case ExpressionType.ExclusiveOrAssign:
              arithmeticNodeType = ExpressionType.ExclusiveOr;

              break;
            case ExpressionType.LeftShiftAssign:
              arithmeticNodeType = ExpressionType.LeftShift;

              break;
            case ExpressionType.RightShiftAssign:
              arithmeticNodeType = ExpressionType.RightShift;

              break;
          }

          if (paramIndex != -1)
          {
            // shift parameter index by one, because the first one will be closure
            if ((closure.Status & ClosureStatus.ShouldBeStaticMethod) == 0)
              ++paramIndex;

            if (leftParamExpr.IsByRef)
              EmitLoadArg(il, paramIndex);

            if (arithmeticNodeType == nodeType)
            {
              if (!TryEmit(right, paramExprs, il, ref closure, setup, flags))
                return false;
            }
            else if (!TryEmitArithmetic(expr, arithmeticNodeType, paramExprs, il, ref closure, setup, parent))
            {
              return false;
            }

            if ((parent & ParentFlags.IgnoreResult) == 0)
              il.Emit(OpCodes.Dup); // duplicate value to assign and return

            if (leftParamExpr.IsByRef)
              EmitStoreByRefValueType(il, leftParamExpr.Type);
            else
              il.Emit(OpCodes.Starg_S, paramIndex);

            return true;
          }
          else if (arithmeticNodeType != nodeType)
          {
            var localVarIdx = closure.GetDefinedLocalVarOrDefault(leftParamExpr);

            if (localVarIdx != -1)
            {
              if (!TryEmitArithmetic(expr, arithmeticNodeType, paramExprs, il, ref closure, setup, parent))
                return false;

              EmitStoreLocalVariable(il, localVarIdx);

              return true;
            }
          }

          // if parameter isn't passed, then it is passed into some outer lambda or it is a local variable,
          // so it should be loaded from closure or from the locals. Then the closure is null will be an invalid state.
          // if it's a local variable, then store the right value in it
          var localVarIndex = closure.GetDefinedLocalVarOrDefault(leftParamExpr);

          if (localVarIndex != -1)
          {
            if (!TryEmit(right, paramExprs, il, ref closure, setup, flags))
              return false;

            if ((right as ParameterExpression)?.IsByRef == true)
              il.Emit(OpCodes.Ldind_I4);

            if ((parent & ParentFlags.IgnoreResult) ==
                0) // if we have to push the result back, duplicate the right value
              il.Emit(OpCodes.Dup);

            EmitStoreLocalVariable(il, localVarIndex);

            return true;
          }

          // check that it's a captured parameter by closure
          var nonPassedParams = closure.NonPassedParameters;
          var nonPassedParamIndex = nonPassedParams.Length - 1;

          while (nonPassedParamIndex != -1 &&
                 !ReferenceEquals(nonPassedParams[nonPassedParamIndex], leftParamExpr))
            --nonPassedParamIndex;

          if (nonPassedParamIndex == -1)
            return false; // what??? no chance

          il.Emit(OpCodes.Ldarg_0); // closure is always a first argument

          if ((parent & ParentFlags.IgnoreResult) == 0)
          {
            if (!TryEmit(right, paramExprs, il, ref closure, setup, flags))
              return false;

            var valueVarIndex = il.GetNextLocalVarIndex(expr.Type); // store left value in variable
            EmitStoreLocalVariable(il, valueVarIndex);

            // load array field and param item index
            il.Emit(OpCodes.Ldfld, ArrayClosureWithNonPassedParamsField);
            EmitLoadConstantInt(il, nonPassedParamIndex);
            EmitLoadLocalVariable(il, valueVarIndex);

            if (expr.Type.IsValueType)
              il.Emit(OpCodes.Box, expr.Type);

            il.Emit(OpCodes.Stelem_Ref); // put the variable into array
            EmitLoadLocalVariable(il, valueVarIndex); // todo: @perf what if we just dup the `valueVar`?
          }
          else
          {
            // load array field and param item index
            il.Emit(OpCodes.Ldfld, ArrayClosureWithNonPassedParamsField);
            EmitLoadConstantInt(il, nonPassedParamIndex);

            if (!TryEmit(right, paramExprs, il, ref closure, setup, flags))
              return false;

            if (expr.Type.IsValueType)
              il.Emit(OpCodes.Box, expr.Type);

            il.Emit(OpCodes.Stelem_Ref); // put the variable into array
          }

          return true;

        case ExpressionType.MemberAccess:
          var assignFromLocalVar = right.NodeType == ExpressionType.Try;

          var resultLocalVarIndex = -1;

          if (assignFromLocalVar)
          {
            resultLocalVarIndex = il.GetNextLocalVarIndex(right.Type);

            if (!TryEmit(right, paramExprs, il, ref closure, setup, ParentFlags.Empty))
              return false;

            EmitStoreLocalVariable(il, resultLocalVarIndex);
          }

          var memberExpr = (MemberExpression)left;
          var objExpr = memberExpr.Expression;

          if (objExpr != null &&
              !TryEmit(
                objExpr,
                paramExprs,
                il,
                ref closure,
                setup,
                flags | ParentFlags.MemberAccess | ParentFlags.InstanceAccess))
            return false;

          if (assignFromLocalVar)
            EmitLoadLocalVariable(il, resultLocalVarIndex);
          else if (!TryEmit(right, paramExprs, il, ref closure, setup, ParentFlags.Empty))
            return false;

          var member = memberExpr.Member;

          if ((parent & ParentFlags.IgnoreResult) != 0)
            return EmitMemberAssign(il, member);

          il.Emit(OpCodes.Dup);

          var rightVarIndex = il.GetNextLocalVarIndex(expr.Type); // store right value in variable
          EmitStoreLocalVariable(il, rightVarIndex);

          if (!EmitMemberAssign(il, member))
            return false;

          EmitLoadLocalVariable(il, rightVarIndex);

          return true;

        case ExpressionType.Index:
          var indexExpr = (IndexExpression)left;

          var obj = indexExpr.Object;

          if (obj != null && !TryEmit(obj, paramExprs, il, ref closure, setup, flags))
            return false;

#if SUPPORTS_ARGUMENT_PROVIDER
          var indexArgExprs = (IArgumentProvider)indexExpr;
          var indexArgCount = indexArgExprs.ArgumentCount;
#else
          var indexArgExprs = indexExpr.Arguments;
          var indexArgCount = indexArgExprs.Count;
#endif
          for (var i = 0; i < indexArgCount; i++)
            if (!TryEmit(indexArgExprs.GetArgument(i), paramExprs, il, ref closure, setup, flags))
              return false;

          if (!TryEmit(right, paramExprs, il, ref closure, setup, flags))
            return false;

          if ((parent & ParentFlags.IgnoreResult) != 0)
            return TryEmitIndexAssign(indexExpr, obj?.Type, expr.Type, il);

          var varIndex = il.GetNextLocalVarIndex(expr.Type); // store value in variable to return
          il.Emit(OpCodes.Dup);
          EmitStoreLocalVariable(il, varIndex);

          if (!TryEmitIndexAssign(indexExpr, obj?.Type, expr.Type, il))
            return false;

          EmitLoadLocalVariable(il, varIndex);

          return true;

        default: // todo: @feature not yet support assignment targets
          if ((setup & CompilerFlags.ThrowOnNotSupportedExpression) != 0)
            throw new NotSupportedExpressionException(
              NotSupported.Assign_Target,
              $"Assignment target `{nodeType}` is not supported");

          return false;
      }
    }

    // todo: @fix check that it is applied only for the ValueType
    private static void EmitStoreByRefValueType(ILGenerator il, Type type)
    {
      if (type == Metadata<int>.Type || type == Metadata<uint>.Type)
        il.Emit(OpCodes.Stind_I4);
      else if (type == Metadata<byte>.Type)
        il.Emit(OpCodes.Stind_I1);
      else if (type == Metadata<short>.Type || type == Metadata<ushort>.Type)
        il.Emit(OpCodes.Stind_I2);
      else if (type == Metadata<long>.Type || type == Metadata<ulong>.Type)
        il.Emit(OpCodes.Stind_I8);
      else if (type == Metadata<float>.Type)
        il.Emit(OpCodes.Stind_R4);
      else if (type == Metadata<double>.Type)
        il.Emit(OpCodes.Stind_R8);
      else if (type == Metadata<object>.Type)
        il.Emit(OpCodes.Stind_Ref);
      else if (type == Metadata<IntPtr>.Type || type == Metadata<UIntPtr>.Type)
        il.Emit(OpCodes.Stind_I);
      else
        il.Emit(OpCodes.Stobj, type);
    }

    private static bool TryEmitIndexAssign(IndexExpression indexExpr, Type instType, Type elementType, ILGenerator il)
    {
      if (indexExpr.Indexer != null)
        return EmitMemberAssign(il, indexExpr.Indexer);

      if (indexExpr.Arguments.Count == 1) // one dimensional array
      {
        if (!elementType.IsValueType)
        {
          il.Emit(OpCodes.Stelem_Ref);

          return true;
        }

        if (elementType == Metadata<int>.Type)
          il.Emit(OpCodes.Stelem_I4);
        else if (elementType == Metadata<long>.Type)
          il.Emit(OpCodes.Stelem_I8);
        else if (elementType == Metadata<short>.Type)
          il.Emit(OpCodes.Stelem_I2);
        else if (elementType == Metadata<sbyte>.Type)
          il.Emit(OpCodes.Stelem_I1);
        else if (elementType == Metadata<float>.Type)
          il.Emit(OpCodes.Stelem_R4);
        else if (elementType == Metadata<double>.Type)
          il.Emit(OpCodes.Stelem_R8);
        else if (elementType == Metadata<IntPtr>.Type)
          il.Emit(OpCodes.Stelem_I);
        else if (elementType == Metadata<UIntPtr>.Type)
          il.Emit(OpCodes.Stelem_I);
        else
          il.Emit(OpCodes.Stelem, elementType);

        return true;
      }

      var setter = instType?.FindMethod("Set");

      return setter != null && EmitMethodCallOrVirtualCall(il, setter); // multi dimensional array
    }

#if LIGHT_EXPRESSION
            private static bool TryEmitMethodCall(Expression expr, IParameterProvider paramExprs, ILGenerator il, ref ClosureInfo closure, 
                CompilerFlags setup, ParentFlags parent)
#else
    private static bool TryEmitMethodCall(Expression expr, IReadOnlyList<ParameterExpression> paramExprs,
      ILGenerator il, ref ClosureInfo closure,
      CompilerFlags setup, ParentFlags parent)
#endif
    {
      var flags = (parent & ~ParentFlags.IgnoreResult) | ParentFlags.Call;
      var callExpr = (MethodCallExpression)expr;
      var objExpr = callExpr.Object;
      var method = callExpr.Method;
      var methodParams = method.GetParameters();

      var objIsValueType = false;

      if (objExpr != null)
      {
        if (!TryEmit(objExpr, paramExprs, il, ref closure, setup, flags | ParentFlags.InstanceAccess))
          return false;

        objIsValueType = objExpr.Type.IsValueType;

        if (objIsValueType && objExpr.NodeType != ExpressionType.Parameter && !closure.LastEmitIsAddress)
          EmitStoreAndLoadLocalVariableAddress(il, objExpr.Type);
      }

      if (methodParams.Length > 0)
      {
        flags = flags & ~ParentFlags.MemberAccess & ~ParentFlags.InstanceAccess;
#if SUPPORTS_ARGUMENT_PROVIDER
        var callArgs = (IArgumentProvider)callExpr;
        for (var i = 0; i < methodParams.Length; i++)
          if (!TryEmit(callArgs.GetArgument(i), paramExprs, il, ref closure, setup, flags, methodParams[i].ParameterType.IsByRef ? i : -1))
            return false;
#else
        var callArgs = callExpr.Arguments;

        for (var i = 0; i < methodParams.Length; i++)
          if (!TryEmit(
                callArgs[i],
                paramExprs,
                il,
                ref closure,
                setup,
                flags,
                methodParams[i].ParameterType.IsByRef ? i : -1))
            return false;
#endif
      }

      if (!objIsValueType)
      {
        EmitMethodCallOrVirtualCall(il, method);
      }
      else if (!method.IsVirtual || objExpr is ParameterExpression p && p.IsByRef)
      {
        EmitMethodCall(il, method);
      }
      else if (method.DeclaringType == objExpr.Type)
      {
        EmitMethodCall(il, method);
      }
      else
      {
        il.Emit(OpCodes.Constrained, objExpr.Type);
        il.Emit(OpCodes.Callvirt, method);
      }

      if (parent.IgnoresResult() && method.ReturnType != Metadata.Void)
        il.Emit(OpCodes.Pop);

      closure.LastEmitIsAddress = false;

      return true;
    }

#if LIGHT_EXPRESSION
            private static bool TryEmitMemberAccess(MemberExpression expr, IParameterProvider paramExprs, ILGenerator il, ref ClosureInfo closure, 
                CompilerFlags setup, ParentFlags parent, int byRefIndex = -1)
#else
    private static bool TryEmitMemberAccess(MemberExpression expr, IReadOnlyList<ParameterExpression> paramExprs,
      ILGenerator il, ref ClosureInfo closure,
      CompilerFlags setup, ParentFlags parent, int byRefIndex = -1)
#endif
    {
      if (expr.Member is PropertyInfo prop)
      {
        var instanceExpr = expr.Expression;

        if (instanceExpr != null)
        {
          var p = (parent | ParentFlags.Call | ParentFlags.MemberAccess | ParentFlags.InstanceAccess)
                  & ~ParentFlags.IgnoreResult & ~ParentFlags.DupMemberOwner;

          if (!TryEmit(instanceExpr, paramExprs, il, ref closure, setup, p))
            return false;

          if ((parent & ParentFlags.DupMemberOwner) != 0)
            il.Emit(OpCodes.Dup);

          // Value type special treatment to load address of value instance in order to access a field or call a method.
          // Parameter should be excluded because it already loads an address via `LDARGA`, and you don't need to.
          // And for field access no need to load address, cause the field stored on stack nearby
          if (!closure.LastEmitIsAddress &&
              instanceExpr.NodeType != ExpressionType.Parameter && instanceExpr.Type.IsValueType)
            EmitStoreAndLoadLocalVariableAddress(il, instanceExpr.Type);
        }

        closure.LastEmitIsAddress = false;
        EmitMethodCallOrVirtualCall(il, prop.GetMethod);

        return true;
      }

      if (expr.Member is FieldInfo field)
      {
        var instanceExpr = expr.Expression;

        if (instanceExpr != null)
        {
          var p = (parent | ParentFlags.MemberAccess | ParentFlags.InstanceAccess)
                  & ~ParentFlags.IgnoreResult & ~ParentFlags.DupMemberOwner;

          if (!TryEmit(instanceExpr, paramExprs, il, ref closure, setup, p))
            return false;

          if ((parent & ParentFlags.DupMemberOwner) != 0)
            il.Emit(OpCodes.Dup);

          var isByAddress = false;

          if (field.FieldType.IsValueType)
          {
            if ((parent & ParentFlags.InstanceAccess) != 0 &&
                (parent & ParentFlags.IndexAccess) == 0) // #302 - if the field is used as an index
              isByAddress = true;
            // #248 indicates that expression is argument passed by ref
            // todo: Maybe introduce ParentFlags.Argument
            else if ((parent & ParentFlags.Call) != 0 && byRefIndex != -1)
              isByAddress = true;
          }

          closure.LastEmitIsAddress = isByAddress;
          il.Emit(isByAddress ? OpCodes.Ldflda : OpCodes.Ldfld, field);
        }
        else if (field.IsLiteral)
        {
          var fieldValue = field.GetValue(null);

          if (fieldValue != null)
            return TryEmitConstantOfNotNullValue(false, field.FieldType, fieldValue, il, ref closure);

          il.Emit(OpCodes.Ldnull);
        }
        else
        {
          il.Emit(OpCodes.Ldsfld, field);
        }

        return true;
      }

      return false;
    }

    // ReSharper disable once FunctionComplexityOverflow
#if LIGHT_EXPRESSION
            private static bool TryEmitNestedLambda(LambdaExpression lambdaExpr, IParameterProvider outerParamExprs, ILGenerator il, ref ClosureInfo closure)
            {
                var outerParamExprCount = outerParamExprs.ParameterCount;
#else
    private static bool TryEmitNestedLambda(LambdaExpression lambdaExpr,
      IReadOnlyList<ParameterExpression> outerParamExprs, ILGenerator il, ref ClosureInfo closure)
    {
      var outerParamExprCount = outerParamExprs.Count;
#endif
      // First, find in closed compiled lambdas the one corresponding to the current lambda expression.
      // Situation with not found lambda is not possible/exceptional,
      // it means that we somehow skipped the lambda expression while collecting closure info.
      var outerNestedLambdas = closure.NestedLambdas;
      var outerNestedLambdaIndex = outerNestedLambdas.Length - 1;

      while (outerNestedLambdaIndex != -1 &&
             !ReferenceEquals(outerNestedLambdas[outerNestedLambdaIndex].LambdaExpression, lambdaExpr))
        --outerNestedLambdaIndex;

      if (outerNestedLambdaIndex == -1)
        return false;

      var nestedLambdaInfo = closure.NestedLambdas[outerNestedLambdaIndex];
      var nestedLambda = nestedLambdaInfo.Lambda;
      var nestedLambdaInClosureIndex = outerNestedLambdaIndex + closure.Constants.Count;

      EmitLoadLocalVariable(il, nestedLambdaInfo.LambdaVarIndex);

      // If lambda does not use any outer parameters to be set in closure, then we're done
      ref var nestedClosureInfo = ref nestedLambdaInfo.ClosureInfo;
      var nestedNonPassedParams = nestedClosureInfo.NonPassedParameters;

      if (nestedNonPassedParams.Length == 0)
        return true;

      //-------------------------------------------------------------------
      // For the lambda with non-passed parameters (or variables) in closure
      // we have loaded `NestedLambdaWithConstantsAndNestedLambdas` pair.

      var containsConstants = nestedClosureInfo.ContainsConstantsOrNestedLambdas();

      if (containsConstants)
      {
        il.Emit(OpCodes.Ldfld, NestedLambdaWithConstantsAndNestedLambdas.NestedLambdaField);
        EmitLoadLocalVariable(il, nestedLambdaInfo.LambdaVarIndex); // load the variable for the second time
        il.Emit(OpCodes.Ldfld, NestedLambdaWithConstantsAndNestedLambdas.ConstantsAndNestedLambdasField);
      }

      // - create `NonPassedParameters` array
      EmitLoadConstantInt(il, nestedNonPassedParams.Length); // load the length of array
      il.Emit(OpCodes.Newarr, Metadata<object>.Type);

      // - populate the `NonPassedParameters` array
      var outerNonPassedParams = closure.NonPassedParameters;

      for (var nestedParamIndex = 0; nestedParamIndex < nestedNonPassedParams.Length; ++nestedParamIndex)
      {
        var nestedParam = nestedNonPassedParams[nestedParamIndex];

        // Duplicate nested array on stack to store the item, and load index to where to store
        il.Emit(OpCodes.Dup);
        EmitLoadConstantInt(il, nestedParamIndex);

        var outerParamIndex = outerParamExprCount - 1;

        while (outerParamIndex != -1 && !ReferenceEquals(outerParamExprs.GetParameter(outerParamIndex), nestedParam))
          --outerParamIndex;

        if (outerParamIndex != -1) // load parameter from input outer params
        {
          // Add `+1` to index because the `0` index is for the closure argument
          if (outerParamIndex == 0)
            il.Emit(OpCodes.Ldarg_1);
          else if (outerParamIndex == 1)
            il.Emit(OpCodes.Ldarg_2);
          else if (outerParamIndex == 2)
            il.Emit(OpCodes.Ldarg_3);
          else
            il.Emit(OpCodes.Ldarg_S, (byte)(1 + outerParamIndex));

          if (nestedParam.Type.IsValueType)
            il.Emit(OpCodes.Box, nestedParam.Type);
        }
        else // load parameter from outer closure or from the local variables
        {
          if (outerNonPassedParams.Length == 0)
            return false; // impossible, better to throw?

          var outerLocalVarIndex = closure.GetDefinedLocalVarOrDefault(nestedParam);

          if (outerLocalVarIndex != -1) // it's a local variable
          {
            EmitLoadLocalVariable(il, outerLocalVarIndex);

            if (nestedParam.Type
                .IsValueType) // don't forget to box the value type when we store it into object array, (fixes #255)
              il.Emit(OpCodes.Box, nestedParam.Type);
          }
          else // it's a parameter from the outer closure
          {
            var outerNonPassedParamIndex = outerNonPassedParams.Length - 1;

            while (outerNonPassedParamIndex != -1 && !ReferenceEquals(
                     outerNonPassedParams[outerNonPassedParamIndex],
                     nestedParam))
              --outerNonPassedParamIndex;

            if (outerNonPassedParamIndex == -1)
              return false; // impossible

            // Load the parameter from outer closure `Items` array
            il.Emit(OpCodes.Ldarg_0); // closure is always a first argument
            il.Emit(OpCodes.Ldfld, ArrayClosureWithNonPassedParamsField);
            EmitLoadConstantInt(il, outerNonPassedParamIndex);
            il.Emit(OpCodes.Ldelem_Ref);
          }
        }

        // Store the item into nested lambda array
        il.Emit(OpCodes.Stelem_Ref);
      }

      // - create `ArrayClosureWithNonPassedParams` out of the both above
      if (containsConstants)
        il.Emit(OpCodes.Newobj, ArrayClosureWithNonPassedParamsConstructor);
      else
        il.Emit(OpCodes.Newobj, ArrayClosureWithNonPassedParamsConstructorWithoutConstants);

      // - call `Curry` method with nested lambda and array closure to produce a closed lambda with the expected signature
      var lambdaTypeArgs = nestedLambda.GetType().GetTypeInfo().GenericTypeArguments;

      var nestedLambdaExpr = nestedLambdaInfo.LambdaExpression;

      var closureMethod = nestedLambdaExpr.ReturnType == Metadata.Void
        ? CurryClosureActions.Methods[lambdaTypeArgs.Length - 1].MakeGenericMethod(lambdaTypeArgs)
        : CurryClosureFuncs.Methods[lambdaTypeArgs.Length - 2].MakeGenericMethod(lambdaTypeArgs);

      EmitMethodCall(il, closureMethod);

      // converting to the original possibly custom delegate type, see #308
      if (closureMethod.ReturnType != nestedLambdaExpr.Type)
      {
        il.Emit(OpCodes.Ldftn, closureMethod.ReturnType.FindDelegateInvokeMethod());
        il.Emit(OpCodes.Newobj, nestedLambdaExpr.Type.GetConstructors()[0]);
      }

      return true;
    }

#if LIGHT_EXPRESSION
            private static bool TryEmitInvoke(InvocationExpression expr, IParameterProvider paramExprs, ILGenerator il, ref ClosureInfo closure, 
                CompilerFlags setup, ParentFlags parent)
            {
                var paramCount = paramExprs.ParameterCount;
#else
    private static bool TryEmitInvoke(InvocationExpression expr, IReadOnlyList<ParameterExpression> paramExprs,
      ILGenerator il, ref ClosureInfo closure,
      CompilerFlags setup, ParentFlags parent)
    {
      var paramCount = paramExprs.Count;
#endif
#if SUPPORTS_ARGUMENT_PROVIDER
      var argExprs = (IArgumentProvider)expr;
      var argCount = argExprs.ArgumentCount;
#else
      var argExprs = expr.Arguments;
      var argCount = argExprs.Count;
#endif
      var lambda = expr.Expression;

      if ((setup & CompilerFlags.NoInvocationLambdaInlining) == 0 && lambda is LambdaExpression la)
      {
        parent |= ParentFlags.InlinedLambdaInvoke;

        if (argCount == 0)
          return TryEmit(la.Body, paramExprs, il, ref closure, setup, parent);
#if LIGHT_EXPRESSION
                    var pars = (IParameterProvider)la;
#else
        var pars = la.Parameters;
#endif
        var exprs = new Expression[argCount + 1];
        List<ParameterExpression> vars = null;

        for (var i = 0; i < argCount; i++)
        {
          var p = pars.GetParameter(i);
          // Check for the case of reusing the parameters in the different lambdas, 
          // see test `Hmm_I_can_use_the_same_parameter_for_outer_and_nested_lambda`
          var j = paramCount - 1;
          while (j != -1 && !ReferenceEquals(p, paramExprs.GetParameter(j))) --j;

          if (j != -1 || closure.IsLocalVar(p))
          {
            // if we found the same parameter let's move the non-found (new) parameters into the separate `vars` list
            if (vars == null)
            {
              vars = new List<ParameterExpression>();

              for (var k = 0; k < i; k++)
                vars.Add(pars.GetParameter(k));
            }
          }
          else if (vars != null) // but vars maybe empty in the result - it is fine
          {
            vars.Add(p);
          }

          exprs[i] = Expression.Assign(p, argExprs.GetArgument(i));
        }

        exprs[argCount] = la.Body;

        if (!TryEmit(
              Expression.Block(vars ?? pars.ToReadOnlyList(), exprs),
              paramExprs,
              il,
              ref closure,
              setup,
              parent))
          return false;

        if ((parent & ParentFlags.IgnoreResult) == 0 && la.Body.Type != Metadata.Void)
        {
          // find if the variable with the result is exist in the label infos
          var li = closure.GetLabelOrInvokeIndex(expr);

          if (li != -1)
          {
            ref var labelInfo = ref closure.Labels.Items[li];
            var returnVariableIndexPlusOne = labelInfo.ReturnVariableIndexPlusOneAndIsDefined >> 1;

            if (returnVariableIndexPlusOne != 0)
            {
              il.MarkLabel(labelInfo.ReturnLabel);
              EmitLoadLocalVariable(il, returnVariableIndexPlusOne - 1);
            }
          }
        }

        return true;
      }

      if (!TryEmit(
            lambda,
            paramExprs,
            il,
            ref closure,
            setup,
            parent & ~ParentFlags
              .IgnoreResult)) // removing the IgnoreResult temporary because we need "full" lambda emit and we will re-apply the IgnoreResult later at the end of the method
        return false;

      var delegateInvokeMethod = lambda.Type.FindDelegateInvokeMethod();

      if (argCount > 0)
      {
        var useResult = parent & ~ParentFlags.IgnoreResult & ~ParentFlags.InstanceAccess;
        var args = delegateInvokeMethod.GetParameters();

        for (var i = 0; i < args.Length; ++i)
        {
          var argExpr = argExprs.GetArgument(i);

          if (!TryEmit(argExpr, paramExprs, il, ref closure, setup, useResult, args[i].ParameterType.IsByRef ? i : -1))
            return false;
        }
      }

      EmitMethodCall(il, delegateInvokeMethod);

      if ((parent & ParentFlags.IgnoreResult) != 0 && delegateInvokeMethod.ReturnType != Metadata.Void)
        il.Emit(OpCodes.Pop);

      return true;
    }

#if LIGHT_EXPRESSION
            private static bool TryEmitSwitch(SwitchExpression expr, IParameterProvider paramExprs, ILGenerator il, ref ClosureInfo closure, 
                CompilerFlags setup, ParentFlags parent)
#else
    private static bool TryEmitSwitch(SwitchExpression expr, IReadOnlyList<ParameterExpression> paramExprs,
      ILGenerator il, ref ClosureInfo closure,
      CompilerFlags setup, ParentFlags parent)
#endif
    {
      // todo: @perf
      //- use switch statement for int comparison (if int difference is less or equal 3 -> use IL switch)
      //- TryEmitComparison should not emit "CEQ" so we could use Beq_S instead of Brtrue_S (not always possible (nullable))
      //- if switch SwitchValue is a nullable parameter, we should call getValue only once and store the result.
      //- use comparison methods (when defined)

      var endLabel = il.DefineLabel();
      var cases = expr.Cases;
      var labels = new Label[cases.Count];
      var dontIgnoreTestResult = parent & ~ParentFlags.IgnoreResult;

      for (var caseIndex = 0; caseIndex < cases.Count; ++caseIndex)
      {
        var cs = cases[caseIndex];
        labels[caseIndex] = il.DefineLabel();

        foreach (var caseTestValue in cs.TestValues)
        {
          if (!TryEmitComparison(
                expr.SwitchValue,
                caseTestValue,
                ExpressionType.Equal,
                Metadata<bool>.Type,
                paramExprs,
                il,
                ref closure,
                setup,
                dontIgnoreTestResult))
            return false;

          il.Emit(OpCodes.Brtrue, labels[caseIndex]);
        }
      }

      if (expr.DefaultBody != null)
      {
        if (!TryEmit(expr.DefaultBody, paramExprs, il, ref closure, setup, parent))
          return false;

        il.Emit(OpCodes.Br, endLabel);
      }

      for (var caseIndex = 0; caseIndex < cases.Count; ++caseIndex)
      {
        il.MarkLabel(labels[caseIndex]);
        var cs = cases[caseIndex];

        if (!TryEmit(cs.Body, paramExprs, il, ref closure, setup, parent))
          return false;

        if (caseIndex != cases.Count - 1)
          il.Emit(OpCodes.Br, endLabel);
      }

      il.MarkLabel(endLabel);

      return true;
    }

    private static bool TryEmitComparison(Expression exprLeft, Expression exprRight, ExpressionType expressionType,
      Type exprType,
#if LIGHT_EXPRESSION
                IParameterProvider paramExprs,
#else
      IReadOnlyList<ParameterExpression> paramExprs,
#endif
      ILGenerator il, ref ClosureInfo closure, CompilerFlags setup, ParentFlags parent)
    {
      var leftOpType = exprLeft.Type;
      var leftIsNullable = leftOpType.IsNullable();
      var rightOpType = exprRight.Type;

      if (exprRight is ConstantExpression r && r.Value == null)
        if (exprRight.Type == Metadata<object>.Type)
          rightOpType = leftOpType;

      int lVarIndex = -1, rVarIndex = -1;
      var operandParent = parent & ~ParentFlags.IgnoreResult & ~ParentFlags.InstanceAccess;

      if (!TryEmit(exprLeft, paramExprs, il, ref closure, setup, operandParent))
        return false;

      if (leftIsNullable)
      {
        lVarIndex = EmitStoreAndLoadLocalVariableAddress(il, leftOpType);
        EmitMethodCall(il, leftOpType.FindNullableGetValueOrDefaultMethod());
        leftOpType = leftOpType.GetUnderlyingTypeCache();
      }

      if (!TryEmit(exprRight, paramExprs, il, ref closure, setup, operandParent))
        return false;

      if (leftOpType != rightOpType)
        if (leftOpType.IsClass && rightOpType.IsClass &&
            (leftOpType == Metadata<object>.Type || rightOpType == Metadata<object>.Type))
        {
          if (expressionType == ExpressionType.Equal)
          {
            il.Emit(OpCodes.Ceq);
          }
          else if (expressionType == ExpressionType.NotEqual)
          {
            il.Emit(OpCodes.Ceq);

            il.Emit(
              OpCodes.Ldc_I4_0); // todo: @perf Currently it produces the same code as a System Compile but I wonder if we can use OpCodes.Not

            il.Emit(OpCodes.Ceq);
          }
          else
          {
            return false;
          }

          return il.EmitPopIfIgnoreResult(parent);
        }

      if (rightOpType.IsNullable())
      {
        rVarIndex = EmitStoreAndLoadLocalVariableAddress(il, rightOpType);
        EmitMethodCall(il, rightOpType.FindNullableGetValueOrDefaultMethod());
        // ReSharper disable once AssignNullToNotNullAttribute
        rightOpType = rightOpType.GetUnderlyingTypeCache();
      }

      if (!leftOpType.IsPrimitive && !leftOpType.IsEnum)
      {
        var methodName
          = expressionType == ExpressionType.Equal ? "op_Equality"
          : expressionType == ExpressionType.NotEqual ? "op_Inequality"
          : expressionType == ExpressionType.GreaterThan ? "op_GreaterThan"
          : expressionType == ExpressionType.GreaterThanOrEqual ? "op_GreaterThanOrEqual"
          : expressionType == ExpressionType.LessThan ? "op_LessThan"
          : expressionType == ExpressionType.LessThanOrEqual ? "op_LessThanOrEqual"
          : null;

        if (methodName == null)
          return false;

        // todo: @bug? for now handling only parameters of the same type
        var methods = leftOpType.GetMethods();

        for (var i = 0; i < methods.Length; i++)
        {
          var m = methods[i];

          if (m.IsSpecialName && m.IsStatic && m.Name == methodName)
          {
            var ps = m.GetParameters();

            if (ps.Length == 2 && ps[0].ParameterType == leftOpType && ps[1].ParameterType == leftOpType)
            {
              EmitMethodCall(il, m);

              return true;
            }
          }
        }

        if (expressionType != ExpressionType.Equal && expressionType != ExpressionType.NotEqual)
          return false; // todo: @unclear what is the alternative?

        EmitMethodCall(il, _objectEqualsMethod);

        if (expressionType == ExpressionType.NotEqual) // invert result for not equal
        {
          il.Emit(OpCodes.Ldc_I4_0);
          il.Emit(OpCodes.Ceq);
        }

        if (leftIsNullable)
          goto nullCheck;

        return il.EmitPopIfIgnoreResult(parent);
      }

      // handle primitives comparison
      switch (expressionType)
      {
        case ExpressionType.Equal:
          il.Emit(OpCodes.Ceq);

          break;

        case ExpressionType.NotEqual:
          il.Emit(OpCodes.Ceq);
          il.Emit(OpCodes.Ldc_I4_0);
          il.Emit(OpCodes.Ceq);

          break;

        case ExpressionType.LessThan:
          il.Emit(OpCodes.Clt);

          break;

        case ExpressionType.GreaterThan:
          il.Emit(OpCodes.Cgt);

          break;

        case ExpressionType.GreaterThanOrEqual:
          // simplifying by using the LessThen (Clt) and comparing with negative outcome (Ceq 0)
          if (leftOpType.IsUnsigned() && rightOpType.IsUnsigned())
            il.Emit(OpCodes.Clt_Un);
          else
            il.Emit(OpCodes.Clt);

          il.Emit(OpCodes.Ldc_I4_0);
          il.Emit(OpCodes.Ceq);

          break;

        case ExpressionType.LessThanOrEqual:
          // simplifying by using the GreaterThen (Cgt) and comparing with negative outcome (Ceq 0)
          if (leftOpType.IsUnsigned() && rightOpType.IsUnsigned())
            il.Emit(OpCodes.Cgt_Un);
          else
            il.Emit(OpCodes.Cgt);

          il.Emit(OpCodes.Ldc_I4_0);
          il.Emit(OpCodes.Ceq);

          break;

        default:
          return false;
      }

      nullCheck:

      if (leftIsNullable)
      {
        var leftNullableHasValueGetterMethod = exprLeft.Type.FindNullableHasValueGetterMethod();

        EmitLoadLocalVariableAddress(il, lVarIndex);
        EmitMethodCall(il, leftNullableHasValueGetterMethod);

        var isLiftedToNull = exprType == Metadata<bool?>.Type;
        var leftHasValueVar = -1;

        if (isLiftedToNull)
          EmitStoreAndLoadLocalVariable(il, leftHasValueVar = il.GetNextLocalVarIndex(Metadata<bool>.Type));

        // ReSharper disable once AssignNullToNotNullAttribute
        EmitLoadLocalVariableAddress(il, rVarIndex);
        EmitMethodCall(il, leftNullableHasValueGetterMethod);

        var rightHasValueVar = -1;

        if (isLiftedToNull)
          EmitStoreAndLoadLocalVariable(il, rightHasValueVar = il.GetNextLocalVarIndex(Metadata<bool>.Type));

        switch (expressionType)
        {
          case ExpressionType.Equal:
            il.Emit(OpCodes.Ceq); // compare both HasValue calls
            il.Emit(OpCodes.And); // both results need to be true

            break;

          case ExpressionType.NotEqual:
            il.Emit(OpCodes.Ceq);
            il.Emit(OpCodes.Ldc_I4_0);
            il.Emit(OpCodes.Ceq);
            il.Emit(OpCodes.Or);

            break;

          case ExpressionType.LessThan:
          case ExpressionType.GreaterThan:
          case ExpressionType.LessThanOrEqual:
          case ExpressionType.GreaterThanOrEqual:
            il.Emit(OpCodes.Ceq);
            il.Emit(OpCodes.Ldc_I4_1);
            il.Emit(OpCodes.Ceq);
            il.Emit(OpCodes.And);

            break;

          default:
            return false;
        }

        if (isLiftedToNull)
        {
          var resultLabel = il.DefineLabel();
          var isNullLabel = il.DefineLabel();
          EmitLoadLocalVariable(il, leftHasValueVar);
          il.Emit(OpCodes.Brfalse, isNullLabel);
          EmitLoadLocalVariable(il, rightHasValueVar);
          il.Emit(OpCodes.Brtrue, resultLabel);
          il.MarkLabel(isNullLabel);
          il.Emit(OpCodes.Pop);
          il.Emit(OpCodes.Ldnull);
          il.MarkLabel(resultLabel);
        }
      }

      return il.EmitPopIfIgnoreResult(parent);
    }

#if LIGHT_EXPRESSION
            private static bool TryEmitArithmetic(BinaryExpression expr, ExpressionType exprNodeType, IParameterProvider paramExprs,
#else
    private static bool TryEmitArithmetic(BinaryExpression expr, ExpressionType exprNodeType,
      IReadOnlyList<ParameterExpression> paramExprs,
#endif
      ILGenerator il, ref ClosureInfo closure, CompilerFlags setup, ParentFlags parent)
    {
      var flags = (parent & ~ParentFlags.IgnoreResult & ~ParentFlags.InstanceCall) | ParentFlags.Arithmetic;

      var leftNoValueLabel = default(Label);
      var leftExpr = expr.Left;
      var lefType = leftExpr.Type;
      var leftIsNullable = lefType.IsNullable();

      if (leftIsNullable)
      {
        leftNoValueLabel = il.DefineLabel();

        if (!TryEmit(leftExpr, paramExprs, il, ref closure, setup, flags | ParentFlags.InstanceCall))
          return false;

        if (!closure.LastEmitIsAddress)
          EmitStoreAndLoadLocalVariableAddress(il, lefType);

        il.Emit(OpCodes.Dup);
        EmitMethodCall(il, lefType.FindNullableHasValueGetterMethod());
        il.Emit(OpCodes.Brfalse, leftNoValueLabel);
        EmitMethodCall(il, lefType.FindNullableGetValueOrDefaultMethod());
      }
      else if (!TryEmit(leftExpr, paramExprs, il, ref closure, setup, flags))
      {
        return false;
      }

      var rightNoValueLabel = default(Label);
      var rightExpr = expr.Right;
      var rightType = rightExpr.Type;
      var rightIsNullable = rightType.IsNullable();

      if (rightIsNullable)
      {
        rightNoValueLabel = il.DefineLabel();

        if (!TryEmit(rightExpr, paramExprs, il, ref closure, setup, flags | ParentFlags.InstanceCall))
          return false;

        if (!closure.LastEmitIsAddress)
          EmitStoreAndLoadLocalVariableAddress(il, rightType);

        il.Emit(OpCodes.Dup);
        EmitMethodCall(il, rightType.FindNullableHasValueGetterMethod());
        il.Emit(OpCodes.Brfalse, rightNoValueLabel);
        EmitMethodCall(il, rightType.FindNullableGetValueOrDefaultMethod());
      }
      else if (!TryEmit(rightExpr, paramExprs, il, ref closure, setup, flags))
      {
        return false;
      }

      var exprType = expr.Type;

      if (!TryEmitArithmeticOperation(expr, exprNodeType, exprType, il))
        return false;

      if (leftIsNullable || rightIsNullable) // todo: @clarify that the code emitted is correct
      {
        var valueLabel = il.DefineLabel();
        il.Emit(OpCodes.Br, valueLabel);

        if (rightIsNullable)
          il.MarkLabel(rightNoValueLabel);

        il.Emit(OpCodes.Pop);

        if (leftIsNullable)
          il.MarkLabel(leftNoValueLabel);

        il.Emit(OpCodes.Pop);

        if (exprType.IsNullable())
        {
          var endL = il.DefineLabel();
          EmitLoadLocalVariable(il, InitValueTypeVariable(il, exprType));
          il.Emit(OpCodes.Br_S, endL);
          il.MarkLabel(valueLabel);
          il.Emit(OpCodes.Newobj, exprType.GetConstructors()[0]);
          il.MarkLabel(endL);
        }
        else
        {
          il.Emit(OpCodes.Ldc_I4_0);
          il.MarkLabel(valueLabel);
        }
      }

      return true;
    }

    private static bool TryEmitArithmeticOperation(BinaryExpression expr, ExpressionType exprNodeType, Type exprType,
      ILGenerator il)
    {
      if (!exprType.IsPrimitive)
      {
        if (exprType.IsNullable())
          exprType = exprType.GetUnderlyingTypeCache();

        if (!exprType.IsPrimitive)
        {
          MethodInfo method = null;

          if (exprType == Metadata<string>.Type)
          {
            var paraType = Metadata<string>.Type;

            if (expr.Left.Type != expr.Right.Type || expr.Left.Type != Metadata<string>.Type)
              paraType = Metadata<object>.Type;

            var methods = Metadata<string>.Type.GetMethods();

            for (var i = 0; i < methods.Length; i++)
            {
              var m = methods[i];

              if (m.IsStatic && m.Name == "Concat" &&
                  m.GetParameters().Length == 2 && m.GetParameters()[0].ParameterType == paraType)
              {
                method = m;

                break;
              }
            }
          }
          else
          {
            var methodName = exprNodeType.GetArithmeticBinaryOperatorMethodName();

            if (methodName != null)
            {
              var methods = exprType.GetMethods();

              for (var i = 0; method == null && i < methods.Length; i++)
              {
                var m = methods[i];

                if (m.IsSpecialName && m.IsStatic && m.Name == methodName)
                  method = m;
              }
            }
          }

          return method != null && EmitMethodCallOrVirtualCall(il, method);
        }
      }

      switch (exprNodeType)
      {
        case ExpressionType.Add:
        case ExpressionType.AddAssign:
          il.Emit(OpCodes.Add);

          return true;

        case ExpressionType.AddChecked:
        case ExpressionType.AddAssignChecked:
          il.Emit(exprType.IsUnsigned() ? OpCodes.Add_Ovf_Un : OpCodes.Add_Ovf);

          return true;

        case ExpressionType.Subtract:
        case ExpressionType.SubtractAssign:
          il.Emit(OpCodes.Sub);

          return true;

        case ExpressionType.SubtractChecked:
        case ExpressionType.SubtractAssignChecked:
          il.Emit(exprType.IsUnsigned() ? OpCodes.Sub_Ovf_Un : OpCodes.Sub_Ovf);

          return true;

        case ExpressionType.Multiply:
        case ExpressionType.MultiplyAssign:
          il.Emit(OpCodes.Mul);

          return true;

        case ExpressionType.MultiplyChecked:
        case ExpressionType.MultiplyAssignChecked:
          il.Emit(exprType.IsUnsigned() ? OpCodes.Mul_Ovf_Un : OpCodes.Mul_Ovf);

          return true;

        case ExpressionType.Divide:
        case ExpressionType.DivideAssign:
          il.Emit(OpCodes.Div);

          return true;

        case ExpressionType.Modulo:
        case ExpressionType.ModuloAssign:
          il.Emit(OpCodes.Rem);

          return true;

        case ExpressionType.And:
        case ExpressionType.AndAssign:
          il.Emit(OpCodes.And);

          return true;

        case ExpressionType.Or:
        case ExpressionType.OrAssign:
          il.Emit(OpCodes.Or);

          return true;

        case ExpressionType.ExclusiveOr:
        case ExpressionType.ExclusiveOrAssign:
          il.Emit(OpCodes.Xor);

          return true;

        case ExpressionType.LeftShift:
        case ExpressionType.LeftShiftAssign:
          il.Emit(OpCodes.Shl);

          return true;

        case ExpressionType.RightShift:
        case ExpressionType.RightShiftAssign:
          il.Emit(OpCodes.Shr);

          return true;

        case ExpressionType.Power:
          EmitMethodCall(il, Metadata.Math.FindMethod("Pow"));

          return true;
      }

      return false;
    }

#if LIGHT_EXPRESSION
            private static bool TryEmitLogicalOperator(BinaryExpression expr, IParameterProvider paramExprs, ILGenerator il, ref ClosureInfo closure, 
                CompilerFlags setup, ParentFlags parent)
#else
    private static bool TryEmitLogicalOperator(BinaryExpression expr, IReadOnlyList<ParameterExpression> paramExprs,
      ILGenerator il, ref ClosureInfo closure,
      CompilerFlags setup, ParentFlags parent)
#endif
    {
      if (!TryEmit(expr.Left, paramExprs, il, ref closure, setup, parent))
        return false;

      var labelSkipRight = il.DefineLabel();
      il.Emit(expr.NodeType == ExpressionType.AndAlso ? OpCodes.Brfalse : OpCodes.Brtrue, labelSkipRight);

      if (!TryEmit(expr.Right, paramExprs, il, ref closure, setup, parent))
        return false;

      var labelDone = il.DefineLabel();
      il.Emit(OpCodes.Br, labelDone);

      il.MarkLabel(labelSkipRight); // label the second branch
      il.Emit(expr.NodeType == ExpressionType.AndAlso ? OpCodes.Ldc_I4_0 : OpCodes.Ldc_I4_1);
      il.MarkLabel(labelDone);

      return true;
    }

#if LIGHT_EXPRESSION
            private static bool TryEmitConditional(ConditionalExpression expr, IParameterProvider paramExprs, ILGenerator il, ref ClosureInfo closure, 
                CompilerFlags setup, ParentFlags parent)
#else
    private static bool TryEmitConditional(ConditionalExpression expr, IReadOnlyList<ParameterExpression> paramExprs,
      ILGenerator il, ref ClosureInfo closure,
      CompilerFlags setup, ParentFlags parent)
#endif
    {
      var testExpr = TryReduceCondition(expr.Test);

      // Detect a simplistic case when we can use `Brtrue` or `Brfalse`.
      // We are checking the negative result to go into the `IfFalse` branch,
      // because for `IfTrue` we don't need to jump and just need to proceed emitting the `IfTrue` expression
      //
      // The cases:
      // `x == true`  => `Brfalse`
      // `x != true`  => `Brtrue`
      // `x == false` => `Brtrue`
      // `x != false` => `Brfalse`
      // `x == null`  => `Brtrue`
      // `x != null`  => `Brfalse`
      // `x == 0`     => `Brtrue`
      // `x != 0`     => `Brfalse`

      var useBrFalseOrTrue = -1; // 0 - is comparison with Zero (0, null, false), 1 - is comparison with (true)
      Type nullOfValueType = null;

      if (testExpr is BinaryExpression b)
        if (b.NodeType == ExpressionType.Equal || b.NodeType == ExpressionType.NotEqual)
        {
          object constVal = null;

          if (b.Right is ConstantExpression rc)
          {
            constVal = rc.Value;

            if (constVal == null)
            {
              useBrFalseOrTrue = 0;

              // The null comparison for the nullable is actually a `nullable.HasValue` check,
              // which implies member access on nullable struct - therefore loading it by address
              if (b.Left.Type.IsNullable())
              {
                nullOfValueType = b.Left.Type;
                parent |= ParentFlags.MemberAccess;
              }
            }
            else if (constVal is bool rcb)
            {
              useBrFalseOrTrue = rcb ? 1 : 0;
            }
            else if (constVal is int n && n == 0 || constVal is byte bn && bn == 0)
            {
              useBrFalseOrTrue = 0;
            }

            if (useBrFalseOrTrue != -1 &&
                !TryEmit(b.Left, paramExprs, il, ref closure, setup, parent & ~ParentFlags.IgnoreResult))
              return false;
          }
          else if (b.Left is ConstantExpression lc)
          {
            constVal = lc.Value;

            if (constVal == null)
            {
              useBrFalseOrTrue = 0;

              if (b.Right.Type.IsNullable())
              {
                nullOfValueType = b.Right.Type;
                parent |= ParentFlags.MemberAccess;
              }
            }
            else if (constVal is bool lcb)
            {
              useBrFalseOrTrue = lcb ? 1 : 0;
            }
            else if (constVal is int n && n == 0 || constVal is byte bn && bn == 0)
            {
              useBrFalseOrTrue = 0;
            }

            if (useBrFalseOrTrue != -1 &&
                !TryEmit(b.Right, paramExprs, il, ref closure, setup, parent & ~ParentFlags.IgnoreResult))
              return false;
          }
        }

      if (useBrFalseOrTrue == -1)
        if (!TryEmit(testExpr, paramExprs, il, ref closure, setup, parent & ~ParentFlags.IgnoreResult))
          return false;

      if (nullOfValueType != null)
      {
        if (!closure.LastEmitIsAddress)
          EmitStoreAndLoadLocalVariableAddress(il, nullOfValueType);

        EmitMethodCall(il, nullOfValueType.FindNullableHasValueGetterMethod());
      }

      var labelIfFalse = il.DefineLabel();

      if (testExpr.NodeType == ExpressionType.Equal && useBrFalseOrTrue == 0 ||
          testExpr.NodeType == ExpressionType.NotEqual && useBrFalseOrTrue == 1)
        // todo: @perf incomplete:
        // try to recognize the pattern like in #301(300) `if (b == null) { goto return_label; }` 
        // and instead of generating two branches e.g. Brtrue to else branch and Br or Ret to the end of the body,
        // let's generate a single one e.g. Brfalse to return.
        il.Emit(OpCodes.Brtrue, labelIfFalse);
      else
        il.Emit(OpCodes.Brfalse, labelIfFalse);

      if (!TryEmit(expr.IfTrue, paramExprs, il, ref closure, setup, parent))
        return false;

      var ifFalseExpr = expr.IfFalse;

      if (ifFalseExpr.NodeType == ExpressionType.Default && ifFalseExpr.Type == Metadata.Void)
      {
        il.MarkLabel(labelIfFalse);
      }
      else
      {
        var labelDone = il.DefineLabel();
        il.Emit(OpCodes.Br, labelDone);
        il.MarkLabel(labelIfFalse);

        if (!TryEmit(ifFalseExpr, paramExprs, il, ref closure, setup, parent))
          return false;

        il.MarkLabel(labelDone);
      }

      return true;
    }

    private static Expression TryReduceCondition(Expression testExpr)
    {
      // removing Not by turning Equal -> NotEqual, NotEqual -> Equal
      if (testExpr.NodeType == ExpressionType.Not)
      {
        // simplify the not `==` -> `!=`, `!=` -> `==`
        var op = TryReduceCondition(((UnaryExpression)testExpr).Operand);

        if (op.NodeType == ExpressionType.Equal) // ensures that it is a BinaryExpression
        {
          var binOp = (BinaryExpression)op;

          return Expression.NotEqual(binOp.Left, binOp.Right);
        }

        if (op.NodeType == ExpressionType.NotEqual) // ensures that it is a BinaryExpression
        {
          var binOp = (BinaryExpression)op;

          return Expression.Equal(binOp.Left, binOp.Right);
        }
      }
      else if (testExpr is BinaryExpression b)
      {
        if (b.NodeType == ExpressionType.OrElse || b.NodeType == ExpressionType.Or)
        {
          if (b.Left is ConstantExpression lc && lc.Value is bool lcb)
            return lcb ? lc : TryReduceCondition(b.Right);

          if (b.Right is ConstantExpression rc && rc.Value is bool rcb && !rcb)
            return TryReduceCondition(b.Left);
        }
        else if (b.NodeType == ExpressionType.AndAlso || b.NodeType == ExpressionType.And)
        {
          if (b.Left is ConstantExpression lc && lc.Value is bool lcb)
            return !lcb ? lc : TryReduceCondition(b.Right);

          if (b.Right is ConstantExpression rc && rc.Value is bool rcb && rcb)
            return TryReduceCondition(b.Left);
        }
      }

      return testExpr;
    }

    // get the advantage of the optimized specialized EmitCall method
    [MethodImpl((MethodImplOptions)256)]
    private static bool EmitMethodCallOrVirtualCall(ILGenerator il, MethodInfo method)
    {
#if SUPPORTS_EMITCALL
      il.EmitCall(method.IsVirtual ? OpCodes.Callvirt : OpCodes.Call, method, null);
#else
      il.Emit(method.IsVirtual ? OpCodes.Callvirt : OpCodes.Call, method);
#endif
      return true;
    }

    // get the advantage of the optimized specialized EmitCall method
    [MethodImpl((MethodImplOptions)256)]
    private static bool EmitMethodCall(ILGenerator il, MethodInfo method)
    {
#if SUPPORTS_EMITCALL
      il.EmitCall(OpCodes.Call, method, null);
#else
      il.Emit(OpCodes.Call, method);
#endif
      return true;
    }

    [MethodImpl((MethodImplOptions)256)]
    private static void EmitLoadConstantInt(ILGenerator il, int i)
    {
      switch (i)
      {
        case -1:
          il.Emit(OpCodes.Ldc_I4_M1);

          break;
        case 0:
          il.Emit(OpCodes.Ldc_I4_0);

          break;
        case 1:
          il.Emit(OpCodes.Ldc_I4_1);

          break;
        case 2:
          il.Emit(OpCodes.Ldc_I4_2);

          break;
        case 3:
          il.Emit(OpCodes.Ldc_I4_3);

          break;
        case 4:
          il.Emit(OpCodes.Ldc_I4_4);

          break;
        case 5:
          il.Emit(OpCodes.Ldc_I4_5);

          break;
        case 6:
          il.Emit(OpCodes.Ldc_I4_6);

          break;
        case 7:
          il.Emit(OpCodes.Ldc_I4_7);

          break;
        case 8:
          il.Emit(OpCodes.Ldc_I4_8);

          break;
        default:
          if (i > -129 && i < 128)
            il.Emit(OpCodes.Ldc_I4_S, (sbyte)i);
          else
            il.Emit(OpCodes.Ldc_I4, i);

          break;
      }
    }

    [MethodImpl((MethodImplOptions)256)]
    private static void EmitLoadLocalVariableAddress(ILGenerator il, int location)
    {
      if ((uint)location <= byte.MaxValue)
        il.Emit(OpCodes.Ldloca_S, (byte)location);
      else
        il.Emit(OpCodes.Ldloca, (short)location);
    }

    [MethodImpl((MethodImplOptions)256)]
    private static void EmitLoadLocalVariable(ILGenerator il, int location)
    {
      if (location == 0)
        il.Emit(OpCodes.Ldloc_0);
      else if (location == 1)
        il.Emit(OpCodes.Ldloc_1);
      else if (location == 2)
        il.Emit(OpCodes.Ldloc_2);
      else if (location == 3)
        il.Emit(OpCodes.Ldloc_3);
      else if ((uint)location <= byte.MaxValue)
        il.Emit(OpCodes.Ldloc_S, (byte)location);
      else
        il.Emit(OpCodes.Ldloc, (short)location);
    }

    [MethodImpl((MethodImplOptions)256)]
    private static void EmitStoreLocalVariable(ILGenerator il, int location)
    {
      if (location == 0)
        il.Emit(OpCodes.Stloc_0);
      else if (location == 1)
        il.Emit(OpCodes.Stloc_1);
      else if (location == 2)
        il.Emit(OpCodes.Stloc_2);
      else if (location == 3)
        il.Emit(OpCodes.Stloc_3);
      else if ((uint)location <= byte.MaxValue)
        il.Emit(OpCodes.Stloc_S, (byte)location);
      else
        il.Emit(OpCodes.Stloc, (short)location);
    }

    [MethodImpl((MethodImplOptions)256)]
    private static void EmitStoreAndLoadLocalVariable(ILGenerator il, int location)
    {
      if (location == 0)
      {
        il.Emit(OpCodes.Stloc_0);
        il.Emit(OpCodes.Ldloc_0);
      }
      else if (location == 1)
      {
        il.Emit(OpCodes.Stloc_1);
        il.Emit(OpCodes.Ldloc_1);
      }
      else if (location == 2)
      {
        il.Emit(OpCodes.Stloc_2);
        il.Emit(OpCodes.Ldloc_2);
      }
      else if (location == 3)
      {
        il.Emit(OpCodes.Stloc_3);
        il.Emit(OpCodes.Ldloc_3);
      }
      else if ((uint)location <= byte.MaxValue)
      {
        il.Emit(OpCodes.Stloc_S, (byte)location);
        il.Emit(OpCodes.Ldloc_S, (byte)location);
      }
      else
      {
        il.Emit(OpCodes.Stloc, (short)location);
        il.Emit(OpCodes.Ldloc, (short)location);
      }
    }

    [MethodImpl((MethodImplOptions)256)]
    private static int EmitStoreAndLoadLocalVariableAddress(ILGenerator il, Type type)
    {
      var location = il.GetNextLocalVarIndex(type);

      if (location == 0)
      {
        il.Emit(OpCodes.Stloc_0);
        il.Emit(OpCodes.Ldloca_S, (byte)0);
      }
      else if (location == 1)
      {
        il.Emit(OpCodes.Stloc_1);
        il.Emit(OpCodes.Ldloca_S, (byte)1);
      }
      else if (location == 2)
      {
        il.Emit(OpCodes.Stloc_2);
        il.Emit(OpCodes.Ldloca_S, (byte)2);
      }
      else if (location == 3)
      {
        il.Emit(OpCodes.Stloc_3);
        il.Emit(OpCodes.Ldloca_S, (byte)3);
      }
      else if ((uint)location <= byte.MaxValue)
      {
        il.Emit(OpCodes.Stloc_S, (byte)location);
        il.Emit(OpCodes.Ldloca_S, (byte)location);
      }
      else
      {
        il.Emit(OpCodes.Stloc, (short)location);
        il.Emit(OpCodes.Ldloca, (short)location);
      }

      return location;
    }

    [MethodImpl((MethodImplOptions)256)]
    private static void EmitLoadArg(ILGenerator il, int paramIndex)
    {
      if (paramIndex == 0)
        il.Emit(OpCodes.Ldarg_0);
      else if (paramIndex == 1)
        il.Emit(OpCodes.Ldarg_1);
      else if (paramIndex == 2)
        il.Emit(OpCodes.Ldarg_2);
      else if (paramIndex == 3)
        il.Emit(OpCodes.Ldarg_3);
      else if ((uint)paramIndex <= byte.MaxValue)
        il.Emit(OpCodes.Ldarg_S, (byte)paramIndex);
      else
        il.Emit(OpCodes.Ldarg, (short)paramIndex);
    }

    [MethodImpl((MethodImplOptions)256)]
    private static void EmitLoadArgAddress(ILGenerator il, int paramIndex)
    {
      if ((uint)paramIndex <= byte.MaxValue)
        il.Emit(OpCodes.Ldarga_S, (byte)paramIndex);
      else
        il.Emit(OpCodes.Ldarga, (short)paramIndex);
    }
  }
}