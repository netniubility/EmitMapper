﻿namespace EmitMapper.AST.Nodes;

/// <summary>
///   The ast read field ref.
/// </summary>
internal class AstReadFieldRef : AstReadField, IAstRef
{
  /// <summary>
  /// </summary>
  /// <param name="context">The context.</param>
  public override void Compile(CompilationContext context)
  {
    CompilationHelper.CheckIsRef(ItemType);
    base.Compile(context);
  }
}