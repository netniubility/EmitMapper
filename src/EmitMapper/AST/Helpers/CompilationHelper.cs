﻿using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using EmitMapper.AST.Interfaces;

namespace EmitMapper.AST.Helpers;

internal static class CompilationHelper
{
  public static void CheckIsRef(Type type)
  {
    if (type.IsValueType)
      throw new ILCompilationException("A reference type was expected, but it was: " + type);
  }

  public static void CheckIsValue(Type type)
  {
    if (!type.IsValueType)
      throw new ILCompilationException("A value type was expected, but it was: " + type);
  }

  public static void EmitCall(
    CompilationContext context,
    IAstRefOrAddr invocationObject,
    MethodInfo methodInfo,
    List<IAstStackItem> arguments)
  {
    arguments ??= new List<IAstStackItem>();

    if (invocationObject != null)
      invocationObject.Compile(context);

    var args = methodInfo.GetParameters();

    if (args.Length != arguments.Count)
      throw new Exception("Invalid method parameters count");

    for (var i = 0; i < args.Length; ++i)
    {
      arguments[i].Compile(context);
      PrepareValueOnStack(context, args[i].ParameterType, arguments[i].ItemType);
    }

    context.EmitCall(methodInfo.IsVirtual ? OpCodes.Callvirt : OpCodes.Call, methodInfo);
  }

  public static void PrepareValueOnStack(CompilationContext context, Type desiredType, Type typeOnStack)
  {
    if (typeOnStack.IsValueType && !desiredType.IsValueType)
      context.Emit(OpCodes.Box, typeOnStack);
    else if (!typeOnStack.IsValueType && desiredType.IsValueType)
      context.Emit(OpCodes.Unbox_Any, desiredType);
    else if (desiredType != typeOnStack)
      context.Emit(OpCodes.Castclass, desiredType);
  }
}