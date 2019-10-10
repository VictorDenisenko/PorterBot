using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.CognitiveServices.Vision.Face.Models;

namespace PorterBot
{
    [Serializable]
    class IdentificationResults
    {
        public string Name { get; set; }
        public double? Age { get; set; }//? означает, что переменная может принимать значение "null"
        public string Blur { get; set; }
        public string Emotion { get; set; }
        public string Exposure { get; set; }
        public string FacialHair { get; set; }
        public string Gender { get; set; }
        public string Glasses { get; set; }
        public string Hair { get; set; }
        public double HeadPose { get; set; }
        public string Makeup { get; set; }
        public string Noise { get; set; }
        public string Occlusion { get; set; }
        public double Smile { get; set; }

    }
}
