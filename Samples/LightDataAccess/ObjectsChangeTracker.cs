﻿// ***********************************************************************
// Assembly         : TSharp.Core
// Author           : tangjingbo
// Created          : 05-23-2013
//
// Last Modified By : tangjingbo
// Last Modified On : 05-23-2013
// ***********************************************************************
// <copyright file="ObjectsChangeTracker.cs" company="T#">
//     Copyright (c) T#. All rights reserved.
// </copyright>
// <summary></summary>
// ***********************************************************************

using EmitMapper.Conversion;

namespace LightDataAccess;

/// <summary>
///   Class ObjectsChangeTracker.
/// </summary>
public class ObjectsChangeTracker
{
  /// <summary>
  ///   The _map manager.
  /// </summary>
  private readonly Mapper _mapManager;

  /// <summary>
  ///   The _tracking objects.
  /// </summary>
  private readonly Dictionary<object, List<TrackingMember>> _trackingObjects = new();

  /// <summary>
  ///   Initializes a new instance of the <see cref="ObjectsChangeTracker" /> class.
  /// </summary>
  public ObjectsChangeTracker()
  {
    _mapManager = Mapper.Default;
  }

  /// <summary>
  ///   Initializes a new instance of the <see cref="ObjectsChangeTracker" /> class.
  /// </summary>
  /// <param name="mapManager">The map manager.</param>
  public ObjectsChangeTracker(Mapper mapManager)
  {
    _mapManager = mapManager;
  }

  /// <summary>
  ///   Gets the changes.
  /// </summary>
  /// <param name="obj">The obj.</param>
  /// <returns>TrackingMember[][].</returns>
  public TrackingMember[] GetChanges(object obj)
  {
    if (!_trackingObjects.TryGetValue(obj, out var originalValues)) return null;

    var currentValues = GetObjectMembers(obj);

    return currentValues.Select(
      (x, idx) =>
      {
        var original = originalValues[idx];
        x.OriginalValue = original.CurrentValue;

        return x;
      }).Where(
      (current, idx) =>
      {
        return current.OriginalValue == null != (current.CurrentValue == null) || current.OriginalValue != null
          && !current.OriginalValue.Equals(current.CurrentValue);
      }).ToArray();
  }

  /// <summary>
  ///   Gets the changes.
  /// </summary>
  /// <param name="originalObj">The original obj.</param>
  /// <param name="currentObj">The current obj.</param>
  /// <returns>An array of TrackingMembers.</returns>
  public TrackingMember[] GetChanges(object originalObj, object currentObj)
  {
    if (originalObj == null || currentObj == null || originalObj.GetType() != currentObj.GetType()) return null;

    var originalValues = GetObjectMembers(originalObj);
    var currentValues = GetObjectMembers(currentObj);

    return currentValues.Select(
      (x, idx) =>
      {
        var original = originalValues[idx];
        x.OriginalValue = original.CurrentValue;

        return x;
      }).Where(
      (current, idx) =>
      {
        return current.OriginalValue == null != (current.CurrentValue == null) || current.OriginalValue != null
          && !current.OriginalValue.Equals(current.CurrentValue);
      }).ToArray();
  }

  /// <summary>
  ///   Registers the object.
  /// </summary>
  /// <param name="obj">The obj.</param>
  public void RegisterObject(object obj)
  {
    // var type = Obj.GetType();
    if (obj != null) _trackingObjects[obj] = GetObjectMembers(obj);
  }

  /// <summary>
  ///   Gets the object members.
  /// </summary>
  /// <param name="obj">The obj.</param>
  /// <returns>List{TrackingMember}.</returns>
  private List<TrackingMember> GetObjectMembers(object obj)
  {
    var type = obj?.GetType();
    while (type != null && type.Assembly.IsDynamic) type = type.BaseType;
    var fields = new TrackingMembersList();
    _mapManager.GetMapper(type, null, new MappingConfiguration()).Map(obj, null, fields);

    return fields.TrackingMembers;
  }

  /// <summary>
  ///   Struct TrackingMember.
  /// </summary>
  public struct TrackingMember
  {
    /// <summary>
    ///   The current value.
    /// </summary>
    public object CurrentValue;

    /// <summary>
    ///   The name.
    /// </summary>
    public string Name;

    /// <summary>
    ///   The original value.
    /// </summary>
    public object OriginalValue;
  }

  /// <summary>
  ///   Class TrackingMembersList.
  /// </summary>
  internal class TrackingMembersList
  {
    /// <summary>
    ///   The tracking members.
    /// </summary>
    public List<TrackingMember> TrackingMembers = new();
  }

  /// <summary>
  ///   Class MappingConfiguration.
  /// </summary>
  private class MappingConfiguration : MapConfigBaseImpl
  {
    /// <summary>
    ///   Gets the name of the configuration.
    /// </summary>
    /// <returns>System.String.</returns>
    public override string GetConfigurationName()
    {
      return "ObjectsTracker";
    }

    /// <summary>
    ///   Gets the mapping operations.
    /// </summary>
    /// <param name="from">From.</param>
    /// <param name="to">To.</param>
    /// <returns>IEnumerable&lt;IMappingOperation&gt;.</returns>
    public override IEnumerable<IMappingOperation> GetMappingOperations(Type from, Type to)
    {
      return ReflectionHelper.GetPublicFieldsAndProperties(from).Select(
        m => new SrcReadOperation
        {
          Source = new MemberDescriptor(m),
          Setter = (obj, value, state) =>
            (state as TrackingMembersList).TrackingMembers.Add(
              new TrackingMember { Name = m.Name, CurrentValue = value })
        }).ToArray();
    }

    /// <summary>
    ///   Gets the root mapping operation.
    /// </summary>
    /// <param name="from">From.</param>
    /// <param name="to">To.</param>
    /// <returns>IRootMappingOperation.</returns>
    public override IRootMappingOperation GetRootMappingOperation(Type from, Type to)
    {
      return null;
    }

    /// <summary>
    ///   Gets the static converters manager.
    /// </summary>
    /// <returns>StaticConvertersManager.</returns>
    public override StaticConvertersManager GetStaticConvertersManager()
    {
      return null;
    }
  }
}