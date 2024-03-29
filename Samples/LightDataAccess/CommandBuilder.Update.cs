﻿// ***********************************************************************
// Assembly         : TSharp.Core
// Author           : tangjingbo
// Created          : 08-21-2013
//
// Last Modified By : tangjingbo
// Last Modified On : 08-21-2013
// ***********************************************************************
// <copyright file="CommandBuilder.Update.cs" company="T#">
//     Copyright (c) T#. All rights reserved.
// </copyright>
// <summary></summary>
// ***********************************************************************

using LightDataAccess.MappingConfigs;

namespace LightDataAccess;

/// <summary>
///   Class CommandBuilder.
/// </summary>
public static partial class CommandBuilder
{
  /// <summary>
  ///   Builds the update command.
  /// </summary>
  /// <param name="cmd">The CMD.</param>
  /// <param name="obj">The obj.</param>
  /// <param name="tableName">Name of the table.</param>
  /// <param name="idFieldNames">The id field names.</param>
  /// <param name="includeFields">The include fields.</param>
  /// <param name="excludeFields">The exclude fields.</param>
  /// <param name="changeTracker">The change tracker.</param>
  /// <param name="dbSettings">The db settings.</param>
  /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
  public static bool BuildUpdateCommand(
    this DbCommand cmd,
    object obj,
    string tableName,
    IEnumerable<string> idFieldNames,
    IEnumerable<string> includeFields,
    IEnumerable<string> excludeFields,
    ObjectsChangeTracker changeTracker,
    DbSettings dbSettings)
  {
    if (idFieldNames == null) idFieldNames = new string[0];
    idFieldNames = idFieldNames.Select(n => n.ToUpper());

    if (changeTracker != null)
    {
      var changedFields = changeTracker.GetChanges(obj);

      if (changedFields != null)
      {
        if (includeFields == null)
          includeFields = changedFields.Select(c => c.Name);
        else
          includeFields = includeFields.Intersect(changedFields.Select(c => c.Name));
      }
    }

    if (includeFields != null) includeFields = includeFields.Concat(idFieldNames);

    IMappingConfigurator config = new AddDbCommandsMappingConfig(
      dbSettings,
      includeFields,
      excludeFields,
      "updateop_inc_" + includeFields.ToCsv("_") + "_exc_" + excludeFields.ToCsv("_"));

    var mapper = Mapper.Default.GetMapper(obj.GetType(), typeof(DbCommand), config);

    var fields = mapper.StoredObjects.OfType<SrcReadOperation>().Select(m => m.Source.MemberInfo.Name)
      .Where(f => !idFieldNames.Contains(f));

    if (!fields.Any()) return false;

    var cmdStr = "UPDATE " + tableName + " SET "
                 + fields.Select(
                   f => dbSettings.GetEscapedName(f.ToUpper()) + "=" + dbSettings.GetParamName(f.ToUpper())).ToCsv(",")
                 + " WHERE " + idFieldNames
                   .Select(fn => dbSettings.GetEscapedName(fn) + "=" + dbSettings.GetParamName(fn)).ToCsv(" AND ");

    cmd.CommandText = cmdStr;
    cmd.CommandType = CommandType.Text;

    mapper.Map(obj, cmd, null);

    return true;
  }

  /// <summary>
  ///   Builds the update operator.
  /// </summary>
  /// <param name="cmd">The CMD.</param>
  /// <param name="obj">The obj.</param>
  /// <param name="tableName">Name of the table.</param>
  /// <param name="idFieldNames">The id field names.</param>
  /// <param name="dbSettings">The db settings.</param>
  /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
  public static bool BuildUpdateOperator(
    this DbCommand cmd,
    object obj,
    string tableName,
    string[] idFieldNames,
    DbSettings dbSettings)
  {
    return BuildUpdateCommand(cmd, obj, tableName, idFieldNames, null, null, null, dbSettings);
  }
}