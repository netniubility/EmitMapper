﻿namespace EmitMapper.xUnit
{
    using Xunit;

    ////[TestFixture]
    public class ValueTypes
    {
        [Fact]
        public void Test_ClassToStruct()
        {
            var a = new A1();
            var b = new B1();
            a = Context.objMan.GetMapper<B1, A1>().Map(b, a);
            Assert.Equal(10, a.fld1);
        }

        [Fact]
        public void Test_StructToStruct()
        {
            var a = new A2();
            var b = new B2 { fld1 = 99 };
            a = Context.objMan.GetMapper<B2, A2>().Map(b, a);
            Assert.Equal(99, a.fld1);
        }

        [Fact]
        public void Test_StructToClass()
        {
            var a = new A3();
            var b = new B3 { fld1 = 87 };
            a = Context.objMan.GetMapper<B3, A3>().Map(b, a);
            Assert.Equal(87, a.fld1);
        }

        [Fact]
        public void Test_StructProperties()
        {
            var a = new A4();
            var b = new B4();
            var mapper = Context.objMan.GetMapper<B4, A4>();
            //DynamicAssemblyManager.SaveAssembly();

            a = mapper.Map(b, a);
            Assert.Equal(b.fld1.fld1.ToString(), a.fld1.fld1);
            Assert.Equal(b.fld2.fld1.ToString(), a.fld2.fld1);
            Assert.Equal(b.fld3.fld1.ToString(), a.fld3.fld1);
        }

        [Fact]
        public void Test_StructFields()
        {
            var mapper = ObjectMapperManager.DefaultInstance.GetMapper<B5, A5>();
            //DynamicAssemblyManager.SaveAssembly();
            var b = new B5();
            b.a.fld1 = 10;
            var a = mapper.Map(b);
            Assert.Equal(10, a.a.fld1);
        }

        [Fact]
        public void Test_NestedStructs()
        {
            var mapper = ObjectMapperManager.DefaultInstance.GetMapper<B6, A6>();
            //DynamicAssemblyManager.SaveAssembly();
            var b = new B6();

            var bs2 = new B6.S2();
            bs2.s.i = 15;
            b.s2 = bs2;
            b.s.s.i = 13;
            b.s3.s.i = 10;
            b.s4 = new B6.C1();
            b.s4.s.i = 11;
            b.s5 = new B6.C3 { c1 = new B6.C1() };
            b.s5.c1.s.i = 1;
            b.s5.c2 = new B6.C1();
            b.s5.c2.s.i = 2;
            b.s5.c3 = new B6.C1();
            b.s5.c3.s.i = 3;

            var a = mapper.Map(b);
            Assert.Equal(13, a.s.s.i);
            Assert.Equal(15, a.s2.s.i);
            Assert.Equal(10, a.s3.s.i);
            Assert.Equal(11, a.s4.s.i);
            Assert.Equal(1, a.s5.c1.s.i);
            Assert.Equal(2, a.s5.c2.s.i);
            Assert.Equal(3, a.s5.c3.s.i);
        }

        public class A6
        {
            public S2 s2;

            public C2 s4;

            public C3 s5;

            public S2 s { get; set; }

            public C1 s3 { get; set; }

            public struct S1
            {
                public int i { get; set; }
            }

            public struct S2
            {
                public S1 s { get; set; }
            }

            public class C1
            {
                public S1 s { get; set; }
            }

            public class C2
            {
                public S1 s;
            }

            public class C3
            {
                public C2 c1;

                public C2 c2;

                public C2 c3;
            }
        }

        public class B6
        {
            public S2 s = new S2();

            public S2 s3;

            public C3 s5;

            public S2 s2 { get; set; }

            public C1 s4 { get; set; }

            public struct S1
            {
                public int i { get; set; }
            }

            public struct S2
            {
                public S1 s;
            }

            public class C1
            {
                public S1 s;
            }

            public class C3
            {
                public C1 c1;

                public C1 c2;

                public C1 c3;
            }
        }

        #region Test types

        public struct A1
        {
            public int fld1;
        }

        public class B1
        {
            public int fld1 = 10;
        }

        public struct A2
        {
            public int fld1;
        }

        public class B2
        {
            public int fld1;
        }

        public class A3
        {
            public int fld1;
        }

        public struct B3
        {
            public int fld1;
        }

        public struct A4
        {
            public struct Int
            {
                public string fld1;
            }

            public Int fld1 { get; set; }

            public Int fld2;

            public Int fld3 { get; set; }
        }

        public class B4
        {
            public Int fld3;

            public B4()
            {
                this.fld1 = new Int { fld1 = 12.444M };
                this.fld2 = new Int { fld1 = 1111 };
                this.fld3.fld1 = 444;
            }

            public Int fld1 { get; set; }

            public Int fld2 { get; set; }

            public struct Int
            {
                public decimal fld1;
            }
        }

        public class A5
        {
            public A1 a;
        }

        public class B5
        {
            public A1 a;
        }

        #endregion
    }
}