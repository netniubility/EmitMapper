﻿using EmitMapper.Utils;
using System;
using System.Collections.Generic;
using System.Linq;

namespace EmitMapper.MappingConfiguration
{
    internal class TypeDictionary<T> where T : class
    {
        private readonly List<ListElement> elements = new List<ListElement>();

        public override string ToString()
        {
            return elements.Select(e => e.types.ToCSV("|") + (e.value == null ? "|" : "|" + e.value)).ToCSV("||");
        }

        public bool IsTypesInList(Type[] types)
        {
            return FindTypes(types) != null;
        }

        public T GetValue(Type[] types)
        {
            ListElement elem = FindTypes(types);
            return elem == null ? null : elem.value;
        }

        public void Add(Type[] types, T value)
        {
            ListElement newElem = new ListElement(types, value);
            if (elements.Contains(newElem))
            {
                elements.Remove(newElem);
            }

            elements.Add(new ListElement(types, value));
        }

        private ListElement FindTypes(Type[] types)
        {
            foreach (ListElement element in elements)
            {
                bool isAssignable = true;
                for (int i = 0, j = 0; i < element.types.Length; ++i)
                {
                    if (i < types.Length)
                    {
                        j = i;
                    }

                    if (!IsGeneralType(element.types[i], types[j]))
                    {
                        isAssignable = false;
                        break;
                    }
                }
                if (isAssignable)
                {
                    return element;
                }
            }
            return null;
        }

        private static bool IsGeneralType(Type generalType, Type type)
        {
            if (generalType == type)
            {
                return true;
            }
            if (generalType.IsGenericTypeDefinition)
            {
                if (generalType.IsInterface)
                {
                    return
                        (type.IsInterface ? new[] { type } : new Type[0]).Concat(type.GetInterfaces())
                            .Any(
                                i =>
                                    i.IsGenericType &&
                                    i.GetGenericTypeDefinition() == generalType
                            );
                }
                return type.IsGenericType && (type.GetGenericTypeDefinition() == generalType
                    || type.GetGenericTypeDefinition().IsSubclassOf(generalType));
            }

            return generalType.IsAssignableFrom(type);
        }

        private class ListElement
        {
            public readonly Type[] types;
            public readonly T value;

            public ListElement(Type[] types, T value)
            {
                this.types = types;
                this.value = value;
            }

            public override int GetHashCode()
            {
                return types.Sum(t => t.GetHashCode());
            }

            public override bool Equals(object obj)
            {
                ListElement rhs = (ListElement)obj;
                for (int i = 0; i < types.Length; ++i)
                {
                    if (types[i] != rhs.types[i])
                    {
                        return false;
                    }
                }
                return true;
            }
        }
    }
}