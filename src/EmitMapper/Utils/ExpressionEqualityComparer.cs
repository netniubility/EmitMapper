﻿using System.Linq.Expressions;

namespace EmitMapper.Utils;

/// <summary>
///   A comparer which implements <see cref="IEqualityComparer{T}" /> for <see cref="Expression" />.
/// </summary>
public sealed class ExpressionEqualityComparer : IEqualityComparer<Expression>
{
  /// <summary>
  ///   Creates a new <see cref="ExpressionEqualityComparer" />.
  /// </summary>
  private ExpressionEqualityComparer()
  {
  }

  /// <summary>
  ///   Gets an instance of <see cref="ExpressionEqualityComparer" />.
  /// </summary>
  public static ExpressionEqualityComparer Instance { get; } = new();

  /// <summary>
  ///   Returns a value indicating whether the given expressions are equal.
  /// </summary>
  /// <param name="x"> The left expression. </param>
  /// <param name="y"> The right expression. </param>
  /// <returns> <see langword="true" /> if the expressions are equal, <see langword="false" /> otherwise. </returns>
  public bool Equals(Expression x, Expression y)
  {
    return new ExpressionComparer().Compare(x, y);
  }

  /// <summary>
  ///   Returns the hash code for given expression.
  /// </summary>
  /// <param name="obj"> The <see cref="Expression" /> obj to compute hash code for. </param>
  /// <returns> The hash code value for <paramref name="obj" />. </returns>
  public int GetHashCode(Expression obj)
  {
    if (obj == null) return 0;

    unchecked
    {
      var hash = new HashCode();
      hash.Add(obj.NodeType);
      hash.Add(obj.Type);

      switch (obj)
      {
        case BinaryExpression binaryExpression:
          hash.Add(binaryExpression.Left, this);
          hash.Add(binaryExpression.Right, this);
          AddExpressionToHashIfNotNull(binaryExpression.Conversion);
          AddToHashIfNotNull(binaryExpression.Method);

          break;

        case BlockExpression blockExpression:
          AddListToHash(blockExpression.Variables);
          AddListToHash(blockExpression.Expressions);

          break;

        case ConditionalExpression conditionalExpression:
          hash.Add(conditionalExpression.Test, this);
          hash.Add(conditionalExpression.IfTrue, this);
          hash.Add(conditionalExpression.IfFalse, this);

          break;

        case ConstantExpression constantExpression:
          if (constantExpression.Value != null && !(constantExpression.Value is IQueryable))
            hash.Add(constantExpression.Value);

          break;

        case DefaultExpression defaultExpression:
          // Intentionally empty. No additional members
          break;

        case GotoExpression gotoExpression:
          hash.Add(gotoExpression.Value, this);
          hash.Add(gotoExpression.Kind);
          hash.Add(gotoExpression.Target);

          break;

        case IndexExpression indexExpression:
          hash.Add(indexExpression.Object, this);
          AddListToHash(indexExpression.Arguments);
          hash.Add(indexExpression.Indexer);

          break;

        case InvocationExpression invocationExpression:
          hash.Add(invocationExpression.Expression, this);
          AddListToHash(invocationExpression.Arguments);

          break;

        case LabelExpression labelExpression:
          AddExpressionToHashIfNotNull(labelExpression.DefaultValue);
          hash.Add(labelExpression.Target);

          break;

        case LambdaExpression lambdaExpression:
          hash.Add(lambdaExpression.Body, this);
          AddListToHash(lambdaExpression.Parameters);
          hash.Add(lambdaExpression.ReturnType);

          break;

        case ListInitExpression listInitExpression:
          hash.Add(listInitExpression.NewExpression, this);
          AddInitializersToHash(listInitExpression.Initializers);

          break;

        case LoopExpression loopExpression:
          hash.Add(loopExpression.Body, this);
          AddToHashIfNotNull(loopExpression.BreakLabel);
          AddToHashIfNotNull(loopExpression.ContinueLabel);

          break;

        case MemberExpression memberExpression:
          hash.Add(memberExpression.Expression, this);
          hash.Add(memberExpression.Member);

          break;

        case MemberInitExpression memberInitExpression:
          hash.Add(memberInitExpression.NewExpression, this);
          AddMemberBindingsToHash(memberInitExpression.Bindings);

          break;

        case MethodCallExpression methodCallExpression:
          hash.Add(methodCallExpression.Object, this);
          AddListToHash(methodCallExpression.Arguments);
          hash.Add(methodCallExpression.Method);

          break;

        case NewArrayExpression newArrayExpression:
          AddListToHash(newArrayExpression.Expressions);

          break;

        case NewExpression newExpression:
          AddListToHash(newExpression.Arguments);
          hash.Add(newExpression.Constructor);

          if (newExpression.Members != null)
            for (var i = 0; i < newExpression.Members.Count; i++)
              hash.Add(newExpression.Members[i]);

          break;

        case ParameterExpression parameterExpression:
          AddToHashIfNotNull(parameterExpression.Name);

          break;

        case RuntimeVariablesExpression runtimeVariablesExpression:
          AddListToHash(runtimeVariablesExpression.Variables);

          break;

        case SwitchExpression switchExpression:
          hash.Add(switchExpression.SwitchValue, this);
          AddExpressionToHashIfNotNull(switchExpression.DefaultBody);
          AddToHashIfNotNull(switchExpression.Comparison);

          for (var i = 0; i < switchExpression.Cases.Count; i++)
          {
            var @case = switchExpression.Cases[i];
            hash.Add(@case.Body, this);
            AddListToHash(@case.TestValues);
          }

          break;

        case TryExpression tryExpression:
          hash.Add(tryExpression.Body, this);
          AddExpressionToHashIfNotNull(tryExpression.Fault);
          AddExpressionToHashIfNotNull(tryExpression.Finally);

          if (tryExpression.Handlers != null)
            for (var i = 0; i < tryExpression.Handlers.Count; i++)
            {
              var handler = tryExpression.Handlers[i];
              hash.Add(handler.Body, this);
              AddExpressionToHashIfNotNull(handler.Variable);
              AddExpressionToHashIfNotNull(handler.Filter);
              hash.Add(handler.Test);
            }

          break;

        case TypeBinaryExpression typeBinaryExpression:
          hash.Add(typeBinaryExpression.Expression, this);
          hash.Add(typeBinaryExpression.TypeOperand);

          break;

        case UnaryExpression unaryExpression:
          hash.Add(unaryExpression.Operand, this);
          AddToHashIfNotNull(unaryExpression.Method);

          break;

        default:
          if (obj.NodeType == ExpressionType.Extension)
          {
            hash.Add(obj);

            break;
          }

          throw new NotSupportedException($"The expression of type {obj.NodeType} has not supported");
      }

      return hash.ToHashCode();

      void AddToHashIfNotNull(object t)
      {
        if (t != null) hash.Add(t);
      }

      void AddExpressionToHashIfNotNull(Expression t)
      {
        if (t != null) hash.Add(t, this);
      }

      void AddListToHash<T>(IReadOnlyList<T> expressions)
        where T : Expression
      {
        for (var i = 0; i < expressions.Count; i++) hash.Add(expressions[i], this);
      }

      void AddInitializersToHash(IReadOnlyList<ElementInit> initializers)
      {
        for (var i = 0; i < initializers.Count; i++)
        {
          AddListToHash(initializers[i].Arguments);
          hash.Add(initializers[i].AddMethod);
        }
      }

      void AddMemberBindingsToHash(IReadOnlyList<MemberBinding> memberBindings)
      {
        for (var i = 0; i < memberBindings.Count; i++)
        {
          var memberBinding = memberBindings[i];

          hash.Add(memberBinding.Member);
          hash.Add(memberBinding.BindingType);

          switch (memberBinding)
          {
            case MemberAssignment memberAssignment:
              hash.Add(memberAssignment.Expression, this);

              break;

            case MemberListBinding memberListBinding:
              AddInitializersToHash(memberListBinding.Initializers);

              break;

            case MemberMemberBinding memberMemberBinding:
              AddMemberBindingsToHash(memberMemberBinding.Bindings);

              break;
          }
        }
      }
    }
  }

  private struct ExpressionComparer
  {
    private Dictionary<ParameterExpression, ParameterExpression> _parameterScope;

    /// <summary>
    /// Compares the.
    /// </summary>
    /// <param name="left">The left.</param>
    /// <param name="right">The right.</param>
    /// <returns>A bool.</returns>
    public bool Compare(Expression left, Expression right)
    {
      if (left == right) return true;

      if (left == null || right == null)
        return false;

      if (left.NodeType != right.NodeType) return false;

      if (left.Type != right.Type) return false;

      return left switch
      {
        BinaryExpression leftBinary => CompareBinary(leftBinary, (BinaryExpression)right),
        BlockExpression leftBlock => CompareBlock(leftBlock, (BlockExpression)right),
        ConditionalExpression leftConditional => CompareConditional(leftConditional, (ConditionalExpression)right),
        ConstantExpression leftConstant => CompareConstant(leftConstant, (ConstantExpression)right),
        DefaultExpression _ => true, // Intentionally empty. No additional members
        GotoExpression leftGoto => CompareGoto(leftGoto, (GotoExpression)right),
        IndexExpression leftIndex => CompareIndex(leftIndex, (IndexExpression)right),
        InvocationExpression leftInvocation => CompareInvocation(leftInvocation, (InvocationExpression)right),
        LabelExpression leftLabel => CompareLabel(leftLabel, (LabelExpression)right),
        LambdaExpression leftLambda => CompareLambda(leftLambda, (LambdaExpression)right),
        ListInitExpression leftListInit => CompareListInit(leftListInit, (ListInitExpression)right),
        LoopExpression leftLoop => CompareLoop(leftLoop, (LoopExpression)right),
        MemberExpression leftMember => CompareMember(leftMember, (MemberExpression)right),
        MemberInitExpression leftMemberInit => CompareMemberInit(leftMemberInit, (MemberInitExpression)right),
        MethodCallExpression leftMethodCall => CompareMethodCall(leftMethodCall, (MethodCallExpression)right),
        NewArrayExpression leftNewArray => CompareNewArray(leftNewArray, (NewArrayExpression)right),
        NewExpression leftNew => CompareNew(leftNew, (NewExpression)right),
        ParameterExpression leftParameter => CompareParameter(leftParameter, (ParameterExpression)right),
        RuntimeVariablesExpression leftRuntimeVariables => CompareRuntimeVariables(
          leftRuntimeVariables,
          (RuntimeVariablesExpression)right),
        SwitchExpression leftSwitch => CompareSwitch(leftSwitch, (SwitchExpression)right),
        TryExpression leftTry => CompareTry(leftTry, (TryExpression)right),
        TypeBinaryExpression leftTypeBinary => CompareTypeBinary(leftTypeBinary, (TypeBinaryExpression)right),
        UnaryExpression leftUnary => CompareUnary(leftUnary, (UnaryExpression)right),

        _ => left.NodeType == ExpressionType.Extension
          ? left.Equals(right)
          : throw new InvalidOperationException(
            $"The comparison operation has not implemented to expression type {left.NodeType}")
      };
    }

    /// <summary>
    ///   Compare constant.
    /// </summary>
    /// <param name="a">The a.</param>
    /// <param name="b">The b.</param>
    /// <returns>A bool.</returns>
    private static bool CompareConstant(ConstantExpression a, ConstantExpression b)
    {
      return Equals(a.Value, b.Value);
    }

    /// <summary>
    ///   Compare binary.
    /// </summary>
    /// <param name="a">The a.</param>
    /// <param name="b">The b.</param>
    /// <returns>A bool.</returns>
    private bool CompareBinary(BinaryExpression a, BinaryExpression b)
    {
      return Equals(a.Method, b.Method) && a.IsLifted == b.IsLifted && a.IsLiftedToNull == b.IsLiftedToNull
             && Compare(a.Left, b.Left) && Compare(a.Right, b.Right) && Compare(a.Conversion, b.Conversion);
    }

    /// <summary>
    ///   Compare binding.
    /// </summary>
    /// <param name="a">The a.</param>
    /// <param name="b">The b.</param>
    /// <exception cref="InvalidOperationException"></exception>
    /// <returns>A bool.</returns>
    private bool CompareBinding(MemberBinding a, MemberBinding b)
    {
      if (a == b) return true;

      if (a == null || b == null)
        return false;

      if (a.BindingType != b.BindingType) return false;

      if (!Equals(a.Member, b.Member)) return false;

#pragma warning disable IDE0066 // Convert switch statement to expression
      switch (a)
#pragma warning restore IDE0066
      {
        // Convert switch statement to expression
        case MemberAssignment aMemberAssignment:
          return Compare(aMemberAssignment.Expression, ((MemberAssignment)b).Expression);

        case MemberListBinding aMemberListBinding:
          return CompareElementInitList(aMemberListBinding.Initializers, ((MemberListBinding)b).Initializers);

        case MemberMemberBinding aMemberMemberBinding:
          return CompareMemberBindingList(aMemberMemberBinding.Bindings, ((MemberMemberBinding)b).Bindings);

        default:
          throw new InvalidOperationException("The expression has not an supported MemberBindingExpression");
      }
    }

    /// <summary>
    ///   Compare block.
    /// </summary>
    /// <param name="a">The a.</param>
    /// <param name="b">The b.</param>
    /// <returns>A bool.</returns>
    private bool CompareBlock(BlockExpression a, BlockExpression b)
    {
      return CompareExpressionList(a.Variables, b.Variables) && CompareExpressionList(a.Expressions, b.Expressions);
    }

    /// <summary>
    ///   Compares the catch block.
    /// </summary>
    /// <param name="a">The a.</param>
    /// <param name="b">The b.</param>
    /// <returns>A bool.</returns>
    private bool CompareCatchBlock(CatchBlock a, CatchBlock b)
    {
      return ReferenceEquals(a.Test, b.Test) && Equals(a.Test, b.Test) && Compare(a.Body, b.Body)
             && Compare(a.Filter, b.Filter) && Compare(a.Variable, b.Variable);
    }

    /// <summary>
    ///   Compares the catch block list.
    /// </summary>
    /// <param name="a">The a.</param>
    /// <param name="b">The b.</param>
    /// <returns>A bool.</returns>
    private bool CompareCatchBlockList(IReadOnlyList<CatchBlock> a, IReadOnlyList<CatchBlock> b)
    {
      if (ReferenceEquals(a, b)) return true;

      if (a == null || b == null || a.Count != b.Count)
        return false;

      for (int i = 0, n = a.Count; i < n; i++)
        if (!CompareCatchBlock(a[i], b[i]))
          return false;

      return true;
    }

    /// <summary>
    ///   Compare conditional.
    /// </summary>
    /// <param name="a">The a.</param>
    /// <param name="b">The b.</param>
    /// <returns>A bool.</returns>
    private bool CompareConditional(ConditionalExpression a, ConditionalExpression b)
    {
      return Compare(a.Test, b.Test) && Compare(a.IfTrue, b.IfTrue) && Compare(a.IfFalse, b.IfFalse);
    }

    /// <summary>
    ///   Compares the element init.
    /// </summary>
    /// <param name="a">The a.</param>
    /// <param name="b">The b.</param>
    /// <returns>A bool.</returns>
    private bool CompareElementInit(ElementInit a, ElementInit b)
    {
      return Equals(a.AddMethod, b.AddMethod) && CompareExpressionList(a.Arguments, b.Arguments);
    }

    /// <summary>
    ///   Compares the element init list.
    /// </summary>
    /// <param name="a">The a.</param>
    /// <param name="b">The b.</param>
    /// <returns>A bool.</returns>
    private bool CompareElementInitList(IReadOnlyList<ElementInit> a, IReadOnlyList<ElementInit> b)
    {
      if (ReferenceEquals(a, b)) return true;

      if (a == null || b == null || a.Count != b.Count)
        return false;

      for (int i = 0, n = a.Count; i < n; i++)
        if (!CompareElementInit(a[i], b[i]))
          return false;

      return true;
    }

    /// <summary>
    ///   Compares the expression list.
    /// </summary>
    /// <param name="a">The a.</param>
    /// <param name="b">The b.</param>
    /// <returns>A bool.</returns>
    private bool CompareExpressionList(IReadOnlyList<Expression> a, IReadOnlyList<Expression> b)
    {
      if (ReferenceEquals(a, b)) return true;

      if (a == null || b == null || a.Count != b.Count)
        return false;

      for (int i = 0, n = a.Count; i < n; i++)
        if (!Compare(a[i], b[i]))
          return false;

      return true;
    }

    /// <summary>
    ///   Compare goto.
    /// </summary>
    /// <param name="a">The a.</param>
    /// <param name="b">The b.</param>
    /// <returns>A bool.</returns>
    private bool CompareGoto(GotoExpression a, GotoExpression b)
    {
      return a.Kind == b.Kind && Equals(a.Target, b.Target) && Compare(a.Value, b.Value);
    }

    /// <summary>
    ///   Compare index.
    /// </summary>
    /// <param name="a">The a.</param>
    /// <param name="b">The b.</param>
    /// <returns>A bool.</returns>
    private bool CompareIndex(IndexExpression a, IndexExpression b)
    {
      return Equals(a.Indexer, b.Indexer) && Compare(a.Object, b.Object)
                                          && CompareExpressionList(a.Arguments, b.Arguments);
    }

    /// <summary>
    ///   Compare invocation.
    /// </summary>
    /// <param name="a">The a.</param>
    /// <param name="b">The b.</param>
    /// <returns>A bool.</returns>
    private bool CompareInvocation(InvocationExpression a, InvocationExpression b)
    {
      return Compare(a.Expression, b.Expression) && CompareExpressionList(a.Arguments, b.Arguments);
    }

    /// <summary>
    ///   Compare label.
    /// </summary>
    /// <param name="a">The a.</param>
    /// <param name="b">The b.</param>
    /// <returns>A bool.</returns>
    private bool CompareLabel(LabelExpression a, LabelExpression b)
    {
      return Equals(a.Target, b.Target) && Compare(a.DefaultValue, b.DefaultValue);
    }

    /// <summary>
    ///   Compare lambda.
    /// </summary>
    /// <param name="a">The a.</param>
    /// <param name="b">The b.</param>
    /// <returns>A bool.</returns>
    private bool CompareLambda(LambdaExpression a, LambdaExpression b)
    {
      var n = a.Parameters.Count;

      if (b.Parameters.Count != n) return false;

      _parameterScope ??= new Dictionary<ParameterExpression, ParameterExpression>();

      for (var i = 0; i < n; i++)
      {
        if (a.Parameters[i].Type != b.Parameters[i].Type)
        {
          for (var j = 0; j < i; j++) _parameterScope.Remove(a.Parameters[j]);

          return false;
        }

        _parameterScope.Add(a.Parameters[i], b.Parameters[i]);
      }

      try
      {
        return Compare(a.Body, b.Body);
      }
      finally
      {
        for (var i = 0; i < n; i++) _parameterScope.Remove(a.Parameters[i]);
      }
    }

    /// <summary>
    ///   Compares the list init.
    /// </summary>
    /// <param name="a">The a.</param>
    /// <param name="b">The b.</param>
    /// <returns>A bool.</returns>
    private bool CompareListInit(ListInitExpression a, ListInitExpression b)
    {
      return Compare(a.NewExpression, b.NewExpression) && CompareElementInitList(a.Initializers, b.Initializers);
    }

    /// <summary>
    ///   Compare loop.
    /// </summary>
    /// <param name="a">The a.</param>
    /// <param name="b">The b.</param>
    /// <returns>A bool.</returns>
    private bool CompareLoop(LoopExpression a, LoopExpression b)
    {
      return Equals(a.BreakLabel, b.BreakLabel) && Equals(a.ContinueLabel, b.ContinueLabel) && Compare(a.Body, b.Body);
    }

    /// <summary>
    ///   Compare member.
    /// </summary>
    /// <param name="a">The a.</param>
    /// <param name="b">The b.</param>
    /// <returns>A bool.</returns>
    private bool CompareMember(MemberExpression a, MemberExpression b)
    {
      return Equals(a.Member, b.Member) && Compare(a.Expression, b.Expression);
    }

    /// <summary>
    ///   Compares the member binding list.
    /// </summary>
    /// <param name="a">The a.</param>
    /// <param name="b">The b.</param>
    /// <returns>A bool.</returns>
    private bool CompareMemberBindingList(IReadOnlyList<MemberBinding> a, IReadOnlyList<MemberBinding> b)
    {
      if (ReferenceEquals(a, b)) return true;

      if (a == null || b == null || a.Count != b.Count)
        return false;

      for (int i = 0, n = a.Count; i < n; i++)
        if (!CompareBinding(a[i], b[i]))
          return false;

      return true;
    }

    /// <summary>
    ///   Compares the member init.
    /// </summary>
    /// <param name="a">The a.</param>
    /// <param name="b">The b.</param>
    /// <returns>A bool.</returns>
    private bool CompareMemberInit(MemberInitExpression a, MemberInitExpression b)
    {
      return Compare(a.NewExpression, b.NewExpression) && CompareMemberBindingList(a.Bindings, b.Bindings);
    }

    /// <summary>
    ///   Compares the member list.
    /// </summary>
    /// <param name="a">The a.</param>
    /// <param name="b">The b.</param>
    /// <returns>A bool.</returns>
    private bool CompareMemberList(IReadOnlyList<MemberInfo> a, IReadOnlyList<MemberInfo> b)
    {
      if (ReferenceEquals(a, b)) return true;

      if (a == null || b == null || a.Count != b.Count)
        return false;

      for (int i = 0, n = a.Count; i < n; i++)
        if (!Equals(a[i], b[i]))
          return false;

      return true;
    }

    /// <summary>
    ///   Compares the method call.
    /// </summary>
    /// <param name="a">The a.</param>
    /// <param name="b">The b.</param>
    /// <returns>A bool.</returns>
    private bool CompareMethodCall(MethodCallExpression a, MethodCallExpression b)
    {
      return Equals(a.Method, b.Method) && Compare(a.Object, b.Object)
                                        && CompareExpressionList(a.Arguments, b.Arguments);
    }

    /// <summary>
    ///   Compare new.
    /// </summary>
    /// <param name="a">The a.</param>
    /// <param name="b">The b.</param>
    /// <returns>A bool.</returns>
    private bool CompareNew(NewExpression a, NewExpression b)
    {
      return Equals(a.Constructor, b.Constructor) && CompareExpressionList(a.Arguments, b.Arguments)
                                                  && CompareMemberList(a.Members, b.Members);
    }

    /// <summary>
    ///   Compares the new array.
    /// </summary>
    /// <param name="a">The a.</param>
    /// <param name="b">The b.</param>
    /// <returns>A bool.</returns>
    private bool CompareNewArray(NewArrayExpression a, NewArrayExpression b)
    {
      return CompareExpressionList(a.Expressions, b.Expressions);
    }

    /// <summary>
    ///   Compare parameter.
    /// </summary>
    /// <param name="a">The a.</param>
    /// <param name="b">The b.</param>
    /// <returns>A bool.</returns>
    private bool CompareParameter(ParameterExpression a, ParameterExpression b)
    {
      return _parameterScope != null && _parameterScope.TryGetValue(a, out var mapped)
        ? mapped.Name == b.Name
        : a.Name == b.Name;
    }

    /// <summary>
    ///   Compares the runtime variables.
    /// </summary>
    /// <param name="a">The a.</param>
    /// <param name="b">The b.</param>
    /// <returns>A bool.</returns>
    private bool CompareRuntimeVariables(RuntimeVariablesExpression a, RuntimeVariablesExpression b)
    {
      return CompareExpressionList(a.Variables, b.Variables);
    }

    /// <summary>
    ///   Compare switch.
    /// </summary>
    /// <param name="a">The a.</param>
    /// <param name="b">The b.</param>
    /// <returns>A bool.</returns>
    private bool CompareSwitch(SwitchExpression a, SwitchExpression b)
    {
      return Equals(a.Comparison, b.Comparison) && Compare(a.SwitchValue, b.SwitchValue)
                                                && Compare(a.DefaultBody, b.DefaultBody)
                                                && CompareSwitchCaseList(a.Cases, b.Cases);
    }

    /// <summary>
    ///   Compares the switch case.
    /// </summary>
    /// <param name="a">The a.</param>
    /// <param name="b">The b.</param>
    /// <returns>A bool.</returns>
    private bool CompareSwitchCase(SwitchCase a, SwitchCase b)
    {
      return Compare(a.Body, b.Body) && CompareExpressionList(a.TestValues, b.TestValues);
    }

    /// <summary>
    ///   Compares the switch case list.
    /// </summary>
    /// <param name="a">The a.</param>
    /// <param name="b">The b.</param>
    /// <returns>A bool.</returns>
    private bool CompareSwitchCaseList(IReadOnlyList<SwitchCase> a, IReadOnlyList<SwitchCase> b)
    {
      if (ReferenceEquals(a, b)) return true;

      if (a == null || b == null || a.Count != b.Count)
        return false;

      for (int i = 0, n = a.Count; i < n; i++)
        if (!CompareSwitchCase(a[i], b[i]))
          return false;

      return true;
    }

    /// <summary>
    ///   Compare try.
    /// </summary>
    /// <param name="a">The a.</param>
    /// <param name="b">The b.</param>
    /// <returns>A bool.</returns>
    private bool CompareTry(TryExpression a, TryExpression b)
    {
      return Compare(a.Body, b.Body) && Compare(a.Fault, b.Fault) && Compare(a.Finally, b.Finally)
             && CompareCatchBlockList(a.Handlers, b.Handlers);
    }

    /// <summary>
    ///   Compares the type binary.
    /// </summary>
    /// <param name="a">The a.</param>
    /// <param name="b">The b.</param>
    /// <returns>A bool.</returns>
    private bool CompareTypeBinary(TypeBinaryExpression a, TypeBinaryExpression b)
    {
      return a.TypeOperand == b.TypeOperand && Compare(a.Expression, b.Expression);
    }

    /// <summary>
    ///   Compare unary.
    /// </summary>
    /// <param name="a">The a.</param>
    /// <param name="b">The b.</param>
    /// <returns>A bool.</returns>
    private bool CompareUnary(UnaryExpression a, UnaryExpression b)
    {
      return Equals(a.Method, b.Method) && a.IsLifted == b.IsLifted && a.IsLiftedToNull == b.IsLiftedToNull
             && Compare(a.Operand, b.Operand);
    }
  }
}