// <auto-generated/>
/*
The MIT License (MIT)

Copyright (c) 2016-2021 Maksim Volkau

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included
all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
THE SOFTWARE.
*/

// ReSharper disable CoVariantArrayConversion

/*
// Lists the target platforms that are Not supported by FEC - simplifies the direct referencing of Expression.cs file

#if !PCL && !NET35 && !NET40 && !NET403 && !NETSTANDARD1_0 && !NETSTANDARD1_1 && !NETSTANDARD1_2 && !NETCOREAPP1_0 && !NETCOREAPP1_1
#define SUPPORTS_FAST_EXPRESSION_COMPILER
#endif

#if SUPPORTS_FAST_EXPRESSION_COMPILER
*/
// #define LIGHT_EXPRESSION
#if LIGHT_EXPRESSION || !NET45
#define SUPPORTS_ARGUMENT_PROVIDER
#endif
#if !NETSTANDARD2_0
#define SUPPORTS_EMITCALL
#endif
#if LIGHT_EXPRESSION
using static FastExpressionCompiler.LightExpression.Expression;
using PE   = FastExpressionCompiler.LightExpression.ParameterExpression;
namespace    FastExpressionCompiler.LightExpression
#else
namespace EmitMapper
#endif
{
    using System;
    using System.Diagnostics;

    /// <summary>FEC Not Supported exception</summary>
    public sealed class NotSupportedExpressionException : InvalidOperationException
    {
        /// <summary>The reason</summary>
        public readonly NotSupported Reason;
        /// <summary>Constructor</summary>
        public NotSupportedExpressionException(NotSupported reason) : base(reason.ToString()) => Reason = reason;
        /// <summary>Constructor</summary>
        public NotSupportedExpressionException(NotSupported reason, string message) : base(reason + ": " + message) => Reason = reason;
    }

    // Helpers targeting the performance. Extensions method names may be a bit funny (non standard), 
    // in order to prevent conflicts with YOUR helpers with standard names
}
//#endif