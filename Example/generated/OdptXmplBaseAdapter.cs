//------------------------------------------------------------------------------
// <auto-generated>
//     This code was auto-generated by Odapter on Tue, 11 Sep 2018 22:52:00 GMT.
//     It can be edited as necessary after initial generation.
//     To avoid overwrite, Deploy Base DTOs? must be unchecked during subsequent generation.
// </auto-generated>
//------------------------------------------------------------------------------

using System;
using Oracle.ManagedDataAccess.Client;

namespace Schema.Odpt.Xmpl {
	public abstract class OdptAdapter {
        protected string GetConnectionString() { return "data source=XE;user id=ODPT;password=odpt;enlist=false"; }

        protected OracleConnection GetConnection() {
			OracleConnection connection = new OracleConnection(GetConnectionString());
			connection.Open();
			return connection;
        }

        /// <summary>
        /// Determine if completion of OracleCommand execution should be traced (hook)
        /// </summary>
        /// <param name="cmd">An OracleCommand prepared for executing</param>
        /// <returns>true if command should be traced</returns>
        protected bool IsTracing(OracleCommand cmd) {
			return false;
        }

        /// <summary>
        /// Perform trace functionality for a completed OracleCommand (hook)
        /// </summary>
        /// <param name="cmdTrace">An OracleCommandTrace just executed</param>
        /// <param name="returnRowCount">Row count returned in cursor</param>
        protected void TraceCompletion(Odapter.OracleCommandTrace cmdTrace, int? returnRowCount) {
			// stop the timer first
			cmdTrace.Stopwatch.Stop();
			// trace logic goes here
			return;
        }

        /// <summary>
        /// Perform trace functionality for a completed OracleCommand (hook)
        /// </summary>
        /// <param name="cmdTrace">An OracleCommandTrace just executed</param>
        protected void TraceCompletion(Odapter.OracleCommandTrace cmdTrace) {
			TraceCompletion(cmdTrace, null);
			return;
        }
    } // OdptAdapter

} // Schema.Odpt.Xmpl