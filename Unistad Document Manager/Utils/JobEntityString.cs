using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace Unistad_Document_Manager.Utils
{
    public class JobEntityString
    {

        public string RowKey { get; set; }

        public string partition { get; set; }

        public int status { get; set; }

        [MaxLength(512)]
        public string statusDescription { get; set; }

        [MaxLength(512)]
        public string fileSource { get; set; }

        [MaxLength(512)]
        public string fileResult { get; set; }

        [MaxLength(512)]
        public string user { get; set; }

        public string timestamp { get; set; }

    }
}
