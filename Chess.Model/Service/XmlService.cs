using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chess.Model.Service
{
    public class XmlFileService
    {
        /// <summary>
        /// Represents the file path for the XML file.
        /// </summary>
        private readonly string filePath;
        /// <summary>
        /// Initializes a new instance of the <see cref="XmlFileService"/> class.
        /// </summary>
        /// <param name="filePath">The file path for the XML file.</param>
        public XmlFileService(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                throw new ArgumentException("File path cannot be null or empty.", nameof(filePath));
            }



            this.FilePath = filePath;
        }
        // Additional methods for reading/writing XML files can be implemented here.
        public XmlFileService() 
        {
            
        }

        public string FilePath { get; private set; }
    }
}
