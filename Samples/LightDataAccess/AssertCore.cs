// ***********************************************************************
// Assembly         : TSharp.Core
// Author           : tangjingbo
// Created          : 05-23-2013
//
// Last Modified By : tangjingbo
// Last Modified On : 05-23-2013
// ***********************************************************************
// <copyright file="Assert.cs" company="T#">
//     Copyright (c) T#. All rights reserved.
// </copyright>
// <summary></summary>
// ***********************************************************************

using System.Globalization;
using System.Xml;

namespace LightDataAccess;

/// <summary>
///   The Assert class.
/// </summary>
public static class AssertCore
{
	/// <summary>
	///   Ares the equal.
	/// </summary>
	/// <typeparam name="T">The exception type.</typeparam>
	/// <param name="value1">The value1.</param>
	/// <param name="value2">The value2.</param>
	/// <param name="getException">The get exception.</param>
	public static void AreEqual<T>(int value1, int value2, Func<T> getException)
	  where T : Exception
	{
		if (value1 == value2)
		{
			return;
		}

		throw getException.Invoke();
	}

	/// <summary>
	///   Asserts that the arguments are equal.
	/// </summary>
	/// <param name="value1">The value1.</param>
	/// <param name="value2">The value2.</param>
	/// <param name="getMessage">The get message.</param>
	public static void AreEqual(int value1, int value2, Func<string?> getMessage)
	{
		AreEqual(value1, value2, () => GetInvalidOperationException(getMessage));
	}

	/// <summary>
	///   Ares the equal.
	/// </summary>
	/// <param name="value1">The value1.</param>
	/// <param name="value2">The value2.</param>
	public static void AreEqual(int value1, int value2)
	{
		AreEqual(value1, value2, () => "断言失败");
	}

	/// <summary>
	///   Ares the equal.
	/// </summary>
	/// <param name="value1">The value1.</param>
	/// <param name="value2">The value2.</param>
	public static void AreEqual(string value1, string value2)
	{
		AreEqual(value1, value2, () => "断言失败");
	}

	/// <summary>
	///   Asserts that the arguments are equal.
	/// </summary>
	/// <param name="value1">The value1.</param>
	/// <param name="value2">The value2.</param>
	/// <param name="message">The message.</param>
	public static void AreEqual(int value1, int value2, string? message)
	{
		AreEqual(value1, value2, () => GetInvalidOperationException(() => message));
	}

	/// <summary>
	///   Ares the equal.
	/// </summary>
	/// <typeparam name="T">The exception type.</typeparam>
	/// <param name="value1">The value1.</param>
	/// <param name="value2">The value2.</param>
	/// <param name="getException">The get exception.</param>
	public static void AreEqual<T>(string value1, string value2, Func<T> getException)
	  where T : Exception
	{
		if (value1 is null && value2 is not null)
		{
			throw getException.Invoke();
		}

		if (value1 is not null && value2 is null)
		{
			throw getException.Invoke();
		}

		if (value1 is not null && value2 is not null && value1.Length != value2.Length)
		{
			throw getException.Invoke();
		}

		if (value1 != value2)
		{
			throw getException.Invoke();
		}
	}

	/// <summary>
	///   Asserts that the arguments are equal.
	/// </summary>
	/// <param name="value1">The value1.</param>
	/// <param name="value2">The value2.</param>
	/// <param name="getMessage">The get message.</param>
	public static void AreEqual(string value1, string value2, Func<string?> getMessage)
	{
		AreEqual(value1, value2, () => GetInvalidOperationException(getMessage));
	}

	/// <summary>
	///   Asserts that the arguments are equal.
	/// </summary>
	/// <param name="value1">The value1.</param>
	/// <param name="value2">The value2.</param>
	/// <param name="message">The message.</param>
	public static void AreEqual(string value1, string value2, string? message)
	{
		AreEqual(value1, value2, () => GetInvalidOperationException(() => message));
	}

	/// <summary>
	///   Arguments the condition.
	/// </summary>
	/// <typeparam name="T">The exception type.</typeparam>
	/// <param name="condition">if set to <c>true</c> [condition].</param>
	/// <param name="getException">The get exception.</param>
	public static void ArgumentCondition<T>(bool condition, Func<T> getException)
	  where T : Exception
	{
		if (condition)
		{
			return;
		}

		throw getException.Invoke();
	}

	/// <summary>
	///   Asserts a condition on an argument.
	/// </summary>
	/// <param name="condition">if set to <c>true</c> [condition].</param>
	/// <param name="argumentName">Name of the argument.</param>
	/// <param name="getMessage">The get message.</param>
	/// <exception cref="System.ArgumentException">
	/// </exception>
	public static void ArgumentCondition(bool condition, string argumentName, Func<string> getMessage)
	{
		if (condition)
		{
			return;
		}

		var message = getMessage.Invoke();

		message = string.IsNullOrEmpty(message)
		  ? "An argument condition was false."
		  : string.Concat("An argument condition was false.", message);

		if (argumentName is not null)
		{
			throw new ArgumentException(message, argumentName);
		}

		throw new ArgumentException(message);
	}

	/// <summary>
	///   Asserts a condition on an argument.
	/// </summary>
	/// <param name="condition">if set to <c>true</c> [condition].</param>
	/// <param name="argumentName">Name of the argument.</param>
	/// <param name="message">The message.</param>
	public static void ArgumentCondition(bool condition, string argumentName, string message)
	{
		ArgumentCondition(condition, argumentName, () => message);
	}

	/// <summary>
	///   Asserts that the arguments are not null.
	/// </summary>
	/// <typeparam name="T">The exception type.</typeparam>
	/// <param name="argument">The argument.</param>
	/// <param name="getException">The get exception.</param>
	public static void ArgumentNotNull<T>(object? argument, Func<T> getException)
	  where T : Exception
	{
		if (argument is not null)
		{
			return;
		}

		throw getException.Invoke();
	}

	/// <summary>
	///   Asserts that the arguments are not null.
	/// </summary>
	/// <param name="argument">The argument.</param>
	/// <param name="getArgumentName">The delegate used to get the parameter name.</param>
	public static void ArgumentNotNull(object? argument, Func<string> getArgumentName)
	{
		if (argument is not null)
		{
			return;
		}

		var argumentName = getArgumentName.Invoke();

		var exception = string.IsNullOrEmpty(argumentName)
		  ? new ArgumentNullException()
		  : new ArgumentNullException(argumentName);

		throw exception;
	}

	/// <summary>
	///   Asserts that the arguments are not null.
	/// </summary>
	/// <param name="argument">The argument.</param>
	/// <param name="argumentName">Name of the argument.</param>
	/// <exception cref="ArgumentNullException"><c>argumentName</c> is null.</exception>
	public static void ArgumentNotNull(object? argument, string argumentName)
	{
		ArgumentNotNull(argument, () => argumentName);
	}

	/// <summary>
	///   Asserts that the arguments are not null or empty.
	/// </summary>
	/// <param name="argument">The argument.</param>
	/// <param name="getArgumentName">Delegate for getting the argument name.</param>
	/// <exception cref="System.ArgumentNullException">
	///   Null strings are not allowed.
	///   or.
	/// </exception>
	/// <exception cref="System.ArgumentException">
	///   Empty strings are not allowed.
	///   or
	///   Empty strings are not allowed.
	/// </exception>
	public static void ArgumentNotNullOrEmpty(string argument, Func<string> getArgumentName)
	{
		if (!string.IsNullOrEmpty(argument))
		{
			return;
		}

		var paramName = getArgumentName.Invoke();

		if (argument is null)
		{
			if (!string.IsNullOrEmpty(paramName))
			{
				throw new ArgumentNullException(paramName, "Null strings are not allowed.");
			}

			throw new ArgumentNullException();
		}

		if (!string.IsNullOrEmpty(paramName))
		{
			throw new ArgumentException("Empty strings are not allowed.", paramName);
		}

		throw new ArgumentException("Empty strings are not allowed.");
	}

	/// <summary>
	///   Arguments the not null or empty.
	/// </summary>
	/// <param name="argument">The argument.</param>
	/// <param name="argumentName">Name of the argument.</param>
	/// <exception cref="ArgumentNullException"><c>argumentName</c> is null.</exception>
	/// <exception cref="ArgumentException">Empty strings are not allowed.</exception>
	public static void ArgumentNotNullOrEmpty(string argument, string argumentName)
	{
		ArgumentNotNullOrEmpty(argument, () => argumentName);
	}

	/// <summary>
	///   Arguments the not null or empty.
	/// </summary>
	/// <typeparam name="T">The type is the collection.</typeparam>
	/// <param name="argument">The argument.</param>
	/// <param name="getArgumentName">Name of the get argument.</param>
	/// <exception cref="System.ArgumentNullException">
	///   Null collections are not allowed.
	///   or.
	/// </exception>
	/// <exception cref="System.ArgumentException">
	///   Empty collections are not allowed.
	///   or
	///   Empty collections are not allowed.
	/// </exception>
	public static void ArgumentNotNullOrEmpty<T>(IEnumerable<T> argument, Func<string> getArgumentName)
	{
		if (argument is null)
		{
			var argumentName = getArgumentName.Invoke();

			if (!string.IsNullOrEmpty(argumentName))
			{
				throw new ArgumentNullException(argumentName, "Null collections are not allowed.");
			}

			throw new ArgumentNullException();
		}

		if (!argument.Any())
		{
			var argumentName = getArgumentName.Invoke();

			if (!string.IsNullOrEmpty(argumentName))
			{
				throw new ArgumentException("Empty collections are not allowed.", argumentName);
			}

			throw new ArgumentException("Empty collections are not allowed.");
		}
	}

	/// <summary>
	///   Arguments the not null or empty.
	/// </summary>
	/// <typeparam name="T">The type is the collection.</typeparam>
	/// <param name="argument">The argument.</param>
	/// <param name="argumentName">Name of the argument.</param>
	public static void ArgumentNotNullOrEmpty<T>(IEnumerable<T> argument, string argumentName)
	{
		ArgumentNotNullOrEmpty(argument, () => argumentName);
	}

	/// <summary>
	///   Asserts that the specified condition is false.
	/// </summary>
	/// <typeparam name="T">The exception type.</typeparam>
	/// <param name="condition">if set to <c>true</c> [condition].</param>
	/// <param name="getException">The get exception.</param>
	public static void IsFalse<T>(bool condition, Func<T> getException)
	  where T : Exception
	{
		if (condition)
		{
			throw getException.Invoke();
		}
	}

	/// <summary>
	///   Asserts that the specified condition is false.
	/// </summary>
	/// <param name="condition">if set to <c>true</c> [condition].</param>
	/// <param name="getMessage">Delegate for getting the message.</param>
	public static void IsFalse(bool condition, Func<string?> getMessage)
	{
		IsFalse(condition, () => GetInvalidOperationException(getMessage));
	}

	/// <summary>
	///   Asserts that the specified condition is false.
	/// </summary>
	/// <param name="condition">if set to <c>true</c> [condition].</param>
	/// <param name="message">The message.</param>
	public static void IsFalse(bool condition, string? message)
	{
		IsFalse(condition, () => GetInvalidOperationException(() => message));
	}

	/// <summary>
	///   Determines whether [is not null] [the specified value].
	/// </summary>
	/// <typeparam name="T">The exception type.</typeparam>
	/// <param name="value">The value.</param>
	/// <param name="getException">The get exception.</param>
	public static void IsNotNull<T>(object value, Func<T> getException)
	  where T : Exception
	{
		if (value is null)
		{
			throw getException.Invoke();
		}
	}

	/// <summary>
	///   Asserts that the specified value is not null.
	/// </summary>
	/// <param name="value">The value.</param>
	/// <param name="getMessage">The get message.</param>
	/// <exception cref="InvalidOperationException"><c>InvalidOperationException</c>.</exception>
	public static void IsNotNull(object value, Func<string?> getMessage)
	{
		IsNotNull(value, () => GetInvalidOperationException(getMessage));
	}

	/// <summary>
	///   Asserts that the specified value is not null.
	/// </summary>
	/// <param name="value">The value.</param>
	/// <param name="message">The message.</param>
	/// <exception cref="InvalidOperationException"><c>InvalidOperationException</c>.</exception>
	public static void IsNotNull(object value, string? message)
	{
		IsNotNull(value, () => GetInvalidOperationException(() => message));
	}

	/// <summary>
	///   Asserts that the specified value is not null.
	/// </summary>
	/// <param name="value">The value.</param>
	/// <param name="type">The type of the object.</param>
	public static void IsNotNull(object? value, Type type)
	{
		if (value is null)
		{
			IsNotNull(value, type, string.Empty);
		}
	}

	/// <summary>
	///   Asserts that the specified value is not null.
	/// </summary>
	/// <param name="value">The value.</param>
	/// <param name="format">The format.</param>
	/// <param name="args">The arguments.</param>
	public static void IsNotNull(object? value, string format, params object[] args)
	{
		if (value is null)
		{
			var message = Format(format, args);
			IsNotNull(value, message);
		}
	}

	/// <summary>
	///   Asserts that the specified value is not null.
	/// </summary>
	/// <param name="value">The value.</param>
	/// <param name="type">The type of the object.</param>
	/// <param name="format">The format.</param>
	/// <param name="args">The arguments.</param>
	public static void IsNotNull(object? value, Type type, string format, params object[] args)
	{
		if (value is null)
		{
			var message = $"An instance of {type} was null.";
			if (format.Length > 0)
			{
				message = message + " Additional information: " + Format(format, args);
			}

			IsNotNull(value, message);
		}
	}

	/// <summary>
	///   Determines whether [is not null or empty] [the specified value].
	/// </summary>
	/// <typeparam name="T">The exception type.</typeparam>
	/// <param name="value">The value.</param>
	/// <param name="getException">The get exception.</param>
	public static void IsNotNullOrEmpty<T>(string value, Func<T> getException)
	  where T : Exception
	{
		if (string.IsNullOrEmpty(value))
		{
			throw getException.Invoke();
		}
	}

	/// <summary>
	///   The is not null or empty.
	/// </summary>
	/// <param name="value">The value.</param>
	/// <param name="getMessage">The get message.</param>
	/// <exception cref="InvalidOperationException"><c>InvalidOperationException</c>.</exception>
	public static void IsNotNullOrEmpty(string value, Func<string?> getMessage)
	{
		IsNotNullOrEmpty(value, () => GetInvalidOperationException(getMessage));
	}

	/// <summary>
	///   The is not null or empty.
	/// </summary>
	/// <param name="value">The value.</param>
	/// <param name="message">The message.</param>
	/// <exception cref="InvalidOperationException"><c>InvalidOperationException</c>.</exception>
	public static void IsNotNullOrEmpty(string value, string? message)
	{
		IsNotNullOrEmpty(value, () => message);
	}

	/// <summary>
	///   Determines whether [is not null or empty] [the specified value].
	/// </summary>
	/// <typeparam name="T">The exception type.</typeparam>
	/// <typeparam name="TE">The type of the exception.</typeparam>
	/// <param name="value">The value.</param>
	/// <param name="getException">The get exception.</param>
	public static void IsNotNullOrEmpty<T, TE>(IEnumerable<T> value, Func<TE> getException)
	  where TE : Exception
	{
		if (value is null)
		{
			throw getException.Invoke();
		}

		if (!value.Any())
		{
			throw getException.Invoke();
		}
	}

	/// <summary>
	///   Determines whether [is not null or empty] [the specified value].
	/// </summary>
	/// <typeparam name="T">The type in the collection.</typeparam>
	/// <param name="value">The value.</param>
	/// <param name="getMessage">The get message.</param>
	/// <exception cref="System.InvalidOperationException">
	///   Null collections are not allowed.
	///   or
	///   or
	///   Empty collections are not allowed.
	/// </exception>
	public static void IsNotNullOrEmpty<T>(IEnumerable<T> value, Func<string> getMessage)
	{
		if (value is null)
		{
			var message = getMessage.Invoke();

			if (!string.IsNullOrEmpty(message))
			{
				throw new InvalidOperationException(message);
			}

			throw new InvalidOperationException("Null collections are not allowed.");
		}

		if (!value.Any())
		{
			var message = getMessage.Invoke();

			if (!string.IsNullOrEmpty(message))
			{
				throw new InvalidOperationException(message);
			}

			throw new InvalidOperationException("Empty collections are not allowed.");
		}
	}

	/// <summary>
	///   Determines whether [is not null or empty] [the specified value].
	/// </summary>
	/// <typeparam name="T">The type in the collection.</typeparam>
	/// <param name="value">The value.</param>
	/// <param name="message">The message.</param>
	public static void IsNotNullOrEmpty<T>(IEnumerable<T> value, string message)
	{
		IsNotNullOrEmpty(value, () => message);
	}

	/// <summary>
	///   Determines whether the specified condition is true.
	/// </summary>
	/// <typeparam name="T">The exception type.</typeparam>
	/// <param name="condition">if set to <c>true</c> [condition].</param>
	/// <param name="getException">The get exception.</param>
	public static void IsTrue<T>(bool condition, Func<T> getException)
	  where T : Exception
	{
		if (!condition)
		{
			throw getException.Invoke();
		}
	}

	/// <summary>
	///   Determines whether the specified condition is true.
	/// </summary>
	/// <param name="condition">if set to <c>true</c> [condition].</param>
	/// <param name="message">The message.</param>
	/// <exception cref="InvalidOperationException"><c>InvalidOperationException</c>.</exception>
	public static void IsTrue(bool condition, string? message)
	{
		IsTrue(condition, () => message);
	}

	/// <summary>
	///   Determines whether the specified condition is true.
	/// </summary>
	/// <param name="condition">if set to <c>true</c> [condition].</param>
	/// <param name="getMessage">The get message delegate.</param>
	public static void IsTrue(bool condition, Func<string?> getMessage)
	{
		IsTrue(condition, () => GetInvalidOperationException(getMessage));
	}

	/// <summary>
	///   Results the not null.
	/// </summary>
	/// <typeparam name="T">The exception type.</typeparam>
	/// <param name="result">The result.</param>
	/// <param name="getException">The get exception.</param>
	/// <returns>The result object.</returns>
	public static T ResultNotNull<T>(T result, Func<T> getException)
	  where T : Exception
	{
		IsNotNull(result, getException);

		return result;
	}

	/// <summary>
	///   Results the not null.
	/// </summary>
	/// <typeparam name="T">The result type.</typeparam>
	/// <param name="result">The result.</param>
	/// <param name="getMessage">The get message.</param>
	/// <returns>The result object.</returns>
	public static T ResultNotNull<T>(T result, Func<string?> getMessage)
	{
		IsNotNull(result, getMessage);

		return result;
	}

	/// <summary>
	///   Results the not null.
	/// </summary>
	/// <typeparam name="T">The result type.</typeparam>
	/// <param name="result">The result.</param>
	/// <param name="message">The message.</param>
	/// <returns>The result object.</returns>
	public static T ResultNotNull<T>(T result, string? message)
	{
		return ResultNotNull(result, () => message);
	}

	/// <summary>
	///   Results the not null.
	/// </summary>
	/// <typeparam name="T">The result type.</typeparam>
	/// <param name="result">The result.</param>
	/// <returns>The result object.</returns>
	public static T ResultNotNull<T>(T result)
	{
		return ResultNotNull(result, "Post condition failed.");
	}

	/// <summary>
	///   Results the not null or empty.
	/// </summary>
	/// <typeparam name="T">The result type.</typeparam>
	/// <typeparam name="TE">The type of the exception.</typeparam>
	/// <param name="result">The result.</param>
	/// <param name="getException">The get exception.</param>
	/// <returns>The result collection.</returns>
	public static IEnumerable<T> ResultNotNullOrEmpty<T, TE>(IEnumerable<T> result, Func<TE> getException)
	  where TE : Exception
	{
		IsNotNullOrEmpty(result, getException);

		return result;
	}

	/// <summary>
	///   Results the not null or empty.
	/// </summary>
	/// <typeparam name="T">The result type.</typeparam>
	/// <param name="result">The result.</param>
	/// <returns>The result collection.</returns>
	public static IEnumerable<T> ResultNotNullOrEmpty<T>(IEnumerable<T> result)
	{
		IsNotNullOrEmpty(result, "Post condition failed.");

		return result;
	}

	/// <summary>
	///   Results the not null or empty.
	/// </summary>
	/// <typeparam name="T">The result type.</typeparam>
	/// <param name="result">The result.</param>
	/// <param name="message">The message.</param>
	/// <returns>The result collection.</returns>
	public static IEnumerable<T> ResultNotNullOrEmpty<T>(IEnumerable<T> result, string message)
	{
		IsNotNullOrEmpty(result, message);

		return result;
	}

	/// <summary>
	///   Results the not null or empty.
	/// </summary>
	/// <typeparam name="T">The result collection type.</typeparam>
	/// <param name="result">The result.</param>
	/// <param name="getMessage">The get message.</param>
	/// <returns>The result collection.</returns>
	public static IEnumerable<T> ResultNotNullOrEmpty<T>(IEnumerable<T> result, Func<string> getMessage)
	{
		IsNotNullOrEmpty(result, getMessage);

		return result;
	}

	/// <summary>
	///   Asserts that some function finished successfully.
	/// </summary>
	/// <typeparam name="T">The exception type.</typeparam>
	/// <param name="condition">The condition.</param>
	/// <param name="getException">The get exception.</param>
	public static void That<T>(Func<bool> condition, Func<T> getException)
	  where T : Exception
	{
		if (condition.Invoke())
		{
			throw getException.Invoke();
		}
	}

	/// <summary>
	///   Asserts that some function finished successfully.
	/// </summary>
	/// <param name="condition">The condition.</param>
	/// <param name="getMessage">The get message.</param>
	public static void That(Func<bool> condition, Func<string?> getMessage)
	{
		That(condition, () => GetInvalidOperationException(getMessage));
	}

	/// <summary>
	///   Asserts that some function finished successfully.
	/// </summary>
	/// <param name="condition">The condition.</param>
	/// <param name="message">The message.</param>
	public static void That(Func<bool> condition, string? message)
	{
		That(condition, () => message);
	}

	/// <summary>
	///   Formats the specified pattern.
	/// </summary>
	/// <param name="pattern">The pattern.</param>
	/// <param name="args">The arguments.</param>
	/// <returns>The formated string.</returns>
	private static string? Format(string? pattern, object[]? args)
	{
		ArgumentNullException.ThrowIfNull(pattern, nameof(pattern));
		if (args is null)
		{
			return pattern;
		}

		for (var i = 0; i < args.Length; i++)
		{
			if (args[i] is XmlNode node)
			{
				args[i] = node.OuterXml;
			}
		}

		return string.Format(CultureInfo.InvariantCulture, pattern, args);
	}

	/// <summary>
	///   Gets the invalid operation exception.
	/// </summary>
	/// <param name="getMessage">The get message.</param>
	/// <returns>The invalid operation exception.</returns>
	private static InvalidOperationException GetInvalidOperationException(Func<string?> getMessage)
	{
		var message = getMessage.Invoke();

		return string.IsNullOrEmpty(message) ? new InvalidOperationException() : new InvalidOperationException(message);
	}
}