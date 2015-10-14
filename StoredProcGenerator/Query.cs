using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;

namespace StoredProcGenerator
{
    //public static class QueryExtensions
    //{
    //    public static Query Chain(this IStoredProc t, Query other)
    //    {
    //        return Query.Start.Chain(t).Chain(other);
    //    }

    //    public static Query Chain(this IStoredProc t, IStoredProc other)
    //    {
    //        return Query.Start.Chain(t).Chain(other);
    //    }
    //}



    public class QueryHelper
    {
        private Query q;
        private Dictionary<IOutParam, string> var_lookup = new Dictionary<IOutParam, string>();
        public Dictionary<IOutParam, string> VarNames { get { return var_lookup; } }

        public QueryHelper(Query q)
        {
            this.q = q;
            this.RegisterOut();
        }

        private void RegisterOut()
        {
            var outparms = from sp in this.q.Procs
                           from prm in sp.ParamList
                           where prm is IOutParam
                           select prm as IOutParam;

            int ct = 1;
            foreach (var op in outparms)
            {
                this.var_lookup.Add(op, string.Format("{0}_{1}", op.Description, ct++));
            }
        }





        public string Declarations()
        {
            var sb = new StringBuilder();
            foreach(var kv in this.var_lookup)
            {

                sb.AppendFormat("DECLARE @{0} {1};\n", kv.Value, kv.Key.VarType);
            }

            return sb.ToString();
        }



        internal string Body()
        {
            var sb = new StringBuilder();

            foreach (var sp in this.q.Procs)
            {
                if (sp is IStoredProcDefn)
                {
                    sb.Append(((IStoredProcDefn)sp).QueryDefn(this));
                    continue;
                }

                var sp_exec = sp as StoredProc;
                sb.AppendFormat("SET @RunningSp='{0}';\n", sp_exec.Name);
                sb.AppendFormat("EXEC {0} ", sp_exec.Name);

                var length=sp_exec.ParamList.Count();
                var ct=0;

                foreach(var p in sp_exec.ParamList)
                {
                    if (p is MapParam)
                        sb.Append("?");
                    else if (p is IOutParam)
                        sb.AppendFormat("OUT @{0}", this.var_lookup[(IOutParam)p]);
                    else if (p is IInParam)
                        sb.AppendFormat("@{0}", this.var_lookup[((IInParam)p).Out]);

                    ct++;
                    if(ct<length)
                        sb.Append(", ");
                }

                sb.Append(";\n");

                foreach (var p in sp_exec.ParamList)
                {
                    if (p is IOutParam)
                        sb.AppendFormat("INSERT INTO @RESULTKV VALUES('{0}', @{0});\n", this.var_lookup[(IOutParam)p]);
                }

            }

            return sb.ToString();
        }
    }









    public class Query
    {
        public string Make()
        {

            var qh = new QueryHelper(this);

            StringBuilder sb = new StringBuilder();
            sb.AppendLine(
@"SET NOCOUNT ON;
DECLARE @RunningSp varchar(40);
DECLARE @RESULTKV TABLE(Key varchar(200), Val varchar(200));");
            sb.AppendLine(qh.Declarations());
            sb.AppendLine(
@"BEGIN TRANSACTION
BEGIN TRY");

            sb.Append("    ");
            sb.Append(qh.Body().Replace("\n", "\n    "));
            sb.AppendLine(@"
    COMMIT;
    INSERT INTO @RESULTKV VALUES('Result', 'ok')
    SELECT * FROM @RESULTKV;
END TRY
BEGIN CATCH
    DECLARE @ErrorMessage nvarchar(max), @ErrorSeverity int, @ErrorState int;
    SELECT @ErrorMessage = @RunningSp + ' - ' + ERROR_MESSAGE(), @ErrorSeverity = ERROR_SEVERITY(), @ErrorState = ERROR_STATE();
    ROLLBACK;
    RAISERROR (@ErrorMessage, @ErrorSeverity, @ErrorState);
END CATCH");


            return sb.ToString();
        }





        private ImmutableList<IStoredProc> procs = ImmutableList<IStoredProc>.Empty;
        public IEnumerable<IStoredProc> Procs { get { return this.procs; } }

        public static Query Start = new Query();
        private Query() { }

        public Query Chain(Query other)
        {
            return new Query() { procs = this.procs.AddRange(other.procs) };
        }
        public Query Chain(IStoredProc other)
        {
            return new Query() { procs = this.procs.Add(other) };
        }


        //public static implicit operator Query(StoredProc sp)
        //{
        //    return Start.Chain((IStoredProc)sp);
        //}
        public static explicit operator Query(StoredProc sp)
        {
            return Start.Chain((IStoredProc)sp);
        }

    }
}
