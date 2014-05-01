using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageDataConnector
{
    class PendingImage
    {
        public FileInfo file;
        public DateTime recieved;

        public PendingImage(FileInfo file, DateTime recieved)
        {
            this.file = file;
            this.recieved = recieved;
        }
    }
}
