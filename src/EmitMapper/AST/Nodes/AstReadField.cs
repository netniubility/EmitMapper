﻿namespace EmitMapper.AST.Nodes;

/// <summary>
///   The ast read field.
/// </summary>
internal class AstReadField : IAstStackItem
{
  public FieldInfo FieldInfo;

  public IAstRefOrAddr SourceObject;

  /// <summary>
  ///   Gets the item type.
  /// </summary>
  public Type ItemType => FieldInfo.FieldType;

  /// <summary>
  /// </summary>
  /// <param name="context">The context.</param>
  public virtual void Compile(CompilationContext context)
  {
    SourceObject.Compile(context);
    context.Emit(OpCodes.Ldfld, FieldInfo);
  }
}