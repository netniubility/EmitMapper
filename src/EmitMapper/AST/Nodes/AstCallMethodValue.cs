﻿namespace EmitMapper.AST.Nodes;

/// <summary>
/// The ast call method value.
/// </summary>
internal class AstCallMethodValue : AstCallMethod, IAstValue
{
  /// <summary>
  /// Initializes a new instance of the <see cref="AstCallMethodValue"/> class.
  /// </summary>
  /// <param name="methodInfo">The method info.</param>
  /// <param name="invocationObject">The invocation object.</param>
  /// <param name="arguments">The arguments.</param>
  public AstCallMethodValue(MethodInfo methodInfo, IAstRefOrAddr invocationObject, List<IAstStackItem> arguments)
    : base(methodInfo, invocationObject, arguments)
  {
  }

  /// <inheritdoc/>
  public override void Compile(CompilationContext context)
  {
    CompilationHelper.CheckIsValue(ItemType);
    base.Compile(context);
  }
}