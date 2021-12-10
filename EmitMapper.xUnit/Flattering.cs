﻿namespace EmitMapperTests
{
    using EmitMapper;
    using EmitMapper.MappingConfiguration;
    using EmitMapper.MappingConfiguration.MappingOperations;

    using Xunit;

    ////[TestFixture]
    public class Flattering
    {
        [Fact]
        public void TestFlattering1()
        {
            var mapper = ObjectMapperManager.DefaultInstance.GetMapper<B, A>(
                new CustomMapConfig
                    {
                        GetMappingOperationFunc = (from, to) =>
                            {
                                return new[]
                                           {
                                               new ReadWriteSimple
                                                   {
                                                       Source = new MemberDescriptor(
                                                           new[]
                                                               {
                                                                   typeof(B).GetMember("intB")[0],
                                                                   typeof(B.IntB).GetMember("message")[0]
                                                               }),
                                                       Destination = new MemberDescriptor(
                                                           new[] { typeof(A).GetMember("message")[0] })
                                                   },
                                               new ReadWriteSimple
                                                   {
                                                       Source = new MemberDescriptor(
                                                           new[]
                                                               {
                                                                   typeof(B).GetMember("intB")[0],
                                                                   typeof(B.IntB).GetMember("GetMessage2")[0]
                                                               }),
                                                       Destination = new MemberDescriptor(
                                                           new[] { typeof(A).GetMember("message2")[0] })
                                                   }
                                           };
                            }
                    });

            var b = new B();
            var a = mapper.Map(b);
            Assert.Equal(b.intB.message, a.message);
            Assert.Equal(b.intB.GetMessage2(), a.message2);
        }

        public class A
        {
            public string message;

            public string message2;
        }

        public class B
        {
            public IntB intB = new IntB();

            public class IntB
            {
                public string message = "hello";

                public string GetMessage2()
                {
                    return "medved";
                }
            }
        }
    }
}