using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StoredProcGenerator
{
    public interface IParam
    {
    }

    public interface IOutParam :IParam
    {   string Description { get; }
        string VarType { get;  }
    }

    public class OutParam<T> : IOutParam {
        public static IDictionary<Type, string> VariableTypeMap = new Dictionary<Type, string>
        {
            {typeof(int), "int"},
            {typeof(string), "varchar(max)"},
            {typeof(decimal), "decimal(18,2)"}
        }.ToImmutableDictionary();

        private Action<T> callback;
        private string desc;
        public OutParam(string desc = null, Action<T> callback = null)
        {
            this.desc = desc;
            this.callback = callback;
        }

        public string Description { get { return this.desc; } }
        public string VarType { get { return VariableTypeMap[typeof(T)]; } }

        public InParam<T> In { get { return new InParam<T>(this); } }

    }
    
    public interface IInParam : IParam
    {
        IOutParam Out { get;  }
    }

    public class InParam<T> : IInParam {
        private OutParam<T> outParam;

        public InParam(OutParam<T> outParam)
        {
            this.outParam = outParam;
        }

        public IOutParam Out { get { return outParam; } }
    }


    public class MapParam : IParam
    {
        public static MapParam<T> Make<T>(Func<T> fo)
        {
            return new MapParam<T>(fo);
        }
        public static MapParam<T> Make<T>(T o)
        {
            return new MapParam<T>(o);
        }
    }

    public class MapParam<T> : MapParam
    {
        private Func<T> fo;

        public MapParam(Func<T> fo)
        {
            this.fo = fo;
        }
        public MapParam(T o)
        {
            this.fo = () => o;
        }
    }

}
