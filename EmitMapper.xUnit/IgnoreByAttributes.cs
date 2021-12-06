﻿using EmitMapper;
using EmitMapper.MappingConfiguration;
using EmitMapper.MappingConfiguration.MappingOperations.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace EmitMapperTests
{
    ////[TestFixture]
    public class IgnoreByAttributes
    {
        [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
        public class MyIgnoreAttribute : Attribute
        {
        }

        public class IgnoreByAttributesSrc
        {
            [MyIgnoreAttribute]
            public string str1 = "IgnoreByAttributesSrc::str1";
            public string str2 = "IgnoreByAttributesSrc::str2";
        }

        public class IgnoreByAttributesDst
        {
            public string str1 = "IgnoreByAttributesDst::str1";
            public string str2 = "IgnoreByAttributesDst::str2";
        }

        public class MyConfigurator : DefaultMapConfig
        {
            public override IMappingOperation[] GetMappingOperations(Type from, Type to)
            {
                base.IgnoreMembers<object, object>(GetIgnoreFields(from).Concat(GetIgnoreFields(to)).ToArray());
                return base.GetMappingOperations(from, to);
            }
            private IEnumerable<string> GetIgnoreFields(Type type)
            {
                return type
                    .GetFields()
                    .Where(f => f.GetCustomAttributes(typeof(MyIgnoreAttribute), false).Any())
                    .Select(f => f.Name)
                    .Concat(type.GetProperties()
                        .Where(p => p.GetCustomAttributes(typeof(MyIgnoreAttribute), false).Any()
                    ).Select(p => p.Name)
                    );
            }
        }

        [Fact]
        public void Test()
        {
            ObjectsMapper<IgnoreByAttributesSrc, IgnoreByAttributesDst> mapper = ObjectMapperManager.DefaultInstance.GetMapper<IgnoreByAttributesSrc, IgnoreByAttributesDst>(new MyConfigurator());
            IgnoreByAttributesDst dst = mapper.Map(new IgnoreByAttributesSrc());
            Assert.Equal("IgnoreByAttributesDst::str1", dst.str1);
            Assert.Equal("IgnoreByAttributesSrc::str2", dst.str2);
        }

    }
}
