﻿using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Shouldly;

namespace EmitMapper.Tests;
/// <summary>
/// The custom assert.
/// </summary>

internal static class CustomAssert
{
  /// <summary>
  /// Are the equal.
  /// </summary>
  /// <param name="expected">The expected.</param>
  /// <param name="actual">The actual.</param>
  public static void AreEqual(ICollection expected, ICollection actual)
  {
    expected.Count.ShouldBe(actual.Count);
    var enumExpected = expected.GetEnumerator();
    var enumActual = actual.GetEnumerator();

    while (enumExpected.MoveNext() && enumActual.MoveNext())
      enumExpected.Current.ShouldBe(enumActual.Current);
  }

  /// <summary>
  /// Are the equal enum.
  /// </summary>
  /// <typeparam name="T"></typeparam>
  /// <param name="expected">The expected.</param>
  /// <param name="actual">The actual.</param>
  public static void AreEqualEnum<T>(IEnumerable<T> expected, IEnumerable<T> actual)
  {
    actual.Count().ShouldBe(expected.Count());
    IEnumerator enumExpected = expected.GetEnumerator();
    IEnumerator enumActual = actual.GetEnumerator();

    while (enumExpected.MoveNext() && enumActual.MoveNext())
      enumExpected.Current.ShouldBe(enumActual.Current);
  }
}