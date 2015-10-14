using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StoredProcGenerator
{

    public interface IStoredProc
    {
        string Name { get;  }
        IEnumerable<IParam> ParamList { get; }
    }



    public class ParamListBulider : List<object>
    {
        public StoredProc Build(string Name) {
            var ps = (
                    from p in this
                    select (p is IParam ? p : MapParam.Make(p)) as IParam
                );
            return new StoredProc(Name, ps); 
        }

        public Query BuildQuery(string p)
        {
            var sp = this.Build(p);
            return Query.Start.Chain(sp);
        }
    }


    public class StoredProc : IStoredProc
    {

        public StoredProc(string spname, IEnumerable<IParam> ps)
        {
            this.Name = spname;
            this.ParamList = ps;
        }

        public string Name { get; private set; }
        public IEnumerable<IParam> ParamList { get; private set; }


    }

    public interface IStoredProcDefn : IStoredProc
    {
        string QueryDefn(QueryHelper qh);
    }

    public class SetValue<T> : IStoredProcDefn
    {
        private OutParam<T> outparam;
        private MapParam<T> valparam;
        public SetValue(OutParam<T> outparam, MapParam<T> valparam)
        {
            this.outparam = outparam;
            this.valparam = valparam;

            this.ParamList = new List<IParam>
            {
                outparam, valparam
            };
        }


        public string Name { get { return "SetValue"; } }
        public IEnumerable<IParam> ParamList { get; private set; }


        public string QueryDefn(QueryHelper qh)
        {
            return string.Format("SET @{0}= ?;\n", qh.VarNames[this.outparam]);
        }
    }
}
