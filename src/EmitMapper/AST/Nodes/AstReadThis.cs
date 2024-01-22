﻿namespace EmitMapper.AST.Nodes;

/// <summary>
///   The ast read this.
/// </summary>
internal class AstReadThis : IAstRefOrAddr
{
	/// <summary>
	/// This type
	/// </summary>
	public required Type ThisType;

	/// <summary>
	///   Gets the item type.
	/// </summary>
	public Type ItemType => ThisType;

	/// <inheritdoc />
	public virtual void Compile(CompilationContext context)
	{
		var arg = new AstReadArgument { ArgumentIndex = 0, ArgumentType = ThisType };
		arg.Compile(context);
	}
}