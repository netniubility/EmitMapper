﻿namespace EmitMapper.Tests;

/// <summary>
///   The custom mapping.
/// </summary>
public class CustomMapping
{
  /// <summary>
  ///   Test_s the custom converter.
  /// </summary>
  [Fact]
  public void Test_CustomConverter()
  {
    var a = Context.ObjMan.GetMapper<B2, A2>(
      new DefaultMapConfig().ConvertUsing<object, string>(v => "333").ConvertUsing<object, string>(v => "hello")
        .SetConfigName("ignore")).Map(new B2());

    a.Fld1.ShouldBeNull();
    a.Fld2.ShouldBe("hello");

    a = Context.ObjMan.GetMapper<B2, A2>().Map(new B2());
    a.Fld2.ShouldBe("B2::fld2");
  }

  /// <summary>
  ///   Test_s the custom converter2.
  /// </summary>
  [Fact]
  public void Test_CustomConverter2()
  {
    var a = Context.ObjMan.GetMapper<Bb, Aa>(new DefaultMapConfig().ConvertUsing<object, string>(v => "converted " + v))
      .Map(new Bb());

    a.Fld1.ShouldBe("converted B2::fld1");
    a.Fld2.ShouldBe("converted B2::fld2");
  }

  /// <summary>
  ///   Test_s the custom converter with interfaces.
  /// </summary>
  [Fact]
  public void Test_CustomConverterWithInterfaces()
  {
    var str = Context.ObjMan.GetMapper<WithName, string>(
        new DefaultMapConfig().ConvertUsing<IWithName, string>(v => v.Name).SetConfigName("withinterfaces"))
      .Map(new WithName { Name = "thisIsMyName" });

    str.ShouldBe("thisIsMyName");
  }

  /// <summary>
  ///   Test_s the post processing.
  /// </summary>
  [Fact]
  public void Test_PostProcessing()
  {
    var a = Context.ObjMan.GetMapper<B3, A3>(
      new DefaultMapConfig().PostProcess<A3.Int>(
          (i, state) =>
          {
            i.Str2 = "processed";

            return i;
          }).PostProcess<A3.SInt?>((i, state) => { return new A3.SInt { Str1 = i.Value.Str1, Str2 = "processed" }; })
        .PostProcess<A3>(
          (i, state) =>
          {
            i.Status = "processed";

            return i;
          })).Map(new B3());

    a.Fld.Str1.ShouldBe("B3::Int::str1");
    a.Fld.Str2.ShouldBe("processed");

    a.Fld2.Value.Str1.ShouldBe("B3::SInt::str1");
    a.Fld2.Value.Str2.ShouldBe("processed");

    a.Status.ShouldBe("processed");
  }

  /// <summary>
  ///   The a1.
  /// </summary>
  public class A1
  {
    public string Fld1 = string.Empty;
    /// <summary>
    ///   Gets the fld2.
    /// </summary>
    public string Fld2 { get; private set; } = string.Empty;

    /// <summary>
    ///   Sets the fld2.
    /// </summary>
    /// <param name="value">The value.</param>
    public void SetFld2(string value)
    {
      Fld2 = value;
    }
  }

  /// <summary>
  ///   The a2.
  /// </summary>
  public class A2
  {
    public string Fld1;
    public string Fld2;
  }

  /// <summary>
  ///   The a3.
  /// </summary>
  public class A3
  {
    public Int Fld;
    public SInt? Fld2;
    public string Status;

    public struct SInt
    {
      public string Str1;
      public string Str2;
    }

    /// <summary>
    ///   The int.
    /// </summary>
    public class Int
    {
      public string Str1;
      public string Str2;
    }
  }

  /// <summary>
  ///   The aa.
  /// </summary>
  public class Aa
  {
    public string Fld1;
    public string Fld2;
  }

  /// <summary>
  ///   The b2.
  /// </summary>
  public class B2
  {
    public string Fld2 = "B2::fld2";
    public string Fld3 = "B2::fld3";
  }

  /// <summary>
  ///   The b3.
  /// </summary>
  public class B3
  {
    public Int Fld = new();
    public SInt Fld2;

    /// <summary>
    ///   Initializes a new instance of the <see cref="B3" /> class.
    /// </summary>
    public B3()
    {
      Fld2.Str1 = "B3::SInt::str1";
    }

    public struct SInt
    {
      public string Str1;
    }

    /// <summary>
    ///   The int.
    /// </summary>
    public class Int
    {
      public string Str1 = "B3::Int::str1";
    }
  }

  /// <summary>
  ///   The bb.
  /// </summary>
  public class Bb
  {
    public string Fld1 = "B2::fld1";
    public string Fld2 = "B2::fld2";
  }

  /// <summary>
  ///   The with name.
  /// </summary>
  public class WithName : IWithName
  {
    /// <summary>
    ///   Gets or Sets the name.
    /// </summary>
    public string Name { get; set; }
  }

  /// <summary>
  ///   The with name interface.
  /// </summary>
  public interface IWithName
  {
    /// <summary>
    ///   Gets or Sets the name.
    /// </summary>
    string Name { get; set; }
  }
}