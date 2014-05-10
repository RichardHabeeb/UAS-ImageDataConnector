using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageDataConnector
{
    class PendingFile
    {
        public FileInfo file;
        public DateTime recieved;

        public PendingFile(FileInfo file, DateTime recieved)
        {
            this.file = file;
            this.recieved = recieved;
        }

        public override string ToString()
        {
            return string.Format("file: {0}, recieved: {1}", file, recieved);
        }
    }
}
