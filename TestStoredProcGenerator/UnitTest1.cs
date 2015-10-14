using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using StoredProcGenerator;

namespace TestStoredProcGenerator
{
    [TestClass]
    public class UnitTest1
    {

        public class TestSpMain
        {
            public Query dbint()
            {
                var pkeyvar = new OutParam<int>("pkey", (val) => { this.pkey = val;  });

                return new ParamListBulider { 
                    pkeyvar,    
                    this.p1,
                    this.p2,
                    this.p3
                }
                .BuildQuery("dbo.test")
                .Chain(this.inner.dbint(pkeyvar.In));

            }

            public int pkey { get; set; }
            public int p1 { get; set; }
            public string p2 { get; set; }
            public string p3 { get; set; }
            public InnerTestSp inner {get;set;}

            public class InnerTestSp { 
                public int inner1;
                public Query dbint(InParam<int> mainpkey)
                {
                    return new ParamListBulider {
                        mainpkey,
                        this.inner1
                    }.BuildQuery("dbo.testinner");
                }
            }

        }

        [TestMethod]
        public void AllStoredProcsInQuery()
        {
            var dbi = new TestSpMain() { p1 = 1, p2 = "hello", p3 = "goodbye", inner = new TestSpMain.InnerTestSp() { inner1=999} };
            string q = dbi.dbint().Make();
            Console.WriteLine(q);
            Assert.IsTrue(q.ToLower().Contains("'dbo.test'"), "Didn't include a test call to dbo.test");
            Assert.IsTrue(q.ToLower().Contains("'dbo.testinner'"), "Didn't include a test call to dbo.testinner");
        }

        [TestMethod]
        public void SetValueTestInString()
        {
            
            var userid=new OutParam<int>("userid");
            var dbi = Query.Start.Chain(new SetValue<int>(userid, MapParam.Make<int>(() => 999)));

            dbi = dbi.Chain(
                new TestSpMain() { p1 = 1, p2 = "hello", p3 = "goodbye", inner = new TestSpMain.InnerTestSp() { inner1 = 999 } }.dbint()
            );

            string q = dbi.Make();

            Console.WriteLine(q);
            Assert.IsTrue(q.ToLower().Contains("@userid"));
        }
    }
}
