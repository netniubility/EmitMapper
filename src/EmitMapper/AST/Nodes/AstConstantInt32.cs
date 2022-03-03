﻿using System;
using System.Reflection.Emit;
using EmitMapper.AST.Interfaces;
using EmitMapper.Utils;

namespace EmitMapper.AST.Nodes;

internal class AstConstantInt32 : IAstValue
{
  public int Value;

  public Type ItemType => Metadata<int>.Type;

  public void Compile(CompilationContext context)
  {
    context.Emit(OpCodes.Ldc_I4, Value);
  }
}