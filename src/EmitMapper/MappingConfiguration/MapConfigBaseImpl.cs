﻿namespace EmitMapper.MappingConfiguration;

/// <summary>
/// </summary>
public abstract class MapConfigBaseImpl : IMappingConfigurator
{
	private readonly TypeDictionary<Delegate> customConstructors = new();

	private readonly TypeDictionary<Delegate> customConverters = new();

	private readonly TypeDictionary<ICustomConverterProvider> customConvertersGeneric = new();

	private readonly TypeDictionary<Delegate> destinationFilters = new();

	private readonly TypeDictionary<List<string>> ignoreMembers = new();

	private readonly TypeDictionary<Delegate> nullSubstitutors = new();

	private readonly TypeDictionary<Delegate> postProcessors = new();

	private readonly TypeDictionary<Delegate> sourceFilters = new();

	private string configurationName;

	/// <summary>
	///   Initializes a new instance of the <see cref="MapConfigBaseImpl" /> class.
	/// </summary>
	public MapConfigBaseImpl()
	{
		RegisterDefaultCollectionConverters();
	}

	/// <summary>
	///   Builds the configuration name.
	/// </summary>
	public virtual void BuildConfigurationName()
	{
		configurationName = new[]
		{
	  ToStr(customConverters), ToStr(nullSubstitutors), ToStr(ignoreMembers), ToStr(postProcessors),
	  ToStr(customConstructors)
	}.ToCsv(";");
	}

	/// <summary>
	///   Define a custom constructor for the specified type
	/// </summary>
	/// <typeparam name="T">Type for which constructor is defining</typeparam>
	/// <param name="constructor">Custom constructor</param>
	/// <returns></returns>
	public IMappingConfigurator ConstructBy<T>(TargetConstructor<T> constructor)
	{
		customConstructors.Add(new[] { Metadata<T>.Type }, constructor);

		return this;
	}

	/// <summary>
	///   Define conversion for a generic. It is able to convert not one particular class but all generic family
	///   providing a generic converter.
	/// </summary>
	/// <param name="from">Type of source. Can be also generic class or abstract array.</param>
	/// <param name="to">Type of destination. Can be also generic class or abstract array.</param>
	/// <param name="converterProvider">Provider for getting detailed information about generic conversion</param>
	/// <returns></returns>
	public IMappingConfigurator ConvertGeneric(Type from, Type to, ICustomConverterProvider converterProvider)
	{
		customConvertersGeneric.Add(new[] { from, to }, converterProvider);

		return this;
	}

	/// <summary>
	///   Define custom type converter
	/// </summary>
	/// <typeparam name="TFrom">Source type</typeparam>
	/// <typeparam name="TTo"></typeparam>
	/// <param name="converter">Function which converts an instance of the source type to an instance of the destination type</param>
	/// <returns></returns>
	public IMappingConfigurator ConvertUsing<TFrom, TTo>(Func<TFrom, TTo> converter)
	{
		customConverters.Add(
		  new[] { Metadata<TFrom>.Type, Metadata<TTo>.Type },
		  (ValueConverter<TFrom, TTo>)((v, s) => converter(v)));

		return this;
	}

	/// <summary>
	///   Filters the destination.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	/// <param name="valuesFilter">The values filter.</param>
	/// <returns>An IMappingConfigurator.</returns>
	public IMappingConfigurator FilterDestination<T>(ValuesFilter<T> valuesFilter)
	{
		destinationFilters.Add(new[] { Metadata<T>.Type }, valuesFilter);

		return this;
	}

	/// <summary>
	///   Filters the source.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	/// <param name="valuesFilter">The values filter.</param>
	/// <returns>An IMappingConfigurator.</returns>
	public IMappingConfigurator FilterSource<T>(ValuesFilter<T> valuesFilter)
	{
		sourceFilters.Add(new[] { Metadata<T>.Type }, valuesFilter);

		return this;
	}

	/// <summary>
	///   Gets the configuration name.
	/// </summary>
	/// <returns>A string.</returns>
	public virtual string GetConfigurationName()
	{
		return configurationName;
	}

	/// <summary>
	///   Gets the mapping operations.
	/// </summary>
	/// <param name="from">The from.</param>
	/// <param name="to">The to.</param>
	/// <returns><![CDATA[IEnumerable<IMappingOperation>]]></returns>
	public abstract IEnumerable<IMappingOperation> GetMappingOperations(Type from, Type to);

	/// <summary>
	///   Gets the root mapping operation.
	/// </summary>
	/// <param name="from">The from.</param>
	/// <param name="to">The to.</param>
	/// <returns>An IRootMappingOperation.</returns>
	public virtual IRootMappingOperation GetRootMappingOperation(Type from, Type to)
	{
		var converter = customConverters.GetValue(new[] { from, to }) ?? GetGenericConverter(from, to);

		return new RootMappingOperation(from, to)
		{
			TargetConstructor = customConstructors.GetValue(to),
			NullSubstitutor = nullSubstitutors.GetValue(to),
			ValuesPostProcessor = postProcessors.GetValue(to),
			Converter = converter,
			DestinationFilter = destinationFilters.GetValue(to),
			SourceFilter = sourceFilters.GetValue(from)
		};
	}

	/// <summary>
	///   Gets the static converters manager.
	/// </summary>
	/// <returns>A StaticConvertersManager.</returns>
	public virtual StaticConvertersManager? GetStaticConvertersManager()
	{
		return null;
	}

	/// <summary>
	///   Define members which should be ignored
	/// </summary>
	/// <param name="typeFrom">Source type for which ignore members are defining</param>
	/// <param name="typeTo">Destination type for which ignore members are defining</param>
	/// <param name="ignoreNames">Array of member names which should be ignored</param>
	/// <returns></returns>
	public IMappingConfigurator IgnoreMembers(Type typeFrom, Type typeTo, string[] ignoreNames)
	{
		var ig = ignoreMembers.GetValue(new[] { typeFrom, typeTo });

		if (ig is null)
		{
			ignoreMembers.Add(new[] { typeFrom, typeTo }, ignoreNames.ToList());
		}
		else
		{
			ig.AddRange(ignoreNames);
		}

		return this;
	}

	/// <summary>
	///   Define members which should be ignored
	/// </summary>
	/// <typeparam name="TFrom">Source type for which ignore members are defining</typeparam>
	/// <typeparam name="TTo">Destination type for which ignore members are defining</typeparam>
	/// <param name="ignoreNames">Array of member names which should be ignored</param>
	/// <returns></returns>
	public IMappingConfigurator IgnoreMembers<TFrom, TTo>(params string[] ignoreNames)
	{
		return IgnoreMembers(Metadata<TFrom>.Type, Metadata<TTo>.Type, ignoreNames);
	}

	/// <summary>
	///   Setup function which returns value for destination if appropriate source member is null.
	/// </summary>
	/// <typeparam name="TFrom">Type of source member</typeparam>
	/// <typeparam name="TTo">Type of destination member</typeparam>
	/// <param name="nullSubstitutor">Function which returns value for destination if appropriate source member is null</param>
	/// <returns></returns>
	public IMappingConfigurator NullSubstitution<TFrom, TTo>(Func<object, TTo> nullSubstitutor)
	{
		nullSubstitutors.Add(new[] { Metadata<TFrom>.Type, Metadata<TTo>.Type }, nullSubstitutor);

		return this;
	}

	/// <summary>
	///   Define postprocessor for specified type
	/// </summary>
	/// <typeparam name="T">Objects of this type and all it's descendants will be postprocessed</typeparam>
	/// <param name="postProcessor"></param>
	/// <returns></returns>
	public IMappingConfigurator PostProcess<T>(ValuesPostProcessor<T> postProcessor)
	{
		postProcessors.Add(new[] { Metadata<T>.Type }, postProcessor);

		return this;
	}

	/// <summary>
	///   Set unique configuration name to force Emit Mapper create new mapper instead using appropriate cached one.
	/// </summary>
	/// <param name="configurationName">Configuration name</param>
	/// <returns></returns>
	public IMappingConfigurator SetConfigName(string configurationName)
	{
		this.configurationName = configurationName;

		return this;
	}

	/// <summary>
	///   Tos the str.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	/// <param name="t">The t.</param>
	/// <returns>A string.</returns>
	protected static string? ToStr<T>(T t)
	  where T : class
	{
		return t is null ? string.Empty : t.ToString();
	}

	/// <summary>
	///   Tos the str enum.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	/// <param name="t">The t.</param>
	/// <returns>A string.</returns>
	protected static string ToStrEnum<T>(IEnumerable<T> t)
	{
		return t is null ? string.Empty : t.ToCsv("|");
	}

	/// <summary>
	///   Filters the operations.
	/// </summary>
	/// <param name="from">The from.</param>
	/// <param name="to">The to.</param>
	/// <param name="operations">The operations.</param>
	/// <returns><![CDATA[IEnumerable<IMappingOperation>]]></returns>
	protected IEnumerable<IMappingOperation> FilterOperations(
	  Type from,
	  Type to,
	  IEnumerable<IMappingOperation> operations)
	{
		return operations.Select(
		  op =>
		  {
			  if (op is IReadWriteOperation readwrite)
			  {
				  if (TestIgnore(from, to, readwrite.Source, readwrite.Destination))
				  {
					  return null;
				  }

				  readwrite.NullSubstitutor =
				nullSubstitutors.GetValue(new[] { readwrite.Source.MemberType, readwrite.Destination.MemberType });

				  readwrite.TargetConstructor = customConstructors.GetValue(readwrite.Destination.MemberType);

				  readwrite.Converter =
				customConverters.GetValue(new[] { readwrite.Source.MemberType, readwrite.Destination.MemberType })
				?? GetGenericConverter(readwrite.Source.MemberType, readwrite.Destination.MemberType);

				  readwrite.DestinationFilter = destinationFilters.GetValue(readwrite.Destination.MemberType);
				  readwrite.SourceFilter = sourceFilters.GetValue(readwrite.Source.MemberType);
			  }

			  if (op is ReadWriteComplex readWriteComplex)
			  {
				  readWriteComplex.ValuesPostProcessor = postProcessors.GetValue(readWriteComplex.Destination.MemberType);
			  }

			  if (op is IComplexOperation complexOperation)
			  {
				  var orw = complexOperation as IReadWriteOperation;

				  complexOperation.Operations = FilterOperations(
				orw is null ? from : orw.Source.MemberType,
				orw is null ? to : orw.Destination.MemberType,
				complexOperation.Operations).ToList();
			  }

			  return op;
		  }).Where(x => x != null);
	}

	/// <summary>
	///   Registers the default collection converters.
	/// </summary>
	protected void RegisterDefaultCollectionConverters()
	{
		ConvertGeneric(Metadata.ICollection1, Metadata<Array>.Type, new ArraysConverterProvider());
	}

	/// <summary>
	///   Gets the generic converter.
	/// </summary>
	/// <param name="from">The from.</param>
	/// <param name="to">The to.</param>
	/// <returns>A Delegate.</returns>
	private Delegate? GetGenericConverter(Type from, Type to)
	{
		var converter = customConvertersGeneric.GetValue(new[] { from, to });

		if (converter is null)
		{
			return null;
		}

		var converterDescr = converter.GetCustomConverterDescr(from, to, this);

		if (converterDescr is null)
		{
			return null;
		}

		var genericConverter = converterDescr.ConverterClassTypeArguments.Any()
		  ? converterDescr.ConverterImplementation.MakeGenericType(
			converterDescr.ConverterClassTypeArguments.ToArray())
		  : converterDescr.ConverterImplementation;

		var mi = genericConverter.GetMethodCache(converterDescr.ConversionMethodName);

		var converterObj = ObjectFactory.CreateInstance(genericConverter);

		if (converterObj is not ICustomConverter customConverter)
		{
			return Delegate.CreateDelegate(Metadata.Func3.MakeGenericType(from, Metadata<object>.Type, to), converterObj, mi);
		}

		customConverter.Initialize(from, to, this);

		return Delegate.CreateDelegate(Metadata.Func3.MakeGenericType(from, Metadata<object>.Type, to), converterObj, mi);
	}

	/// <summary>
	///   Test ignore.
	/// </summary>
	/// <param name="from">The from.</param>
	/// <param name="to">The to.</param>
	/// <param name="fromDescr">The from descr.</param>
	/// <param name="toDescr">The to descr.</param>
	/// <returns>A bool.</returns>
	private bool TestIgnore(Type from, Type to, MemberDescriptor fromDescr, MemberDescriptor toDescr)
	{
		var ignore = ignoreMembers.GetValue(new[] { from, to });

		if (ignore != null && (ignore.Contains(fromDescr.MemberInfo.Name) || ignore.Contains(toDescr.MemberInfo.Name)))
		{
			return true;
		}

		return false;
	}
}