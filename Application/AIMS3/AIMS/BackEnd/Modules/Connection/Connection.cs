using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AIMS3.BackEnd.Modules
{
    public abstract class Connection
    {
		public enum ModuleConnectionType { Serial, TCP, GPRS };
        public ModuleConnectionType Type { get; set; }

        public abstract bool Write(string data);
        public abstract bool Write(byte[] data);
        public abstract string Read();
    }
}